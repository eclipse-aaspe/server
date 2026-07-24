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

namespace AasxServerDB;

using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.DbRequests;

/// <summary>
/// Batch projection of explicit full idShortPaths for a set of result submodels.
/// Reads the values directly from SMSets/SMRefSets/SMESets/ValueSets with a fixed
/// number of set-based queries — no per-hit submodel materialization.
/// Cross-submodel paths are resolved to sibling submodels of the same AAS via
/// SMSets.AASId and, for submodels only referenced by the shell, via SMRefSets.
/// </summary>
public static class ProjectionOperator
{
    // Keep IN() parameter lists comfortably below SQLite's default 999-variable limit
    // (each chunk query additionally carries the path/idShort lists as parameters).
    private const int ChunkSize = 400;

    public static List<DbProjectionRow> Project(AasContext db, DbProjectionRequest? request)
    {
        var identifiers = (request?.SubmodelIdentifiers ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
        var paths = (request?.Paths ?? new List<DbProjectionPath>())
            .Where(p => p != null
                && !string.IsNullOrWhiteSpace(p.RawPath)
                && !string.IsNullOrWhiteSpace(p.ElementIdShortPath))
            .ToList();

        var rows = identifiers.Select(id => new DbProjectionRow { SubmodelIdentifier = id }).ToList();
        if (rows.Count == 0 || paths.Count == 0)
        {
            return rows;
        }

        // 1) Result submodels: identifier -> (SMSets.Id, IdShort, AASId). Duplicate identifiers in the
        //    table are not expected; the row with the smallest Id wins for determinism.
        var hitByIdentifier = new Dictionary<string, (int SmId, string? IdShort, int? AasId)>(StringComparer.Ordinal);
        foreach (var chunk in identifiers.Distinct(StringComparer.Ordinal).Chunk(ChunkSize))
        {
            var found = db.SMSets
                .Where(sm => sm.Identifier != null && chunk.Contains(sm.Identifier))
                .Select(sm => new { sm.Id, sm.Identifier, sm.IdShort, sm.AASId })
                .ToList();
            foreach (var sm in found.OrderBy(sm => sm.Id))
            {
                if (sm.Identifier != null && !hitByIdentifier.ContainsKey(sm.Identifier))
                {
                    hitByIdentifier[sm.Identifier] = (sm.Id, sm.IdShort, sm.AASId);
                }
            }
        }

        var samePathStrings = paths
            .Where(p => p.TargetSubmodelIdShort == null)
            .Select(p => p.ElementIdShortPath)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var crossPaths = paths.Where(p => p.TargetSubmodelIdShort != null).ToList();

        // (SMId, IdShortPath) -> matched SME row; smallest Id wins when duplicated.
        var smeByKey = new Dictionary<(int SmId, string Path), (int SmeId, string? SmeType, string? TValue)>();

        // 2) Elements of the result submodels themselves.
        var hitSmIds = hitByIdentifier.Values.Select(v => v.SmId).Distinct().ToList();
        var hitPathStrings = samePathStrings
            .Concat(crossPaths.Select(p => p.ElementIdShortPath))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (hitPathStrings.Count > 0)
        {
            CollectSmes(db, hitSmIds, hitPathStrings, smeByKey);
        }

        // 3) Cross-submodel paths: resolve the AAS of each hit, then the sibling submodels.
        //    siblingByAasAndIdShort: (AASId, submodel idShort) -> (sibling SMId, sibling identifier)
        var aasIdByHitSmId = new Dictionary<int, int>();
        var siblingByAasAndIdShort = new Dictionary<(int AasId, string IdShort), (int SmId, string Identifier)>();
        if (crossPaths.Count > 0)
        {
            foreach (var (smId, _, aasId) in hitByIdentifier.Values)
            {
                if (aasId.HasValue)
                {
                    aasIdByHitSmId[smId] = aasId.Value;
                }
            }

            // Hits without a direct AASId link: resolve the AAS via the shell's submodel references.
            var unresolved = hitByIdentifier
                .Where(kv => !kv.Value.AasId.HasValue)
                .Select(kv => (Identifier: kv.Key, kv.Value.SmId))
                .ToList();
            if (unresolved.Count > 0)
            {
                var refAasByIdentifier = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var chunk in unresolved.Select(u => u.Identifier).Chunk(ChunkSize))
                {
                    var refs = db.SMRefSets
                        .Where(r => r.AASId != null && r.Identifier != null && chunk.Contains(r.Identifier))
                        .Select(r => new { r.Identifier, r.AASId })
                        .ToList();
                    foreach (var r in refs)
                    {
                        if (r.Identifier != null && !refAasByIdentifier.ContainsKey(r.Identifier))
                        {
                            refAasByIdentifier[r.Identifier] = r.AASId!.Value;
                        }
                    }
                }

                foreach (var (identifier, smId) in unresolved)
                {
                    if (refAasByIdentifier.TryGetValue(identifier, out var aasId))
                    {
                        aasIdByHitSmId[smId] = aasId;
                    }
                }
            }

            var aasIds = aasIdByHitSmId.Values.Distinct().ToList();
            var targetIdShorts = crossPaths
                .Select(p => p.TargetSubmodelIdShort!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // 3a) Siblings linked directly via SMSets.AASId.
            foreach (var chunk in aasIds.Chunk(ChunkSize))
            {
                var siblings = db.SMSets
                    .Where(sm => sm.AASId != null && chunk.Contains(sm.AASId.Value)
                        && sm.IdShort != null && targetIdShorts.Contains(sm.IdShort)
                        && sm.Identifier != null)
                    .Select(sm => new { sm.Id, sm.Identifier, sm.AASId, sm.IdShort })
                    .ToList();
                foreach (var sm in siblings.OrderBy(sm => sm.Id))
                {
                    var key = (sm.AASId!.Value, sm.IdShort!);
                    if (!siblingByAasAndIdShort.ContainsKey(key))
                    {
                        siblingByAasAndIdShort[key] = (sm.Id, sm.Identifier!);
                    }
                }
            }

            // 3b) Siblings only referenced by the shell (SMSets.AASId is null): follow SMRefSets.
            var refPairs = new List<(int AasId, string Identifier)>();
            foreach (var chunk in aasIds.Chunk(ChunkSize))
            {
                refPairs.AddRange(db.SMRefSets
                    .Where(r => r.AASId != null && chunk.Contains(r.AASId.Value) && r.Identifier != null)
                    .Select(r => new { r.AASId, r.Identifier })
                    .ToList()
                    .Select(r => (r.AASId!.Value, r.Identifier!)));
            }

            if (refPairs.Count > 0)
            {
                var aasByRefIdentifier = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var (aasId, identifier) in refPairs)
                {
                    if (!aasByRefIdentifier.ContainsKey(identifier))
                    {
                        aasByRefIdentifier[identifier] = aasId;
                    }
                }

                foreach (var chunk in aasByRefIdentifier.Keys.Chunk(ChunkSize))
                {
                    var siblings = db.SMSets
                        .Where(sm => sm.Identifier != null && chunk.Contains(sm.Identifier)
                            && sm.IdShort != null && targetIdShorts.Contains(sm.IdShort))
                        .Select(sm => new { sm.Id, sm.Identifier, sm.IdShort })
                        .ToList();
                    foreach (var sm in siblings.OrderBy(sm => sm.Id))
                    {
                        var key = (aasByRefIdentifier[sm.Identifier!], sm.IdShort!);
                        if (!siblingByAasAndIdShort.ContainsKey(key))
                        {
                            siblingByAasAndIdShort[key] = (sm.Id, sm.Identifier!);
                        }
                    }
                }
            }

            // 3c) Elements of the sibling submodels.
            var crossPathStrings = crossPaths
                .Select(p => p.ElementIdShortPath)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var siblingSmIds = siblingByAasAndIdShort.Values.Select(v => v.SmId).Distinct().ToList();
            CollectSmes(db, siblingSmIds, crossPathStrings, smeByKey);
        }

        // 4) Values of all matched elements in one pass (ordered for stable MLP language order).
        var valuesBySmeId = new Dictionary<int, List<DbProjectionValue>>();
        var allSmeIds = smeByKey.Values.Select(v => v.SmeId).Distinct().ToList();
        foreach (var chunk in allSmeIds.Chunk(ChunkSize))
        {
            var values = db.ValueSets
                .Where(v => chunk.Contains(v.SMEId))
                .OrderBy(v => v.Id)
                .Select(v => new { v.SMEId, v.SValue, v.NValue, v.Annotation })
                .ToList();
            foreach (var v in values)
            {
                if (!valuesBySmeId.TryGetValue(v.SMEId, out var list))
                {
                    list = new List<DbProjectionValue>();
                    valuesBySmeId[v.SMEId] = list;
                }

                list.Add(new DbProjectionValue { SValue = v.SValue, NValue = v.NValue, Annotation = v.Annotation });
            }
        }

        // 5) Assemble the rows in the requested order.
        var identifierBySmId = hitByIdentifier.ToDictionary(kv => kv.Value.SmId, kv => kv.Key);
        foreach (var row in rows)
        {
            var hasHit = hitByIdentifier.TryGetValue(row.SubmodelIdentifier, out var hit);
            foreach (var path in paths)
            {
                var cell = new DbProjectionCell();
                row.Cells[path.RawPath] = cell;
                if (!hasHit)
                {
                    continue;
                }

                int targetSmId;
                string targetIdentifier;
                if (path.TargetSubmodelIdShort == null
                    || string.Equals(hit.IdShort, path.TargetSubmodelIdShort, StringComparison.Ordinal))
                {
                    targetSmId = hit.SmId;
                    targetIdentifier = row.SubmodelIdentifier;
                }
                else if (aasIdByHitSmId.TryGetValue(hit.SmId, out var aasId)
                    && siblingByAasAndIdShort.TryGetValue((aasId, path.TargetSubmodelIdShort), out var sibling))
                {
                    targetSmId = sibling.SmId;
                    targetIdentifier = sibling.Identifier;
                }
                else
                {
                    continue;
                }

                if (smeByKey.TryGetValue((targetSmId, path.ElementIdShortPath), out var sme))
                {
                    cell.Found = true;
                    cell.SmeType = sme.SmeType;
                    cell.TValue = sme.TValue;
                    cell.SourceSubmodelIdentifier = targetIdentifier;
                    if (valuesBySmeId.TryGetValue(sme.SmeId, out var values))
                    {
                        cell.Values = values;
                    }
                }
            }
        }

        return rows;
    }

    private static void CollectSmes(
        AasContext db,
        List<int> smIds,
        List<string> idShortPaths,
        Dictionary<(int SmId, string Path), (int SmeId, string? SmeType, string? TValue)> smeByKey)
    {
        if (smIds.Count == 0 || idShortPaths.Count == 0)
        {
            return;
        }

        foreach (var chunk in smIds.Chunk(ChunkSize))
        {
            var smes = db.SMESets
                .Where(sme => chunk.Contains(sme.SMId)
                    && sme.IdShortPath != null && idShortPaths.Contains(sme.IdShortPath))
                .Select(sme => new { sme.Id, sme.SMId, sme.IdShortPath, sme.SMEType, sme.TValue })
                .ToList();
            foreach (var sme in smes.OrderBy(sme => sme.Id))
            {
                var key = (sme.SMId, sme.IdShortPath!);
                if (!smeByKey.ContainsKey(key))
                {
                    smeByKey[key] = (sme.Id, sme.SMEType, sme.TValue);
                }
            }
        }
    }
}
