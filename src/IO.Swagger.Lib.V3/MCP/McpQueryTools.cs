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
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AasCore.Aas3_1;
using AasxServer;
using Contracts;
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

    [Description("Feldname je nach scope. aas: idShort|id|assetInformation.assetKind|assetInformation.assetType|assetInformation.globalAssetId|assetInformation.specificAssetIds[].name|... ; sm: semanticId|idShort|id ; sme: semanticId|idShort|value|valueType|language.")]
    public string Field { get; set; } = "";

    [Description("Optionaler idShortPath innerhalb des Submodels, nur für scope=\"sme\". Enthält der Wert einen Punkt, wird er als verschachtelter Pfad behandelt (z.B. \"TechnicalProperties.flowMax\" oder \"Documents[].DocumentVersion.Title\"). Ein einzelner Name ohne Punkt (z.B. \"flowMax\") wird als idShort des Elements gesucht und unabhängig von der Verschachtelungstiefe gefunden.")]
    public string? IdShortPath { get; set; }

    [Description("Vergleichsoperator: eq|ne|gt|ge|lt|le|contains|starts-with|ends-with|regex.")]
    public string Op { get; set; } = "eq";

    [Description("Vergleichswert als String (Zahlen als String angeben, z.B. \"100\"). Bei eq/ne/gt/ge/lt/le werden numerische Werte automatisch numerisch verglichen — \"eq 80\" findet also auch einen Double-Wert 80.")]
    public string Value { get; set; } = "";
}

/// <summary>
/// MCP-Tools für die AASPE-Query-Pipeline. Dünner Adapter über <see cref="IDbRequestHandlerService"/>
/// (analog zum GraphQL-Adapter). Das Modell füllt strukturierte Slots, der Server baut die AASQL
/// (JSON-Grammatik) und gibt nur die gefundenen Identifier zurück — keine Volldaten.
/// </summary>
[McpServerToolType]
public sealed class McpQueryTools
{
    private const int MaxPageSize = 500;

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

    private readonly IDbRequestHandlerService _dbRequestHandlerService;
    private readonly IMappingService _mappingService;

    public McpQueryTools(IDbRequestHandlerService dbRequestHandlerService, IMappingService mappingService)
    {
        _dbRequestHandlerService = dbRequestHandlerService;
        _mappingService = mappingService;
    }

    [McpServerTool(Name = "aas_query")]
    [Description(
        "Sucht im AAS-Repository und liefert nur die gefundenen Identifier zurück (keine Volldaten). " +
        "Mehrere Bedingungen werden mit combine (\"and\"/\"or\") verknüpft. " +
        "target=\"submodels\" gibt Submodel-Identifier zurück, target=\"shells\" gibt AAS-Identifier zurück. " +
        "Beispiele: " +
        "(1) eine Bedingung: target=\"submodels\", conditions=[{scope:\"sm\",field:\"idShort\",op:\"eq\",value:\"TechnicalData\"}]. " +
        "(2) UND: combine=\"and\", conditions=[{scope:\"sm\",field:\"idShort\",op:\"eq\",value:\"TechnicalData\"},{scope:\"sme\",field:\"value\",op:\"lt\",value:\"100\"}]. " +
        "(3) ODER: combine=\"or\", conditions=[{scope:\"sme\",field:\"value\",op:\"eq\",value:\"A\"},{scope:\"sme\",field:\"value\",op:\"eq\",value:\"B\"}]. " +
        "Bei großen Treffermengen vorher aas_count aufrufen.")]
    public async Task<object> AasQuery(
        [Description("Zielobjekt: \"submodels\" oder \"shells\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\". Bei einer Bedingung ohne Bedeutung.")] string combine = "and",
        [Description("Maximale Trefferzahl (Default und Maximum 500).")] int? limit = null,
        [Description("Cursor (Offset) zum Weiterblättern; aus nextCursor einer vorigen Antwort.")] string? cursor = null)
    {
        LogCall($"aas_query {target} {DescribeConditions(conditions, combine)} limit={(limit?.ToString(CultureInfo.InvariantCulture) ?? "-")} cursor={cursor ?? "-"}");

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
        var pagination = new PaginationParameters(cursor, NormalizeLimit(limit));

        var list = await _dbRequestHandlerService.QueryGetSMs(securityConfig, pagination, resultType, expression);

        var identifiers = (list ?? new List<object>())
            .OfType<IIdentifiable>()
            .Select(x => x.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        string? nextCursor = identifiers.Count >= pagination.Limit
            ? (pagination.Cursor + identifiers.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        return new
        {
            target,
            count = identifiers.Count,
            identifiers,
            nextCursor,
        };
    }

    [McpServerTool(Name = "aas_count")]
    [Description(
        "Zählt die Treffer einer Suche, bevor man sie abruft. Gleiche Bedingungs-/combine-Logik wie aas_query. " +
        "Hinweis: in dieser Version nur für target=\"submodels\" verfügbar; für target=\"shells\" bitte aas_query mit limit verwenden. " +
        "Die Zählung ist auf 500 gedeckelt; ist das Feld \"capped\" im Ergebnis true, kann die echte Gesamtzahl höher sein.")]
    public async Task<object> AasCount(
        [Description("Zielobjekt: aktuell nur \"submodels\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\".")] string combine = "and")
    {
        LogCall($"aas_count {target} {DescribeConditions(conditions, combine)}");

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
        var pagination = new PaginationParameters(null, MaxPageSize);

        // In dieser Version gibt es keinen dedizierten COUNT-Pfad im Query-Engine
        // (DbRequestOp.QueryCountSMs ist nicht implementiert). Wir zählen daher über
        // den funktionierenden QueryGetSMs-Pfad und liefern die Anzahl der Identifier.
        var list = await _dbRequestHandlerService.QueryGetSMs(securityConfig, pagination, ResultType.Submodel, expression);

        var totalCount = (list ?? new List<object>())
            .OfType<IIdentifiable>()
            .Select(x => x.Id)
            .Count(id => !string.IsNullOrEmpty(id));

        // capped=true bedeutet: Das Limit wurde erreicht, die echte Gesamtzahl
        // kann höher sein (kein exakter COUNT in dieser Version).
        var capped = totalCount >= MaxPageSize;

        return new { target, totalCount, capped };
    }

    [McpServerTool(Name = "aas_get_submodel")]
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
        LogCall($"aas_get_submodel id={identifier} format={format}");
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

    [McpServerTool(Name = "aas_get_element")]
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
        LogCall($"aas_get_element sm={submodelIdentifier} path={idShortPath} format={format}");
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

    [McpServerTool(Name = "aas_get_shell")]
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
        LogCall($"aas_get_shell id={identifier} format={format}");
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

        if (fmt == "full")
        {
            return new { identifier, format = fmt, shell = Jsonization.Serialize.ToJsonObject(shell) };
        }

        // Kompakt: AssetInformation + Submodel-Identifier (aus den Referenzen).
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

        var assetInformation = new JsonObject
        {
            ["assetKind"] = ai != null ? ai.AssetKind.ToString() : null,
            ["globalAssetId"] = ai?.GlobalAssetId,
            ["specificAssetIds"] = specificAssetIds,
        };

        return new
        {
            identifier,
            idShort = shell.IdShort,
            format = fmt,
            assetInformation,
            submodels = submodelIds,
        };
    }

    // --------------- Helfer ---------------

    // Kompaktes Console-Log jedes MCP-Aufrufs (eine Zeile), passend zum übrigen Server-Log.
    private static void LogCall(string line)
    {
        Console.WriteLine("[MCP] " + line);
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
            return $"{scope}{path}.{c?.Field} {c?.Op} {c?.Value}";
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

    private static ResultType ParseTarget(string target) => target?.Trim().ToLowerInvariant() switch
    {
        "submodels" or "submodel" or "sm" => ResultType.Submodel,
        "shells" or "shell" or "aas" => ResultType.AssetAdministrationShell,
        _ => throw new ArgumentException($"Unbekanntes target \"{target}\". Erlaubt: \"submodels\" oder \"shells\"."),
    };

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null || limit <= 0)
        {
            return MaxPageSize;
        }

        return Math.Min(limit.Value, MaxPageSize);
    }

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
        if (!OpMap.TryGetValue(op, out var opKey))
        {
            throw new ArgumentException($"Unbekannter Operator \"{c.Op}\". Erlaubt: {string.Join(", ", OpMap.Keys)}.");
        }

