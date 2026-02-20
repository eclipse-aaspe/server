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
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Registry.Lib.V3.Services;

using System.Globalization;
using AasxServerDB;
using AdminShellNS;

public class RegistryInitializerService : IRegistryInitializerService
{
    private const string AasxFilesChainPfx = "/aasx/files/Andreas_Orzelski_Chain.pfx";
    private static bool init;
    private static ISubmodel? aasRegistry;
    private static ISubmodel? submodelRegistry;
    private static int initiallyEmpty;
    private static List<string> getRegistry = [];
    private static List<string> getRegistryOfRegistry = [];
    private static List<string> postRegistry = [];
    private static List<string> postSubmodelRegistry = [];
    private static List<string?> federatedElemensSemanticId = [];
    private static List<AssetAdministrationShellDescriptor> aasDescriptorsForSubmodelView = [];

    public RegistryInitializerService(IAasDescriptorWritingService aasDescriptorWritingService, AasxTaskService aasxTaskService)
    {
        _aasDescriptorWritingService = aasDescriptorWritingService;
        _aasxTaskService = aasxTaskService;
    }

    public List<string> GetRegistryList() => getRegistry;

    public ISubmodel? GetAasRegistry() => aasRegistry;

    public List<AssetAdministrationShellDescriptor> GetAasDescriptorsForSubmodelView() => aasDescriptorsForSubmodelView;

    public static X509Certificate2? Certificate;
    private readonly IAasDescriptorWritingService _aasDescriptorWritingService;
    private readonly AasxTaskService _aasxTaskService;

