/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

using AasxServer;
using Extensions;
using IdentityModel;
using IdentityModel.Client;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Serializers;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Registry.Lib.V3.Services
{
    public class RegistryInitializerService : IRegistryInitializerService
    {
        static bool init;
        static ISubmodel? aasRegistry;
        static ISubmodel? submodelRegistry;
        static int initiallyEmpty = 0;
        static AasCore.Aas3_0.Environment envRegistry;
        static List<string> getRegistry = [];
        static List<string> postRegistry = [];
        static List<string?> federatedElemensSemanticId = [];
        static int submodelRegistryCount;
        static List<AssetAdministrationShellDescriptor> aasDescriptorsForSubmodelView = new List<AssetAdministrationShellDescriptor>();

        public List<string> GetRegistryList()
        {
            return getRegistry;
        }

        public ISubmodel? GetAasRegistry()
        {
            return aasRegistry;
        }

        public List<AssetAdministrationShellDescriptor> GetAasDescriptorsForSubmodelView()
        {
            return aasDescriptorsForSubmodelView;
        }

        public static X509Certificate2 certificate = null;

        public void InitRegistry(List<AasxCredentialsEntry> cList, DateTime timestamp, bool initAgain = false)
        {
            if (!initAgain && init)
            {
                AasxServer.Program.signalNewData(2);
                return;
            }

            AasxServer.Program.initializingRegistry = true;

            init = true;
            if (initAgain)
            {
                aasRegistry      = null;
                submodelRegistry = null;

                int i = initiallyEmpty;
                while (i < AasxServer.Program.env.Length)
                {
                    AasxServer.Program.env[ i ] = null;
                    i++;
                }
            }

            if (aasRegistry == null || submodelRegistry == null)
            {
                foreach (AdminShellNS.AdminShellPackageEnv env in AasxServer.Program.env)
                {
                    if (env != null)
                    {
                        var aas = env.AasEnv.AssetAdministrationShells[ 0 ];
                        if (aas.IdShort == "REGISTRY")
                        {
                            envRegistry = env.AasEnv;
                            if (aas.Submodels != null && aas.Submodels.Count > 0)
                            {
                                foreach (var smr in aas.Submodels)
                                {
                                    var sm = env.AasEnv.FindSubmodel(smr);
                                    if (sm != null && sm.IdShort != null)
                                    {
                                        if (sm.IdShort == "AASREGISTRY")
                                        {
                                            aasRegistry = sm;
                                            if (aasRegistry.SubmodelElements == null)
                                                aasRegistry.SubmodelElements = new List<ISubmodelElement>();
                                        }

                                        if (sm.IdShort == "SUBMODELREGISTRY")
                                        {
                                            submodelRegistry = sm;
                                            if (submodelRegistry.SubmodelElements == null)
                                                submodelRegistry.SubmodelElements = new List<ISubmodelElement>();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (aasRegistry != null)
                {
                    getRegistry.Clear();
                    foreach (var sme in aasRegistry.SubmodelElements)
                    {
                        if (sme is Property p)
                        {
                            if (p.IdShort.ToLower() == "postregistry")
                            {
                                string registryURL = TranslateURL(p.Value);
                                Console.WriteLine("POST to Registry: " + registryURL);
                                if (registryURL != "")
                                {
                                    postRegistry.Add(registryURL);
                                }
                            }

                            if (p.IdShort.ToLower() == "getregistry")
                            {
                                string registryURL = TranslateURL(p.Value);
                                Console.WriteLine("GET from Registry: " + registryURL);
                                if (registryURL != "")
                                {
                                    getRegistry.Add(registryURL);
                                }
                            }
                        }

                        if (sme is SubmodelElementCollection smc)
                        {
                            if (smc.IdShort == "federatedElements")
                            {
                                foreach (var sme2 in smc.Value)
                                {
                                    if (sme2 is Property p2)
                                    {
                                        if (p2.IdShort.Contains("semanticId"))
                                            federatedElemensSemanticId.Add(p2.Value);
                                    }
                                }
                            }
                        }
                    }

                    foreach (AdminShellNS.AdminShellPackageEnv env in AasxServer.Program.env)
                    {
                        if (env != null)
                        {
                            if (env != null)
                            {
                                var aas = env.AasEnv.AssetAdministrationShells[ 0 ];
                                if (aas.IdShort != "REGISTRY" && aas.IdShort != "myAASwithGlobalSecurityMetaModel")
                                {
                                    AddAasToRegistry(env, timestamp);
                                }

                                if (aas.IdShort == "PcfViewTask")
                                {
                                    string certificatePassword = "i40";
                                    Stream s2                  = null;
                                    try
                                    {
                                        s2 = env.GetLocalStreamFromPackage("/aasx/files/Andreas_Orzelski_Chain.pfx", access: FileAccess.Read);
                                    }
                                    catch
                                    {
                                    }

                                    if (s2 == null)
                                    {
                                        Console.WriteLine("Stream error!");
                                        continue;
                                    }

                                    X509Certificate2Collection xc = new X509Certificate2Collection();
                                    using (var m = new System.IO.MemoryStream())
                                    {
                                        s2.CopyTo(m);
                                        var b = m.GetBuffer();
                                        xc.Import(b, certificatePassword, X509KeyStorageFlags.PersistKeySet);
                                        certificate = new X509Certificate2(b, certificatePassword);
                                        Console.WriteLine("Client certificate: " + "/aasx/files/Andreas_Orzelski_Chain.pfx");
                                        s2.Close();
                                    }
                                }
                            }
                        }
                    }
                }

                if (getRegistry.Count != 0)
                {
                    var    submodelDescriptors = new List<SubmodelDescriptor>();
                    string submodelRegistryUrl = System.Environment.GetEnvironmentVariable("SUBMODELREGISTRY");
                    if (submodelRegistryUrl != null)
                    {
                        string json        = null;
                        string accessToken = null;

                        submodelRegistryUrl = submodelRegistryUrl.Replace("\r", "");
                        submodelRegistryUrl = submodelRegistryUrl.Replace("\n", "");

                        // basyx with Submodel Registry: read submodel descriptors
                        string requestPath   = submodelRegistryUrl + "/submodel-descriptors";
                        string queryPara     = "";
                        string userPW        = "";
                        string urlEdcWrapper = "";
                        string replace       = "";

                        if (AasxCredentials.get(cs.credentials, requestPath, out queryPara, out userPW, out urlEdcWrapper, out replace))
                        {
                            if (replace != "")
                                requestPath = replace;
                        }

                        var handler = new HttpClientHandler() {ServerCertificateCustomValidationCallback = delegate { return true; }};

                        if (!requestPath.Contains("localhost"))
                        {
                            if (AasxServer.AasxTask.proxy != null)
                                handler.Proxy = AasxServer.AasxTask.proxy;
                            else
                                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                        }

                        var client = new HttpClient(handler);
                        client.Timeout = TimeSpan.FromSeconds(10);
                        if (accessToken != null)
                            client.SetBearerToken(accessToken);

                        bool                error    = false;
                        HttpResponseMessage response = new HttpResponseMessage();
                        try
                        {
                            Console.WriteLine("GET " + requestPath);
                            var task = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                            task.Wait();
                            json = response.Content.ReadAsStringAsync().Result;
                            // TODO (jtikekar, 2023-09-04): check this call flow
                            // aasDescriptors = JsonConvert.DeserializeObject<List<AssetAdministrationShellDescriptor>>(json);
                            if (!string.IsNullOrEmpty(json))
                            {
                                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                JsonNode     node  = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                if (node is JsonObject jo)
                                {
                                    if (jo.ContainsKey("result"))
                                    {
                                        node = (JsonNode) jo[ "result" ];
                                        if (node is JsonArray a)
                                        {
                                            foreach (JsonNode n in a)
                                            {
                                                if (n != null)
                                                    submodelDescriptors.Add(DescriptorDeserializer.SubmodelDescriptorFrom(n));
                                            }
                                        }
                                    }
                                }
                            }

                            error = !response.IsSuccessStatusCode;
                        }
                        catch
                        {
                            error = true;
                        }

                        if (error)
                        {
                            string r = "ERROR GET; " + response.StatusCode.ToString();
                            r += " ; " + requestPath;
                            if (response.Content != null)
                                r += " ; " + response.Content.ReadAsStringAsync().Result;
                            Console.WriteLine(r);
                        }
                        else // OK
                        {
                        }
                    }

                    foreach (var greg in getRegistry)
                    {
                        List<AssetAdministrationShellDescriptor> aasDescriptors = new List<AssetAdministrationShellDescriptor>();

                        string json        = null;
                        string accessToken = null;
                        //string requestPath = greg + "/" + "registry/shell-descriptors";
                        string?  requestPath   = greg + "/shell-descriptors";
                        string? queryPara     = "";
                        string  userPW        = "";
                        string? urlEdcWrapper = "";
                        string? replace       = "";

                        if (AasxCredentials.get(cs.credentials, requestPath, out queryPara, out userPW, out urlEdcWrapper, out replace))
                        {
                            if (replace != "")
                                requestPath = replace;
                        }

                        var handler = new HttpClientHandler() {ServerCertificateCustomValidationCallback = delegate { return true; }};

                        if (!requestPath.Contains("localhost"))
                        {
                            if (AasxServer.AasxTask.proxy != null)
                                handler.Proxy = AasxServer.AasxTask.proxy;
                            else
                                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                        }

                        var client = new HttpClient(handler);
                        client.Timeout = TimeSpan.FromSeconds(10);
                        if (accessToken != null)
                            client.SetBearerToken(accessToken);

                        bool                error    = false;
                        HttpResponseMessage response = new HttpResponseMessage();
                        try
                        {
                            Console.WriteLine("GET " + requestPath);
                            var task = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                            task.Wait();
                            json = response.Content.ReadAsStringAsync().Result;
                            // TODO (jtikekar, 2023-09-04): check this call flow
                            // aasDescriptors = JsonConvert.DeserializeObject<List<AssetAdministrationShellDescriptor>>(json);
                            if (!string.IsNullOrEmpty(json))
                            {
                                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                JsonNode     node  = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                if (node is JsonObject jo)
                                {
                                    if (jo.ContainsKey("result"))
                                    {
                                        node = (JsonNode) jo[ "result" ];
                                        if (node is JsonArray a)
                                        {
                                            foreach (JsonNode n in a)
                                            {
                                                if (n != null)
                                                {
                                                    var ad = DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(n);
                                                    aasDescriptors.Add(ad);
                                                    if (ad.SubmodelDescriptors == null)
                                                        ad.SubmodelDescriptors = new List<SubmodelDescriptor>();
                                                    if (ad.SubmodelDescriptors.Count == 0)
                                                    {
                                                        requestPath = ad.Endpoints[ 0 ].ProtocolInformation.Href;
                                                        Console.WriteLine("GET " + requestPath);
                                                        task = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                                                        task.Wait();
                                                        json  = response.Content.ReadAsStringAsync().Result;
                                                        mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                                        node  = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                                        var aas = Jsonization.Deserialize.AssetAdministrationShellFrom(node);

                                                        var ids = new List<string?>();
                                                        foreach (var s in aas.Submodels)
                                                        {
                                                            var id = s.Keys[ 0 ].Value;
                                                            ids.Add(id);
                                                        }

                                                        foreach (var sd in submodelDescriptors)
                                                        {
                                                            if (ids.Contains(sd.Id))
                                                                ad.SubmodelDescriptors.Add(sd);
                                                        }

                                                        aasDescriptorsForSubmodelView.Add(ad);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            error = !response.IsSuccessStatusCode;
                        }
                        catch
                        {
                            error = true;
                        }

                        if (error)
                        {
                            string r = "ERROR GET; " + response.StatusCode.ToString();
                            r += " ; " + requestPath;
                            if (response.Content != null)
                                r += " ; " + response.Content.ReadAsStringAsync().Result;
                            Console.WriteLine(r);
                        }
                        else
                        {
                            int i = 0;
                            while (i < AasxServer.Program.env.Length)
                            {
                                var env = AasxServer.Program.env[ i ];
                                if (env == null)
                                {
                                    break;
                                }

                                i++;
                            }

                            initiallyEmpty = i;
                            foreach (var ad in aasDescriptors)
                            {
                                if (ad == null || ad.IdShort == "REGISTRY" || ad.IdShort == "myAASwithGlobalSecurityMetaModel")
                                    continue;

                                var watch = System.Diagnostics.Stopwatch.StartNew();

                                // check, if AAS is exisiting and must be replaced
                                var extensions = new List<IExtension> {new Extension("endpoint", value: ad.Endpoints[ 0 ].ProtocolInformation.Href)};
                                var aas = new AssetAdministrationShell(ad.Id, new AssetInformation(AssetKind.Instance, ad.GlobalAssetId), extensions,
                                                                       idShort: ad.IdShort + " - EXTERNAL");
                                aas.TimeStamp       = timestamp;
                                aas.TimeStampCreate = timestamp;
                                var newEnv = new AdminShellNS.AdminShellPackageEnv();
                                newEnv.AasEnv.AssetAdministrationShells.Add(aas);

                                foreach (var sd in ad.SubmodelDescriptors)
                                {
                                    if (sd.IdShort == "NameplateVC")
                                        continue;

                                    bool    success   = false;
                                    bool    external  = false;
                                    string  idEncoded = "";
                                    string? endpoint  = sd.Endpoints[ 0 ].ProtocolInformation.Href;
                                    var     s1        = endpoint.Split("/shells/");
                                    if (s1.Length == 2)
                                    {
                                        var s2 = s1[ 1 ].Split("/submodels/");
                                        if (s2.Length == 2)
                                        {
                                            idEncoded = s2[ 1 ].Replace("/submodel/", "");
                                            ;
                                            endpoint = s1[ 0 ] + "/submodels/" + idEncoded;
                                        }
                                    }

                                    requestPath   = endpoint;
                                    queryPara     = "";
                                    userPW        = "";
                                    urlEdcWrapper = "";
                                    replace       = "";
                                    client.DefaultRequestHeaders.Clear();
                                    if (AasxCredentials.get(cList, requestPath, out queryPara, out userPW, out urlEdcWrapper, out replace))
                                    {
                                        if (replace != "")
                                            requestPath = replace;
                                        if (queryPara != "")
                                            queryPara = "?" + queryPara;
                                        if (userPW != "")
                                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userPW);
                                        if (urlEdcWrapper != "")
                                            requestPath = urlEdcWrapper;
                                    }

                                    AasxServer.Program.submodelAPIcount++;
                                    string clientToken = "";

                                    switch (sd.IdShort)
                                    {
                                        case "BillOfMaterial":
                                        case "ProductCarbonFootprint":
                                        case "CarbonFootprint":
                                        case "TechnicalData":
                                        case "Nameplate":
                                        case "WeightInformation":
                                            // copy specific submodels locally
                                            try
                                            {
                                                requestPath += queryPara;
                                                // HEAD to get policy for submodel
                                                if (Program.withPolicy)
                                                {
                                                    // requestPath += queryPara;
                                                    Console.WriteLine("HEAD Submodel " + requestPath);
                                                    var task = Task.Run(async () => { response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, requestPath)); });
                                                    task.Wait();

                                                    string userName                = "aorzelski@phoenixcontact.com";
                                                    string policy                  = "";
                                                    string policyRequestedResource = "";
                                                    foreach (var kvp in response.Headers)
                                                    {
                                                        if (kvp.Key == "policy")
                                                            policy = kvp.Value.FirstOrDefault();
                                                        if (kvp.Key == "policyRequestedResource")
                                                            policyRequestedResource = kvp.Value.FirstOrDefault();
                                                    }

                                                    if (policy != "")
                                                    {
                                                        var    credential = new X509SigningCredentials(certificate);
                                                        string clientId   = "client.jwt";
                                                        string subject    = certificate.Subject;
                                                        var    now        = DateTime.UtcNow;
                                                        var claimList =
                                                            new List<Claim>()
                                                            {
                                                                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                                                                new Claim(JwtClaimTypes.Subject, clientId),
                                                                new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                                                                new Claim("userName", userName)
                                                            };
                                                        if (policy != "")
                                                            claimList.Add(new Claim("policy", policy, ClaimValueTypes.String));
                                                        if (policyRequestedResource != "")
                                                            claimList.Add(new Claim("policyRequestedResource", policyRequestedResource, ClaimValueTypes.String));
                                                        var token = new JwtSecurityToken(
                                                                                         clientId,
                                                                                         policyRequestedResource,
                                                                                         claimList,
                                                                                         now,
                                                                                         now.AddDays(1),
                                                                                         credential)
                                                            ;
                                                        var tokenHandler = new JwtSecurityTokenHandler();
                                                        clientToken = tokenHandler.WriteToken(token);
                                                        client.SetBearerToken(clientToken);
                                                    }
                                                }

                                                Console.WriteLine("GET Submodel " + requestPath);
                                                var task1 = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                                                task1.Wait();
                                                if (response.IsSuccessStatusCode)
                                                {
                                                    json = response.Content.ReadAsStringAsync().Result;
                                                    var sm = new Submodel("");
                                                    try
                                                    {
                                                        MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                                        JsonNode     node  = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                                        sm = Jsonization.Deserialize.SubmodelFrom(node);

                                                        sm.IdShort += " - COPY";
                                                        sm.Extensions = new List<IExtension>
                                                                        {
                                                                            new Extension("endpoint", value: sd.Endpoints[ 0 ].ProtocolInformation.Href),
                                                                            new Extension("clientToken", value: clientToken)
                                                                        };
                                                        sm.SetAllParentsAndTimestamps(null, timestamp, timestamp);
                                                        aas.AddSubmodelReference(sm.GetReference());
                                                        newEnv.AasEnv.Submodels.Add(sm);
                                                        success = true;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine("ERROR Deserialization " + requestPath + " " + ex.Message);

                                                        success = false;
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                success = false;
                                            }

                                            break;
                                        default:
                                            // test if submodel is accessible
                                            try
                                            {
                                                if (urlEdcWrapper == "")
                                                {
                                                    if (queryPara == "")
                                                    {
                                                        queryPara = "?level=core";
                                                    }
                                                    else
                                                    {
                                                        queryPara += "&level=core";
                                                    }
                                                }

                                                requestPath += queryPara;
                                                Console.WriteLine("GET Submodel Core " + requestPath);
                                                var task2 = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                                                task2.Wait();
                                                if (response.IsSuccessStatusCode)
                                                {
                                                    success  = true;
                                                    external = true;
                                                }
                                            }
                                            catch
                                            {
                                                success = false;
                                            }

                                            break;
                                    }

                                    if (!success || external)
                                    {
                                        var sm = new Submodel(sd.Id);
                                        if (!success)
                                        {
                                            sm.IdShort = sd.IdShort + " - NO ACCESS";
                                        }
                                        else
                                        {
                                            if (external)
                                                sm.IdShort = sd.IdShort + " - EXTERNAL";
                                        }

                                        sm.Extensions = new List<IExtension>
                                                        {
                                                            new Extension("endpoint", value: sd.Endpoints[ 0 ].ProtocolInformation.Href),
                                                            new Extension("clientToken", value: clientToken)
                                                        };
                                        sm.SetAllParentsAndTimestamps(null, timestamp, timestamp);
                                        aas.AddSubmodelReference(sm.GetReference());
                                        newEnv.AasEnv.Submodels.Add(sm);
                                    }
                                }

                                watch.Stop();
                                Console.WriteLine(watch.ElapsedMilliseconds + " ms");

                                AasxServer.Program.env[ i ] = newEnv;
                                i++;
                            }
                        }
                    }
                }
            }

            AasxServer.Program.signalNewData(2);

            AasxServer.Program.initializingRegistry = false;

            if (aasRegistry != null && aasRegistry.SubmodelElements != null)
            {
                Console.WriteLine($"Registry initialization complete.{aasRegistry.SubmodelElements.Count}");
            }
            else
            {
                Console.WriteLine($"Registry is empty");
            }
        }

        #region PrivateMethods

        static string TranslateURL(string url)
        {
            // get from environment
            if (url.Substring(0, 1) == "$")
            {
                string envVar = url.Substring(1);
                url = System.Environment.GetEnvironmentVariable(envVar);
                if (url == null)
                {
                    url = "";
                }
                url = url.Replace("\r", "");
                url = url.Replace("\n", "");
            }

            return url;
        }

        static void AddAasToRegistry(AdminShellNS.AdminShellPackageEnv env, DateTime timestamp)
#pragma warning restore IDE1006 // Benennungsstile
        {
            var aas = env.AasEnv.AssetAdministrationShells[ 0 ];

            AssetAdministrationShellDescriptor ad            = new AssetAdministrationShellDescriptor();
            string?                            globalAssetId = aas.AssetInformation.GlobalAssetId!;

            // ad.Administration.Version = aas.administration.version;
            // ad.Administration.Revision = aas.administration.revision;
            ad.IdShort = aas.IdShort!;
            ad.Id      = aas.Id;
            var e = new Endpoint();
            e.ProtocolInformation = new ProtocolInformation();
            e.ProtocolInformation.Href =
                Program.externalRepository + "/shells/" +
                Base64UrlEncoder.Encode(ad.Id);
            Console.WriteLine("AAS " + ad.IdShort + " " + e.ProtocolInformation.Href);
            e.Interface  = "AAS-1.0";
            ad.Endpoints = new List<Endpoint>();
            ad.Endpoints.Add(e);
            ad.GlobalAssetId = globalAssetId;
            //
            var extSubjId       = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() {new Key(KeyTypes.GlobalReference, "assetKind")});
            var specificAssetId = new SpecificAssetId("assetKind", aas.AssetInformation.AssetKind.ToString(), externalSubjectId: extSubjId);
            ad.SpecificAssetIds = new List<SpecificAssetId> {specificAssetId};

            // Submodels
            if (aas.Submodels != null && aas.Submodels.Count > 0)
            {
                ad.SubmodelDescriptors = new List<SubmodelDescriptor>();
                foreach (var smr in aas.Submodels)
                {
                    var sm = env.AasEnv.FindSubmodel(smr);
                    if (sm != null && sm.IdShort != null)
                    {
                        SubmodelDescriptor sd = new SubmodelDescriptor();
                        sd.IdShort = sm.IdShort;
                        sd.Id      = sm.Id;
                        var esm = new Models.Endpoint();
                        esm.ProtocolInformation = new ProtocolInformation();
                        esm.ProtocolInformation.Href =
                            AasxServer.Program.externalRepository + "/shells/" +
                            Base64UrlEncoder.Encode(ad.Id) + "/submodels/" +
                            Base64UrlEncoder.Encode(sd.Id);
                        // Console.WriteLine("SM " + sd.IdShort + " " + esm.ProtocolInformation.EndpointAddress);
                        esm.Interface = "SUBMODEL-1.0";
                        sd.Endpoints  = new List<Models.Endpoint>();
                        sd.Endpoints.Add(esm);
                        if (sm.SemanticId != null)
                        {
                            var sid = sm.SemanticId.GetAsExactlyOneKey();
                            if (sid != null)
                            {
                                var semanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() {new Key(KeyTypes.GlobalReference, sid.Value)});
                                sd.SemanticId = semanticId;
                            }
                        }

                        // add searchData for registry
                        if (sm.SubmodelElements != null)
                            foreach (var se in sm.SubmodelElements)
                            {
                                var  sme      = se;
                                bool federate = false;
                                if (sme.SemanticId != null && sme.SemanticId.Keys != null && sme.SemanticId.Keys.Count != 0 && sme.SemanticId.Keys[ 0 ] != null)
                                {
                                    if (federatedElemensSemanticId.Contains(sme.SemanticId.Keys[ 0 ].Value))
                                        federate = true;
                                }

                                if (sme.Qualifiers != null && sme.Qualifiers.Count != 0)
                                {
                                    if (sme.Qualifiers[ 0 ].Type == "federatedElement")
                                        federate = true;
                                }
                            }

                        ad.SubmodelDescriptors.Add(sd);
                        if (sm.IdShort.ToLower() == "nameplate")
                        {
                            // Add special entry for verifiable credentials
                            sd                      = new SubmodelDescriptor();
                            sd.IdShort              = "NameplateVC";
                            sd.Id                   = sm.Id + "_VC";
                            esm                     = new Models.Endpoint();
                            esm.ProtocolInformation = new ProtocolInformation();
                            // TODO (jtikekar, 2023-09-04): @Andreas why hardcoded=> Verifiable creditentials
                            esm.ProtocolInformation.Href =
                                "https://nameplate.h2894164.stratoserver.net/demo/selfdescriptiononthefly/" +
                                "aHR0cHM6Ly9yZWdpc3RyeS5oMjg5NDE2NC5zdHJhdG9zZXJ2ZXIubmV0/" +
                                Base64UrlEncoder.Encode(ad.Id);
                            esm.Interface = "VC-1.0";
                            sd.Endpoints  = new List<Models.Endpoint>();
                            sd.Endpoints.Add(esm);
                            if (sm.SemanticId != null)
                            {
                                var sid = sm.SemanticId.GetAsExactlyOneKey();
                                if (sid != null)
                                {
                                    var semanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() {new Key(KeyTypes.GlobalReference, sid.Value)});
                                    sd.SemanticId = semanticId;
                                }
                            }

                            ad.SubmodelDescriptors.Add(sd);
                        }
                    }
                }
            }

            // add to internal registry
            if (postRegistry.Contains("this"))
                AddAasDescriptorToRegistry(ad, timestamp, true);

            // Test serialize + deserialize;
            // TODO (jtikekar, 2023-09-04): @Andreas why? Gives an exception due to interface IReference Needs to extend Jsonization class
            //bool test = true;
            //if (test)
            //{
            //    string json = JsonConvert.SerializeObject(ad);
            //    Console.WriteLine(json);
            //    var adTest = JsonConvert.DeserializeObject<AssetAdministrationShellDescriptor>(json);
            //}

            // POST Descriptor to Registry
            foreach (var pr in postRegistry)
            {
                if (pr == "this")
                {
                    continue;
                }

                var    watch       = System.Diagnostics.Stopwatch.StartNew();
                string accessToken = null;
                string requestPath = pr;
                if (!requestPath.Contains("?"))
                {
                    //requestPath = requestPath + "/registry/shell-descriptors";
                    requestPath = requestPath + "/shell-descriptors";
                }

                //string json = JsonConvert.SerializeObject(ad);
                string json = DescriptorSerializer.ToJsonObject(ad).ToJsonString();

                var handler = new HttpClientHandler() {ServerCertificateCustomValidationCallback = delegate { return true; }};

                if (!requestPath.Contains("localhost"))
                {
                    if (AasxTask.proxy != null)
                    {
                        handler.Proxy = AasxTask.proxy;
                    }
                    else
                    {
                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    }
                }

                var client = new HttpClient(handler);
                if (accessToken != null)
                    client.SetBearerToken(accessToken);
                client.Timeout = TimeSpan.FromSeconds(20);

                if (json != "")
                {
                    bool                error    = false;
                    HttpResponseMessage response = new HttpResponseMessage();
                    try
                    {
                        Console.WriteLine("POST " + requestPath);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        var task = Task.Run(async () =>
                                            {
                                                response = await client.PostAsync(
                                                                                  requestPath, content);
                                            });
                        task.Wait();
                        error = !response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        error = true;
                    }

                    if (error)
                    {
                        string r = "ERROR POST; " + response.StatusCode.ToString();
                        r += " ; " + requestPath;
                        if (response.Content != null)
                            r += " ; " + response.Content.ReadAsStringAsync().Result;
                        Console.WriteLine(r);
                    }
                }

                watch.Stop();
                Console.WriteLine(watch.ElapsedMilliseconds + " ms");
            }
        }

        public void CreateAssetAdministrationShellDescriptor(AssetAdministrationShellDescriptor newAasDesc, DateTime timestamp, bool initial = false)
        {
            AddAasDescriptorToRegistry(newAasDesc, timestamp, initial);
            Program.signalNewData(2);
        }

        static void AddAasDescriptorToRegistry(AssetAdministrationShellDescriptor ad, DateTime timestamp, bool initial = false)
        {
            string? aasID    = ad.Id;
            string? assetID  = ad.GlobalAssetId;
            string? endpoint = "";
            if (ad.Endpoints != null && ad.Endpoints.Count != 0)
            {
                endpoint = ad.Endpoints[ 0 ].ProtocolInformation.Href;
            }

            // overwrite existing entry, if assetID AND aasID are identical
            if (!initial)
            {
                foreach (var e in aasRegistry?.SubmodelElements)
                {
                    if (e is SubmodelElementCollection ec)
                    {
                        int      found = 0;
                        Property pjson = null;
                        Property pep   = null;
                        foreach (var e2 in ec.Value)
                        {
                            if (e2 is Property ep)
                            {
                                if (ep.IdShort == "aasID" && ep.Value == aasID)
                                    found++;
                                if (ep.IdShort == "assetID" && ep.Value == assetID)
                                    found++;
                                if (ep.IdShort == "descriptorJSON")
                                    pjson = ep;
                                if (ep.IdShort == "endpoint")
                                    pep = ep;
                            }
                        }

                        if (found == 2 && pjson != null)
                        {
                            //string s = JsonConvert.SerializeObject(ad);
                            string s = DescriptorSerializer.ToJsonObject(ad).ToJsonString();
                            // if (s != pjson.Value)
                            {
                                pjson.TimeStampCreate = timestamp;
                                pjson.TimeStamp       = timestamp;
                                pjson.Value           = s;
                                pep.TimeStampCreate   = timestamp;
                                pep.TimeStamp         = timestamp;
                                pep.Value             = endpoint;
                                Console.WriteLine("Replace Descriptor:");
                                Console.WriteLine(s);
                            }
                            return;
                        }
                    }
                }
            }

            // add new entry
            SubmodelElementCollection c = new SubmodelElementCollection(
                                                                        idShort: "ShellDescriptor_" + aasRegistry.SubmodelElements.Count,
                                                                        value: new List<ISubmodelElement>());
            c.TimeStampCreate = timestamp;
            c.TimeStamp       = timestamp;
            var p = new Property(DataTypeDefXsd.String, idShort: "idShort");
            p.TimeStampCreate = timestamp;
            p.TimeStamp       = timestamp;
            p.Value           = ad.IdShort;
            c.Value.Add(p);
            p                 = new Property(DataTypeDefXsd.String, idShort: "aasID");
            p.TimeStampCreate = timestamp;
            p.TimeStamp       = timestamp;
            p.Value           = aasID;
            c.Value.Add(p);
            p                 = new Property(DataTypeDefXsd.String, idShort: "assetID");
            p.TimeStampCreate = timestamp;
            p.TimeStamp       = timestamp;
            if (assetID != "")
            {
                p.Value = assetID;
            }

            c.Value.Add(p);
            p                 = new Property(DataTypeDefXsd.String, idShort: "endpoint");
            p.TimeStampCreate = timestamp;
            p.TimeStamp       = timestamp;
            p.Value           = endpoint;
            c.Value.Add(p);
            p                 = new Property(DataTypeDefXsd.String, idShort: "descriptorJSON");
            p.TimeStampCreate = timestamp;
            p.TimeStamp       = timestamp;
            p.Value           = DescriptorSerializer.ToJsonObject(ad).ToJsonString();
            c.Value.Add(p);
            aasRegistry?.SubmodelElements.Add(c);
            /*
            int federatedElementsCount = 0;
            var smc = new SubmodelElementCollection(
                idShort: "federatedElements",
                value: new List<ISubmodelElement>());
            smc.TimeStampCreate = timestamp;
            smc.TimeStamp = timestamp;
            c.Value.Add(smc);
            */
            // iterate submodels
            int iSubmodel = 0;
            foreach (var sd in ad.SubmodelDescriptors)
            {
                // add new entry
                SubmodelElementCollection cs = new SubmodelElementCollection(
                                                                             idShort: "SubmodelDescriptor_" + submodelRegistryCount++,
                                                                             value: new List<ISubmodelElement>());
                cs.TimeStampCreate = timestamp;
                cs.TimeStamp       = timestamp;
                var ps = new Property(DataTypeDefXsd.String, idShort: "idShort");
                ps.TimeStampCreate = timestamp;
                ps.TimeStamp       = timestamp;
                ps.Value           = sd.IdShort;
                cs.Value.Add(ps);
                ps                 = new Property(DataTypeDefXsd.String, idShort: "submodelID");
                ps.TimeStampCreate = timestamp;
                ps.TimeStamp       = timestamp;
                ps.Value           = sd.Id;
                cs.Value.Add(ps);
                ps                 = new Property(DataTypeDefXsd.String, idShort: "semanticID");
                ps.TimeStampCreate = timestamp;
                ps.TimeStamp       = timestamp;
                if (sd.SemanticId != null && sd.SemanticId.GetAsExactlyOneKey().Value != null)
                    ps.Value = sd.SemanticId.GetAsExactlyOneKey().Value;
                cs.Value.Add(ps);
                if (sd.Endpoints != null && sd.Endpoints.Count != 0)
                {
                    endpoint = sd.Endpoints[ 0 ].ProtocolInformation.Href;
                }

                ps                 = new Property(DataTypeDefXsd.String, idShort: "endpoint");
                ps.TimeStampCreate = timestamp;
                ps.TimeStamp       = timestamp;
                ps.Value           = endpoint;
                cs.Value.Add(ps);
                ps                 = new Property(DataTypeDefXsd.String, idShort: "descriptorJSON");
                ps.TimeStampCreate = timestamp;
                ps.TimeStamp       = timestamp;
                //ps.Value = JsonConvert.SerializeObject(sd);
                ps.Value = DescriptorSerializer.ToJsonObject(sd).ToJsonString();
                cs.Value.Add(ps);
                // iterate submodels
                int federatedElementsCount = 0;
                var smc = new SubmodelElementCollection(
                                                        idShort: "federatedElements",
                                                        value: new List<ISubmodelElement>());
                smc.TimeStampCreate = timestamp;
                smc.TimeStamp       = timestamp;
                cs.Value.Add(smc);
                if (submodelRegistry.SubmodelElements == null)
                    submodelRegistry.SubmodelElements = new List<ISubmodelElement>();
                submodelRegistry?.SubmodelElements.Add(cs);
                submodelRegistry?.SetAllParents(timestamp);
                var r = new ReferenceElement(idShort: "ref_Submodel_" + iSubmodel++);
                r.TimeStampCreate = timestamp;
                r.TimeStamp       = timestamp;
                var mr = cs.GetModelReference(true);
                r.Value = mr;
                // revert order in references
                int first = 0;
                int last  = r.Value.Keys.Count - 1;
                while (first < last)
                {
                    var temp = r.Value.Keys[ first ];
                    r.Value.Keys[ first ] = r.Value.Keys[ last ];
                    r.Value.Keys[ last ]  = temp;
                    first++;
                    last--;
                }

                c.Value.Add(r);

                if (sd.IdShort == "NameplateVC")
                {
                    if (sd.Endpoints != null && sd.Endpoints.Count > 0)
                    {
                        var ep = sd.Endpoints[ 0 ].ProtocolInformation.Href;
                        p                 = new Property(DataTypeDefXsd.String, idShort: "NameplateVC");
                        p.TimeStampCreate = timestamp;
                        p.TimeStamp       = timestamp;
                        p.Value           = ep;
                        cs.Value.Add(p);
                    }
                }

                // TODO (jtikekar, 2023-09-04): @Andreas
                if (sd.FederatedElements != null && sd.FederatedElements.Count != 0)
                {
                    foreach (var fe in sd.FederatedElements)
                    {
                        try
                        {
                            federatedElementsCount++;
                            /*
                            var sme = Newtonsoft.Json.JsonConvert.DeserializeObject<ISubmodelElement>(
                                fe, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                            */
                            MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(fe));
                            JsonNode     node  = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                            var          sme   = Jsonization.Deserialize.ISubmodelElementFrom(node);

                            // p = AdminShell.Property.CreateNew("federatedElement" + federatedElementsCount);
                            sme.TimeStampCreate = timestamp;
                            sme.TimeStamp       = timestamp;
                            // p.value = fe;
                            smc.Value.Add(sme);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            aasRegistry?.SetAllParents();
        }

        public void CreateMultipleAssetAdministrationShellDescriptor(List<AssetAdministrationShellDescriptor> body, DateTime timestamp)
        {
            if (aasRegistry != null)
                aasRegistry.SubmodelElements.Clear();
            if (submodelRegistry != null)
                submodelRegistry.SubmodelElements.Clear();
            foreach (var ad in body)
            {
                if (ad == null)
                    continue;
                lock (Program.changeAasxFile)
                {
                    AddAasDescriptorToRegistry(ad, timestamp);
                }
            }

            Program.signalNewData(2);
        }

        #endregion
    }
}