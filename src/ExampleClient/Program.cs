/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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

// See https://aka.ms/new-console-template for more information
using AasCore.Aas3_0;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

Console.WriteLine("AAS Example Client V3");
Console.WriteLine();

// Find full API at https://v3.admin-shell-io.com/swagger
// GET shells, which is with pagenation
Console.WriteLine("GET https://v3.admin-shell-io.com/shells");

string requestPath = "https://v3.admin-shell-io.com/shells";

var handler = new HttpClientHandler();

if (!requestPath.Contains("localhost"))
{
    handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
}

var client = new HttpClient(handler);

bool error = false;
HttpResponseMessage response = new HttpResponseMessage();

var task = Task.Run(async () =>
{
    response = await client.GetAsync(requestPath);
});
task.Wait();
var json = response.Content.ReadAsStringAsync().Result;
if (!string.IsNullOrEmpty(json))
{
    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
    JsonNode? node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
    if (node is JsonObject jo)
    {
        if (jo.ContainsKey("result"))
        {
            node = (JsonNode)jo["result"];
            if (node is JsonArray a)
            {
                // iterate shells
                foreach (JsonNode n in a)
                {
                    if (n != null)
                    {
                        try
                        {
                            // Deserialize shell
                            var aas = Jsonization.Deserialize.AssetAdministrationShellFrom(n);
                            Console.WriteLine("Received AAS: " + aas.IdShort);

                            // Iterate submodels
                            if (aas.Submodels != null && aas.Submodels.Count > 0)
                            {
                                foreach (var smr in aas.Submodels)
                                {
                                    requestPath = "https://v3.admin-shell-io.com/submodels/" + Base64UrlEncoder.Encode(smr.Keys[0].Value);
                                    Console.WriteLine("GET " + requestPath);

                                    task = Task.Run(async () =>
                                    {
                                        response = await client.GetAsync(requestPath);
                                    });
                                    task.Wait();
                                    json = response.Content.ReadAsStringAsync().Result;
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        var       mStrm2   = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                        var       node2    = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm2).Result;
                                        var submodel = Jsonization.Deserialize.SubmodelFrom(node2);
                                        Console.WriteLine("Received Submodel: " + submodel.IdShort);
                                        // Iterate submodel here
                                        // See VisitorThrough in AasCore
                                        // See VisitorAASX in entityFW.cs
                                    }
                                }
                            }
                        }
                        catch
                        {
                            string r = "ERROR GET; " + response.StatusCode.ToString();
                            r += " ; " + requestPath;
                            if (response.Content != null)
                                r += " ; " + response.Content.ReadAsStringAsync().Result;
                            Console.WriteLine(r);
                        }
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}