    public async Task InitRegistry(List<AasxCredentialsEntry> cList, DateTime timestamp, bool initAgain = false)
    {
        if (!initAgain && init)
        {
            Program.signalNewData(2);
            return;
        }

        Program.initializingRegistry = true;

        init = true;
        if (initAgain)
        {
            aasRegistry = null;
            submodelRegistry = null;

            var i = initiallyEmpty;
            while (i < Program.env.Length)
            {
                Program.env[i] = null;
                i++;
            }
        }

        if (aasRegistry == null || submodelRegistry == null)
        {
            foreach (var env in Program.env)
            {
                // Keep this null check as the env is initialized with 1000 null elements
                if (env == null)
                {
                    continue;
                }
                
                var aas = env.AasEnv?.AssetAdministrationShells?[0];
                if (aas?.IdShort != "REGISTRY")
                {
                    continue;
                }

                if (aas.Submodels == null || aas.Submodels.Count <= 0)
                {
                    continue;
                }

                foreach (var sm in aas.Submodels.Select(smr => env.AasEnv.FindSubmodel(smr)))
                {
                    if (sm is not {IdShort: not null})
                    {
                        continue;
                    }

                    switch (sm.IdShort)
                    {
                        case "AASREGISTRY":
                            aasRegistry                  =   sm;
                            aasRegistry.SubmodelElements ??= [];
                            break;
                        case "SUBMODELREGISTRY":
                            submodelRegistry                  =   sm;
                            submodelRegistry.SubmodelElements ??= [];
                            break;
                    }
                }
            }

            if (aasRegistry != null)
            {
                getRegistry.Clear();
                getRegistryOfRegistry.Clear();
                if (aasRegistry.SubmodelElements != null)
                {
                    foreach (var sme in aasRegistry.SubmodelElements)
                    {
                        switch (sme)
                        {
                            case Property p:
                            {
                                if (p.IdShort?.ToLower(CultureInfo.InvariantCulture) == "postregistry")
                                {
                                    if (p.Value != null)
                                    {
                                        var registryURL = TranslateURL(p.Value);
                                        Console.WriteLine("POST to Registry: " + registryURL);
                                        if (registryURL != "")
                                        {
                                            postRegistry.Add(registryURL);
                                        }
                                    }
                                }

                                if (p.IdShort?.ToLower(CultureInfo.InvariantCulture) == "postsubmodelregistry")
                                {
                                    if (p.Value != null)
                                    {
                                        var registryURL = TranslateURL(p.Value);
                                        Console.WriteLine("POST to Submodel Registry: " + registryURL);
                                        if (registryURL != "")
                                        {
                                            postSubmodelRegistry.Add(registryURL);
                                        }
                                    }
                                }

                                if (p.IdShort?.ToLower(CultureInfo.InvariantCulture) == "getregistry")
                                {
                                    if (p.Value != null)
                                    {
                                        var registryURL = TranslateURL(p.Value);
                                        Console.WriteLine("GET from Registry: " + registryURL);
                                        if (registryURL != "")
                                        {
                                            getRegistry.Add(registryURL);
                                        }
                                    }
                                }

                                if (p.IdShort?.ToLower(CultureInfo.InvariantCulture) == "getregistryofregistry")
                                {
                                    if (p.Value != null)
                                    {
                                        var registryURL = TranslateURL(p.Value);
                                        Console.WriteLine("GET from Registry of Registry: " + registryURL);
                                        if (registryURL != "")
                                        {
                                            getRegistryOfRegistry.Add(registryURL);
                                        }
                                    }
                                }

                                break;
                            }
                            case SubmodelElementCollection smc:
                            {
                                if (smc.IdShort == "federatedElements")
                                {
                                    foreach (var sme2 in smc.Value)
                                    {
                                        if (sme2 is not Property p2)
                                        {
                                            continue;
                                        }

                                        if (p2.IdShort != null && p2.IdShort.Contains("semanticId"))
                                        {
                                            federatedElemensSemanticId.Add(p2.Value);
                                        }
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                if (Program.withDb)
                {
                    await using (var db = new AasContext())
                    {
                        foreach (var aasDB in db.AASSets)
                        {
                            if (aasDB.IdShort == "REGISTRY" || aasDB.IdShort == "myAASwithGlobalSecurityMetaModel")
                            {
                                continue;
                            }

                            var aasDesc = AasRegistryService.GlobalCreateAasDescriptorFromDB(aasDB);
                            AddAasToRegistry(null, timestamp, aasDesc);
                        }
                    }
                }
                else
                {
                    foreach (var env in Program.env)
                    {
                        if (env != null)
                        {
                            var aas = env.AasEnv?.AssetAdministrationShells?[0];
                            if (aas?.IdShort != null && !aas.IdShort.Equals("REGISTRY", StringComparison.Ordinal) &&
                                !aas.IdShort.Equals("myAASwithGlobalSecurityMetaModel", StringComparison.InvariantCulture))
                            {
                                AddAasToRegistry(env, timestamp);
                            }

                            if (aas?.IdShort != "PcfViewTask")
                            {
                                continue;
                            }

                            const string certificatePassword = "i40";
                            Stream? s2 = null;
                            try
                            {
                                s2 = env.GetLocalStreamFromPackage(AasxFilesChainPfx, access: FileAccess.Read);
                            }
                            catch
                            {
                                // Well? It seems like a problem with the certificate...
                            }

                            if (s2 == null)
                            {
                                Console.WriteLine("Stream error!");
                                continue;
                            }

                            var xc = new X509Certificate2Collection();
                            using var m = new MemoryStream();
                            await s2.CopyToAsync(m);
                            var b = m.GetBuffer();
                            xc.Import(b, certificatePassword, X509KeyStorageFlags.PersistKeySet);
                            Certificate = new X509Certificate2(b, certificatePassword);
                            Console.WriteLine($"Client certificate: {AasxFilesChainPfx}");
                            s2.Close();
                        }
                    }
                }

                if (getRegistryOfRegistry.Count != 0)
                {
                    var requestPath = "";
                    Console.WriteLine("GET RegistryOfRegistry");
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    foreach (var regOfReg in getRegistryOfRegistry)
                    {
                        requestPath = regOfReg;
                        var error = false;
                        var response = new HttpResponseMessage();

                        if (AasxCredentials.get(_aasxTaskService, cs.credentials, requestPath, out _, out _, out _, out var replace) && !string.IsNullOrEmpty(replace))
                        {
                            requestPath = replace;
                        }

                        var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = delegate { return true; } };
                        if (_aasxTaskService.proxy != null)
                        {
                            handler.Proxy = _aasxTaskService.proxy;
                        }
                        else
                        {
                            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                        }

                        var client = new HttpClient(handler);
                        client.Timeout = TimeSpan.FromSeconds(10);
                        try
                        {
                            Console.WriteLine($"GET {requestPath}");
                            var task = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                            task.Wait();
                            var json = response.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrEmpty(json))
                            {
                                if (requestPath.EndsWith("/registry-descriptors"))
                                {
                                    var jsonArray = JsonNode.Parse(json)?.AsArray();

                                    if (jsonArray != null)
                                    {
                                        foreach (var item in jsonArray)
                                        {
                                            var url = item?["url"]?.ToString();
                                            if (!string.IsNullOrEmpty(url) && !getRegistry.Contains(url))
                                            {
                                                getRegistry.Add(url);
                                            }
                                        }
                                    }
                                }
                                if (requestPath.EndsWith("/company_endpoints"))
                                {
                                    var root = JsonNode.Parse(json);
                                    var endpoints = root?["data"]?["endpoints"]?.AsArray();

                                    if (endpoints is not null)
                                    {
                                        foreach (var ep in endpoints)
                                        {
                                            var type = ep?["type"]?.GetValue<string>();
                                            if (string.Equals(type, "aas-registry", StringComparison.OrdinalIgnoreCase))
                                            {
                                                var url = ep?["url"]?.GetValue<string>()?.Trim();
                                                if (!string.IsNullOrWhiteSpace(url) &&
                                                    !getRegistry.Contains(url, StringComparer.OrdinalIgnoreCase))
                                                {
                                                    getRegistry.Add(url);
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
                    }
                    watch.Stop();
                    Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
                }

                if (getRegistry.Count != 0)
                {
                    var requestPathGetReg = "";
                    var submodelDescriptors = new List<SubmodelDescriptor>();
                    var submodelRegistryUrl = System.Environment.GetEnvironmentVariable("SUBMODELREGISTRY");
                    if (submodelRegistryUrl != null)
                    {
                        string? accessToken = null;

                        submodelRegistryUrl = submodelRegistryUrl.Replace("\r", "");
                        submodelRegistryUrl = submodelRegistryUrl.Replace("\n", "");

                        // basyx with Submodel Registry: read submodel descriptors
                        requestPathGetReg = $"{submodelRegistryUrl}/submodel-descriptors";

                        if (AasxCredentials.get(_aasxTaskService, cs.credentials, requestPathGetReg, out _, out _, out _, out var replace) && !string.IsNullOrEmpty(replace))
                        {
                            requestPathGetReg = replace;
                        }

                        var handler = new HttpClientHandler() {ServerCertificateCustomValidationCallback = delegate { return true; }};

                        if (!requestPathGetReg.Contains("localhost"))
                        {
                            handler.Proxy = _aasxTaskService.proxy;
                        }

                        var client = new HttpClient(handler);
                        client.Timeout = TimeSpan.FromSeconds(10);
                        if (accessToken != null)
                        {
                            client.SetBearerToken(accessToken);
                        }

                        var error    = false;
                        var response = new HttpResponseMessage();
                        try
                        {
                            Console.WriteLine($"GET {requestPathGetReg}");
                            var task = Task.Run(async () => { response = await client.GetAsync(requestPathGetReg); });
                            task.Wait();
                            var json = response.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrEmpty(json))
                            {
                                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                var node         = await System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(memoryStream);
                                if (node is JsonObject jo)
                                {
                                    if (jo.ContainsKey("result"))
                                    {
                                        node = jo["result"];
                                        if (node is JsonArray a)
                                        {
                                            submodelDescriptors.AddRange(a.OfType<JsonNode>().Select(jsonNode => DescriptorDeserializer.SubmodelDescriptorFrom(jsonNode)));
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
                            var r = $"ERROR GET; {response.StatusCode}";
                            r += $" ; {requestPathGetReg}";
                            r += $" ; {response.Content.ReadAsStringAsync().Result}";
                            Console.WriteLine(r);
                        }
                    }

                    var i = 0;
                    while (i < Program.env.Length)
                    {
                        var env = Program.env[i];
                        if (env == null)
                        {
                            break;
                        }
                        i++;
                    }
                    initiallyEmpty = i;

                    aasDescriptorsForSubmodelView = [];
                    foreach (var greg in getRegistry)
                    {
                        Console.WriteLine("GET Registry");
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        var aasDescriptors = new List<AssetAdministrationShellDescriptor>();
                        string? json        = null;
                        string? accessToken = null;
                        requestPathGetReg = $"{greg}/shell-descriptors";
                        var userPW = "";
                        var urlEdcWrapper = "";
                        var replace = "";

                        if (AasxCredentials.get(_aasxTaskService, cs.credentials, requestPathGetReg, out var queryPara, out userPW, out urlEdcWrapper, out replace) && !string.IsNullOrEmpty(replace))
                        {
                            requestPathGetReg = replace;
                        }
                        if (queryPara.StartsWith("bearerHead="))
                        {
                            accessToken = queryPara.Replace("bearerHead=", "");
                        }

                        var handler = new HttpClientHandler {ServerCertificateCustomValidationCallback = delegate { return true; }};

                        if (!requestPathGetReg.Contains("localhost"))
                        {
                            if (_aasxTaskService.proxy != null)
                            {
                                handler.Proxy = _aasxTaskService.proxy;
                            }
                            else
                            {
                                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                            }
                        }

                        var client = new HttpClient(handler);
                        client.Timeout = TimeSpan.FromSeconds(10);
                        if (accessToken != null)
                            client.SetBearerToken(accessToken);

                        var error    = false;
                        var response = new HttpResponseMessage();
                        try
                        {
                            Console.WriteLine($"GET {requestPathGetReg}");
                            var path = requestPathGetReg;
                            var task = Task.Run(async () => { response = await client.GetAsync(path); });
                            task.Wait();
                            json = response.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrEmpty(json))
                            {
                                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                var node         = await System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(memoryStream);
                                if (node is JsonObject jo)
                                {
                                    if (jo.ContainsKey("result"))
                                    {
                                        node = jo["result"];
                                        if (node is JsonArray a)
                                        {
                                            foreach (var jsonNode in a)
                                            {
                                                if (jsonNode == null)
                                                {
                                                    continue;
                                                }

                                                var ad = DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(jsonNode);
                                                if (ad.IdShort.ToLower() == "events")
                                                {
                                                    // for setting breakpoint
                                                }
                                                else
                                                {
                                                    ad.Extensions = new List<Extension> { new Extension("registry", value: greg) };
                                                    aasDescriptors.Add(ad);
                                                    aasDescriptorsForSubmodelView.Add(ad);
                                                    ad.SubmodelDescriptors ??= [];

                                                    if (ad.SubmodelDescriptors.Count != 0)
                                                    {
                                                        continue;
                                                    }

                                                    requestPathGetReg = ad.Endpoints?[0].ProtocolInformation?.Href;
                                                    Console.WriteLine($"GET {requestPathGetReg}");
                                                    var path1 = requestPathGetReg;
                                                    task = Task.Run(async () => { response = await client.GetAsync(path1); });
                                                    task.Wait();
                                                    json = response.Content.ReadAsStringAsync().Result;
                                                    memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                                    node = await System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(memoryStream);
                                                    var aas = Jsonization.Deserialize.AssetAdministrationShellFrom(node);

                                                    var ids = (from s in aas?.Submodels select s.Keys[0].Value).ToList();

                                                    foreach (var sd in submodelDescriptors.Where(sd => ids.Contains(sd.Id)))
                                                    {
                                                        ad.SubmodelDescriptors.Add(sd);
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
                        Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
                        watch.Stop();

                        if (error)
                        {
                            var r = $"ERROR GET; {response.StatusCode} ; {requestPathGetReg} ; {response.Content.ReadAsStringAsync().Result}";
                            Console.WriteLine(r);
                        }
                        else
                        {
                            foreach (var ad in aasDescriptors)
                            {
                                if (ad.IdShort != null && (ad.IdShort.Equals("REGISTRY", StringComparison.Ordinal) ||
                                                           ad.IdShort.Equals("myAASwithGlobalSecurityMetaModel", StringComparison.Ordinal)))
                                {
                                    continue;
                                }

                                var watch1 = System.Diagnostics.Stopwatch.StartNew();

                                // check, if AAS is existing and must be replaced
                                var extensions = new List<IExtension>();
                                extensions.Add(new Extension("endpoint", value: ad.Endpoints?[0].ProtocolInformation?.Href));
                                // extensions.Add(new Extension("registry", value: ad.Extensions?[0].Value));
                                var aas = new AssetAdministrationShell(ad.Id, new AssetInformation(AssetKind.Instance, ad.GlobalAssetId), extensions,
                                                                       idShort: $"{ad.IdShort} - EXTERNAL");
                                aas.TimeStamp       = timestamp;
                                aas.TimeStampCreate = timestamp;
                                var newEnv = new AdminShellNS.AdminShellPackageEnv();
                                newEnv.AasEnv?.AssetAdministrationShells?.Add(aas);

                                if (ad.SubmodelDescriptors != null)
                                {
                                    foreach (var sd in ad.SubmodelDescriptors)
                                    {
                                        if (sd.IdShort != null && sd.IdShort.Equals("NameplateVC", StringComparison.Ordinal))
                                        {
                                            continue;
                                        }

                                        var    success  = false;
                                        var    external = false;
                                        string idEncoded;
                                        var    endpoint = sd.Endpoints?[0].ProtocolInformation?.Href;

                                        if (endpoint != null && !AasxCredentials.keep(cList, endpoint, false))
                                        {
                                            var s1 = endpoint?.Split("/shells/");
                                            if (s1 != null && s1.Length == 2)
                                            {
                                                var s2 = s1[1].Split("/submodels/");
                                                if (s2.Length == 2)
                                                {
                                                    idEncoded = s2[1].Replace("/submodel/", "");
                                                    endpoint = s1[0] + "/submodels/" + idEncoded;
                                                }
                                            }
                                        }

                                        requestPathGetReg = endpoint;
                                        client.DefaultRequestHeaders.Clear();
                                        if (requestPathGetReg != null && AasxCredentials.get(_aasxTaskService , cList, requestPathGetReg, out queryPara, out userPW, out urlEdcWrapper, out replace))
                                        {
                                            if (replace != "")
                                            {
                                                requestPathGetReg = replace;
                                            }

                                            if (queryPara != "")
                                            {
                                                if (queryPara.StartsWith("bearerHead="))
                                                {
                                                    var bearer = queryPara.Replace("bearerHead=", "");
                                                    queryPara = "";
                                                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                                                }
                                                else
                                                {
                                                    queryPara = "?" + queryPara;
                                                }
                                            }

                                            if (userPW != "")
                                            {
                                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userPW);
                                            }

                                            if (urlEdcWrapper != "")
                                            {
                                                requestPathGetReg = urlEdcWrapper;
                                            }
                                        }

                                        Program.submodelAPIcount++;
                                        string? clientToken = null;

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
                                                    requestPathGetReg += queryPara;
                                                    // HEAD to get policy for submodel
                                                    if (Program.withPolicy)
                                                    {
                                                        // requestPath += queryPara;
                                                        Console.WriteLine($"HEAD Submodel {requestPathGetReg}");
                                                        var path = requestPathGetReg;
                                                        var task = Task.Run(async () =>
                                                                            {
                                                                                response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, path));
                                                                            });
                                                        task.Wait();

                                                        const string userName                = "aorzelski@phoenixcontact.com";
                                                        var          policy                  = string.Empty;
                                                        var          policyRequestedResource = string.Empty;
                                                        foreach (var kvp in response.Headers)
                                                        {
                                                            switch (kvp.Key)
                                                            {
                                                                case "policy":
                                                                    policy = kvp.Value.FirstOrDefault();
                                                                    break;
                                                                case "policyRequestedResource":
                                                                    policyRequestedResource = kvp.Value.FirstOrDefault() ?? string.Empty;
                                                                    break;
                                                            }
                                                        }

                                                        if (policy != "")
                                                        {
                                                            var          credential = new X509SigningCredentials(Certificate);
                                                            const string clientId   = "client.jwt";
                                                            var          now        = DateTime.UtcNow;
                                                            var claimList =
                                                                new List<Claim>()
                                                                {
                                                                    new(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                                                                    new(JwtClaimTypes.Subject, clientId),
                                                                    new(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(CultureInfo.InvariantCulture),
                                                                        ClaimValueTypes.Integer64),
                                                                    new("userName", userName)
                                                                };
                                                            if (policy != "")
                                                                if (policy != null)
                                                                {
                                                                    claimList.Add(new Claim("policy", policy, ClaimValueTypes.String));
                                                                }

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

                                                    Console.WriteLine("GET Submodel " + requestPathGetReg);
                                                    var task1 = Task.Run(async () => { response = await client.GetAsync(requestPathGetReg); });
                                                    task1.Wait();
                                                    if (response.IsSuccessStatusCode)
                                                    {
                                                        json = response.Content.ReadAsStringAsync().Result;
                                                        Submodel sm;
                                                        try
                                                        {
                                                            var mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                                            var node  = await System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm);
                                                            sm = Jsonization.Deserialize.SubmodelFrom(node);

                                                            if (sm != null)
                                                            {
                                                                sm.IdShort += " - COPY";
                                                            }

                                                            if (sm != null)
                                                            {
                                                                sm.Extensions = new List<IExtension>
                                                                                {
                                                                                    new Extension("endpoint", value: sd.Endpoints?[0].ProtocolInformation?.Href),
                                                                                    new Extension("clientToken", value: clientToken)
                                                                                };
                                                                sm.SetAllParentsAndTimestamps(null, timestamp, timestamp, timestamp);
                                                                aas.AddSubmodelReference(sm.GetReference());
                                                                newEnv.AasEnv?.Submodels.Add(sm);
                                                            }

                                                            success = true;
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine($"ERROR Deserialization {requestPathGetReg} {ex.Message}");

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

                                                    requestPathGetReg += queryPara;
                                                    Console.WriteLine("GET Submodel Core " + requestPathGetReg);
                                                    var path  = requestPathGetReg;
                                                    var task2 = Task.Run(async () => { response = await client.GetAsync(path); });
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

                                        if (success && !external)
                                        {
                                            continue;
                                        }

                                        {
                                            var sm = new Submodel(sd.Id);
                                            if (!success)
                                            {
                                                sm.IdShort = $"{sd.IdShort} - NO ACCESS";
                                            }
                                            else
                                            {
                                                if (external)
                                                    sm.IdShort = $"{sd.IdShort} - EXTERNAL";
                                            }

                                            sm.Extensions = new List<IExtension>
                                                            {
                                                                new Extension("endpoint", value: sd.Endpoints?[0].ProtocolInformation.Href),
                                                                new Extension("clientToken", value: clientToken)
                                                            };
                                            sm.SetAllParentsAndTimestamps(null, timestamp, timestamp, timestamp);
                                            aas.AddSubmodelReference(sm.GetReference());
                                            newEnv.AasEnv.Submodels?.Add(sm);
                                        }
                                    }
                                }

                                watch1.Stop();
                                Console.WriteLine($"{watch1.ElapsedMilliseconds} ms");

                                Program.env[i] = newEnv;
                                i++;
                            }
                        }
                    }
                }
            }

            _aasxTaskService.createCfpTree(DateTime.UtcNow);
            Program.signalNewData(2);

            Program.initializingRegistry = false;

            if (aasRegistry != null && aasRegistry.SubmodelElements != null)
            {
                Console.WriteLine($"Registry initialization complete.{aasRegistry.SubmodelElements.Count}");
            }
            else
            {
                Console.WriteLine($"Registry is empty");
            }
        }
    }

    #region PrivateMethods

    private static string TranslateURL(string? url)
    {
        // get from environment
        if (url?[..1] != "$")
        {
            return url ?? string.Empty;
        }

        var envVar = url[1..];
        url = System.Environment.GetEnvironmentVariable(envVar);
        if (url == null)
        {
            url = "";
        }

        url = url.Replace("\r", "");
        url = url.Replace("\n", "");

        return url;
    }

    void AddAasToRegistry(AdminShellPackageEnv? env, DateTime timestamp, AssetAdministrationShellDescriptor? aasDesc = null)
    {
        var aas = env?.AasEnv?.AssetAdministrationShells?[0];

        var ad = new AssetAdministrationShellDescriptor();

        if (aasDesc != null)
        {
            ad = aasDesc;
        }
        else
        {
            var globalAssetId = aas?.AssetInformation?.GlobalAssetId!;

            ad.IdShort = aas?.IdShort;
            ad.Id      = aas?.Id;
            var e = new Endpoint();
            e.ProtocolInformation = new ProtocolInformation();
            e.ProtocolInformation.Href =
                $"{Program.externalRepository}/shells/{Base64UrlEncoder.Encode(ad.Id)}";
            Console.WriteLine($"AAS {ad.IdShort} {e.ProtocolInformation.Href}");
            e.Interface  = "AAS-1.0";
            ad.Endpoints = new List<Endpoint>();
            ad.Endpoints.Add(e);
            ad.GlobalAssetId = globalAssetId;
            //
            var extSubjId       = new Reference(ReferenceTypes.ExternalReference, new List<IKey> {new Key(KeyTypes.GlobalReference, "assetKind")});
            var specificAssetId = new SpecificAssetId("assetKind", aas?.AssetInformation?.AssetKind.ToString(CultureInfo.InvariantCulture), externalSubjectId: extSubjId);
            ad.SpecificAssetIds = new List<SpecificAssetId> {specificAssetId};

            // Submodels
            if (aas?.Submodels != null && aas.Submodels.Count > 0)
            {
                ad.SubmodelDescriptors = new List<SubmodelDescriptor>();
                foreach (var sm in aas.Submodels.Select(smr => env.AasEnv.FindSubmodel(smr)))
                {
                    if (sm.IdShort == null)
                    {
                        continue;
                    }

                    var sd = new SubmodelDescriptor();
                    sd.IdShort = sm.IdShort;
                    sd.Id      = sm.Id;
                    var esm = new Models.Endpoint();
                    esm.ProtocolInformation = new ProtocolInformation();
                    esm.ProtocolInformation.Href =
                        $"{Program.externalRepository}/shells/{Base64UrlEncoder.Encode(ad.Id)}/submodels/{Base64UrlEncoder.Encode(sd.Id)}";

                    esm.Interface = "SUBMODEL-1.0";
                    sd.Endpoints  = new List<Models.Endpoint>();
                    sd.Endpoints.Add(esm);
                    if (sm.SemanticId != null)
                    {
                        var sid = sm.SemanticId.GetAsExactlyOneKey();
                        if (sid != null && sid.Value != null)
                        {
                            var semanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey> {new Key(KeyTypes.GlobalReference, sid.Value)});
                            sd.SemanticId = semanticId;
                        }
                    }

                    ad.SubmodelDescriptors.Add(sd);
                    if (!sm.IdShort.Equals("nameplate", StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    /*
                    // Add special entry for verifiable credentials
                    sd                      = new SubmodelDescriptor();
                    sd.IdShort              = "NameplateVC";
                    sd.Id                   = $"{sm.Id}_VC";
                    esm                     = new Endpoint();
                    esm.ProtocolInformation = new ProtocolInformation();
                    // TODO (jtikekar, 2023-09-04): @Andreas why hardcoded=> Verifiable credentials
                    esm.ProtocolInformation.Href = $"https://nameplate.h2894164.stratoserver.net/demo/selfdescriptiononthefly/" +
                                                   $"aHR0cHM6Ly9yZWdpc3RyeS5oMjg5NDE2NC5zdHJhdG9zZXJ2ZXIubmV0/{Base64UrlEncoder.Encode(ad.Id)}";
                    esm.Interface = "VC-1.0";
                    sd.Endpoints  = new List<Endpoint>();
                    sd.Endpoints.Add(esm);
                    if (sm.SemanticId != null)
                    {
                        var sid        = sm.SemanticId.GetAsExactlyOneKey();
                        var semanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() {new Key(KeyTypes.GlobalReference, sid.Value)});
                        sd.SemanticId = semanticId;
                    }

                    ad.SubmodelDescriptors.Add(sd);
                    */
                }
            }
        }

        // add to internal registry
        if (postRegistry.Contains("this"))
        {
            CreateAssetAdministrationShellDescriptor(ad, timestamp, true);
        }

        // POST Descriptor to Registry
        foreach (var pr in postRegistry)
        {
            if (pr == "this")
            {
                continue;
            }

            transmitDescriptor(ad, null, pr);

            if (ad.SubmodelDescriptors != null && ad.SubmodelDescriptors.Count != 0)
            {
                foreach (var sd in ad.SubmodelDescriptors)
                {
                    foreach (var psr in postSubmodelRegistry)
                    {
                        if (psr == "this")
                        {
                            continue;
                        }

                        transmitDescriptor(null, sd, psr);
                    }
                }
            }
        }
    }

    private void transmitDescriptor(AssetAdministrationShellDescriptor? ad, SubmodelDescriptor? sd, string requestPath)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string? accessToken = null;

        var json = "";
        if (ad != null)
        {
            if (!requestPath.Contains("?", StringComparison.InvariantCulture))
            {
                requestPath += "/shell-descriptors";
            }
            json = DescriptorSerializer.ToJsonObject(ad)?.ToJsonString();
        }
        else
        {
            if (sd != null)
            {
                if (!requestPath.Contains("?", StringComparison.InvariantCulture))
                {
                    requestPath += "/submodel-descriptors";
                }
                json = DescriptorSerializer.ToJsonObject(sd)?.ToJsonString();
            }
        }
        if (json == "")
        {
            return;
        }

        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = delegate { return true; } };

        if (!requestPath.Contains("localhost"))
        {
            if (_aasxTaskService.proxy != null)
            {
                handler.Proxy = _aasxTaskService.proxy;
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
            var error = false;
            var response = new HttpResponseMessage();
            try
            {
                Console.WriteLine($"POST {requestPath}");
                if (json != null)
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var task = Task.Run(async () =>
                    {
                        response = await client.PostAsync(
                                                          requestPath, content);
                    });
                    task.Wait();
                }

                error = !response.IsSuccessStatusCode;
            }
            catch
            {
                error = true;
            }

            if (error)
            {
                var r = $"ERROR POST; {response.StatusCode} ; {requestPath} ; {response.Content}";
                Console.WriteLine(r);
            }
        }

        watch.Stop();
        Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
    }

    public void CreateAssetAdministrationShellDescriptor(AssetAdministrationShellDescriptor newAasDesc, DateTime timestamp, bool initial = false)
    {
        var aasID    = newAasDesc.Id;
        var assetID  = newAasDesc.GlobalAssetId;
        var endpoint = string.Empty;
        if (newAasDesc.Endpoints != null && newAasDesc.Endpoints.Count != 0)
        {
            endpoint = newAasDesc.Endpoints[0].ProtocolInformation?.Href;
        }

        if (aasRegistry != null && _aasDescriptorWritingService.OverwriteExistingEntryForEdenticalIds(newAasDesc, aasRegistry, timestamp, initial, aasID, assetID, endpoint))
        {
            return;
        }

        if (aasRegistry != null && submodelRegistry != null)
        {
            _aasDescriptorWritingService.AddNewEntry(newAasDesc, aasRegistry, submodelRegistry, timestamp, aasID, assetID, endpoint);

            aasRegistry.SetAllParents();
        }

        Program.signalNewData(2);
    }

    public void CreateMultipleAssetAdministrationShellDescriptor(List<AssetAdministrationShellDescriptor> body, DateTime timestamp)
    {
        aasRegistry?.SubmodelElements?.Clear();
        submodelRegistry?.SubmodelElements?.Clear();
        foreach (var ad in body.OfType<AssetAdministrationShellDescriptor>())
        {
            lock (Program.changeAasxFile)
            {
                CreateAssetAdministrationShellDescriptor(ad, timestamp);
            }
        }

        Program.signalNewData(2);
    }

    #endregion
}