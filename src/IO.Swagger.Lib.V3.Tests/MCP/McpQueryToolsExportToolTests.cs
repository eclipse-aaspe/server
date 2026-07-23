namespace IO.Swagger.Lib.V3.Tests.MCP;

using AasCore.Aas3_1;
using Contracts;
using Contracts.DbRequests;
using Contracts.Pagination;
using Contracts.QueryResult;
using Contracts.Security;
using IO.Swagger.Lib.V3.MCP;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using Moq;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// End-to-End-Tests der Export-Tools gegen einen gemockten <see cref="IDbRequestHandlerService"/>:
/// Fast Path (SQL-Batchprojektion) für volle Pfade, Objektpfad-Fallback für Blattnamen
/// und die Datei-Metadaten der Antwort (downloadUrl/resourceUri/contentBase64).
/// Die Tests laufen sequenziell in einer Klasse, weil sie Program.noSecurity umschalten.
/// </summary>
public class McpQueryToolsExportToolTests
{
    private static JsonNode ToJson(object result)
        => JsonNode.Parse(JsonSerializer.Serialize(result))!;

    private static McpQueryTools CreateTools(
        out Mock<IDbRequestHandlerService> dbMock, List<string> queryIds, List<DbProjectionRow>? projectionRows = null)
    {
        dbMock = new Mock<IDbRequestHandlerService>(MockBehavior.Loose);
        dbMock
            .Setup(m => m.QueryGetSMs(It.IsAny<ISecurityConfig>(), It.IsAny<IPaginationParameters>(), It.IsAny<ResultType>(), It.IsAny<string>()))
            .ReturnsAsync(queryIds.Cast<object>().ToList());
        if (projectionRows != null)
        {
            dbMock
                .Setup(m => m.QueryProjectSMs(It.IsAny<ISecurityConfig>(), It.IsAny<DbProjectionRequest>()))
                .ReturnsAsync(projectionRows);
        }

        return new McpQueryTools(dbMock.Object, new Mock<IMappingService>().Object);
    }

    private static McpQueryCondition[] AnyCondition()
        => new[] { new McpQueryCondition { Scope = "sm", Field = "idShort", Op = "eq", Value = "TechnicalData" } };

    [Fact]
    public async Task ExportXlsx_UsesFastPathAndReturnsFileMetadata()
    {
        var previousNoSecurity = AasxServer.Program.noSecurity;
        AasxServer.Program.noSecurity = true;
        try
        {
            const string path = "GeneralInformation.ManufacturerArticleNumber";
            var projection = new List<DbProjectionRow>
            {
                new()
                {
                    SubmodelIdentifier = "sm1",
                    Cells =
                    {
                        [path] = new DbProjectionCell
                        {
                            Found = true,
                            SmeType = "Prop",
                            TValue = "S",
                            Values = { new DbProjectionValue { SValue = "4711" } },
                            SourceSubmodelIdentifier = "sm1",
                        },
                    },
                },
            };
            var tools = CreateTools(out var dbMock, new List<string> { "sm1" }, projection);

            var result = ToJson(await tools.AasQueryExportXlsx("submodels", AnyCondition(), new[] { path }));

            Assert.Equal(1, result["count"]!.GetValue<int>());
            Assert.EndsWith(".xlsx", result["fileName"]!.GetValue<string>());
            Assert.Equal(XlsxBuilder.MimeType, result["mimeType"]!.GetValue<string>());
            Assert.Contains("/mcp-exports/", result["downloadUrl"]!.GetValue<string>());
            Assert.StartsWith("aas-export://", result["resourceUri"]!.GetValue<string>());
            Assert.False(result["hasMore"]!.GetValue<bool>());

            // Kleine Datei -> contentBase64 inline; muss der gespeicherten Datei entsprechen.
            var contentBase64 = result["contentBase64"]!.GetValue<string>();
            var token = result["resourceUri"]!.GetValue<string>()["aas-export://".Length..];
            Assert.True(McpExportFileStore.TryGet(token, out var stored, out _, out _));
            Assert.Equal(Convert.ToBase64String(stored), contentBase64);

            // Fast Path: Batchprojektion statt Submodel-Reads.
            dbMock.Verify(m => m.QueryProjectSMs(It.IsAny<ISecurityConfig>(), It.IsAny<DbProjectionRequest>()), Times.Once);
            dbMock.Verify(m => m.ReadSubmodelById(It.IsAny<ISecurityConfig>(), It.IsAny<string>(), It.IsAny<string>(), null, null), Times.Never);
            dbMock.Verify(m => m.ReadSubmodelElementByPath(It.IsAny<ISecurityConfig>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null), Times.Never);
        }
        finally
        {
            AasxServer.Program.noSecurity = previousNoSecurity;
        }
    }

