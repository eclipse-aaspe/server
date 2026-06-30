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

    [Description("Vergleichsoperator: eq|ne|gt|ge|lt|le|contains|starts-with|ends-with|regex|in. contains benötigt mindestens 3 Zeichen, damit der Trigrammindex genutzt werden kann. \"in\" prüft, ob der Wert in der Liste values vorkommt (ODER-Verknüpfung).")]
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
    private const int MaxPageSize = 500;

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
        "Beispiele: " +
        "(1) eine Bedingung: target=\"submodels\", conditions=[{scope:\"sm\",field:\"idShort\",op:\"eq\",value:\"TechnicalData\"}]. " +
        "(2) UND: combine=\"and\", conditions=[{scope:\"sm\",field:\"idShort\",op:\"eq\",value:\"TechnicalData\"},{scope:\"sme\",field:\"value\",op:\"lt\",value:\"100\"}]. " +
        "(3) ODER: combine=\"or\", conditions=[{scope:\"sme\",field:\"value\",op:\"eq\",value:\"A\"},{scope:\"sme\",field:\"value\",op:\"eq\",value:\"B\"}]. " +
        "Bei großen Treffermengen vorher aas_count aufrufen. " +
        "Wichtig: aas_query liefert standardmäßig nur Identifier. Danach den Inhalt mit aas_get_submodel lesen — oder, wenn die Frage mehrere Submodelle eines Produkts betrifft (z.B. technische Daten + Hersteller + CO2), in EINEM Schritt mit aas_get_product(identifier). " +
        "TIPP für Tabellen/Listen: Übergib select=[idShortPaths], dann liefert aas_query je Treffer direkt diese Feldwerte (Projektion) — das ersetzt viele Einzelabrufe.")]
    public async Task<object> AasQuery(
        [Description("Zielobjekt: \"submodels\" oder \"shells\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\". Bei einer Bedingung ohne Bedeutung.")] string combine = "and",
        [Description("Maximale Trefferzahl (Default und Maximum 500).")] int? limit = null,
        [Description("Cursor (Offset) zum Weiterblättern; aus nextCursor einer vorigen Antwort.")] string? cursor = null,
        [Description("Optional: Liste von idShortPaths, deren Werte je Treffer direkt mitgeliefert werden (Projektion = Tabellenspalten), z.B. [\"GeneralInformation.ManufacturerArticleNumber\", \"TechnicalProperties.Power_output\"]. Dann gibt das Tool statt nur Identifier eine Zeile pro Treffer mit diesen Feldern zurück — spart viele aas_get_submodel-Aufrufe. Volle Pfade (mit Punkt) sind präzise; ein Blattname ohne Punkt nimmt den ersten Treffer im Submodel. Nur für target=\"submodels\".")] string[]? select = null,
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
        var pagination = new PaginationParameters(cursor, NormalizeLimit(limit));

        var (list, queryError) = await TryQuery(securityConfig, pagination, resultType, expression);
        if (queryError != null)
        {
            return new { error = "Query fehlgeschlagen: " + queryError };
        }

        var identifiers = ExtractIdentifiers(list);

        string? nextCursor = identifiers.Count >= pagination.Limit
            ? (pagination.Cursor + identifiers.Count).ToString(CultureInfo.InvariantCulture)
            : null;

        // Projektion: Wenn select angegeben ist, je Treffer eine Zeile mit den gewählten Feldwerten liefern
        // (statt nur Identifier) — spart die vielen aas_get_submodel-Folgeaufrufe. Nur für Submodelle sinnvoll.
        if (select is { Length: > 0 } && resultType == ResultType.Submodel)
        {
            var rows = new JsonArray();
            for (var index = 0; index < identifiers.Count; index++)
            {
                var id = identifiers[index];
                var projectionWatch = Stopwatch.StartNew();
                Console.WriteLine($"[MCP] aas_query projection {index + 1}/{identifiers.Count}: {id}");
                rows.Add(await BuildProjectionRow(securityConfig, id, select, lang, priority, deprioritize, withPaths));
                Console.WriteLine(
                    $"[MCP] aas_query projection {index + 1}/{identifiers.Count} done " +
                    $"in {projectionWatch.ElapsedMilliseconds} ms");
            }

            return new { target, count = identifiers.Count, columns = select, rows, nextCursor };
        }

        return new
        {
            target,
            count = identifiers.Count,
            identifiers,
            nextCursor,
            nextStep = identifiers.Count > 0
                ? "Dies sind nur Identifier. Inhalte lesen: aas_get_submodel(identifier), oder gleich Felder mitliefern via select=[...]. Für ALLE Daten eines Produkts (Technik+Hersteller+CO2): aas_get_product(identifier)."
                : "Keine Treffer. Bedingung lockern (z.B. op=contains statt eq), Groß-/Kleinschreibung des idShort prüfen oder breiter suchen.",
        };
    }

    [McpServerTool(Name = "aas_count", Title = "Count AAS Search Results", Destructive = false, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Zählt die Treffer einer Suche, bevor man sie abruft. Gleiche Bedingungs-/combine-Logik wie aas_query. " +
        "Hinweis: in dieser Version nur für target=\"submodels\" verfügbar; für target=\"shells\" bitte aas_query mit limit verwenden. " +
        "Liefert die exakte Gesamtzahl (ungedeckelt) und ist auch bei großen Treffermengen schnell.")]
    public async Task<object> AasCount(
        [Description("Zielobjekt: aktuell nur \"submodels\".")] string target,
        [Description("Liste von Suchbedingungen (mindestens eine).")] McpQueryCondition[] conditions,
        [Description("Verknüpfung mehrerer Bedingungen: \"and\" oder \"or\".")] string combine = "and")
    {
        using var _ = LogCallTimed($"aas_count {target} {DescribeConditions(conditions, combine)}");

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
        "Hinweis: bei vielen/großen Submodellen kann das Ergebnis umfangreich werden.")]
    public async Task<object> AasGetProduct(
        [Description("AAS-Identifier ODER Submodel-Identifier (vollständige id, NICHT Base64-kodiert). Ein Submodel-Treffer aus aas_query genügt — die Shell wird automatisch aufgelöst.")] string identifier,
        [Description("Ausgabeformat je Submodel: \"value\" (kompakt, Default, nur idShort->Wert) oder \"full\" (vollständiges AAS-JSON inkl. semanticId, valueType, Qualifier). Nutze \"full\", wenn nach semanticId, Einheiten oder Datentyp gefragt wird.")] string format = "value")
    {
        using var _ = LogCallTimed($"aas_get_product id={identifier} format={format}");
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

        return await BuildProductObject(securityConfig, shell, fmt);
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
        var pagination = new PaginationParameters(null, MaxPageSize);
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
        Console.WriteLine("[MCP] " + line);
    }

    // Loggt den Aufruf (Eingang) und beim Dispose die Dauer — für Performance-Analyse bei großer DB.
    // Verwendung: using var _ = LogCallTimed($"aas_query ...");  -> beim Methodenende kommt "[MCP] <tool> done in X ms".
    private static CallTimer LogCallTimed(string line)
    {
        LogCall(line);
        return new CallTimer(line.Split(' ', 2)[0]);
    }

    private sealed class CallTimer : IDisposable
    {
        private readonly string _tool;
        private readonly System.Diagnostics.Stopwatch _watch = System.Diagnostics.Stopwatch.StartNew();

        public CallTimer(string tool) => _tool = tool;

        public void Dispose() => Console.WriteLine($"[MCP] {_tool} done in {_watch.ElapsedMilliseconds} ms");
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

    // Eine Projektions-Zeile für ein Submodel: liest es und extrahiert die select-Pfade als {id, <path>:<wert>}.
    private async Task<JsonObject> BuildProjectionRow(
        SecurityConfig securityConfig, string id, string[] select, string lang,
        string[]? priority, string[]? deprioritize, bool withPaths)
    {
        var row = new JsonObject { ["id"] = id };
        JsonObject? selectedPaths = withPaths ? new JsonObject() : null;
        ISubmodel? submodel = null;
        var submodelLoadAttempted = false;

        foreach (var rawPath in select)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var path = rawPath.Trim();

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
    private async Task<JsonObject> BuildProductObject(SecurityConfig securityConfig, IAssetAdministrationShell shell, string fmt, bool coreOnly = false)
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
        var truncated = false;
        if (shell.Submodels != null)
        {
            foreach (var smRef in shell.Submodels)
            {
                if (submodels.Count >= MaxProductSubmodels)
                {
                    truncated = true;
                    break;
                }

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
            ["truncated"] = truncated,
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
