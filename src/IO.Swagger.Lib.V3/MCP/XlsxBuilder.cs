/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace IO.Swagger.Lib.V3.MCP;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

/// <summary>
/// Minimaler XLSX-Writer ohne externe Abhängigkeit: ein Sheet, Inline-Strings,
/// Zahlen als numerische Zellen. Reicht für tabellarische Query-Exporte und wird
/// von Excel/LibreOffice/Google Sheets gelesen.
/// </summary>
internal static class XlsxBuilder
{
    public const string MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(string[] columns, IReadOnlyList<JsonObject> rows, string sheetName = "Export")
    {
        var sheet = BuildSheetXml(columns, rows);

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>");
            AddEntry(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            AddEntry(zip, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                $"<sheets><sheet name=\"{XmlEscape(SanitizeSheetName(sheetName))}\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                "</workbook>");
            AddEntry(zip, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>");
            AddEntry(zip, "xl/worksheets/sheet1.xml", sheet);
        }

        return stream.ToArray();
    }

    private static string BuildSheetXml(string[] columns, IReadOnlyList<JsonObject> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sb.Append("<sheetData>");

        AppendRow(sb, 1, columns.Length, col => ((string?)columns[col], null));
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            AppendRow(sb, r + 2, columns.Length, col =>
                GetCellValue(row.TryGetPropertyValue(columns[col], out var value) ? value : null));
        }

        sb.Append("</sheetData>");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, int rowNumber, int columnCount, Func<int, (string? Text, double? Number)> cell)
    {
        sb.Append("<row r=\"").Append(rowNumber).Append("\">");
        for (var col = 0; col < columnCount; col++)
        {
            var (text, number) = cell(col);
            if (number.HasValue)
            {
                sb.Append("<c r=\"").Append(CellRef(col, rowNumber)).Append("\"><v>")
                    .Append(number.Value.ToString("R", CultureInfo.InvariantCulture))
                    .Append("</v></c>");
            }
            else if (text is not null)
            {
                sb.Append("<c r=\"").Append(CellRef(col, rowNumber)).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                    .Append(XmlEscape(text))
                    .Append("</t></is></c>");
            }
        }

        sb.Append("</row>");
    }

    // Zellwert aus dem Projektions-JSON: Zahlen als numerische Zelle, alles andere als Text.
    internal static (string? Text, double? Number) GetCellValue(JsonNode? value)
    {
        if (value is null)
        {
            return (null, null);
        }

        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<int>(out var i))
                return (null, i);
            if (jsonValue.TryGetValue<long>(out var l))
                return (null, l);
            if (jsonValue.TryGetValue<double>(out var d))
                return double.IsFinite(d) ? (null, d) : (d.ToString(CultureInfo.InvariantCulture), null);
            if (jsonValue.TryGetValue<decimal>(out var m))
                return (null, (double)m);
            if (jsonValue.TryGetValue<bool>(out var b))
                return (b ? "true" : "false", null);
            if (jsonValue.TryGetValue<string>(out var s))
                return (s, null);
        }

        return (value.ToJsonString(), null);
    }

    internal static string CellRef(int columnIndex, int rowNumber)
    {
        var column = string.Empty;
        var remaining = columnIndex;
        while (remaining >= 0)
        {
            column = (char)('A' + (remaining % 26)) + column;
            remaining = remaining / 26 - 1;
        }

        return column + rowNumber.ToString(CultureInfo.InvariantCulture);
    }

    internal static string XmlEscape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '&': sb.Append("&amp;"); break;
                case '"': sb.Append("&quot;"); break;
                default:
                    // Steuerzeichen sind in XML 1.0 nicht erlaubt (Excel verweigert die Datei sonst).
                    if (c < 0x20 && c != '\t' && c != '\n' && c != '\r')
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    private static string SanitizeSheetName(string? sheetName)
    {
        var name = string.IsNullOrWhiteSpace(sheetName) ? "Export" : sheetName.Trim();
        foreach (var invalid in new[] { ':', '\\', '/', '?', '*', '[', ']' })
        {
            name = name.Replace(invalid, '_');
        }

        return name.Length > 31 ? name[..31] : name;
    }

    private static void AddEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }
}
