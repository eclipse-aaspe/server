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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AasCore.Aas3_1;
using AasxServer;
using Contracts;
using Contracts.DbRequests;
using Contracts.Exceptions;
using Contracts.Pagination;
using Contracts.QueryResult;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using ModelContextProtocol.Server;

/// <summary>
/// Eine einzelne Suchbedingung. Der Server baut daraus eine gültige AASQL-Bedingung;
/// das Modell muss keine AASQL-Syntax erzeugen.
/// </summary>
public sealed class McpQueryCondition
{
    [Description("Worauf sich das Feld bezieht: \"aas\" (Shell), \"sm\" (Submodel) oder \"sme\" (Submodel-Element, nur als Filter). Default: \"sme\".")]
    public string Scope { get; set; } = "sme";

    [Description("Feldname je nach scope. aas: idShort|id|assetInformation.assetKind|assetInformation.assetType|assetInformation.globalAssetId|assetInformation.specificAssetIds[].name|... ; sm: semanticId|idShort|id ; sme: semanticId|idShort|idShortPath|value|valueType|language. Wildcard-/Teilstring-Operatoren contains, starts-with und ends-with sind nur für value, idShort und idShortPath erlaubt. Für semanticId und id ausschließlich eq oder in verwenden.")]
    public string Field { get; set; } = "";

    [Description("Optionaler idShortPath innerhalb des Submodels, nur für scope=\"sme\". Enthält der Wert einen Punkt, wird er als verschachtelter Pfad behandelt (z.B. \"TechnicalProperties.flowMax\" oder \"Documents[].DocumentVersion.Title\"). Ein einzelner Name ohne Punkt (z.B. \"flowMax\") wird als idShort des Elements gesucht und unabhängig von der Verschachtelungstiefe gefunden.")]
    public string? IdShortPath { get; set; }

    [Description("Vergleichsoperator: eq|ne|gt|ge|lt|le|contains|starts-with|ends-with|regex|in. contains, starts-with und ends-with nur für value, idShort und idShortPath verwenden; semanticId und id nur mit eq oder in. regex ist derzeit nicht unterstützt. contains benötigt mindestens 3 Zeichen, damit der Trigrammindex genutzt werden kann. \"in\" prüft, ob der Wert in der Liste values vorkommt (ODER-Verknüpfung).")]
    public string Op { get; set; } = "eq";

    [Description("Vergleichswert als String (Zahlen als String angeben, z.B. \"100\"). Bei eq/ne/gt/ge/lt/le werden numerische Werte automatisch numerisch verglichen — \"eq 80\" findet also auch einen Double-Wert 80.")]
    public string Value { get; set; } = "";

    [Description("Werteliste für op=\"in\" (z.B. mehrere Artikelnummern). Trifft, wenn das Feld einem der Werte entspricht — ersetzt viele or-verknüpfte eq-Bedingungen.")]
    public string[]? Values { get; set; }
}

/// <summary>
/// MCP-Tools für die AASPE-Query-Pipeline. Dünner Adapter über <see cref="IDbRequestHandlerService"/>
/// (analog zum GraphQL-Adapter). Das Modell füllt strukturierte Slots, der Server baut die AASQL
/// (JSON-Grammatik) und gibt nur die gefundenen Identifier zurück — keine Volldaten.
/// </summary>
[McpServerToolType]
public sealed class McpQueryTools
{
    private const int DefaultPageSize = 500;
    private const int MaxPageSize = 5000;

    // Ab dieser Dateigröße wird der Inhalt nicht mehr inline (content/contentBase64) in die
    // Tool-Antwort gelegt — große Tabellen laufen sonst durch das Kontextfenster des Modells.
    // Der Abruf erfolgt dann über downloadUrl (HTTP) oder resourceUri (MCP-Resource).
    private const int InlineExportContentMaxBytes = 200_000;

    // Tool-Sätze für die reduzierten Endpunkte (Pfad-Filter siehe ServerConfiguration).
    // /mcp-basic: das mächtige conditions-Tool — ein Call, aber komplexe Suchen (AND/OR, Operatoren). Für mittlere Modelle (z.B. Gemini Flash).
    public static readonly HashSet<string> BasicToolNames = new(StringComparer.Ordinal) { "aas_find_product" };

    // /mcp-simple: nur das Tool mit flachen Parametern (idShort/value) — für sehr kleine Modelle (z.B. Qwen 7B),
    // die das conditions[]-Schema nicht korrekt füllen.
    public static readonly HashSet<string> SimpleToolNames = new(StringComparer.Ordinal) { "aas_find_product_simple" };

    // Obergrenze für aas_get_product: max. so viele Submodelle pro AAS werden geladen (Schutz gegen
    // pathologisch große Shells). Reale Produkte haben i.d.R. <10 Submodelle.
    private const int MaxProductSubmodels = 50;

    // Default order for ambiguous leaf-name projections; callers can override both lists.
    private static readonly string[] DefaultProjectionPriority =
        { "Nameplate", "GeneralInformation", "TechnicalData", "TechnicalProperties", "HandoverDocumentation", "ProductCarbonFootprint" };

    private static readonly string[] DefaultProjectionDeprioritize =
        { "Associated_part", "Part_relation" };

    // Submodel-idShort-Keywords, die in der simple-Stufe (coreOnly) weggelassen werden: sperrige Datei-/Doku-
    // Submodelle (CAD, BoM_SpareParts, HandoverDocumentation), die sehr kleine Modelle nur ablenken/überfluten.
    private static readonly HashSet<string> NoiseSubmodelKeywords = new(StringComparer.OrdinalIgnoreCase)
        { "handover", "documentation", "cad", "bom", "sparepart" };

    private static readonly HashSet<string> AllowedScopes = new(StringComparer.Ordinal) { "aas", "sm", "sme" };

    // Slot-Operator -> AASQL-JSON-Schlüssel
    private static readonly Dictionary<string, string> OpMap = new(StringComparer.Ordinal)
    {
        ["eq"] = "$eq",
        ["ne"] = "$ne",
        ["gt"] = "$gt",
        ["ge"] = "$ge",
        ["lt"] = "$lt",
        ["le"] = "$le",
        ["contains"] = "$contains",
        ["starts-with"] = "$starts-with",
        ["ends-with"] = "$ends-with",
        ["regex"] = "$regex",
    };

    // Operatoren, die bei einem als Zahl parsebaren Wert numerisch verglichen werden (sonst String).
    // eq/ne sind bewusst dabei: ein Property mit valueType=Double speichert z.B. "80" nicht als exakten
    // String, daher schlägt ein reiner String-eq fehl. Bei nicht-numerischen Werten greift automatisch $strVal.
    private static readonly HashSet<string> NumericCapableOps = new(StringComparer.Ordinal) { "eq", "ne", "gt", "ge", "lt", "le" };

    private static readonly HashSet<string> WildcardOps = new(StringComparer.Ordinal) { "contains", "starts-with", "ends-with", "regex" };

    private static readonly HashSet<string> WildcardFields = new(StringComparer.Ordinal) { "value", "idShort", "idShortPath" };

    private readonly IDbRequestHandlerService _dbRequestHandlerService;
    private readonly IMappingService _mappingService;

    public McpQueryTools(IDbRequestHandlerService dbRequestHandlerService, IMappingService mappingService)
    {
        _dbRequestHandlerService = dbRequestHandlerService;
        _mappingService = mappingService;
    }

    [McpServerTool(Name = "aas_query", Title = "Search AAS Repository", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Sucht im AAS-Repository und liefert nur die gefundenen Identifier zurück (keine Volldaten). " +
        "Mehrere Bedingungen werden mit combine (\"and\"/\"or\") verknüpft. " +
        "target=\"submodels\" gibt Submodel-Identifier zurück, target=\"shells\" gibt AAS-Identifier zurück. " +
        "Wildcard-/Teilstring-Suchen (contains, starts-with, ends-with) dürfen ausschließlich auf value, idShort und idShortPath verwendet werden. Für semanticId und id ausschließlich eq oder in verwenden; regex ist derzeit nicht unterstützt. " +
        "Wenn unklar ist, wie ein Merkmal heißt (z.B. \"Ausgangsspannung\"): ZUERST aas_find_concepts aufrufen und dann über field=\"semanticId\" suchen — das ist exakt und herstellerunabhängig, statt idShort-Schreibweisen zu raten. " +
        "Für einen Überblick, welche Submodel-Typen und Feldpfade es überhaupt gibt: aas_describe_model. " +
        "Beispiele: " +
        "(1) eine Bedingung: target=\"submodels\", conditions=[{scope:\"sm\",field:\"idShort\",op:\"eq\",value:\"TechnicalData\"}]. " +
        "(2) UND: combine=\"and\", conditions=[{scope:\"sm\",field:\"idShort\",op:\"eq\",value:\"TechnicalData\"},{scope:\"sme\",field:\"value\",op:\"lt\",value:\"100\"}]. " +
        "(3) ODER: combine=\"or\", conditions=[{scope:\"sme\",field:\"value\",op:\"eq\",value:\"A\"},{scope:\"sme\",field:\"value\",op:\"eq\",value:\"B\"}]. " +
        "Nicht automatisch aas_count vorschalten; aas_count nur verwenden, wenn der Benutzer explizit die exakte Gesamtzahl braucht. Wenn hasMore=true, mit cursor weiterblättern, nicht count aufrufen. " +
        "Wichtig: aas_query liefert standardmäßig nur Identifier. Danach den Inhalt mit aas_get_submodel lesen — oder, wenn die Frage mehrere Submodelle eines Produkts betrifft (z.B. technische Daten + Hersteller + CO2), in EINEM Schritt mit aas_get_product(identifier). " +
        "TIPP für Tabellen/Listen: Übergib select=[idShortPaths], dann liefert aas_query je Treffer direkt diese Feldwerte (Projektion) — das ersetzt viele Einzelabrufe. " +
        "Für Daten aus einem anderen Submodel derselben AAS nutze /SubmodelIdShort/idShortPath, z.B. /TechnicalData/GeneralInformation.ManufacturerArticleNumber. " +
        "Wenn der Benutzer eine Tabelle, Excel- oder CSV-Datei möchte, stattdessen direkt aas_query_export_xlsx (bevorzugt) bzw. aas_query_export_csv verwenden. " +
        "Bei vielen erwarteten Treffern limit explizit setzen (Maximum 5000).")]
    public async Task<object> AasQuery(
        [Description("Zielobjekt: \"submodels\" oder \"shells\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\". Bei einer Bedingung ohne Bedeutung.")] string combine = "and",
        [Description("Maximale Trefferzahl (Default 500, Maximum 5000).")] int? limit = null,
        [Description("Cursor (Offset) zum Weiterblättern; aus nextCursor einer vorigen Antwort.")] string? cursor = null,
        [Description("Optional: Liste von idShortPaths, deren Werte je Treffer direkt mitgeliefert werden (Projektion = Tabellenspalten), z.B. [\"GeneralInformation.ManufacturerArticleNumber\", \"TechnicalProperties.Power_output\"]. Pfade ohne / beziehen sich auf das Treffer-Submodel. Für andere Submodelle derselben AAS nutze /SubmodelIdShort/idShortPath, z.B. /TechnicalData/GeneralInformation.ManufacturerArticleNumber. Nur für target=\"submodels\".")] string[]? select = null,
        [Description("Sprache für mehrsprachige Felder (MultiLanguageProperty) in der Projektion: nur dieser Sprachwert kommt zurück (Default \"en\"; fehlt die Sprache, wird die erste vorhandene genommen). Nur relevant zusammen mit select.")] string lang = "en",
        [Description("Optionale Prioritätsreihenfolge für mehrdeutige Blattnamen. Default: Nameplate, GeneralInformation, TechnicalData, TechnicalProperties, HandoverDocumentation, ProductCarbonFootprint.")] string[]? priority = null,
        [Description("Optionale Pfadsegmente, die bei mehrdeutigen Blattnamen ans Ende gestellt werden. Default: Associated_part, Part_relation.")] string[]? deprioritize = null,
        [Description("Wenn true, enthält jede Projektionszeile zusätzlich ein paths-Objekt mit den tatsächlich gewählten idShortPaths.")] bool withPaths = false)
    {
        using var _ = LogCallTimed($"aas_query {target} {DescribeConditions(conditions, combine)} limit={(limit?.ToString(CultureInfo.InvariantCulture) ?? "-")} cursor={cursor ?? "-"} select={(select is { Length: > 0 } ? string.Join(",", select) : "-")}");

        ResultType resultType;
        string expression;
        try
        {
            resultType = ParseTarget(target);
            expression = BuildExpression(conditions, combine);
        }
        catch (ArgumentException ex)
        {
            // Lesbare Fehlermeldung an die KI zurückgeben (statt opakem MCP-"An error occurred"),
            // damit sie den Aufruf selbst korrigieren kann.
            return new { error = ex.Message };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var requestedLimit = NormalizeLimit(limit);
        var pagination = new PaginationParameters(cursor, requestedLimit + 1);

        var (list, queryError) = await TryQuery(securityConfig, pagination, resultType, expression);
        if (queryError != null)
        {
            return new { error = "Query fehlgeschlagen: " + queryError };
        }

        var fetchedIdentifiers = ExtractIdentifiers(list);
        var hasMore = fetchedIdentifiers.Count > requestedLimit;
        var identifiers = fetchedIdentifiers.Take(requestedLimit).ToList();

        string? nextCursor = hasMore
            ? (pagination.Cursor + identifiers.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        // Projektion: Wenn select angegeben ist, je Treffer eine Zeile mit den gewählten Feldwerten liefern
        // (statt nur Identifier) — spart die vielen aas_get_submodel-Folgeaufrufe. Nur für Submodelle sinnvoll.
        if (select is { Length: > 0 } && resultType == ResultType.Submodel)
        {
            var projectedRows = await BuildProjectionRows(
                securityConfig, identifiers, select, lang, priority, deprioritize, withPaths, "aas_query");
            var rows = new JsonArray();
            foreach (var row in projectedRows)
            {
                rows.Add(row);
            }

            return new { target, count = identifiers.Count, columns = select, rows, hasMore, nextCursor };
        }

        return new
        {
            target,
            count = identifiers.Count,
            identifiers,
            hasMore,
            nextCursor,
            nextStep = identifiers.Count > 0
                ? "Dies sind nur Identifier. Inhalte lesen: aas_get_submodel(identifier), oder gleich Felder mitliefern via select=[...]. Für ALLE Daten eines Produkts (Technik+Hersteller+CO2): aas_get_product(identifier)."
                : "Keine Treffer. Bedingung lockern (z.B. op=contains statt eq), Groß-/Kleinschreibung des idShort prüfen — oder mit aas_find_concepts die richtige semanticId ermitteln und über field=\"semanticId\" suchen.",
        };
    }

    [McpServerTool(Name = "aas_query_export_csv", Title = "Export AAS Query as CSV", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Führt eine aas_query mit select aus und liefert das Ergebnis als echte CSV-Datei. " +
        "Für Tabellen/Excel bevorzugt aas_query_export_xlsx verwenden; CSV nur, wenn explizit CSV gewünscht ist. " +
        "Die Antwort enthält fileName, mimeType=text/csv, count, hasMore, downloadUrl (HTTP-Download) und resourceUri (MCP-Resource); " +
        "content/contentBase64 sind nur bei kleinen Dateien (<200 KB) zusätzlich enthalten. " +
        "select ist erforderlich; exportiert wird eine Zeile pro Treffer mit Spalte id plus den select-Spalten. " +
        "Pfade ohne / beziehen sich auf das Treffer-Submodel. Für Felder aus einem anderen Submodel derselben AAS die Syntax /SubmodelIdShort/idShortPath nutzen, z.B. /TechnicalData/GeneralInformation.ManufacturerArticleNumber. " +
        "Für mehr als 500 Treffer limit explizit setzen, z.B. limit=1000. Nicht vorher aas_count aufrufen — bei hasMore=true mit cursor weiterblättern.")]
    public async Task<object> AasQueryExportCsv(
        [Description("Zielobjekt: aktuell \"submodels\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("idShortPaths/Blattnamen als CSV-Spalten, z.B. [\"ProductCarbonFootprintCradleToGate00.PCFCO2eq\"]. Für andere Submodelle derselben AAS nutze /SubmodelIdShort/idShortPath, z.B. /TechnicalData/GeneralInformation.ManufacturerArticleNumber.")] string[] select,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\".")] string combine = "and",
        [Description("Maximale Trefferzahl (Default 500, Maximum 5000).")] int? limit = null,
        [Description("Cursor (Offset) zum Weiterblättern; aus nextCursor einer vorigen Antwort.")] string? cursor = null,
        [Description("Sprache für mehrsprachige Felder (Default \"en\").")] string lang = "en",
        [Description("CSV-Trennzeichen; Default Semikolon für deutschsprachiges Excel.")] string delimiter = ";",
        [Description("Dateiname der exportierten CSV.")] string fileName = "aas_query_export.csv",
        [Description("Optionale Prioritätsreihenfolge für mehrdeutige Blattnamen.")] string[]? priority = null,
        [Description("Optionale Pfadsegmente, die bei mehrdeutigen Blattnamen ans Ende gestellt werden.")] string[]? deprioritize = null)
    {
        using var _ = LogCallTimed($"aas_query_export_csv {target} {DescribeConditions(conditions, combine)} limit={(limit?.ToString(CultureInfo.InvariantCulture) ?? "-")} cursor={cursor ?? "-"} select={(select is { Length: > 0 } ? string.Join(",", select) : "-")}");

        var export = await RunExportQuery("aas_query_export_csv", target, conditions, select, combine, limit, cursor, lang, priority, deprioritize);
        if (export.Error != null)
        {
            return export.Error;
        }

        var exportWatch = Stopwatch.StartNew();
        var normalizedDelimiter = NormalizeCsvDelimiter(delimiter);
        var csv = BuildCsv(export.Columns, export.Rows, normalizedDelimiter);
        var bytes = Encoding.UTF8.GetBytes("\uFEFF" + csv);

        var normalizedFileName = NormalizeExportFileName(fileName, ".csv", "aas_query_export.csv");
        var token = McpExportFileStore.Add(bytes, normalizedFileName, "text/csv");
        LogMcpLine($"aas_query_export_csv file: {bytes.Length} bytes in {exportWatch.ElapsedMilliseconds} ms");

        var inline = bytes.Length <= InlineExportContentMaxBytes;
        return new
        {
            target,
            count = export.Rows.Count,
            hasMore = export.HasMore,
            nextCursor = export.NextCursor,
            fileName = normalizedFileName,
            mimeType = "text/csv",
            encoding = "utf-8-sig",
            delimiter = normalizedDelimiter,
            columns = export.Columns,
            fileSizeBytes = bytes.Length,
            downloadUrl = BuildExportDownloadUrl(token),
            resourceUri = McpExportResources.BuildResourceUri(token),
            contentBase64 = inline ? Convert.ToBase64String(bytes) : null,
            content = inline ? csv : null,
            note = inline
                ? null
                : "Datei zu groß für Inline-Inhalt; über downloadUrl herunterladen oder als MCP-Resource (resourceUri) lesen.",
        };
    }

    [McpServerTool(Name = "aas_query_export_xlsx", Title = "Export AAS Query as Excel (XLSX)", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Führt eine aas_query mit select aus und liefert das Ergebnis als echte Excel-Datei (XLSX). " +
        "BEVORZUGTES Tool, wenn der Benutzer eine Tabelle, Excel-Datei oder einen Datei-Download möchte — nicht viele aas_query-Zeilen im Chat weiterverarbeiten. " +
        "Die Antwort enthält fileName, mimeType, count, hasMore, downloadUrl (HTTP-Download, dem Benutzer als Link geben) und resourceUri (MCP-Resource); " +
        "contentBase64 ist nur bei kleinen Dateien (<200 KB) zusätzlich enthalten. " +
        "select ist erforderlich; exportiert wird eine Zeile pro Treffer mit Spalte id plus den select-Spalten. " +
        "Pfade ohne / beziehen sich auf das Treffer-Submodel. Für Felder aus einem anderen Submodel derselben AAS die Syntax /SubmodelIdShort/idShortPath nutzen, z.B. /TechnicalData/GeneralInformation.ManufacturerArticleNumber oder /TechnicalData/TechnicalProperties.Manufacturer.Product_type. " +
        "Für mehr als 500 Treffer limit explizit setzen, z.B. limit=1000 (Maximum 5000). " +
        "Nicht automatisch aas_count vorschalten — bei hasMore=true mit cursor weiterblättern.")]
    public async Task<object> AasQueryExportXlsx(
        [Description("Zielobjekt: aktuell \"submodels\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("idShortPaths/Blattnamen als Tabellenspalten, z.B. [\"GeneralInformation.ManufacturerArticleNumber\"]. Für andere Submodelle derselben AAS nutze /SubmodelIdShort/idShortPath, z.B. /TechnicalData/GeneralInformation.ManufacturerArticleNumber.")] string[] select,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\".")] string combine = "and",
        [Description("Maximale Trefferzahl (Default 500, Maximum 5000).")] int? limit = null,
        [Description("Cursor (Offset) zum Weiterblättern; aus nextCursor einer vorigen Antwort.")] string? cursor = null,
        [Description("Sprache für mehrsprachige Felder (Default \"en\").")] string lang = "en",
        [Description("Dateiname der exportierten Excel-Datei.")] string fileName = "aas_query_export.xlsx",
        [Description("Optionale Prioritätsreihenfolge für mehrdeutige Blattnamen.")] string[]? priority = null,
        [Description("Optionale Pfadsegmente, die bei mehrdeutigen Blattnamen ans Ende gestellt werden.")] string[]? deprioritize = null)
    {
        using var _ = LogCallTimed($"aas_query_export_xlsx {target} {DescribeConditions(conditions, combine)} limit={(limit?.ToString(CultureInfo.InvariantCulture) ?? "-")} cursor={cursor ?? "-"} select={(select is { Length: > 0 } ? string.Join(",", select) : "-")}");

        var export = await RunExportQuery("aas_query_export_xlsx", target, conditions, select, combine, limit, cursor, lang, priority, deprioritize);
        if (export.Error != null)
        {
            return export.Error;
        }

        var exportWatch = Stopwatch.StartNew();
        var bytes = XlsxBuilder.Build(export.Columns, export.Rows);
        var normalizedFileName = NormalizeExportFileName(fileName, ".xlsx", "aas_query_export.xlsx");
        var token = McpExportFileStore.Add(bytes, normalizedFileName, XlsxBuilder.MimeType);
        LogMcpLine($"aas_query_export_xlsx file: {bytes.Length} bytes in {exportWatch.ElapsedMilliseconds} ms");

        var inline = bytes.Length <= InlineExportContentMaxBytes;
        return new
        {
            target,
            count = export.Rows.Count,
            hasMore = export.HasMore,
            nextCursor = export.NextCursor,
            fileName = normalizedFileName,
            mimeType = XlsxBuilder.MimeType,
            columns = export.Columns,
            fileSizeBytes = bytes.Length,
            downloadUrl = BuildExportDownloadUrl(token),
            resourceUri = McpExportResources.BuildResourceUri(token),
            contentBase64 = inline ? Convert.ToBase64String(bytes) : null,
            note = inline
                ? null
                : "Datei zu groß für Inline-Inhalt; über downloadUrl herunterladen oder als MCP-Resource (resourceUri) lesen.",
        };
    }

