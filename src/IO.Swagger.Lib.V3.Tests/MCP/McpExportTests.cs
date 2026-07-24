namespace IO.Swagger.Lib.V3.Tests.MCP;

using IO.Swagger.Lib.V3.MCP;
using ModelContextProtocol.Protocol;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

public class McpExportTests
{
    [Fact]
    public void XlsxBuilder_ProducesWorkbookWithHeaderStringsAndNumbers()
    {
        var rows = new[]
        {
            new JsonObject
            {
                ["id"] = "submodel-1",
                ["GeneralInformation.ManufacturerArticleNumber"] = "4711 <&> \"x\"",
                ["TechnicalProperties.flowMax"] = 80.5,
            },
        };

        var bytes = XlsxBuilder.Build(new[] { "id", "GeneralInformation.ManufacturerArticleNumber", "TechnicalProperties.flowMax" }, rows);

        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));

        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheetEntry);
        using var reader = new StreamReader(sheetEntry!.Open(), Encoding.UTF8);
        var sheet = reader.ReadToEnd();

        Assert.Contains("GeneralInformation.ManufacturerArticleNumber", sheet);
        Assert.Contains("4711 &lt;&amp;&gt; &quot;x&quot;", sheet); // XML-escaped
        Assert.Contains("<v>80.5</v>", sheet); // numerische Zelle
        Assert.Contains("t=\"inlineStr\"", sheet);
    }

    [Fact]
    public void XlsxBuilder_CellRefUsesExcelColumnLetters()
    {
        Assert.Equal("A1", XlsxBuilder.CellRef(0, 1));
        Assert.Equal("Z2", XlsxBuilder.CellRef(25, 2));
        Assert.Equal("AA3", XlsxBuilder.CellRef(26, 3));
    }

    [Fact]
    public void McpExportFileStore_RoundtripsContentAndRejectsUnknownToken()
    {
        var content = Encoding.UTF8.GetBytes("id;value\r\nsm1;42\r\n");
        var token = McpExportFileStore.Add(content, "export.csv", "text/csv");

        Assert.True(McpExportFileStore.TryGet(token, out var stored, out var fileName, out var mimeType));
        Assert.Equal(content, stored);
        Assert.Equal("export.csv", fileName);
        Assert.Equal("text/csv", mimeType);

        Assert.False(McpExportFileStore.TryGet("does-not-exist", out _, out _, out _));
        Assert.False(McpExportFileStore.TryGet(null, out _, out _, out _));
    }

    [Fact]
    public void McpExportResources_GetExportFile_ReturnsBase64EncodedBlob()
    {
        // Binärinhalt mit ungültigen UTF-8-Sequenzen: würde der Blob nicht base64-kodiert,
        // käme er auf dem Draht als zerstörter String an (U+FFFD-Ersatzzeichen).
        var content = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xFF, 0x00, 0x10 };
        var token = McpExportFileStore.Add(content, "x.xlsx", XlsxBuilder.MimeType);

        var result = Assert.IsType<BlobResourceContents>(McpExportResources.GetExportFile(token));

        Assert.Equal("aas-export://" + token, result.Uri);
        Assert.Equal(XlsxBuilder.MimeType, result.MimeType);
        Assert.Equal(content, result.DecodedData.ToArray());
        Assert.Equal(Convert.ToBase64String(content), Encoding.UTF8.GetString(result.Blob.ToArray()));
    }

    [Fact]
    public void NormalizeExportFileName_AppendsExtensionAndStripsPathSeparators()
    {
        Assert.Equal("aas_query_export.xlsx", McpQueryTools.NormalizeExportFileName(null, ".xlsx", "aas_query_export.xlsx"));
        Assert.Equal("pumpen.xlsx", McpQueryTools.NormalizeExportFileName("pumpen", ".xlsx", "aas_query_export.xlsx"));
        Assert.Equal("Pumpen.XLSX", McpQueryTools.NormalizeExportFileName("Pumpen.XLSX", ".xlsx", "aas_query_export.xlsx"));
        Assert.Equal("a_b.csv", McpQueryTools.NormalizeExportFileName("a/b.csv", ".csv", "aas_query_export.csv"));
        Assert.Equal("__secret.csv", McpQueryTools.NormalizeExportFileName("..\\secret.csv", ".csv", "aas_query_export.csv"));
    }
}