    [Fact]
    public async Task ExportCsv_FallsBackToObjectPathForLeafNames()
    {
        var previousNoSecurity = AasxServer.Program.noSecurity;
        AasxServer.Program.noSecurity = true;
        try
        {
            var submodel = new Submodel(
                "sm1",
                submodelElements: new List<ISubmodelElement>
                {
                    new Property(DataTypeDefXsd.String, idShort: "flowMax", value: "80"),
                });

            var tools = CreateTools(out var dbMock, new List<string> { "sm1" });
            dbMock
                .Setup(m => m.ReadSubmodelById(It.IsAny<ISecurityConfig>(), null, "sm1", null, null))
                .ReturnsAsync(submodel);

            // Blattname ohne Punkt -> kein Fast Path, Ranking-Suche über das geladene Submodel.
            var result = ToJson(await tools.AasQueryExportCsv("submodels", AnyCondition(), new[] { "flowMax" }));

            Assert.Equal(1, result["count"]!.GetValue<int>());
            Assert.Contains("flowMax", result["columns"]!.AsArray().Select(c => c!.GetValue<string>()));
            Assert.Contains("80", result["content"]!.GetValue<string>());
            Assert.Contains("/mcp-exports/", result["downloadUrl"]!.GetValue<string>());

            dbMock.Verify(m => m.QueryProjectSMs(It.IsAny<ISecurityConfig>(), It.IsAny<DbProjectionRequest>()), Times.Never);
            dbMock.Verify(m => m.ReadSubmodelById(It.IsAny<ISecurityConfig>(), null, "sm1", null, null), Times.Once);
        }
        finally
        {
            AasxServer.Program.noSecurity = previousNoSecurity;
        }
    }

    [Fact]
    public async Task ExportXlsx_WithSecurityEnabledUsesObjectPath()
    {
        var previousNoSecurity = AasxServer.Program.noSecurity;
        AasxServer.Program.noSecurity = false;
        try
        {
            const string path = "GeneralInformation.ManufacturerArticleNumber";
            var tools = CreateTools(out var dbMock, new List<string> { "sm1" });
            dbMock
                .Setup(m => m.ReadSubmodelElementByPath(It.IsAny<ISecurityConfig>(), null, "sm1", path, null, null))
                .ReturnsAsync(new Property(DataTypeDefXsd.String, idShort: "ManufacturerArticleNumber", value: "4711"));

            var result = ToJson(await tools.AasQueryExportXlsx("submodels", AnyCondition(), new[] { path }));

            Assert.Equal(1, result["count"]!.GetValue<int>());

            // Mit aktiver Security darf die SQL-Batchprojektion nicht verwendet werden.
            dbMock.Verify(m => m.QueryProjectSMs(It.IsAny<ISecurityConfig>(), It.IsAny<DbProjectionRequest>()), Times.Never);
            dbMock.Verify(m => m.ReadSubmodelElementByPath(It.IsAny<ISecurityConfig>(), null, "sm1", path, null, null), Times.Once);
        }
        finally
        {
            AasxServer.Program.noSecurity = previousNoSecurity;
        }
    }

    [Fact]
    public async Task ExportCsv_RequiresSelect()
    {
        var tools = CreateTools(out _, new List<string>());

        var result = ToJson(await tools.AasQueryExportCsv("submodels", AnyCondition(), Array.Empty<string>()));

        Assert.NotNull(result["error"]);
    }
}