        // Für scope=sme ist "value" der sinnvolle Default, wenn field leer ist — LLMs lassen field
        // bei idShortPath-/Wert-Suchen oft weg (z.B. {scope:sme, idShortPath:"flowMax", op:eq, value:"80"}).
        var field = string.IsNullOrWhiteSpace(c.Field) && scope == "sme" ? "value" : (c.Field ?? string.Empty).Trim();
        ValidateField(scope, field);

        var rawPath = scope == "sme" && !string.IsNullOrWhiteSpace(c.IdShortPath)
            ? c.IdShortPath!.Trim()
            : null;

        // idShortPath nur als positionalen Pfad behandeln, wenn er einen "." enthält
        // (z.B. "TechnicalProperties.flowMax"). Ein einzelner Name ohne "." ist faktisch ein
        // idShort-Blattname, der unabhängig von der Verschachtelungstiefe gefunden wird. In dem Fall
        // wird die Bedingung zu: $sme#idShort == <name> UND $sme#<field> <op> <wert> (gleiches Element).
        if (rawPath != null && !rawPath.Contains('.', StringComparison.Ordinal) && field != "idShort")
        {
            var idShortCmp = BuildFieldComparison("$eq", "$sme#idShort", rawPath, allowNumeric: false);
            var valueCmp = BuildFieldComparison(opKey, "$sme#" + field, c.Value, allowNumeric: NumericCapableOps.Contains(op));
            return new JsonObject { ["$and"] = new JsonArray(idShortCmp, valueCmp) };
        }

        var pathSegment = rawPath != null ? "." + rawPath : string.Empty;
        var fieldRef = "$" + scope + pathSegment + "#" + field;
        return BuildFieldComparison(opKey, fieldRef, c.Value, allowNumeric: NumericCapableOps.Contains(op));
    }

    private static JsonObject BuildFieldComparison(string opKey, string fieldRef, string? value, bool allowNumeric)
    {
        // Werttyp: bei numerikfähigem Operator und parsebarer Zahl -> $numVal, sonst $strVal.
        // NumberStyles.Float (NICHT .Any!): keine Tausendertrennung, sonst würde "1,32" als 132
        // fehlinterpretiert. Ein "1,32" ist damit keine Zahl -> $strVal (deutsches Komma bleibt String).
        JsonObject rhs;
        if (allowNumeric && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
        {
            rhs = new JsonObject { ["$numVal"] = num };
        }
        else
        {
            rhs = new JsonObject { ["$strVal"] = value ?? string.Empty };
        }

        return new JsonObject
        {
            [opKey] = new JsonArray(
                new JsonObject { ["$field"] = fieldRef },
                rhs),
        };
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
            "sme" => f is "idShort" or "value" or "valueType" or "language" || f.StartsWith("semanticId", StringComparison.Ordinal),
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