    // Gemeinsamer Unterbau der Export-Tools: Query ausführen, Treffer paginieren, Projektions-
    // zeilen bauen (Fast Path, wenn möglich) und die Spaltenliste (id + select) liefern.
    private sealed class ExportQueryData
    {
        public object? Error { get; init; }
        public bool HasMore { get; init; }
        public string? NextCursor { get; init; }
        public List<JsonObject> Rows { get; init; } = new();
        public string[] Columns { get; init; } = [];
    }

    private async Task<ExportQueryData> RunExportQuery(
        string toolName, string target, McpQueryCondition[] conditions, string[] select, string combine,
        int? limit, string? cursor, string lang, string[]? priority, string[]? deprioritize)
    {
        if (select is null || select.Length == 0)
        {
            return new ExportQueryData { Error = new { error = $"select ist für {toolName} erforderlich, damit Tabellenspalten erzeugt werden können." } };
        }

        string expression;
        try
        {
            var resultType = ParseTarget(target);
            if (resultType != ResultType.Submodel)
            {
                throw new ArgumentException($"{toolName} unterstützt aktuell nur target=\"submodels\".");
            }

            expression = BuildExpression(conditions, combine);
        }
        catch (ArgumentException ex)
        {
            return new ExportQueryData { Error = new { error = ex.Message } };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var requestedLimit = NormalizeLimit(limit);
        var pagination = new PaginationParameters(cursor, requestedLimit + 1);

        var (list, queryError) = await TryQuery(securityConfig, pagination, ResultType.Submodel, expression);
        if (queryError != null)
        {
            return new ExportQueryData { Error = new { error = "Query fehlgeschlagen: " + queryError } };
        }

        var fetchedIdentifiers = ExtractIdentifiers(list);
        var hasMore = fetchedIdentifiers.Count > requestedLimit;
        var identifiers = fetchedIdentifiers.Take(requestedLimit).ToList();
        string? nextCursor = hasMore
            ? (pagination.Cursor + identifiers.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        var rows = await BuildProjectionRows(
            securityConfig, identifiers, select, lang, priority, deprioritize, withPaths: false, toolName);
        var columns = new[] { "id" }.Concat(select.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())).ToArray();

        return new ExportQueryData { HasMore = hasMore, NextCursor = nextCursor, Rows = rows, Columns = columns };
    }

    // Externer Download-Link für eine Exportdatei; nutzt die konfigurierte externe Server-URL.
    private static string BuildExportDownloadUrl(string token)
    {
        var baseUrl = (Program.externalBlazor ?? string.Empty).TrimEnd('/');
        return baseUrl + "/mcp-exports/" + token;
    }

    internal static string NormalizeExportFileName(string? fileName, string extension, string defaultName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? defaultName : fileName.Trim();

        // Nur der blanke Dateiname ist erlaubt (Token-URL und Content-Disposition).
        name = name.Replace('\\', '_').Replace('/', '_').Replace("..", "_", StringComparison.Ordinal);
        return name.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? name : name + extension;
    }

    [McpServerTool(Name = "aas_count", Title = "Exact Count Only When Explicitly Requested", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Nicht für Exploration, Plausibilitätschecks oder vor normalen Suchen verwenden. Nutze stattdessen aas_query mit limit/cursor. " +
        "Dieses Tool zählt nur, wenn der Benutzer ausdrücklich eine exakte Gesamtzahl verlangt; dann exactTotalRequested=true setzen. " +
        "Gleiche Bedingungs-/combine-Logik wie aas_query. Nur für target=\"submodels\". " +
        "Liefert die exakte Gesamtzahl (ungedeckelt), kann auf großen Datenbanken aber teuer sein.")]
    public async Task<object> AasCount(
        [Description("Zielobjekt: aktuell nur \"submodels\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\".")] string combine = "and",
        [Description("Muss true sein, und nur setzen, wenn der Benutzer explizit nach der exakten Gesamtzahl fragt (z.B. \"wie viele insgesamt?\"). Für normale Suche/Listen false lassen und aas_query verwenden.")] bool exactTotalRequested = false)
    {
        using var _ = LogCallTimed($"aas_count {target} {DescribeConditions(conditions, combine)} exactTotalRequested={exactTotalRequested}");

        if (!exactTotalRequested)
        {
            return new
            {
                skipped = true,
                message = "aas_count wurde nicht ausgeführt. Nutze aas_query mit limit/cursor; count nur mit exactTotalRequested=true verwenden, wenn der Benutzer explizit die exakte Gesamtzahl verlangt.",
                nextStep = "Rufe aas_query mit derselben Bedingung auf. Bei hasMore=true mit nextCursor weiterblättern."
            };
        }

        string expression;
        try
        {
            var resultType = ParseTarget(target);
            if (resultType != ResultType.Submodel)
            {
                throw new ArgumentException("aas_count unterstützt in dieser Version nur target=\"submodels\". Für shells bitte aas_query nutzen.");
            }

            expression = BuildExpression(conditions, combine);
        }
        catch (ArgumentException ex)
        {
            return new { error = ex.Message };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);

        // Dedizierter COUNT-Pfad: zählt die Treffer-Ids im Query-Engine, OHNE die
        // vollständigen Submodelle zu materialisieren ("Collect results" entfällt).
        // Das skaliert auf große DBs und liefert die echte Gesamtzahl (ungedeckelt).
        // pageSize = int.MaxValue => SQL-LIMIT umfasst alle Treffer (SQLite & Postgres).
        var pagination = new PaginationParameters(null, int.MaxValue);
        int totalCount;
        try
        {
            totalCount = await _dbRequestHandlerService.QueryCountSMs(
                securityConfig, string.Empty, string.Empty, string.Empty, pagination, ResultType.Submodel, expression);
        }
        catch (Exception ex)
        {
            return new { error = "Query fehlgeschlagen: " + ex.Message };
        }

        return new { target, totalCount, nextStep = "Treffer abrufen mit aas_query (gleiche Bedingung); danach aas_get_submodel oder aas_get_product für die Inhalte." };
    }

    [McpServerTool(Name = "aas_get_submodel", Title = "Get AAS Submodel", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Holt ein komplettes Submodel anhand seines Identifiers (z.B. einen Treffer aus aas_query), damit man mit den Inhalten weiterarbeiten kann. " +
        "format=\"value\" (Default) liefert eine kompakte idShort->Wert-Darstellung — token-sparsam und ideal zum Auslesen von Werten. " +
        "format=\"full\" liefert das vollständige, standardkonforme AAS-JSON (inkl. semanticId, valueType, Qualifier, Beschreibungen). " +
        "Bei sehr großen Submodellen bevorzugt format=\"value\" verwenden. " +
        "Brauchst du Daten aus einem ANDEREN Submodel desselben Produkts (z.B. Hersteller aus Nameplate, CO2 aus CarbonFootprint)? Dann über die Shell navigieren: aas_query target=\"shells\" -> aas_get_shell liefert alle Submodelle der Shell.")]
    public async Task<object> AasGetSubmodel(
        [Description("Submodel-Identifier (die vollständige id, NICHT Base64-kodiert), typischerweise ein Treffer aus aas_query.")] string identifier,
        [Description("Ausgabeformat: \"value\" (kompakt, Default) oder \"full\" (vollständiges AAS-JSON).")] string format = "value")
    {
        using var _ = LogCallTimed($"aas_get_submodel id={identifier} format={format}");
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("identifier darf nicht leer sein.");
        }

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            throw new ArgumentException($"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\".");
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);

        IClass? submodel;
        try
        {
            submodel = await _dbRequestHandlerService.ReadSubmodelById(securityConfig, aasIdentifier: null, identifier.Trim(), level: null, extent: null);
        }
        catch (NotFoundException ex)
        {
            return new { identifier, found = false, message = ex.Message };
        }

        if (submodel is null)
        {
            return new { identifier, found = false };
        }

