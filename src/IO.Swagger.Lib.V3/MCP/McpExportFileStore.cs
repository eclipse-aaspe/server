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
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;

/// <summary>
/// In-Memory-Ablage für MCP-Exportdateien (CSV/XLSX). Jede Datei bekommt ein zufälliges
/// URL-Token und ist darüber als HTTP-Download (/mcp-exports/{token}) und als
/// MCP-Resource (aas-export://{token}) abrufbar. Einträge verfallen nach <see cref="Ttl"/>;
/// zusätzlich begrenzen <see cref="MaxEntries"/>/<see cref="MaxTotalBytes"/> den Speicher.
/// </summary>
public static class McpExportFileStore
{
    private const int MaxEntries = 100;
    private const long MaxTotalBytes = 256L * 1024 * 1024;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(60);

    private sealed record Entry(byte[] Content, string FileName, string MimeType, DateTime CreatedUtc);

    private static readonly ConcurrentDictionary<string, Entry> Files = new(StringComparer.Ordinal);

    public static string Add(byte[] content, string fileName, string mimeType)
    {
        Prune();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        Files[token] = new Entry(content, fileName, mimeType, DateTime.UtcNow);
        return token;
    }

    public static bool TryGet(string? token, out byte[] content, out string fileName, out string mimeType)
    {
        content = [];
        fileName = string.Empty;
        mimeType = string.Empty;
        if (string.IsNullOrWhiteSpace(token) || !Files.TryGetValue(token.Trim(), out var entry))
        {
            return false;
        }

        if (DateTime.UtcNow - entry.CreatedUtc > Ttl)
        {
            Files.TryRemove(token.Trim(), out _);
            return false;
        }

        content = entry.Content;
        fileName = entry.FileName;
        mimeType = entry.MimeType;
        return true;
    }

    private static void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in Files)
        {
            if (now - kv.Value.CreatedUtc > Ttl)
            {
                Files.TryRemove(kv.Key, out _);
            }
        }

        // Älteste Einträge entfernen, bis Anzahl- und Größenbudget wieder eingehalten sind.
        while (Files.Count >= MaxEntries || Files.Sum(kv => (long)kv.Value.Content.Length) > MaxTotalBytes)
        {
            var oldest = Files.OrderBy(kv => kv.Value.CreatedUtc).FirstOrDefault();
            if (oldest.Key is null || !Files.TryRemove(oldest.Key, out _))
            {
                break;
            }
        }
    }
}
