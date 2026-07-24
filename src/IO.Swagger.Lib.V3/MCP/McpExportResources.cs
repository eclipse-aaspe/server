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
using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
/// MCP-Resources für die von aas_query_export_csv/aas_query_export_xlsx erzeugten Dateien.
/// Clients, die MCP-Resources unterstützen, können die Datei über resources/read mit der
/// resourceUri aus der Tool-Antwort (aas-export://{token}) als Binärinhalt laden.
/// </summary>
[McpServerResourceType]
public sealed class McpExportResources
{
    public const string UriScheme = "aas-export";

    public static string BuildResourceUri(string token) => $"{UriScheme}://{token}";

    [McpServerResource(UriTemplate = "aas-export://{token}", Name = "aas_query_export_file", Title = "AAS Query Export File", MimeType = "application/octet-stream")]
    [Description("Exportdatei (CSV/XLSX) eines aas_query_export-Aufrufs; token stammt aus dem Feld resourceUri der Tool-Antwort. Dateien verfallen nach ca. 60 Minuten.")]
    public static ResourceContents GetExportFile(string token)
    {
        if (!McpExportFileStore.TryGet(token, out var content, out _, out var mimeType))
        {
            throw new McpException($"Export \"{token}\" ist nicht (mehr) verfügbar. Bitte den Export erneut ausführen.");
        }

        // FromBytes base64-kodiert die Rohdaten; Blob direkt zu setzen erwartet bereits
        // base64-kodierte UTF-8-Bytes und würde die Datei auf dem Draht zerstören.
        return BlobResourceContents.FromBytes(content, BuildResourceUri(token), mimeType);
    }
}