        return new { identifier, format = fmt, submodel = SerializeValueOrFull(submodel, fmt) };
    }

    [McpServerTool(Name = "aas_get_submodels", Title = "Get Multiple AAS Submodels", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Holt MEHRERE Submodelle in EINEM Aufruf — eine Liste von Identifiern statt vieler Einzelabrufe. " +
        "OHNE select: je Submodel der volle Inhalt (value/full). " +
        "MIT select=[idShortPaths]: je Submodel nur eine kompakte Zeile mit diesen Feldern (Tabelle/Projektion, wie bei aas_query). " +
        "Ideal, wenn man bereits eine Liste von Submodel-IDs hat (z.B. aus aas_query) und gezielt Felder oder Inhalte braucht.")]
    public async Task<object> AasGetSubmodels(
        [Description("Liste von Submodel-Identifiern (vollständige ids, NICHT Base64-kodiert).")] string[] identifiers,
        [Description("Ausgabeformat je Submodel, wenn KEIN select: \"value\" (kompakt, Default) oder \"full\".")] string format = "value",
        [Description("Optional: idShortPaths zur Projektion (Tabellenspalten), z.B. [\"GeneralInformation.ManufacturerArticleNumber\", \"TechnicalProperties.Power_output\"]. Mit select wird je ID nur eine Zeile mit diesen Feldern geliefert.")] string[]? select = null,
        [Description("Sprache für mehrsprachige Felder (MultiLanguageProperty) in der Projektion (Default \"en\"). Nur mit select relevant.")] string lang = "en",
        [Description("Optionale Prioritätsreihenfolge für mehrdeutige Blattnamen. Default: Nameplate, GeneralInformation, TechnicalData, TechnicalProperties, HandoverDocumentation, ProductCarbonFootprint.")] string[]? priority = null,
        [Description("Optionale Pfadsegmente, die bei mehrdeutigen Blattnamen ans Ende gestellt werden. Default: Associated_part, Part_relation.")] string[]? deprioritize = null,
        [Description("Wenn true, enthält jede Projektionszeile zusätzlich ein paths-Objekt mit den tatsächlich gewählten idShortPaths.")] bool withPaths = false)
    {
        using var _ = LogCallTimed($"aas_get_submodels n={(identifiers?.Length ?? 0)} format={format} select={(select is { Length: > 0 } ? string.Join(",", select) : "-")}");

        if (identifiers is null || identifiers.Length == 0)
        {
            return new { error = "identifiers darf nicht leer sein (Liste von Submodel-Identifiern)." };
        }

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            return new { error = $"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\"." };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);

        // Mit select: Tabelle (eine Zeile pro ID mit den gewählten Feldern).
        if (select is { Length: > 0 })
        {
            var rows = new JsonArray();
            foreach (var id in identifiers)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    rows.Add(await BuildProjectionRow(securityConfig, id.Trim(), select, lang, priority, deprioritize, withPaths));
                }
            }

            return new { count = rows.Count, columns = select, rows };
        }

        // Ohne select: je Submodel der (value/full) Inhalt.
        var submodels = new JsonArray();
        foreach (var id in identifiers)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            IClass? sm = null;
            try
            {
                sm = await _dbRequestHandlerService.ReadSubmodelById(securityConfig, null, id.Trim(), level: null, extent: null);
            }
            catch (NotFoundException)
            {
                sm = null;
            }

            var entry = new JsonObject { ["id"] = id.Trim() };
            if (sm is null)
            {
                entry["found"] = false;
            }
            else
            {
                if (sm is ISubmodel s)
                {
                    entry["idShort"] = s.IdShort;
                }

                entry["value"] = SerializeValueOrFull(sm, fmt);
            }

            submodels.Add(entry);
        }

        return new { count = submodels.Count, format = fmt, submodels };
    }

    [McpServerTool(Name = "aas_get_element", Title = "Get AAS Element", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Liest ein einzelnes SubmodelElement aus einem Submodel — gezielt, statt das ganze Submodel zu laden (spart Tokens). " +
        "Ein idShortPath MIT Punkt (z.B. \"TechnicalProperties.flowMax\") adressiert exakt ein Element (normale AAS-API). " +
        "Ein einzelner Name OHNE Punkt (z.B. \"flowMax\") wird als idShort im gesamten Submodel gesucht (beliebige Verschachtelungstiefe) und liefert ALLE Treffer mit ihrem vollen idShortPath — nützlich, wenn nur der Blattname bekannt ist und der Pfad nicht. " +
        "format=\"value\" (Default) = kompakter Wert, \"full\" = vollständiges Element-JSON.")]
    public async Task<object> AasGetElement(
        [Description("Submodel-Identifier (vollständige id, NICHT Base64-kodiert), typischerweise ein Treffer aus aas_query.")] string submodelIdentifier,
        [Description("Voller idShortPath mit Punkt (exaktes Element) ODER einzelner idShort-Blattname ohne Punkt (Suche in beliebiger Tiefe, alle Treffer).")] string idShortPath,
        [Description("Ausgabeformat: \"value\" (kompakt, Default) oder \"full\" (vollständiges Element-JSON).")] string format = "value")
    {
        using var _ = LogCallTimed($"aas_get_element sm={submodelIdentifier} path={idShortPath} format={format}");
        if (string.IsNullOrWhiteSpace(submodelIdentifier))
        {
            throw new ArgumentException("submodelIdentifier darf nicht leer sein.");
        }

        if (string.IsNullOrWhiteSpace(idShortPath))
        {
            throw new ArgumentException("idShortPath darf nicht leer sein.");
        }

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            throw new ArgumentException($"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\".");
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var smId = submodelIdentifier.Trim();
        var path = idShortPath.Trim();

        // Mit Punkt: exakter Pfad über die normale AAS-API.
        if (path.Contains('.', StringComparison.Ordinal))
        {
            IClass? element;
            try
            {
                element = await _dbRequestHandlerService.ReadSubmodelElementByPath(securityConfig, null, smId, path, level: null, extent: null);
            }
            catch (NotFoundException ex)
            {
                return new { submodelIdentifier = smId, idShortPath = path, found = false, message = ex.Message };
            }

            if (element is null)
            {
                return new { submodelIdentifier = smId, idShortPath = path, found = false };
            }

            return new { submodelIdentifier = smId, idShortPath = path, format = fmt, element = SerializeValueOrFull(element, fmt) };
        }

        // Nur Blattname: Submodel serverseitig laden und rekursiv nach idShort suchen (alle Treffer).
        IClass? submodel;
        try
        {
            submodel = await _dbRequestHandlerService.ReadSubmodelById(securityConfig, null, smId, level: null, extent: null);
        }
        catch (NotFoundException ex)
        {
            return new { submodelIdentifier = smId, idShort = path, found = false, message = ex.Message };
        }

        if (submodel is not ISubmodel sm)
        {
            return new { submodelIdentifier = smId, idShort = path, found = false };
        }

        var matches = new List<(string Path, ISubmodelElement Element)>();
        CollectByIdShort(sm.SubmodelElements, string.Empty, path, matches);

        var results = matches
            .Select(m => new { idShortPath = m.Path, value = SerializeValueOrFull(m.Element, fmt) })
            .ToList();

        return new { submodelIdentifier = smId, idShort = path, format = fmt, count = results.Count, matches = results };
    }

    [McpServerTool(Name = "aas_get_shell", Title = "Get AAS Shell", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Liest eine AAS (Asset Administration Shell) anhand ihres Identifiers — die Asset-Ebene ÜBER den Submodellen. " +
        "WANN BENUTZEN: Immer wenn du zu einem Produkt weitere Daten aus einem ANDEREN Submodel DERSELBEN Shell brauchst — " +
        "z.B. Hersteller/Seriennummer (Nameplate) oder CO2-Fußabdruck (CarbonFootprint), während du bisher nur z.B. das TechnicalData-Submodel hast. " +
        "Der direkte Weg ist NICHT querfeldein nach Teilenummern zu suchen, sondern: erst die Shell ermitteln (aas_query target=\"shells\" mit derselben Bedingung), dann aas_get_shell aufrufen. " +
        "Das liefert die Asset-Informationen (assetKind, globalAssetId, specificAssetIds wie Serien-/Herstellernummern) UND die Liste ALLER Submodel-Identifier der Shell; " +
        "danach das passende Submodel gezielt mit aas_get_submodel / aas_get_element auslesen. " +
        "format=\"value\" (Default) = kompakt, \"full\" = vollständiges AAS-JSON. " +
        "Alternativ liefert auch aas_query target=\"submodels\" mit {scope:\"aas\", field:\"id\", op:\"eq\", value:<aasId>} alle Submodelle einer AAS.")]
    public async Task<object> AasGetShell(
        [Description("AAS-Identifier (vollständige id, NICHT Base64-kodiert), typischerweise ein Treffer aus aas_query target=\"shells\".")] string identifier,
        [Description("Ausgabeformat: \"value\" (kompakt, Default) oder \"full\" (vollständiges AAS-JSON).")] string format = "value")
    {
        using var _ = LogCallTimed($"aas_get_shell id={identifier} format={format}");
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("identifier darf nicht leer sein.");
        }

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            throw new ArgumentException($"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\".");
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);

        IAssetAdministrationShell? shell;
        try
        {
            shell = await _dbRequestHandlerService.ReadAssetAdministrationShellById(securityConfig, identifier.Trim());
        }
        catch (NotFoundException ex)
        {
            return new { identifier, found = false, message = ex.Message };
        }

        if (shell is null)
        {
            return new { identifier, found = false };
        }

        var result = BuildShellObject(shell, fmt);
        result["identifier"] = identifier;
        result["format"] = fmt;
        return result;
    }

    [McpServerTool(Name = "aas_get_shells", Title = "Get Multiple AAS Shells", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Holt MEHRERE AAS (Shells) in EINEM Aufruf — eine Liste von AAS-Identifiern statt vieler Einzelabrufe. " +
        "Je Shell die AssetInformation + Submodel-Referenzen (format=\"value\", Default) oder das volle AAS-JSON (format=\"full\").")]
    public async Task<object> AasGetShells(
        [Description("Liste von AAS-Identifiern (vollständige ids, NICHT Base64-kodiert).")] string[] identifiers,
        [Description("Ausgabeformat je Shell: \"value\" (kompakt, Default) oder \"full\".")] string format = "value")
    {
        using var _ = LogCallTimed($"aas_get_shells n={(identifiers?.Length ?? 0)} format={format}");

        if (identifiers is null || identifiers.Length == 0)
        {
            return new { error = "identifiers darf nicht leer sein (Liste von AAS-Identifiern)." };
        }

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            return new { error = $"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\"." };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);

        var shells = new JsonArray();
        foreach (var id in identifiers)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            IAssetAdministrationShell? shell = null;
            try
            {
                shell = await _dbRequestHandlerService.ReadAssetAdministrationShellById(securityConfig, id.Trim());
            }
            catch (NotFoundException)
            {
                shell = null;
            }

            if (shell is null)
            {
                shells.Add(new JsonObject { ["identifier"] = id.Trim(), ["found"] = false });
                continue;
            }

            var entry = BuildShellObject(shell, fmt);
            entry["identifier"] = id.Trim();
            shells.Add(entry);
        }

        return new { count = shells.Count, format = fmt, shells };
    }

    [McpServerTool(Name = "aas_get_product", Title = "Get Complete AAS Product", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Liefert in EINEM Aufruf ein komplettes Produkt: die AAS (assetInformation) PLUS die Werte ALLER ihrer Submodelle " +
        "(z.B. TechnicalData + Nameplate + CarbonFootprint zusammen). " +
        "BENUTZE DIES, wenn eine Frage Daten aus mehreren Submodellen braucht — etwa technische Daten UND Hersteller UND CO2-Fußabdruck — " +
        "damit du NICHT einzeln navigieren musst. " +
        "identifier kann eine AAS-id ODER eine Submodel-id sein (z.B. ein Treffer aus aas_query); die zugehörige Shell wird automatisch ermittelt. " +
        "Typischer Ablauf: aas_query findet ein Submodel -> dessen id hier übergeben -> alles zum Produkt kommt zurück. " +
        "Bei vielen/großen Submodellen über submodelLimit/submodelCursor weiterblättern; nextSubmodelCursor aus der Antwort verwenden.")]
    public async Task<object> AasGetProduct(
        [Description("AAS-Identifier ODER Submodel-Identifier (vollständige id, NICHT Base64-kodiert). Ein Submodel-Treffer aus aas_query genügt — die Shell wird automatisch aufgelöst.")] string identifier,
        [Description("Ausgabeformat je Submodel: \"value\" (kompakt, Default, nur idShort->Wert) oder \"full\" (vollständiges AAS-JSON inkl. semanticId, valueType, Qualifier). Nutze \"full\", wenn nach semanticId, Einheiten oder Datentyp gefragt wird.")] string format = "value",
        [Description("Maximale Anzahl Submodelle dieses Produkts in dieser Antwort (Default und Maximum 50).")] int? submodelLimit = null,
        [Description("Cursor zum Weiterblättern der Produkt-Submodelle; aus nextSubmodelCursor einer vorigen Antwort.")] string? submodelCursor = null)
    {
        using var _ = LogCallTimed($"aas_get_product id={identifier} format={format} submodelLimit={(submodelLimit?.ToString(CultureInfo.InvariantCulture) ?? "-")} submodelCursor={submodelCursor ?? "-"}");
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return new { error = "identifier darf nicht leer sein." };
        }

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            return new { error = $"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\"." };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var shell = await ResolveShellForIdentifier(securityConfig, identifier.Trim());
        if (shell is null)
        {
            return new { identifier, found = false, message = "Identifier ist weder eine bekannte AAS noch ein bekanntes Submodel." };
        }

        return await BuildProductObject(
            securityConfig,
            shell,
            fmt,
            submodelCursor: NormalizeCursor(submodelCursor),
            submodelLimit: NormalizeProductSubmodelLimit(submodelLimit));
    }

    [McpServerTool(Name = "aas_find_product", Title = "Find Complete AAS Product", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Sucht ein Produkt anhand von Bedingungen UND liefert es in EINEM Aufruf komplett zurück — ohne Verkettung. " +
        "Das richtige Tool für Fragen wie 'finde das Ventil mit flowMax 80 und nenne Hersteller, technische Daten und CO2-Fußabdruck'. " +
        "Bedingungslogik identisch zu aas_query (conditions/combine, Slots scope/field/idShortPath/op/value). " +
        "Liefert zum ERSTEN Treffer die AAS (assetInformation) plus die Werte ALLER ihrer Submodelle (TechnicalData + Nameplate + CarbonFootprint usw.). " +
        "format=\"value\" (Default, kompakt) oder \"full\" (vollständiges AAS-JSON je Submodel). " +
        "totalMatches zeigt, wie viele Produkte insgesamt passen (geliefert wird das erste).")]
    public async Task<object> AasFindProduct(
        [Description("Liste von Suchbedingungen (mindestens eine), gleiche Slots wie aas_query: scope/field/idShortPath/op/value.")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\".")] string combine = "and",
        [Description("Ausgabeformat je Submodel: \"value\" (kompakt, Default, nur idShort->Wert) oder \"full\" (vollständiges AAS-JSON inkl. semanticId, valueType, Qualifier). Nutze \"full\", wenn nach semanticId, Einheiten oder Datentyp gefragt wird.")] string format = "value")
    {
        using var _ = LogCallTimed($"aas_find_product {DescribeConditions(conditions, combine)} format={format}");

        var fmt = (format ?? "value").Trim().ToLowerInvariant();
        if (fmt != "value" && fmt != "full")
        {
            return new { error = $"Unbekanntes format \"{format}\". Erlaubt: \"value\" oder \"full\"." };
        }

        string expression;
        try
        {
            expression = BuildExpression(conditions, combine);
        }
        catch (ArgumentException ex)
        {
            return new { error = ex.Message };
        }

        return await FindProductCore(expression, fmt);
    }

    [McpServerTool(Name = "aas_find_product_simple", Title = "Find Product by Simple Property", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Findet ein Produkt anhand EINES Merkmals und liefert es komplett zurück — der einfachste Weg, ohne Bedingungs-Syntax. " +
        "Gib einfach den Merkmalsnamen (idShort, z.B. \"flowMax\") und den gesuchten Wert (z.B. \"80\") an. " +
        "Das Tool findet das Produkt, dessen Element mit diesem idShort den angegebenen Wert hat, und liefert die AAS plus die Werte ALLER Submodelle " +
        "(Hersteller/Nameplate, technische Daten, CO2-Fußabdruck usw.) — alles in einem Aufruf. " +
        "KEIN scope/op/conditions/format nötig: nur idShort und value. Das Ergebnis ist bewusst kompakt gehalten.")]
    public async Task<object> AasFindProductSimple(
        [Description("Name des gesuchten Merkmals/Elements (idShort), z.B. \"flowMax\" oder \"ManufacturerName\".")] string idShort,
        [Description("Gesuchter Wert dieses Merkmals, z.B. \"80\".")] string value)
    {
        using var _ = LogCallTimed($"aas_find_product_simple idShort={idShort} value={value}");

        if (string.IsNullOrWhiteSpace(idShort))
        {
            return new { error = "idShort darf nicht leer sein (z.B. \"flowMax\")." };
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return new { error = "value darf nicht leer sein (z.B. \"80\")." };
        }

        var conditions = new[]
        {
            new McpQueryCondition { Scope = "sme", Field = "value", IdShortPath = idShort.Trim(), Op = "eq", Value = value },
        };

        string expression;
        try
        {
            expression = BuildExpression(conditions, "and");
        }
        catch (ArgumentException ex)
        {
            return new { error = ex.Message };
        }

        // Bewusst immer "value" (kompakt) + coreOnly: full und Datei-/Doku-Submodelle überfordern sehr kleine Modelle (Halluzination).
        return await FindProductCore(expression, "value", coreOnly: true);
    }

    [McpServerTool(Name = "aas_find_concepts", Title = "Find Concept Definitions (Semantic IDs)", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Durchsucht den Merkmalskatalog (ConceptDescriptions, ECLASS/IEC 61360) nach Stichwörtern und liefert die semantischen Definitionen der Datenfelder: " +
        "IRDI (= semanticId, z.B. 0173-1#02-AAM635#003), Namen (de/en), Einheit, Datentyp und Definition. " +
        "ZUERST verwenden, wenn unklar ist, wie ein Merkmal in den Daten heißt (z.B. \"Ausgangsspannung\", \"output power\", \"Gewicht\") — " +
        "danach mit aas_query exakt suchen, statt idShort-Schreibweisen zu raten: " +
        "EIN Merkmal: field=\"semanticId\" op=\"eq\" value=<id> plus optional eine value-Bedingung (combine=\"and\") — beide beziehen sich auf DASSELBE Element. " +
        "MEHRERE Merkmale (z.B. Spannung UND Leistung): NICHT mehrere semanticId/value-Paare kombinieren (ergibt immer 0 Treffer) — stattdessen je Merkmal eine Bedingung mit idShortPath=<idShort des Konzepts> und op/value; der idShort des Konzepts ist zugleich der idShort des Elements in den Daten. " +
        "Die Suche ist case-insensitiv; mehrere Wörter sind UND-verknüpft und werden in idShort, preferredName, shortName und definition (de+en) gesucht. " +
        "Bei 0 Treffern: Synonyme oder die jeweils andere Sprache probieren (z.B. \"voltage\" statt \"Spannung\"). " +
        "usageCount gibt an, in wie vielen Submodellen die semanticId tatsächlich vorkommt (gedeckelt bei 1000; 1000 = 1000 oder mehr) — Konzepte mit usageCount=0 existieren nur im Katalog und sind für Suchen nutzlos.")]
    public async Task<object> AasFindConcepts(
        [Description("Suchbegriffe, z.B. \"output voltage\" oder \"Ausgangsspannung\". Mehrere Wörter müssen alle vorkommen (UND). Gesucht wird in deutschen UND englischen Texten.")] string search,
        [Description("Maximale Trefferzahl (Default 20, Maximum 100).")] int? limit = null,
        [Description("Bevorzugte Sprache der definition im Ergebnis: \"en\" oder \"de\" (Default \"en\"; fehlt die Sprache, wird die andere genommen).")] string lang = "en",
        [Description("Wenn true (Default), wird je Treffer gezählt, in wie vielen Submodellen die semanticId vorkommt (usageCount; wird für maximal die ersten 15 Treffer berechnet).")] bool withUsage = true)
    {
        using var _ = LogCallTimed($"aas_find_concepts search=\"{search}\" limit={(limit?.ToString(CultureInfo.InvariantCulture) ?? "-")} withUsage={withUsage}");

        if (string.IsNullOrWhiteSpace(search))
        {
            return new { error = "search darf nicht leer sein, z.B. \"output voltage\" oder \"Ausgangsspannung\"." };
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);

        List<ConceptInfo> catalog;
        try
        {
            catalog = await GetConceptCatalog(securityConfig);
        }
        catch (Exception ex)
        {
            return new { error = "ConceptDescriptions konnten nicht geladen werden: " + ex.Message };
        }

        var terms = search.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var requestedLimit = Math.Min(limit is > 0 ? limit.Value : DefaultConceptLimit, MaxConceptLimit);

        // Alle Terme müssen irgendwo vorkommen; Treffer im Namen (idShort/preferredName/shortName)
        // zählen doppelt, damit "voltage" MaxOutputVoltage vor Konzepten mit "voltage" nur in der Definition listet.
        // Mit withUsage wird ein größerer Kandidaten-Pool gezogen und erst NACH der Usage-Sortierung auf limit
        // geschnitten — sonst verdrängen textlich gleichwertige Katalogleichen bei kleinem limit die real
        // genutzten Konzepte (Counts sind dank 1000er-Deckel billig).
        var ranked = catalog
            .Select(c => new
            {
                Concept = c,
                Score = terms.Sum(t => c.NameText.Contains(t, StringComparison.Ordinal) ? 2
                    : c.SearchText.Contains(t, StringComparison.Ordinal) ? 1 : 0),
            })
            .Where(x => terms.All(t => x.Concept.SearchText.Contains(t, StringComparison.Ordinal)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Concept.IdShort, StringComparer.OrdinalIgnoreCase)
            .Take(withUsage ? Math.Max(requestedLimit, ConceptRankingPoolSize) : requestedLimit)
            .ToList();

        // Usage nur für die vorderen Treffer zählen (je ein indizierter COUNT über die Query-Pipeline, gecacht).
        var usage = new Dictionary<string, int?>(StringComparer.Ordinal);
        if (withUsage)
        {
            foreach (var x in ranked.Take(MaxConceptUsageLookups))
            {
                usage[x.Concept.Id] = await GetConceptUsageCount(securityConfig, x.Concept.Id);
            }
        }

        var preferGerman = string.Equals(lang?.Trim(), "de", StringComparison.OrdinalIgnoreCase);
        var concepts = ranked
            .OrderByDescending(x => usage.TryGetValue(x.Concept.Id, out var u) ? u ?? -1 : -1)
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.Concept.IdShort, StringComparer.OrdinalIgnoreCase)
            .Take(requestedLimit)
            .Select(x => new
            {
                id = x.Concept.Id,
                idShort = x.Concept.IdShort,
                nameEn = x.Concept.NameEn,
                nameDe = x.Concept.NameDe,
                unit = x.Concept.Unit,
                dataType = x.Concept.DataType,
                definition = preferGerman ? (x.Concept.DefDe ?? x.Concept.DefEn) : (x.Concept.DefEn ?? x.Concept.DefDe),
                usageCount = usage.TryGetValue(x.Concept.Id, out var u) ? u : null,
            })
            .ToList();

        return new
        {
            search,
            count = concepts.Count,
            concepts,
            nextStep = concepts.Count > 0
                ? "In den Daten suchen mit aas_query. EIN Merkmal: conditions=[{scope:\"sme\",field:\"semanticId\",op:\"eq\",value:\"<id>\"},{scope:\"sme\",field:\"value\",op:\"gt\",value:\"...\"}] mit combine=\"and\" — " +
                  "beide Bedingungen beziehen sich auf DASSELBE Element. MEHRERE Merkmale: je Merkmal EINE Bedingung mit idShortPath=<idShort des Konzepts> und op/value " +
                  "(z.B. {scope:\"sme\",idShortPath:\"Power_output\",op:\"gt\",value:\"500\"}); mehrere semanticId/value-Paare in einem and ergeben immer 0 Treffer. " +
                  "Konzepte mit usageCount=0 nicht verwenden; bei mehreren passenden Kandidaten op=\"in\" mit values=[<id1>,<id2>]."
                : "Keine Konzepte gefunden. Synonyme oder die andere Sprache probieren (z.B. \"voltage\" statt \"Spannung\") oder weniger/kürzere Suchwörter verwenden.",
        };
    }

    // --------------- ConceptDescription-Katalog (Cache für aas_find_concepts) ---------------

    private const int DefaultConceptLimit = 20;
    private const int MaxConceptLimit = 100;
    private const int MaxConceptUsageLookups = 40;
    private const int ConceptRankingPoolSize = 30;
    private const int UsageCountCap = 1000;
    private static readonly TimeSpan ConceptCacheTtl = TimeSpan.FromMinutes(10);

    // Kompakte, durchsuchbare Sicht auf eine ConceptDescription (dedupliziert nach Id).
    private sealed class ConceptInfo
    {
        public required string Id { get; init; }
        public string? IdShort { get; init; }
        public string? NameEn { get; init; }
        public string? NameDe { get; init; }
        public string? Unit { get; init; }
        public string? DataType { get; init; }
        public string? DefEn { get; init; }
        public string? DefDe { get; init; }
        public required string NameText { get; init; }   // lowercase: idShort + Namen
        public required string SearchText { get; init; } // lowercase: NameText + Definitionen + Id
    }

    private static List<ConceptInfo>? s_conceptCatalog;
    private static DateTime s_conceptCatalogTime;
    private static readonly System.Threading.SemaphoreSlim s_conceptCatalogLock = new(1, 1);

    // usageCount-Cache je IRDI: COUNT-Queries auf großen DBs nicht bei jeder Konzeptsuche wiederholen.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime Time)> s_conceptUsage =
        new(StringComparer.Ordinal);

    private async Task<List<ConceptInfo>> GetConceptCatalog(SecurityConfig securityConfig)
    {
        var cached = s_conceptCatalog;
        if (cached != null && DateTime.UtcNow - s_conceptCatalogTime < ConceptCacheTtl)
        {
            return cached;
        }

        await s_conceptCatalogLock.WaitAsync();
        try
        {
            cached = s_conceptCatalog;
            if (cached != null && DateTime.UtcNow - s_conceptCatalogTime < ConceptCacheTtl)
            {
                return cached;
            }

            var watch = Stopwatch.StartNew();
            var pagination = new PaginationParameters(null, int.MaxValue);
            var cdList = await _dbRequestHandlerService.ReadPagedConceptDescriptions(pagination, securityConfig);

            // Die Liste enthält je Environment dieselbe CD erneut — nach Id deduplizieren.
            var byId = new Dictionary<string, ConceptInfo>(StringComparer.Ordinal);
            foreach (var cd in (cdList ?? new List<IClass>()).OfType<IConceptDescription>())
            {
                if (string.IsNullOrWhiteSpace(cd.Id) || byId.ContainsKey(cd.Id))
                {
                    continue;
                }

                byId[cd.Id] = BuildConceptInfo(cd);
            }

            var catalog = byId.Values.ToList();
            s_conceptCatalog = catalog;
            s_conceptCatalogTime = DateTime.UtcNow;
            LogMcpLine($"aas_find_concepts catalog: {catalog.Count} distinct concepts loaded in {watch.ElapsedMilliseconds} ms");
            return catalog;
        }
        finally
        {
            s_conceptCatalogLock.Release();
        }
    }

    private static ConceptInfo BuildConceptInfo(IConceptDescription cd)
    {
        var iec = cd.EmbeddedDataSpecifications?
            .Select(e => e?.DataSpecificationContent)
            .OfType<IDataSpecificationIec61360>()
            .FirstOrDefault();

        var nameEn = PickLangText(iec?.PreferredName, "en") ?? PickLangText(iec?.ShortName, "en");
        var nameDe = PickLangText(iec?.PreferredName, "de") ?? PickLangText(iec?.ShortName, "de");
        var defEn = TruncateText(PickLangText(iec?.Definition, "en"), 300);
        var defDe = TruncateText(PickLangText(iec?.Definition, "de"), 300);

        var nameText = JoinLower(cd.IdShort, nameEn, nameDe, PickLangText(iec?.ShortName, null));
        var searchText = JoinLower(nameText, defEn, defDe, cd.Id);

        return new ConceptInfo
        {
            Id = cd.Id,
            IdShort = cd.IdShort,
            NameEn = nameEn,
            NameDe = nameDe,
            Unit = string.IsNullOrWhiteSpace(iec?.Unit) ? null : iec.Unit,
            DataType = iec?.DataType is { } dataType ? Stringification.ToString(dataType) : null,
            DefEn = defEn,
            DefDe = defDe,
            NameText = nameText,
            SearchText = searchText,
        };
    }

    // Text einer LangString-Liste in der gewünschten Sprache; language=null nimmt den ersten Eintrag.
    private static string? PickLangText(System.Collections.IEnumerable? langStrings, string? language)
    {
        if (langStrings is null)
        {
            return null;
        }

        string? first = null;
        foreach (var ls in langStrings.OfType<IAbstractLangString>())
        {
            if (string.IsNullOrWhiteSpace(ls.Text))
            {
                continue;
            }

            first ??= ls.Text;
            if (language != null && string.Equals(ls.Language?.Trim(), language, StringComparison.OrdinalIgnoreCase))
            {
                return ls.Text;
            }
        }

        return language is null ? first : null;
    }

    private static string JoinLower(params string?[] parts)
        => string.Join('\n', parts.Where(p => !string.IsNullOrWhiteSpace(p))).ToLowerInvariant();

    private static string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "…";
    }

    // Anzahl Submodelle, in denen die semanticId vorkommt — indizierter COUNT über die Query-Pipeline, gecacht.
    private async Task<int?> GetConceptUsageCount(SecurityConfig securityConfig, string semanticId)
    {
        if (s_conceptUsage.TryGetValue(semanticId, out var entry) && DateTime.UtcNow - entry.Time < ConceptCacheTtl)
        {
            return entry.Count;
        }

        try
        {
            var expression = BuildExpression(
                new[] { new McpQueryCondition { Scope = "sme", Field = "semanticId", Op = "eq", Value = semanticId } }, "and");

            // Gedeckelter Count (LIMIT im Subselect bricht früh ab): "1000" bedeutet "1000 oder mehr".
            // Für die Frage "existiert das Konzept in den Daten und wie verbreitet ist es?" reicht das —
            // exakte Zahlen über hunderttausende Treffer kosten Sekunden pro semanticId.
            var count = await _dbRequestHandlerService.QueryCountSMs(
                securityConfig, string.Empty, string.Empty, string.Empty,
                new PaginationParameters(null, UsageCountCap), ResultType.Submodel, expression);
            s_conceptUsage[semanticId] = (count, DateTime.UtcNow);
            return count;
        }
        catch
        {
            // usageCount ist Zusatzinfo — ein Fehler (z.B. Engine-Limit) darf die Konzeptsuche nicht scheitern lassen.
            return null;
        }
    }

    [McpServerTool(Name = "aas_describe_model", Title = "Describe Submodel Structures", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Liefert einen Überblick über die real vorhandenen Datenstrukturen: welche Submodel-Typen es gibt (idShort, semanticId, exakte Gesamtzahl — " +
        "deterministisch und vollständig aus der Datenbank ermittelt, inkl. semanticId-Varianten je Typ) " +
        "und welche Felder darin vorkommen — je Feld idShortPath, semanticId, Element-Typ, valueType, Einheit, Name und ein Beispielwert. " +
        "Die Feldlisten stammen aus einer über den Bestand gestreuten Stichprobe echter Submodelle, angereichert mit den ConceptDescriptions. " +
        "IDEALER ERSTER AUFRUF einer Session, BEVOR Suchanfragen formuliert werden: danach sind die korrekten idShortPaths und semanticIds " +
        "für aas_query-Bedingungen, select-Projektionen und Exporte bekannt, statt Feldnamen zu raten. " +
        "seenInSamples zeigt, in wie vielen der Stichproben-Submodelle das Feld vorkam — Felder, die nur bestimmte Produktklassen haben " +
        "(z.B. Ausgangsspannung nur bei Netzteilen), können in der Stichprobe fehlen; solche Merkmale gezielt über aas_find_concepts suchen. " +
        "Das Ergebnis wird serverseitig 10 Minuten gecacht.")]
    public async Task<object> AasDescribeModel(
        [Description("Optional: nur diesen Submodel-Typ beschreiben (idShort, z.B. \"TechnicalData\"). Ohne Angabe werden alle gefundenen Submodel-Typen beschrieben.")] string? submodel = null,
        [Description("Stichprobengröße je Submodel-Typ (Default 5, Maximum 20). Mehr Stichproben finden mehr produktklassen-spezifische Felder, dauern aber länger.")] int? samples = null,
        [Description("Maximale Feldzahl je Submodel-Typ im Ergebnis (Default 200, Maximum 500). Häufige Felder zuerst; truncated=true zeigt Kürzung an.")] int? maxFields = null,
        [Description("Bevorzugte Sprache für Namen aus den ConceptDescriptions und Beispielwerte mehrsprachiger Felder (Default \"en\").")] string lang = "en",
        [Description("Wenn true, wird für SELTENE Felder (nicht in allen Stichproben) gezählt, in wie vielen Submodellen des Bestands ihre semanticId vorkommt (usageCount, gedeckelt bei 1000 = \"1000 oder mehr\"; gecacht, max. 30 frische Zählungen pro Aufruf). Allgegenwärtige Felder erhalten keinen usageCount — dort genügt seenInSamples. Default false.")] bool withUsage = false)
    {
        using var _ = LogCallTimed($"aas_describe_model submodel={submodel ?? "-"} samples={(samples?.ToString(CultureInfo.InvariantCulture) ?? "-")} withUsage={withUsage}");

        var sampleCount = Math.Clamp(samples ?? DefaultDescribeSamples, 1, MaxDescribeSamples);
        var fieldCap = Math.Clamp(maxFields ?? DefaultDescribeMaxFields, 10, MaxDescribeMaxFields);
        var normalizedLang = string.IsNullOrWhiteSpace(lang) ? "en" : lang.Trim();
        var templateFilter = string.IsNullOrWhiteSpace(submodel) ? null : submodel.Trim();

        var cacheKey = $"{templateFilter ?? "*"}|{sampleCount}|{fieldCap}|{normalizedLang}|{withUsage}";
        if (s_describeCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.Time < ConceptCacheTtl)
        {
            return cached.Value;
        }

        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        object result;
        try
        {
            result = await BuildModelDescription(securityConfig, templateFilter, sampleCount, fieldCap, normalizedLang, withUsage);
        }
        catch (Exception ex)
        {
            return new { error = "Modellbeschreibung fehlgeschlagen: " + ex.Message };
        }

        s_describeCache[cacheKey] = (result, DateTime.UtcNow);
        return result;
    }

    // --------------- Modellbeschreibung (aas_describe_model) ---------------

    private const int DefaultDescribeSamples = 5;
    private const int MaxDescribeSamples = 20;
    private const int DefaultDescribeMaxFields = 200;
    private const int MaxDescribeMaxFields = 500;
    private const int MaxDescribeUsageLookups = 30;
    private const int DescribeDiscoveryPageSize = 40;
    private const int DescribeEmptyRetryPageSize = 10;
    private const int MaxDescribeTemplates = 50;
    private static readonly TimeSpan DescribeUsageTimeBudget = TimeSpan.FromSeconds(15);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (object Value, DateTime Time)> s_describeCache =
        new(StringComparer.Ordinal);

    // Aggregat eines Feldes über die Stichprobe: gleiche Struktur (Pfad mit neutralisierten Ziffern) + gleiche semanticId = ein Feld.
    private sealed class FieldAggregate
    {
        public required string Path { get; init; }
        public string? SemanticId { get; init; }
        public required string Type { get; init; }
        public string? ValueType { get; set; }
        public string? Example { get; set; }
        public int SeenIn { get; set; }
        public HashSet<string> Variants { get; } = new(StringComparer.Ordinal);
    }

    private async Task<object> BuildModelDescription(
        SecurityConfig securityConfig, string? templateFilter, int sampleCount, int fieldCap, string lang, bool withUsage)
    {
        // 1) DETERMINISTISCH: vollständiges Template-Inventar der DB (GROUP BY IdShort+SemanticId über die
        //    SMSets — zwei Spalten, eine Zeile je Submodel; NICHT die großen SMESets). Erst danach Stichprobe.
        List<DbSubmodelTemplateRow>? inventory = null;
        var inventoryWatch = Stopwatch.StartNew();
        try
        {
            inventory = await _dbRequestHandlerService.ReadSubmodelTemplates(securityConfig);
            LogMcpLine($"aas_describe_model inventory: {inventory.Count} rows in {inventoryWatch.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            // Fallback (z.B. Security aktiv): Templates aus der Discovery-Seite ableiten — dann stochastisch.
            LogMcpLine($"aas_describe_model inventory unavailable ({ex.Message}); using page discovery");
        }

        // 2) Erste Stichproben aus einer Seite echter Submodelle; ohne Inventar zugleich Template-Entdeckung.
        var discovery = await _dbRequestHandlerService.ReadPagedSubmodels(
            new PaginationParameters(null, DescribeDiscoveryPageSize), securityConfig, null, null, null, null);

        var localSamples = new Dictionary<string, List<ISubmodel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var sm in (discovery ?? new List<IClass>()).OfType<ISubmodel>())
        {
            if (string.IsNullOrWhiteSpace(sm.IdShort))
            {
                continue;
            }

            if (templateFilter != null && !string.Equals(sm.IdShort, templateFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!localSamples.TryGetValue(sm.IdShort, out var list))
            {
                localSamples[sm.IdShort] = list = new List<ISubmodel>();
            }

            // Höchstens 2 Muster aus dem Anfang des Bestands — der Rest wird gestreut nachgeladen,
            // damit auch produktklassen-spezifische Felder (variable TechnicalData) auftauchen.
            if (list.Count < 2)
            {
                list.Add(sm);
            }
        }

        // 3) Template-Liste: aus dem Inventar (vollständig, exakte Zahlen, semanticId-Varianten),
        //    im Fallback aus den Discovery-Stichproben.
        List<(string Name, int? TotalCount, List<DbSubmodelTemplateRow>? Variants)> templateInfos;
        if (inventory != null)
        {
            templateInfos = inventory
                .Where(row => !string.IsNullOrWhiteSpace(row.IdShort))
                .GroupBy(row => row.IdShort!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => templateFilter == null || string.Equals(g.Key, templateFilter, StringComparison.OrdinalIgnoreCase))
                .Select(g => (
                    Name: g.Key,
                    TotalCount: (int?)g.Sum(row => row.Count),
                    Variants: (List<DbSubmodelTemplateRow>?)g.OrderByDescending(row => row.Count).ToList()))
                .OrderByDescending(t => t.TotalCount)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            // Explizit angefragter Typ, der in der Discovery-Seite nicht vorkam:
            // leer starten, die Stichprobe kommt dann komplett über die Query-Pipeline.
            if (templateFilter != null && !localSamples.ContainsKey(templateFilter))
            {
                localSamples[templateFilter] = new List<ISubmodel>();
            }

            templateInfos = localSamples.Keys
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => (name, (int?)null, (List<DbSubmodelTemplateRow>?)null))
                .ToList();
        }

        var catalog = await GetConceptCatalog(securityConfig);
        var cdById = new Dictionary<string, ConceptInfo>(StringComparer.Ordinal);
        foreach (var concept in catalog)
        {
            cdById.TryAdd(concept.Id, concept);
        }

        var templates = new List<object>();
        foreach (var info in templateInfos.Take(MaxDescribeTemplates))
        {
            localSamples.TryGetValue(info.Name, out var samplesForTemplate);
            templates.Add(await DescribeTemplate(
                securityConfig, info.Name, samplesForTemplate ?? new List<ISubmodel>(),
                sampleCount, fieldCap, lang, withUsage, cdById, info.TotalCount, info.Variants));
        }

        return new
        {
            templateCount = templateInfos.Count,
            truncatedTemplates = templateInfos.Count > MaxDescribeTemplates ? true : (bool?)null,
            templates,
            nextStep = templates.Count > 0
                ? "So damit suchen (aas_query): EIN Merkmal: conditions=[{scope:\"sme\",field:\"semanticId\",op:\"eq\",value:<semanticId>},{scope:\"sme\",field:\"value\",op:\"gt\",value:\"...\"}] mit combine=\"and\" (beide auf DASSELBE Element bezogen). " +
                  "MEHRERE Merkmale: je Merkmal EINE Bedingung {scope:\"sme\",idShortPath:<path>,op,value}. " +
                  "Tabellen/Exporte: select=[<path>], für Felder anderer Submodelle derselben AAS /<SubmodelIdShort>/<path>. " +
                  "Einzelne Merkmale per Stichwort: aas_find_concepts."
                : "Keine Submodelle gefunden. Ohne submodel-Filter aufrufen oder Schreibweise des idShort prüfen.",
        };
    }

    private async Task<object> DescribeTemplate(
        SecurityConfig securityConfig, string templateName, List<ISubmodel> localSamples,
        int sampleCount, int fieldCap, string lang, bool withUsage, Dictionary<string, ConceptInfo> cdById,
        int? knownTotalCount, List<DbSubmodelTemplateRow>? semanticVariants)
    {
        var expression = BuildExpression(
            new[] { new McpQueryCondition { Scope = "sm", Field = "idShort", Op = "eq", Value = templateName } }, "and");

        var totalCount = knownTotalCount ?? 0;
        if (knownTotalCount == null)
        {
            try
            {
                totalCount = await _dbRequestHandlerService.QueryCountSMs(
                    securityConfig, string.Empty, string.Empty, string.Empty,
                    new PaginationParameters(null, int.MaxValue), ResultType.Submodel, expression);
            }
            catch
            {
                // Gesamtzahl ist Zusatzinfo; Beschreibung geht mit totalCount=0 weiter.
            }
        }

        // Stichprobe auffüllen: gestreute Offsets über den Bestand statt nur Anfangsbereich.
        var sampled = new List<ISubmodel>(localSamples);
        var sampledIds = new HashSet<string>(sampled.Select(s => s.Id ?? string.Empty), StringComparer.Ordinal);
        var needed = Math.Min(sampleCount, totalCount > 0 ? totalCount : sampleCount) - sampled.Count;
        for (var i = 1; i <= needed && totalCount > localSamples.Count; i++)
        {
            var offset = (long)totalCount * i / (needed + 1);
            var (list, err) = await TryQuery(
                securityConfig, new PaginationParameters(offset.ToString(CultureInfo.InvariantCulture), 1), ResultType.Submodel, expression);
            if (err != null)
            {
                break;
            }

            var id = ExtractIdentifiers(list).FirstOrDefault();
            if (id == null || !sampledIds.Add(id))
            {
                continue;
            }

            try
            {
                if (await _dbRequestHandlerService.ReadSubmodelById(securityConfig, null, id, null, null) is ISubmodel sm)
                {
                    sampled.Add(sm);
                }
            }
            catch (NotFoundException)
            {
                // Treffer zwischen Query und Read verschwunden — Stichprobe bleibt einfach kleiner.
            }
        }

        var fields = new Dictionary<string, FieldAggregate>(StringComparer.Ordinal);

        void AggregateSample(ISubmodel sm)
        {
            var seenThisSample = new HashSet<string>(StringComparer.Ordinal);
            CollectFieldInfos(sm.SubmodelElements, string.Empty, (path, element) =>
            {
                var semanticId = LastKeyValue(element.SemanticId);
                var key = NormalizeStructurePath(path) + "|" + (semanticId ?? string.Empty);
                if (!fields.TryGetValue(key, out var agg))
                {
                    fields[key] = agg = new FieldAggregate
                    {
                        Path = path,
                        SemanticId = semanticId,
                        Type = element.GetType().Name,
                    };
                }

                agg.Variants.Add(path);
                if (seenThisSample.Add(key))
                {
                    agg.SeenIn++;
                }

                agg.Example ??= ExampleValue(element, lang);
                if (agg.ValueType == null && element is IProperty property)
                {
                    agg.ValueType = Stringification.ToString(property.ValueType);
                }
            });
        }

        foreach (var sm in sampled)
        {
            AggregateSample(sm);
        }

        // Alle Stichproben leer (z.B. CarbonFootprint: bei jedem Produkt vorhanden, aber überwiegend
        // unbefüllt)? Dann gezielt eine Seite dieses Typs laden und die ersten befüllten Exemplare beschreiben.
        if (fields.Count == 0 && totalCount > 0)
        {
            try
            {
                var retry = await _dbRequestHandlerService.ReadPagedSubmodels(
                    new PaginationParameters(null, DescribeEmptyRetryPageSize), securityConfig, null, templateName, null, null);
                var added = 0;
                foreach (var sm in (retry ?? new List<IClass>()).OfType<ISubmodel>())
                {
                    if (sm.SubmodelElements is not { Count: > 0 } || !sampledIds.Add(sm.Id ?? string.Empty))
                    {
                        continue;
                    }

                    sampled.Add(sm);
                    AggregateSample(sm);
                    if (++added >= 3)
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Nachfass ist Best Effort; die Beschreibung bleibt sonst bei "alle Stichproben leer".
            }
        }

        var emptySamples = sampled.Count(s => s.SubmodelElements is not { Count: > 0 });
        string? note = null;
        if (fields.Count == 0 && totalCount > 0)
        {
            note = $"Alle {sampled.Count} Stichproben sind leer — dieser Submodel-Typ existiert {totalCount}-mal, ist aber überwiegend unbefüllt. " +
                   "Befüllte Exemplare über aas_query finden (sm.idShort eq + eine sme-Bedingung, z.B. sme.idShort eq <Feldname>).";
        }
        else if (emptySamples > 0)
        {
            note = $"{emptySamples} von {sampled.Count} Stichproben waren leer — dieser Submodel-Typ ist nicht bei allen Produkten befüllt.";
        }

        var ordered = fields.Values
            .OrderByDescending(f => f.SeenIn)
            .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usage = new Dictionary<string, int?>(StringComparer.Ordinal);
        if (withUsage)
        {
            // Allgegenwärtige Felder nicht zählen: der COUNT läuft dann über hunderttausende Treffer
            // (teuer, gemessen ~5 s/Stück) und sagt nichts Neues — seenInSamples == sampledCount genügt.
            // WICHTIG: über die semanticId ausschließen, nicht nur über die Feldzeile — dieselbe IRDI
            // taucht oft zusätzlich an seltenen Pfaden auf (z.B. Zubehör-Äste) und würde dort doch gezählt.
            var ubiquitousIds = new HashSet<string>(
                fields.Values.Where(f => f.SeenIn >= sampled.Count && f.SemanticId != null).Select(f => f.SemanticId!),
                StringComparer.Ordinal);

            // Seltenste zuerst (selektivste = billigste + informativste Counts); Stückzahl- UND Zeitbudget,
            // denn auch sample-seltene Felder können hunderttausendfach vorkommen.
            var budget = MaxDescribeUsageLookups;
            var usageWatch = Stopwatch.StartNew();
            foreach (var f in ordered.Take(fieldCap).OrderBy(f => f.SeenIn))
            {
                if (f.SemanticId == null || usage.ContainsKey(f.SemanticId) || ubiquitousIds.Contains(f.SemanticId))
                {
                    continue;
                }

                if (s_conceptUsage.TryGetValue(f.SemanticId, out var entry) && DateTime.UtcNow - entry.Time < ConceptCacheTtl)
                {
                    usage[f.SemanticId] = entry.Count;
                    continue;
                }

                if (budget-- <= 0 || usageWatch.Elapsed > DescribeUsageTimeBudget)
                {
                    continue;
                }

                usage[f.SemanticId] = await GetConceptUsageCount(securityConfig, f.SemanticId);
            }
        }

        var preferGerman = string.Equals(lang, "de", StringComparison.OrdinalIgnoreCase);
        var rows = ordered
            .Take(fieldCap)
            .Select(f =>
            {
                ConceptInfo? cd = null;
                if (f.SemanticId != null)
                {
                    cdById.TryGetValue(f.SemanticId, out cd);
                }

                return new
                {
                    path = f.Path,
                    variants = f.Variants.Count > 1 ? (int?)f.Variants.Count : null,
                    type = f.Type,
                    valueType = f.ValueType,
                    semanticId = f.SemanticId,
                    name = cd == null ? null : (preferGerman ? cd.NameDe ?? cd.NameEn : cd.NameEn ?? cd.NameDe),
                    unit = cd?.Unit,
                    example = f.Example,
                    seenInSamples = f.SeenIn,
                    usageCount = f.SemanticId != null && usage.TryGetValue(f.SemanticId, out var u) ? u : null,
                };
            })
            .ToList();

        return new
        {
            submodel = templateName,
            semanticId = semanticVariants is { Count: > 0 }
                ? semanticVariants[0].SemanticId
                : LastKeyValue(sampled.FirstOrDefault()?.SemanticId),
            semanticIdVariants = semanticVariants is { Count: > 1 }
                ? (object?)semanticVariants.Select(v => new { semanticId = v.SemanticId, count = v.Count }).ToList()
                : null,
            totalCount,
            sampledCount = sampled.Count,
            note,
            fieldCount = fields.Count,
            truncated = fields.Count > fieldCap ? true : (bool?)null,
            fields = rows,
        };
    }

    // Rekursiver Strukturlauf: ruft visit(idShortPath, element) für jedes Element inkl. Kindern von Collection/List/Entity.
    private static void CollectFieldInfos(List<ISubmodelElement>? elements, string prefix, Action<string, ISubmodelElement> visit)
    {
        if (elements == null)
        {
            return;
        }

        var index = 0;
        foreach (var element in elements)
        {
            if (element == null)
            {
                continue;
            }

            var name = string.IsNullOrEmpty(element.IdShort) ? $"[{index}]" : element.IdShort;
            var path = prefix.Length == 0 ? name : prefix + "." + name;
            visit(path, element);

            List<ISubmodelElement>? children = element switch
            {
                ISubmodelElementCollection collection => collection.Value,
                ISubmodelElementList list => list.Value,
                IEntity entity => entity.Statements,
                _ => null,
            };
            CollectFieldInfos(children, path, visit);
            index++;
        }
    }

    // Nummerierte Wiederholungen (Application_standards00/01/02, [0]/[1]) auf eine Strukturzeile kollabieren:
    // Ziffern am Segmentende werden für den Gruppierungsschlüssel neutralisiert.
    private static string NormalizeStructurePath(string path)
        => System.Text.RegularExpressions.Regex.Replace(path, @"\d+(?=\.|$)", "#");

    private static string? LastKeyValue(IReference? reference)
        => reference?.Keys is { Count: > 0 } keys ? keys[^1]?.Value : null;

    // Kompakter Beispielwert eines Elements für die Modellbeschreibung (Skalar bzw. eine Sprache, gekürzt).
    private static string? ExampleValue(ISubmodelElement element, string lang)
        => element switch
        {
            IProperty property => TruncateText(property.Value, 40),
            IMultiLanguageProperty mlp => TruncateText(PickLangText(mlp.Value, lang) ?? PickLangText(mlp.Value, null), 40),
            IRange range => TruncateText($"{range.Min}..{range.Max}", 40),
            _ => null,
        };

    // --------------- Helfer ---------------

    // Führt eine Query aus und fängt Engine-Fehler ab (z.B. ungültiges SQL durch aas.* in Submodel-Queries),
    // damit daraus ein lesbares {error} wird statt einer unhandled exception.
    private async Task<(List<object>? List, string? Error)> TryQuery(SecurityConfig securityConfig, PaginationParameters pagination, ResultType resultType, string expression)
    {
        try
        {
            var list = await _dbRequestHandlerService.QueryGetSMs(securityConfig, pagination, resultType, expression);
            return (list, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // Gemeinsame Produkt-Suche: Expression ausführen, ersten Submodel-Treffer zur Shell auflösen, ganzes Produkt liefern.
    private async Task<object> FindProductCore(string expression, string fmt, bool coreOnly = false)
    {
        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var pagination = new PaginationParameters(null, DefaultPageSize);
        var (list, queryError) = await TryQuery(securityConfig, pagination, ResultType.Submodel, expression);
        if (queryError != null)
        {
            return new { error = "Query fehlgeschlagen: " + queryError };
        }

        var ids = ExtractIdentifiers(list);

        if (ids.Count == 0)
        {
            return new { found = false, totalMatches = 0, message = "Keine Treffer. Anderen Wert/Schreibweise versuchen oder Groß-/Kleinschreibung des idShort prüfen." };
        }

        var firstId = ids[0];
        var shell = await ResolveShellForIdentifier(securityConfig, firstId);
        if (shell is null)
        {
            return new { found = false, matchedSubmodel = firstId, totalMatches = ids.Count, message = "Treffer gefunden, aber keine zugehörige Shell auflösbar." };
        }

        var product = await BuildProductObject(securityConfig, shell, fmt, coreOnly);
        product["matchedSubmodel"] = firstId;
        product["totalMatches"] = ids.Count;
        return product;
    }

    // Kompaktes Console-Log jedes MCP-Aufrufs (eine Zeile), passend zum übrigen Server-Log.
    private static void LogCall(string line)
    {
        DiagnosticsLog.WriteMcp(string.Empty);
        DiagnosticsLog.WriteMcp(line, timestamp: true);
    }

    private static void LogMcpLine(string line)
    {
        DiagnosticsLog.WriteMcp(line);
    }

    // Loggt den Aufruf (Eingang) und beim Dispose die Dauer — für Performance-Analyse bei großer DB.
    // Verwendung: using var _ = LogCallTimed($"aas_query ...");  -> beim Methodenende kommt "[MCP] <tool> done in X ms".
    private static CallTimer LogCallTimed(string line)
    {
        var scope = DiagnosticsLog.BeginMcpScope();
        LogCall(line);
        return new CallTimer(line.Split(' ', 2)[0], scope);
    }

    private sealed class CallTimer : IDisposable
    {
        private readonly string _tool;
        private readonly System.Diagnostics.Stopwatch _watch = System.Diagnostics.Stopwatch.StartNew();
        private readonly IDisposable _scope;
        private bool _disposed;

        public CallTimer(string tool, IDisposable scope)
        {
            _tool = tool;
            _scope = scope;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            LogMcpLine($"{_tool} done in {_watch.ElapsedMilliseconds} ms");
            _scope.Dispose();
        }
    }

    // Kompakte Darstellung der Suchbedingungen fürs Log, z.B. [sm.idShort eq TechnicalData and sme.value ge 80].
    private static string DescribeConditions(McpQueryCondition[]? conditions, string? combine)
    {
        if (conditions == null || conditions.Length == 0)
        {
            return "[]";
        }

        var sep = " " + (string.IsNullOrWhiteSpace(combine) ? "and" : combine.Trim()) + " ";
        var parts = conditions.Select(c =>
        {
            var scope = string.IsNullOrWhiteSpace(c?.Scope) ? "sme" : c!.Scope!.Trim();
            var path = !string.IsNullOrWhiteSpace(c?.IdShortPath) ? "." + c!.IdShortPath!.Trim() : string.Empty;
            var val = string.Equals(c?.Op?.Trim(), "in", StringComparison.OrdinalIgnoreCase) && c?.Values is { Length: > 0 }
                ? "[" + string.Join("|", c.Values) + "]"
                : c?.Value;
            return $"{scope}{path}.{c?.Field} {c?.Op} {val}";
        });
        return "[" + string.Join(sep, parts) + "]";
    }

    private JsonNode? SerializeValueOrFull(IClass obj, string fmt)
    {
        if (fmt == "full")
        {
            return Jsonization.Serialize.ToJsonObject(obj);
        }

        var dto = _mappingService.Map(obj, "value");
        return dto is IValueDTO valueDto ? ValueOnlyJsonSerializer.ToJsonObject(valueDto) : null;
    }

    // Navigiert einen idShortPath im Submodel: mit Punkt exakt, ohne Punkt per konfigurierbarem Ranking.
    private static (string Path, ISubmodelElement Element)? GetProjectionMatch(
        ISubmodel submodel, string path, string[]? priority, string[]? deprioritize)
    {
        if (path.Contains('.', StringComparison.Ordinal))
        {
            var exact = NavigatePath(submodel.SubmodelElements, path.Split('.'), 0);
            return exact is null ? null : (path, exact);
        }

        var matches = new List<(string Path, ISubmodelElement Element)>();
        CollectByIdShort(submodel.SubmodelElements, string.Empty, path, matches);
        if (matches.Count == 0)
        {
            return null;
        }

        var preferredPath = SelectPreferredProjectionPath(
            matches.Select(match => match.Path).ToArray(), priority, deprioritize);
        var preferred = matches.First(match => string.Equals(match.Path, preferredPath, StringComparison.Ordinal));
        return preferred;
    }

    internal static string? SelectPreferredProjectionPath(
        IReadOnlyList<string> paths, string[]? priority = null, string[]? deprioritize = null)
    {
        if (paths.Count == 0)
        {
            return null;
        }

        var priorities = NormalizeRankingList(priority, DefaultProjectionPriority);
        var penalties = NormalizeRankingList(deprioritize, DefaultProjectionDeprioritize);

        return paths
            .Select((path, index) => new
            {
                Path = path,
                Index = index,
                Penalty = PathContainsAnySegment(path, penalties) ? 1 : 0,
                Priority = GetPathPriority(path, priorities),
                Depth = path.Split('.', StringSplitOptions.RemoveEmptyEntries).Length,
            })
            .OrderBy(x => x.Penalty)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Depth)
            .ThenBy(x => x.Index)
            .Select(x => x.Path)
            .FirstOrDefault();
    }

    private static string[] NormalizeRankingList(string[]? configured, string[] defaults)
        => configured is null
            ? defaults
            : configured.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

    private static int GetPathPriority(string path, string[] priorities)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < priorities.Length; i++)
        {
            if (segments.Any(segment => string.Equals(segment, priorities[i], StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return priorities.Length;
    }

    private static bool PathContainsAnySegment(string path, string[] candidates)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return candidates.Any(candidate =>
            segments.Any(segment => string.Equals(segment, candidate, StringComparison.OrdinalIgnoreCase)));
    }

    private static ISubmodelElement? NavigatePath(IReadOnlyList<ISubmodelElement>? elements, string[] segments, int index)
    {
        if (elements == null || index >= segments.Length)
        {
            return null;
        }

        var match = elements.FirstOrDefault(e => string.Equals(e?.IdShort, segments[index], StringComparison.Ordinal));
        if (match == null)
        {
            return null;
        }

        if (index == segments.Length - 1)
        {
            return match;
        }

        IReadOnlyList<ISubmodelElement>? children = match switch
        {
            ISubmodelElementCollection collection => collection.Value,
            ISubmodelElementList list => list.Value,
            IEntity entity => entity.Statements,
            _ => null,
        };

        return NavigatePath(children, segments, index + 1);
    }

    // Projizierter Wert eines Elements: bei Property der Skalar, bei MultiLanguageProperty die bevorzugte Sprache,
    // sonst die kompakte value-Darstellung.
    private JsonNode? GetProjectedValue(ISubmodelElement? element, string lang)
    {
        if (element is null)
        {
            return null;
        }

        if (element is IProperty property)
        {
            return property.Value;
        }

        // MLP auf eine Sprache reduzieren (Default en), damit Tabellenzellen schlank bleiben statt aller Sprachen.
        if (element is IMultiLanguageProperty mlp)
        {
            var langs = mlp.Value;
            if (langs == null || langs.Count == 0)
            {
                return null;
            }

            var pick = langs.FirstOrDefault(l => string.Equals(l?.Language, lang, StringComparison.OrdinalIgnoreCase))
                       ?? langs[0];
            return pick?.Text;
        }

        var node = SerializeValueOrFull(element, "value");

        // ValueOnly wickelt als { idShort: <wert> } ein — für eine Tabellenzelle den inneren Wert auspacken.
        if (node is JsonObject obj && obj.Count == 1)
        {
            return obj.First().Value?.DeepClone();
        }

        return node;
    }

    // Projektions-Zeilen für alle Treffer: nutzt den SQL-Fast-Path, wenn alle select-Einträge
    // explizite volle Pfade sind und keine Security aktiv ist; sonst den bisherigen Objektpfad.
    private async Task<List<JsonObject>> BuildProjectionRows(
        SecurityConfig securityConfig, List<string> identifiers, string[] select, string lang,
        string[]? priority, string[]? deprioritize, bool withPaths, string toolName)
    {
        var fastRows = await TryBuildProjectionRowsFast(securityConfig, identifiers, select, lang, withPaths, toolName);
        if (fastRows != null)
        {
            return fastRows;
        }

        var rows = new List<JsonObject>(identifiers.Count);
        for (var index = 0; index < identifiers.Count; index++)
        {
            var id = identifiers[index];
            var projectionWatch = Stopwatch.StartNew();
            LogMcpLine($"{toolName} projection {index + 1}/{identifiers.Count}: {id}");
            rows.Add(await BuildProjectionRow(securityConfig, id, select, lang, priority, deprioritize, withPaths));
            LogMcpLine(
                $"{toolName} projection {index + 1}/{identifiers.Count} done " +
                $"in {projectionWatch.ElapsedMilliseconds} ms");
        }

        return rows;
    }

    // SQL-Fast-Path: EINE Batch-Projektion (SMSets/SMRefSets/SMESets/ValueSets) für alle Treffer
    // und alle select-Pfade statt vieler ReadSubmodel-/ReadSubmodelElement-Aufrufe pro Treffer.
    // Liefert null, wenn der Fast Path nicht anwendbar ist (Security aktiv, Blattnamen im select,
    // Indexpfade) — dann übernimmt der bisherige Objektpfad.
    private async Task<List<JsonObject>?> TryBuildProjectionRowsFast(
        SecurityConfig securityConfig, List<string> identifiers, string[] select, string lang, bool withPaths, string toolName)
    {
        if (!Program.noSecurity || !TryPlanFastProjection(select, out var plan))
        {
            return null;
        }

        var watch = Stopwatch.StartNew();
        List<DbProjectionRow> projection;
        try
        {
            projection = await _dbRequestHandlerService.QueryProjectSMs(securityConfig, new DbProjectionRequest
            {
                SubmodelIdentifiers = identifiers,
                Paths = plan,
            });
        }
        catch (Exception ex)
        {
            LogMcpLine($"{toolName} fast projection failed ({ex.Message}); falling back to object path");
            return null;
        }

        var sqlMs = watch.ElapsedMilliseconds;

        var rows = new List<JsonObject>(projection.Count);
        var fallbackCells = 0;
        foreach (var projRow in projection)
        {
            var row = new JsonObject { ["id"] = projRow.SubmodelIdentifier };
            JsonObject? selectedPaths = withPaths ? new JsonObject() : null;
            foreach (var path in plan)
            {
                projRow.Cells.TryGetValue(path.RawPath, out var cell);
                JsonNode? value = null;
                string? resolvedPath = null;
                if (cell is { Found: true })
                {
                    if (IsFastProjectableCell(cell))
                    {
                        value = ProjectFastCellValue(cell, lang);
                    }
                    else
                    {
                        // Komplexes Element (SMC, File, Range, ...): nur diese Zelle über den
                        // Objektpfad lesen, damit der Wert exakt dem bisherigen Verhalten entspricht.
                        fallbackCells++;
                        var (element, _) = await ReadProjectedElement(
                            securityConfig, cell.SourceSubmodelIdentifier!, path.ElementIdShortPath,
                            priority: null, deprioritize: null);
                        value = GetProjectedValue(element, lang);
                    }

                    resolvedPath = path.RawPath;
                }

                row[path.RawPath] = value;
                if (selectedPaths is not null)
                {
                    selectedPaths[path.RawPath] = resolvedPath;
                }
            }

            if (selectedPaths is not null)
            {
                row["paths"] = selectedPaths;
            }

            rows.Add(row);
        }

        LogMcpLine(
            $"{toolName} fast projection: {rows.Count} rows x {plan.Count} fields, sql {sqlMs} ms"
            + (fallbackCells > 0 ? $", {fallbackCells} cells via object fallback" : string.Empty));
        return rows;
    }

    // Fast-Path-Erkennung: JEDER select-Eintrag muss explizit adressierbar sein —
    // "A.B.C" im Treffer-Submodel oder "/SubmodelIdShort/idShortPath" in einem Submodel derselben AAS.
    // Blattnamen ohne Submodel-Präfix bleiben beim Objektpfad-Fallback, weil sie Ranking-Suche brauchen.
    // Indexpfade mit "[" bleiben ebenfalls beim Fallback, weil SML-Kinder in SMESets.IdShortPath
    // nicht bracket-adressierbar sind.
    internal static bool TryPlanFastProjection(string[]? select, out List<DbProjectionPath> plan)
    {
        plan = new List<DbProjectionPath>();
        if (select is null || select.Length == 0)
        {
            return false;
        }

        foreach (var rawPath in select)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var path = rawPath.Trim();
            if (path.Contains('[', StringComparison.Ordinal))
            {
                return false;
            }

            if (TryParseCrossSubmodelProjectionPath(path, out var targetSubmodelIdShort, out var elementPath))
            {
                plan.Add(new DbProjectionPath
                {
                    RawPath = path,
                    TargetSubmodelIdShort = targetSubmodelIdShort,
                    ElementIdShortPath = elementPath,
                });
                continue;
            }

            if (path.StartsWith("/", StringComparison.Ordinal) || !path.Contains('.', StringComparison.Ordinal))
            {
                return false;
            }

            plan.Add(new DbProjectionPath { RawPath = path, ElementIdShortPath = path });
        }

        return plan.Count > 0;
    }

    // Nur Property und MultiLanguageProperty lassen sich direkt aus den ValueSets projizieren;
    // alle anderen Typen brauchen die kompakte value-Serialisierung des Objektpfads.
    private static readonly HashSet<string> FastProjectableSmeTypes = new(StringComparer.Ordinal) { "Prop", "MLP" };

    internal static bool IsFastProjectableCell(DbProjectionCell? cell)
        => cell is { Found: true } && FastProjectableSmeTypes.Contains(NormalizeSmeType(cell.SmeType));

    // SMEType kann einen Operation-Prefix tragen (z.B. "In-Prop"); nur der Basistyp zählt.
    internal static string NormalizeSmeType(string? smeType)
    {
        if (string.IsNullOrEmpty(smeType))
        {
            return string.Empty;
        }

        var index = smeType.LastIndexOf('-');
        return index >= 0 ? smeType[(index + 1)..] : smeType;
    }

    // Zellwert aus den ValueSets-Zeilen: Property als Skalar (numerisch bei TValue="D"),
    // MultiLanguageProperty auf die gewünschte Sprache reduziert (sonst erste vorhandene).
    internal static JsonNode? ProjectFastCellValue(DbProjectionCell cell, string lang)
    {
        if (NormalizeSmeType(cell.SmeType) == "MLP")
        {
            var pick = cell.Values.FirstOrDefault(v => string.Equals(v.Annotation, lang, StringComparison.OrdinalIgnoreCase))
                       ?? cell.Values.FirstOrDefault();
            return pick?.SValue;
        }

        var value = cell.Values.FirstOrDefault();
        if (value is null)
        {
            return null;
        }

        if (value.NValue.HasValue)
        {
            return value.NValue.Value;
        }

        return value.SValue;
    }

    // Eine Projektions-Zeile für ein Submodel: liest es und extrahiert die select-Pfade als {id, <path>:<wert>}.
    private async Task<JsonObject> BuildProjectionRow(
        SecurityConfig securityConfig, string id, string[] select, string lang,
        string[]? priority, string[]? deprioritize, bool withPaths)
    {
        var row = new JsonObject { ["id"] = id };
        JsonObject? selectedPaths = withPaths ? new JsonObject() : null;
        ISubmodel? submodel = null;
        var submodelLoadAttempted = false;
        IAssetAdministrationShell? shell = null;
        var shellResolveAttempted = false;
        var crossSubmodelIdCache = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var rawPath in select)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var path = rawPath.Trim();

            if (TryParseCrossSubmodelProjectionPath(path, out var targetSubmodelIdShort, out var targetPath))
            {
                if (!shellResolveAttempted)
                {
                    shellResolveAttempted = true;
                    shell = await ResolveShellForIdentifier(securityConfig, id);
                }

                var targetSubmodelId = shell is null
                    ? null
                    : await ResolveSubmodelIdByIdShort(
                        securityConfig, shell, targetSubmodelIdShort, crossSubmodelIdCache);

                if (targetSubmodelId is null)
                {
                    row[path] = null;
                    if (selectedPaths is not null)
                    {
                        selectedPaths[path] = null;
                    }

                    continue;
                }

                var (element, resolvedPath) = await ReadProjectedElement(
                    securityConfig, targetSubmodelId, targetPath, priority, deprioritize);
                row[path] = GetProjectedValue(element, lang);
                if (selectedPaths is not null)
                {
                    selectedPaths[path] = element is null ? null : "/" + targetSubmodelIdShort + "/" + resolvedPath;
                }

                continue;
            }

            // A full idShortPath can be fetched directly. Loading the complete
            // submodel for every projected cell is prohibitively expensive for
            // very large TechnicalData submodels.
            if (path.Contains('.', StringComparison.Ordinal))
            {
                ISubmodelElement? element = null;
                try
                {
                    element = await _dbRequestHandlerService.ReadSubmodelElementByPath(
                        securityConfig, null, id, path, level: null, extent: null) as ISubmodelElement;
                }
                catch (NotFoundException)
                {
                    element = null;
                }

                row[path] = GetProjectedValue(element, lang);
                if (selectedPaths is not null)
                    selectedPaths[path] = element is null ? null : path;
                continue;
            }

            // A leaf name can be ambiguous at any depth, so retain the ranked
            // whole-submodel fallback only for this case and load it at most once.
            if (!submodelLoadAttempted)
            {
                submodelLoadAttempted = true;
                try
                {
                    submodel = await _dbRequestHandlerService.ReadSubmodelById(
                        securityConfig, null, id, level: null, extent: null) as ISubmodel;
                }
                catch (NotFoundException)
                {
                    submodel = null;
                }
            }

            var match = submodel is null ? null : GetProjectionMatch(submodel, path, priority, deprioritize);
            row[path] = GetProjectedValue(match?.Element, lang);
            if (selectedPaths is not null)
            {
                selectedPaths[path] = match?.Path;
            }
        }

        if (selectedPaths is not null)
        {
            row["paths"] = selectedPaths;
        }

        return row;
    }

    internal static bool TryParseCrossSubmodelProjectionPath(
        string? rawPath, out string submodelIdShort, out string elementPath)
    {
        submodelIdShort = string.Empty;
        elementPath = string.Empty;

        if (string.IsNullOrWhiteSpace(rawPath) || !rawPath.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        var trimmed = rawPath.Trim();
        var separator = trimmed.IndexOf('/', 1);
        if (separator <= 1 || separator == trimmed.Length - 1)
        {
            return false;
        }

        submodelIdShort = trimmed[1..separator].Trim();
        elementPath = trimmed[(separator + 1)..].Trim();
        return submodelIdShort.Length > 0 && elementPath.Length > 0;
    }

    private async Task<string?> ResolveSubmodelIdByIdShort(
        SecurityConfig securityConfig,
        IAssetAdministrationShell shell,
        string submodelIdShort,
        Dictionary<string, string?> cache)
    {
        if (cache.TryGetValue(submodelIdShort, out var cached))
        {
            return cached;
        }

        if (shell.Submodels != null)
        {
            foreach (var smRef in shell.Submodels)
            {
                var smId = smRef?.Keys?.LastOrDefault()?.Value;
                if (string.IsNullOrEmpty(smId))
                {
                    continue;
                }

                try
                {
                    if (await _dbRequestHandlerService.ReadSubmodelById(
                            securityConfig, null, smId, level: null, extent: null) is ISubmodel submodel
                        && string.Equals(submodel.IdShort, submodelIdShort, StringComparison.Ordinal))
                    {
                        cache[submodelIdShort] = smId;
                        return smId;
                    }
                }
                catch (NotFoundException)
                {
                    // Ignore dangling references in the shell.
                }
            }
        }

        cache[submodelIdShort] = null;
        return null;
    }

    private async Task<(ISubmodelElement? Element, string? ResolvedPath)> ReadProjectedElement(
        SecurityConfig securityConfig,
        string submodelId,
        string path,
        string[]? priority,
        string[]? deprioritize)
    {
        if (path.Contains('.', StringComparison.Ordinal))
        {
            try
            {
                return (await _dbRequestHandlerService.ReadSubmodelElementByPath(
                    securityConfig, null, submodelId, path, level: null, extent: null) as ISubmodelElement, path);
            }
            catch (NotFoundException)
            {
                return (null, null);
            }
        }

        try
        {
            if (await _dbRequestHandlerService.ReadSubmodelById(
                    securityConfig, null, submodelId, level: null, extent: null) is ISubmodel submodel)
            {
                var match = GetProjectionMatch(submodel, path, priority, deprioritize);
                return (match?.Element, match?.Path);
            }
        }
        catch (NotFoundException)
        {
            // Handled as empty projection cell.
        }

        return (null, null);
    }

    private async Task<IAssetAdministrationShell?> TryReadShell(SecurityConfig securityConfig, string aasId)
    {
        try
        {
            return await _dbRequestHandlerService.ReadAssetAdministrationShellById(securityConfig, aasId);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    // Ermittelt die AAS-id, zu der ein Submodel gehört (Query: Shells, die ein Submodel mit dieser id haben).
    private async Task<string?> ResolveShellOfSubmodel(SecurityConfig securityConfig, string submodelId)
    {
        var condition = new JsonObject
        {
            ["$eq"] = new JsonArray(
                new JsonObject { ["$field"] = "$sm#id" },
                new JsonObject { ["$strVal"] = submodelId }),
        };
        var expression = "$JSONGRAMMAR " + new JsonObject
        {
            ["Query"] = new JsonObject { ["$condition"] = condition },
        }.ToJsonString();

        var pagination = new PaginationParameters(null, 1);
        var (shells, _) = await TryQuery(securityConfig, pagination, ResultType.AssetAdministrationShell, expression);

        return ExtractIdentifiers(shells).FirstOrDefault();
    }

    private static List<string> ExtractIdentifiers(List<object>? items)
    {
        return (items ?? [])
            .Select(item => item switch
            {
                string id => id,
                IIdentifiable identifiable => identifiable.Id,
                _ => null,
            })
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .ToList();
    }

    // Löst eine id (AAS-Identifier ODER Submodel-Identifier) zur zugehörigen Shell auf.
    private async Task<IAssetAdministrationShell?> ResolveShellForIdentifier(SecurityConfig securityConfig, string id)
    {
        var shell = await TryReadShell(securityConfig, id);
        if (shell is null)
        {
            var aasId = await ResolveShellOfSubmodel(securityConfig, id);
            if (aasId != null)
            {
                shell = await TryReadShell(securityConfig, aasId);
            }
        }

        return shell;
    }

    // Baut das Shell-Objekt: bei value AssetInformation + Submodel-Referenzen, bei full das ganze AAS-JSON.
    private static JsonObject BuildShellObject(IAssetAdministrationShell shell, string fmt)
    {
        var result = new JsonObject { ["idShort"] = shell.IdShort };

        if (fmt == "full")
        {
            result["shell"] = Jsonization.Serialize.ToJsonObject(shell);
            return result;
        }

        var submodelIds = new JsonArray();
        if (shell.Submodels != null)
        {
            foreach (var smRef in shell.Submodels)
            {
                var key = smRef?.Keys?.LastOrDefault();
                if (key != null && !string.IsNullOrEmpty(key.Value))
                {
                    submodelIds.Add(key.Value);
                }
            }
        }

        var ai = shell.AssetInformation;
        var specificAssetIds = new JsonArray();
        if (ai?.SpecificAssetIds != null)
        {
            foreach (var said in ai.SpecificAssetIds)
            {
                specificAssetIds.Add(new JsonObject { ["name"] = said.Name, ["value"] = said.Value });
            }
        }

        result["assetInformation"] = new JsonObject
        {
            ["assetKind"] = ai != null ? ai.AssetKind.ToString() : null,
            ["globalAssetId"] = ai?.GlobalAssetId,
            ["specificAssetIds"] = specificAssetIds,
        };
        result["submodels"] = submodelIds;
        return result;
    }

    // Baut das Produkt-Objekt: AssetInformation + Werte ALLER Submodelle der Shell im gewählten Format.
    // Jeder Submodel-Read ist indexiert (SMSet.Identifier, SMESet.SMId) und auf die Größe DIESES Submodels begrenzt.
    private async Task<JsonObject> BuildProductObject(
        SecurityConfig securityConfig,
        IAssetAdministrationShell shell,
        string fmt,
        bool coreOnly = false,
        int submodelCursor = 0,
        int submodelLimit = MaxProductSubmodels)
    {
        var ai = shell.AssetInformation;
        var specificAssetIds = new JsonArray();
        if (ai?.SpecificAssetIds != null)
        {
            foreach (var said in ai.SpecificAssetIds)
            {
                specificAssetIds.Add(new JsonObject { ["name"] = said.Name, ["value"] = said.Value });
            }
        }

        var assetInformation = new JsonObject
        {
            ["assetKind"] = ai != null ? ai.AssetKind.ToString() : null,
            ["globalAssetId"] = ai?.GlobalAssetId,
            ["specificAssetIds"] = specificAssetIds,
        };

        var submodels = new JsonArray();
        var totalSubmodelRefs = shell.Submodels?.Count ?? 0;
        var hasMoreSubmodels = false;
        string? nextSubmodelCursor = null;
        if (shell.Submodels != null)
        {
            var endExclusive = Math.Min(totalSubmodelRefs, submodelCursor + submodelLimit);
            hasMoreSubmodels = endExclusive < totalSubmodelRefs;
            nextSubmodelCursor = hasMoreSubmodels
                ? endExclusive.ToString(CultureInfo.InvariantCulture)
                : null;

            foreach (var smRef in shell.Submodels.Skip(submodelCursor).Take(submodelLimit))
            {
                var smId = smRef?.Keys?.LastOrDefault()?.Value;
                if (string.IsNullOrEmpty(smId))
                {
                    continue;
                }

                IClass? sm = null;
                try
                {
                    sm = await _dbRequestHandlerService.ReadSubmodelById(securityConfig, null, smId, level: null, extent: null);
                }
                catch (NotFoundException)
                {
                    sm = null;
                }

                // coreOnly (simple-Stufe): sperrige Datei-/Doku-Submodelle (CAD, BoM, HandoverDocumentation) weglassen,
                // damit kleine Modelle die relevanten Daten (Nameplate/TechnicalData/CarbonFootprint) nicht im Rauschen verlieren.
                var smIdShort = (sm as ISubmodel)?.IdShort;
                if (coreOnly && smIdShort != null
                    && NoiseSubmodelKeywords.Any(k => smIdShort.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var entry = new JsonObject { ["id"] = smId };
                if (sm is ISubmodel s)
                {
                    entry["idShort"] = s.IdShort;
                }

                entry["value"] = sm != null ? SerializeValueOrFull(sm, fmt) : null;
                submodels.Add(entry);
            }
        }

        return new JsonObject
        {
            ["aasId"] = shell.Id,
            ["idShort"] = shell.IdShort,
            ["format"] = fmt,
            ["assetInformation"] = assetInformation,
            ["submodels"] = submodels,
            ["submodelCursor"] = submodelCursor,
            ["submodelLimit"] = submodelLimit,
            ["returnedSubmodels"] = submodels.Count,
            ["totalSubmodelRefs"] = totalSubmodelRefs,
            ["hasMoreSubmodels"] = hasMoreSubmodels,
            ["nextSubmodelCursor"] = nextSubmodelCursor,
            ["truncated"] = hasMoreSubmodels,
        };
    }

    // Rekursive Suche nach allen SubmodelElementen mit passendem idShort; baut dabei den vollen idShortPath.
    private static void CollectByIdShort(
        IReadOnlyList<ISubmodelElement>? elements, string prefix, string targetIdShort, List<(string Path, ISubmodelElement Element)> matches)
    {
        if (elements == null)
        {
            return;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            var sme = elements[i];
            if (sme is null)
            {
                continue;
            }

            // Listen-Kinder haben oft keinen idShort -> Index-Notation [i].
            var segment = sme.IdShort ?? "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
            string path;
            if (prefix.Length == 0)
            {
                path = segment;
            }
            else
            {
                path = segment.StartsWith("[", StringComparison.Ordinal) ? prefix + segment : prefix + "." + segment;
            }

            if (string.Equals(sme.IdShort, targetIdShort, StringComparison.Ordinal))
            {
                matches.Add((path, sme));
            }

            switch (sme)
            {
                case ISubmodelElementCollection collection:
                    CollectByIdShort(collection.Value, path, targetIdShort, matches);
                    break;
                case ISubmodelElementList list:
                    CollectByIdShort(list.Value, path, targetIdShort, matches);
                    break;
                case IEntity entity:
                    CollectByIdShort(entity.Statements, path, targetIdShort, matches);
                    break;
                case IAnnotatedRelationshipElement annotated:
                    CollectByIdShort(annotated.Annotations?.Cast<ISubmodelElement>().ToList(), path, targetIdShort, matches);
                    break;
            }
        }
    }

    internal static string BuildCsv(string[] columns, IReadOnlyList<JsonObject> rows, string delimiter)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(delimiter, columns.Select(column => CsvEscape(column, delimiter))));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(delimiter, columns.Select(column =>
                CsvEscape(row.TryGetPropertyValue(column, out var value) ? value : null, delimiter))));
        }

        return sb.ToString();
    }

    internal static string CsvEscape(JsonNode? value, string delimiter)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var s))
                return CsvEscape(s, delimiter);
            if (jsonValue.TryGetValue<int>(out var i))
                return i.ToString(CultureInfo.InvariantCulture);
            if (jsonValue.TryGetValue<long>(out var l))
                return l.ToString(CultureInfo.InvariantCulture);
            if (jsonValue.TryGetValue<double>(out var d))
                return d.ToString(CultureInfo.InvariantCulture);
            if (jsonValue.TryGetValue<decimal>(out var m))
                return m.ToString(CultureInfo.InvariantCulture);
            if (jsonValue.TryGetValue<bool>(out var b))
                return b ? "true" : "false";
        }

        return CsvEscape(value.ToJsonString(), delimiter);
    }

    internal static string CsvEscape(string? value, string delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var mustQuote = value.Contains('"', StringComparison.Ordinal)
                        || value.Contains('\r', StringComparison.Ordinal)
                        || value.Contains('\n', StringComparison.Ordinal)
                        || value.Contains(delimiter, StringComparison.Ordinal);
        if (!mustQuote)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string NormalizeCsvDelimiter(string? delimiter)
    {
        if (string.IsNullOrEmpty(delimiter))
        {
            return ";";
        }

        return delimiter is "," or ";" or "\t" ? delimiter : ";";
    }

    private static ResultType ParseTarget(string target) => target?.Trim().ToLowerInvariant() switch
    {
        "submodels" or "submodel" or "sm" => ResultType.Submodel,
        "shells" or "shell" or "aas" => ResultType.AssetAdministrationShell,
        _ => throw new ArgumentException($"Unbekanntes target \"{target}\". Erlaubt: \"submodels\" oder \"shells\"."),
    };

    internal static int NormalizeLimit(int? limit)
    {
        if (limit is null || limit <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(limit.Value, MaxPageSize);
    }

    internal static int NormalizeProductSubmodelLimit(int? limit)
    {
        if (limit is null || limit <= 0)
        {
            return MaxProductSubmodels;
        }

        return Math.Min(limit.Value, MaxProductSubmodels);
    }

    internal static int NormalizeCursor(string? cursor)
        => string.IsNullOrWhiteSpace(cursor) || !int.TryParse(cursor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0
            ? 0
            : parsed;

    /// <summary>
    /// Baut aus den Slots eine AASQL-JSON-Query (Modus-Präfix "$JSONGRAMMAR").
    /// Eine Bedingung -> direkte Vergleichsoperation; mehrere -> $and/$or.
    /// </summary>
    private static string BuildExpression(McpQueryCondition[] conditions, string combine)
    {
        if (conditions is null || conditions.Length == 0)
        {
            throw new ArgumentException("Es muss mindestens eine Bedingung (conditions) angegeben werden.");
        }

        JsonNode condition;
        if (conditions.Length == 1)
        {
            condition = BuildComparison(conditions[0]);
        }
        else
        {
            var combineKey = combine?.Trim().ToLowerInvariant() switch
            {
                "or" => "$or",
                "and" or null or "" => "$and",
                _ => throw new ArgumentException($"Unbekanntes combine \"{combine}\". Erlaubt: \"and\" oder \"or\"."),
            };

            var array = new JsonArray();
            foreach (var c in conditions)
            {
                array.Add(BuildComparison(c));
            }

            condition = new JsonObject { [combineKey] = array };
        }

        var query = new JsonObject
        {
            ["Query"] = new JsonObject
            {
                // aas_query needs identifiers first. Without this, Query.GetQueryData
                // materializes every complete submodel before MCP projects the selected
                // fields, causing the same large submodels to be loaded twice.
                ["$select"] = "id",
                ["$condition"] = condition,
            },
        };

        return "$JSONGRAMMAR " + query.ToJsonString();
    }

    private static JsonObject BuildComparison(McpQueryCondition c)
    {
        if (c is null)
        {
            throw new ArgumentException("Leere Bedingung.");
        }

        var scope = (c.Scope ?? "sme").Trim().ToLowerInvariant();
        if (!AllowedScopes.Contains(scope))
        {
            throw new ArgumentException($"Unbekannter scope \"{c.Scope}\". Erlaubt: \"aas\", \"sm\", \"sme\".");
        }

        var op = (c.Op ?? string.Empty).Trim().ToLowerInvariant();

        // op="in": ODER-Verknüpfung von eq-Vergleichen über die Werteliste — nutzt die komplette Feld-/Pfad-/Numerik-Logik wieder.
        if (op == "in")
        {
            var values = c.Values ?? System.Array.Empty<string>();
            if (values.Length == 0)
            {
                throw new ArgumentException("op=\"in\" benötigt eine nicht-leere Werteliste (values).");
            }

            var orArray = new JsonArray();
            foreach (var v in values)
            {
                orArray.Add(BuildComparison(new McpQueryCondition
                {
                    Scope = c.Scope,
                    Field = c.Field,
                    IdShortPath = c.IdShortPath,
                    Op = "eq",
                    Value = v,
                }));
            }

            return new JsonObject { ["$or"] = orArray };
        }

        if (!OpMap.TryGetValue(op, out var opKey))
        {
            throw new ArgumentException($"Unbekannter Operator \"{c.Op}\". Erlaubt: {string.Join(", ", OpMap.Keys)}, in.");
        }

        var validationField = string.IsNullOrWhiteSpace(c.Field) && scope == "sme" ? "value" : (c.Field ?? string.Empty).Trim();
        ValidateField(scope, validationField);
        var validationPath = scope == "sme" && !string.IsNullOrWhiteSpace(c.IdShortPath)
            ? c.IdShortPath!.Trim()
            : null;
        ValidateWildcardOperatorForField(op, scope, validationField, validationPath);

        if (op == "regex")
        {
            throw new ArgumentException(
                "Der Operator regex wird vom SQL-Backend derzeit nicht unterstützt. " +
                "Verwende eq, starts-with, ends-with oder contains.");
        }

        if (op == "contains" && (c.Value ?? string.Empty).Length < 3)
        {
            throw new ArgumentException(
                $"contains mit \"{c.Value}\" ist zu kurz: Der SQLite-Trigrammindex benötigt mindestens 3 Zeichen. " +
                "Verwende eq, starts-with oder einen spezifischeren Teilstring mit mindestens 3 Zeichen " +
                "(z.B. 24V, 24D oder /24).");
        }

        // Für scope=sme ist "value" der sinnvolle Default, wenn field leer ist — LLMs lassen field
        // bei idShortPath-/Wert-Suchen oft weg (z.B. {scope:sme, idShortPath:"flowMax", op:eq, value:"80"}).
        var field = string.IsNullOrWhiteSpace(c.Field) && scope == "sme" ? "value" : (c.Field ?? string.Empty).Trim();
        ValidateField(scope, field);

        var rawPath = scope == "sme" && !string.IsNullOrWhiteSpace(c.IdShortPath)
            ? c.IdShortPath!.Trim()
            : null;

        // idShortPath nur als positionalen Pfad behandeln, wenn er einen "." enthält
        // (z.B. "TechnicalProperties.flowMax"). Ein einzelner Name ohne "." ist ein idShort-Blattname
        // in beliebiger Tiefe. WICHTIG (BUG A): Das muss am SELBEN Element korreliert sein. Früher wurde
        // $and[$sme#idShort==X, $sme#<field> op Y] gebaut — das matcht aber "irgendein idShort=X UND
        // irgendein Wert op Y" (verschiedene Elemente!) -> Falsch-Treffer. Korrekt ist der korrelierte
        // Pfad-Mechanismus der Engine: top-level ($sme.<leaf>#field) ODER verschachtelt ($sme.%.<leaf>#field,
        // %-Wildcard = beliebiger Pfad-Präfix). Beides ist je ein korreliertes Pfad-Subquery (idShortPath + Wert in einer Zeile).
        if (rawPath != null && !rawPath.Contains('.', StringComparison.Ordinal) && field != "idShort")
        {
            var allowNumeric = NumericCapableOps.Contains(op);
            return BuildFieldComparison(
                opKey,
                "$" + scope + ".%." + rawPath + "#" + field,
                c.Value,
                allowNumeric);
        }

        var pathSegment = rawPath != null ? "." + rawPath : string.Empty;
        var fieldRef = "$" + scope + pathSegment + "#" + field;
        return BuildFieldComparison(opKey, fieldRef, c.Value, allowNumeric: NumericCapableOps.Contains(op));
    }

    private static JsonObject BuildFieldComparison(string opKey, string fieldRef, string? value, bool allowNumeric)
    {
        JsonObject StrRhs() => new JsonObject { ["$strVal"] = value ?? string.Empty };
        JsonObject Leaf(string op, JsonObject rhs) => new JsonObject
        {
            [op] = new JsonArray(new JsonObject { ["$field"] = fieldRef }, rhs),
        };

        // Nicht numerikfähig oder keine Zahl -> reiner String-Vergleich.
        // NumberStyles.Float (NICHT .Any!): keine Tausendertrennung, sonst würde "1,32" als 132 fehlinterpretiert.
        if (!allowNumeric || !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
        {
            return Leaf(opKey, StrRhs());
        }

        JsonObject NumRhs() => new JsonObject { ["$numVal"] = num };

        // eq/ne: gegen String ODER Zahl prüfen — der valueType (xs:string vs xs:double) ist bei der Query unbekannt.
        // So findet eq "80" einen Double-Wert 80 UND eq "2908936" eine ziffrige String-Artikelnummer.
        if (opKey == "$eq")
        {
            return new JsonObject { ["$or"] = new JsonArray(Leaf("$eq", StrRhs()), Leaf("$eq", NumRhs())) };
        }

        if (opKey == "$ne")
        {
            return new JsonObject { ["$and"] = new JsonArray(Leaf("$ne", StrRhs()), Leaf("$ne", NumRhs())) };
        }

        // gt/ge/lt/le: numerischer Vergleich.
        return Leaf(opKey, NumRhs());
    }

    internal static void ValidateWildcardOperatorForField(string op, string scope, string field, string? idShortPath = null)
    {
        if (!WildcardOps.Contains(op))
        {
            return;
        }

        var normalizedField = NormalizeWildcardFieldName(scope, field, idShortPath);
        if (WildcardFields.Contains(normalizedField))
        {
            return;
        }

        throw new ArgumentException(FormatWildcardFieldError(normalizedField));
    }

    private static string NormalizeWildcardFieldName(string scope, string field, string? idShortPath)
    {
        var f = (field ?? string.Empty).Trim();
        if (scope == "sme" && !string.IsNullOrWhiteSpace(idShortPath) && f == "idShort")
        {
            return "idShortPath";
        }

        return f is "id" or "identifier" ? "identifier" : f;
    }

    private static string DisplayWildcardFieldName(string field)
        => field == "identifier" ? "id" : field;

    private static string FormatWildcardFieldError(string field)
    {
        var display = DisplayWildcardFieldName(field);
        return $"Wildcard search is not supported for field '{display}'.{System.Environment.NewLine}" +
               $"Allowed wildcard fields are:{System.Environment.NewLine}" +
               $"- value{System.Environment.NewLine}" +
               $"- idShort{System.Environment.NewLine}" +
               $"- idShortPath{System.Environment.NewLine}" +
               $"Use 'eq' or 'in' for {display}.";
    }

    private static void ValidateField(string scope, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("field darf nicht leer sein.");
        }

        var f = field.Trim();
        bool ok = scope switch
        {
            "sme" => f is "idShort" or "idShortPath" or "value" or "valueType" or "language" || f.StartsWith("semanticId", StringComparison.Ordinal),
            "sm" => f is "idShort" or "id" || f.StartsWith("semanticId", StringComparison.Ordinal),
            "aas" => f is "idShort" or "id"
                     || f.StartsWith("assetInformation", StringComparison.Ordinal)
                     || f.StartsWith("submodels", StringComparison.Ordinal),
            _ => false,
        };

        if (!ok)
        {
            throw new ArgumentException($"Feld \"{field}\" ist für scope \"{scope}\" nicht erlaubt.");
        }
    }
}
