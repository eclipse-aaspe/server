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

using System;
using System.Collections.Generic;
using System.Globalization;
using AasxServerDB;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasxRestServerLibrary;
using AdminShellNS;
using Extensions;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Linq;

namespace AasxServer
{
    public class AasxTask
    {
        public SubmodelElementCollection def = null;
        public Property taskType = null;
        public Property cycleTime = null;
        public Property cycleCount = null;
        public Property nextCycle = null;
        public DateTime nextExecution = new DateTime();
        public int envIndex = -1;

        public static List<AasxTask> taskList = null;

        public static WebProxy proxy = null;

        public static void taskInit()
        {
            string proxyFile = "proxy.txt";
            if (System.IO.File.Exists(proxyFile))
            {
                try
                {
                    // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(proxyFile))
                    {
                        proxyFile = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("proxy.txt could not be read:");
                    Console.WriteLine(e.Message);
                }
            }

            if (System.IO.File.Exists(proxyFile))
            {
                // Test for proxy
                Console.WriteLine("Read proxyFile: " + proxyFile);
                bool error = false;
                string proxyAddress = "";
                string username = "";
                string password = "";
                try
                {
                    // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(proxyFile))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("The file " + proxyFile + " could not be read:");
                    Console.WriteLine(e.Message);
                    error = true;
                }

                if (!error)
                {
                    Console.WriteLine("Proxy: " + proxyAddress);
                    proxy = new WebProxy();
                    Uri newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    if (username != "" && password != "")
                        proxy.Credentials = new NetworkCredential(username, password);
                }
            }

            DateTime timeStamp = DateTime.UtcNow;
            taskList = new List<AasxTask>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[ i ];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[ 0 ];
                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var smr in aas.Submodels)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null && sm.IdShort.ToLower().Contains("tasks"))
                            {
                                sm.SetTimeStamp(timeStamp);
                                int countSme = 0;
                                if (sm.SubmodelElements != null)
                                    countSme = sm.SubmodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.SubmodelElements[ iSme ];
                                    if (sme is SubmodelElementCollection smec)
                                    {
                                        var nextTask = new AasxTask();
                                        AasxTask.taskList.Add(nextTask);
                                        nextTask.def = smec;
                                        nextTask.envIndex = i;

                                        int countSmec = smec.Value.Count;
                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.Value[ iSmec ];
                                            var idShort = sme2.IdShort.ToLower();

                                            switch (idShort)
                                            {
                                                case "tasktype":
                                                    if (sme2 is Property)
                                                    {
                                                        nextTask.taskType = sme2 as Property;
                                                    }

                                                    break;
                                                case "cycletime":
                                                    if (sme2 is Property)
                                                    {
                                                        nextTask.cycleTime = sme2 as Property;
                                                    }

                                                    break;
                                                case "cyclecount":
                                                    if (sme2 is Property)
                                                    {
                                                        nextTask.cycleCount = sme2 as Property;
                                                    }

                                                    break;
                                                case "nextcycle":
                                                    if (sme2 is Property)
                                                    {
                                                        nextTask.nextCycle = sme2 as Property;
                                                    }

                                                    break;
                                            }
                                        }

                                        if (nextTask.taskType?.Value.ToLower() == "init")
                                            runOperations(nextTask.def, nextTask.envIndex, timeStamp);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            tasksThread = new Thread(new ThreadStart(tasksSamplingLoop));
            // MICHA
            tasksThread.Start();
        }

        static void runOperations(SubmodelElementCollection smec, int envIndex, DateTime timeStamp)
        {
            int countSmec = smec.Value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.Value[ iSmec ];

                if (sme2 is Operation op)
                {
                    var idShort = sme2.IdShort.ToLower();
                    switch (idShort)
                    {
                        case "authenticate":
                            operation_authenticate(op, envIndex, timeStamp);
                            break;
                        case "get":
                        case "getdiff":
                        case "put":
                        case "putdiff":
                            operation_get_put(op, envIndex, timeStamp);
                            break;
                        case "limitcount":
                            operation_limitCount(op, envIndex, timeStamp);
                            break;
                        case "calculatecfp":
                        case "calculate_cfp":
                            operation_calculate_cfp(op, envIndex, timeStamp);
                            break;
                        default:
                            if (idShort.Substring(0, 3) == "get")
                            {
                                operation_get_put(op, envIndex, timeStamp);
                            }

                            break;
                    }
                }
            }
        }

        static void operation_authenticate(Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection

            if (op.InputVariables.Count != 1)
            {
                return;
            }

            Property authType = null;
            Property authServerEndPoint = null;
            AasCore.Aas3_0.File authServerCertificate = null;
            AasCore.Aas3_0.File clientCertificate = null;
            Property clientCertificatePassWord = null;
            Property accessToken = null;
            Property userName = null;
            Property passWord = null;
            Property clientToken = null;

            var smec = new SubmodelElementCollection();
            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (!(inputRef is ReferenceElement))
                    return;
                var refElement = Program.env[ envIndex ].AasEnv.FindReferableByReference((inputRef as ReferenceElement).Value);
                if (refElement is SubmodelElementCollection re)
                    smec = re;
            }

            int countSmec = smec.Value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.Value[ iSmec ];
                var idShort = sme2.IdShort.ToLower();

                switch (idShort)
                {
                    case "authtype":
                        if (sme2 is Property)
                        {
                            authType = sme2 as Property;
                        }

                        break;
                    case "authserverendpoint":
                        if (sme2 is Property)
                        {
                            authServerEndPoint = sme2 as Property;
                        }

                        break;
                    case "accesstoken":
                        if (sme2 is Property)
                        {
                            accessToken = sme2 as Property;
                        }

                        break;
                    case "clienttoken":
                        if (sme2 is Property)
                        {
                            clientToken = sme2 as Property;
                        }

                        break;
                    case "username":
                        if (sme2 is Property)
                        {
                            userName = sme2 as Property;
                        }

                        break;
                    case "password":
                        if (sme2 is Property)
                        {
                            passWord = sme2 as Property;
                        }

                        break;
                    case "authservercertificate":
                        if (sme2 is AasCore.Aas3_0.File)
                        {
                            authServerCertificate = sme2 as AasCore.Aas3_0.File;
                        }

                        break;
                    case "clientcertificate":
                        if (sme2 is AasCore.Aas3_0.File)
                        {
                            clientCertificate = sme2 as AasCore.Aas3_0.File;
                        }

                        break;
                    case "clientcertificatepassword":
                        if (sme2 is Property)
                        {
                            clientCertificatePassWord = sme2 as Property;
                        }

                        break;
                }
            }

            if (authType != null)
            {
                switch (authType.Value.ToLower())
                {
                    case "openid":
                        if (authServerEndPoint != null && authServerCertificate != null && clientCertificate != null
                            && accessToken != null)
                        {
                            if (accessToken.Value == null)
                                accessToken.Value = "";

                            if (accessToken.Value != "")
                            {
                                bool valid = true;
                                var jwtToken = new JwtSecurityToken(accessToken.Value);
                                if ((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow))
                                    valid = false;
                                if (valid) return;
                                accessToken.Value = "";
                            }

                            if (createAccessToken(envIndex, authServerEndPoint, authServerCertificate,
                                    clientCertificate, clientCertificatePassWord,
                                    accessToken, clientToken))
                                accessToken.SetTimeStamp(timeStamp);
                        }

                        break;
                }
            }
        }

        static bool createAccessToken(int envIndex, Property authServerEndPoint, AasCore.Aas3_0.File authServerCertificate,
            AasCore.Aas3_0.File clientCertificate, Property clientCertificatePassWord,
            Property accessToken, Property clientToken,
            string policy = "", string policyRequestedResource = "")
        {
            var handler = new HttpClientHandler();

            if (proxy != null)
                handler.Proxy = proxy;
            else
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);
            DiscoveryDocumentResponse disco = null;

            if (authServerEndPoint == null)
                return false;

            client.Timeout = TimeSpan.FromSeconds(20);
            Task task;

            Stream s = null;
            try
            {
                s = AasxServer.Program.env[ envIndex ].GetLocalStreamFromPackage(authServerCertificate.Value, access: FileAccess.Read);
            }
            catch
            {

            }

            if (s == null)
            {
                Console.WriteLine("Stream error!");
                return false;
            }

            using (var m = new System.IO.MemoryStream())
            {
                s.CopyTo(m);
                var b = m.GetBuffer();
                var serverCert = new X509Certificate2(b);
                Console.WriteLine("Auth server certificate: " + authServerCertificate.Value);
                s.Close();
            }

            string[] x5c = null;
            X509Certificate2 certificate = null;
            string certificatePassword = clientCertificatePassWord.Value;
            Stream s2 = null;
            try
            {
                s2 = AasxServer.Program.env[ envIndex ].GetLocalStreamFromPackage(clientCertificate.Value, access: FileAccess.Read);
            }
            catch
            {

            }

            if (s2 == null)
            {
                Console.WriteLine("Stream error!");
                return false;
            }

            X509Certificate2Collection xc = new X509Certificate2Collection();
            using (var m = new System.IO.MemoryStream())
            {
                s2.CopyTo(m);
                var b = m.GetBuffer();
                xc.Import(b, certificatePassword, X509KeyStorageFlags.PersistKeySet);
                certificate = new X509Certificate2(b, certificatePassword);
                Console.WriteLine("Client certificate: " + clientCertificate.Value);
                s2.Close();
            }

            // get new access token
            if (policy == "" && accessToken.Value == "")
            {
                task = Task.Run(async () => { disco = await client.GetDiscoveryDocumentAsync(authServerEndPoint.Value); });
                task.Wait();
                if (disco.IsError) return false;
                Console.WriteLine("Get OpenID Discovery JSON");
                // Console.WriteLine(disco.Raw);

                string[] X509Base64 = new string[xc.Count];

                int j = xc.Count;
                var xce = xc.GetEnumerator();
                for (int i = 0; i < xc.Count; i++)
                {
                    xce.MoveNext();
                    X509Base64[ --j ] = Convert.ToBase64String(xce.Current.GetRawCertData());
                    // X509Base64[ --j ] = Base64UrlEncoder.Encode(xce.Current.GetRawCertData());
                }
                x5c = X509Base64;

                var credential = new X509SigningCredentials(certificate);
                string clientId = "client.jwt";
                string email = "";
                string subject = certificate.Subject;
                var split = subject.Split(new Char[] { ',' });
                if (split[ 0 ] != "")
                {
                    var split2 = split[ 0 ].Split(new Char[] { '=' });
                    if (split2[ 0 ] == "E")
                    {
                        email = split2[ 1 ];
                    }
                }

                Console.WriteLine("email: " + email);

                var now = DateTime.UtcNow;
                var claimList =
                    new List<Claim>()
                    {
                        new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                        new Claim(JwtClaimTypes.Subject, clientId),
                        new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                        // OZ
                        new Claim(JwtClaimTypes.Email, email),
                    };
                if (policy != "")
                    claimList.Add(new Claim("policy", policy, ClaimValueTypes.String));
                if (policyRequestedResource != "")
                    claimList.Add(new Claim("policyRequestedResource", policyRequestedResource, ClaimValueTypes.String));
                var token = new JwtSecurityToken(
                        clientId,
                        disco.TokenEndpoint,
                        claimList,
                        now,
                        now.AddMinutes(1),
                        credential
                    );

                token.Header.Add("x5c", x5c);
                var tokenHandler = new JwtSecurityTokenHandler();
                string clientLongToken = tokenHandler.WriteToken(token);

                TokenResponse response = null;
                // client.Timeout = TimeSpan.FromSeconds(20);
                task = Task.Run(async () =>
                {
                    response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                    {
                        Address = disco.TokenEndpoint,
                        Scope = "resource1.scope1",

                        ClientAssertion =
                        {
                            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                            Value = clientLongToken
                        }
                    });
                });
                task.Wait();

                if (response.IsError) return false;

                var t = response.AccessToken;

                // create clientToken
                if (t != null)
                {
                    accessToken.Value = t;
                }

                return true;
            }

            // create client token
            if (policy != "" && accessToken.Value != "" && clientToken.Value == "")
            {
                var parsed = JObject.Parse(Jose.JWT.Payload(accessToken.Value));

                string userName = "";
                try
                {
                    userName = parsed.SelectToken("userName").Value<string>();
                }
                catch
                {

                }

                string expires = "";
                try
                {
                    expires = parsed.SelectToken("exp").Value<string>();
                }
                catch
                {

                }

                if (userName != "" && expires != "")
                {
                    var          credential = new X509SigningCredentials(certificate);
                    const string clientId   = "client.jwt";
                    var          now        = DateTime.UtcNow;
                    var claimList =
                        new List<Claim>()
                        {
                            new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                            new Claim(JwtClaimTypes.Subject, clientId),
                            new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),

                            new Claim("userName", userName),
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
                    var t = tokenHandler.WriteToken(token);
                    if (t != null)
                    {
                        clientToken.Value = t;
                    }
                }

                return true;
            }

            return false;
        }

        static void operation_get_put(Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection
            // inputVariable sourceEndPoint: property
            // inputVariable  sourcePath: property
            // inputVariable reference destinationElement: collection
            // inputVariable HEAD: property

            string opName = op.IdShort.ToLower();
            if (opName.Substring(0, 4) == "get-")
                opName = "get";

            SubmodelElementCollection authentication = null;
            Property authType = null;
            Property authServerEndPoint = null;
            Property accessToken = null;
            Property userName = null;
            Property passWord = null;
            AasCore.Aas3_0.File authServerCertificate = null;
            AasCore.Aas3_0.File clientCertificate = null;
            Property clientCertificatePassWord = null;
            Property clientToken = null;

            Property endPoint = null;
            Property path = null;
            SubmodelElementCollection elementCollection = null;
            Submodel elementSubmodel = null;
            Property steps = null;
            Property loop = null;
            Property duration = null;

            Property lastDiff = null;
            Property status = null;
            Property mode = null;

            SubmodelElementCollection smec = null;
            Submodel sm = null;
            Property p = null;

            HttpClient client = null;
            HttpClientHandler handler = null;

            foreach (var input in op.InputVariables)
            {
                smec = null;
                sm = null;
                p = null;
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    p = (inputRef as Property);
                }

                if (inputRef is ReferenceElement)
                {
                    var refElement = Program.env[ envIndex ].AasEnv.FindReferableByReference((inputRef as ReferenceElement).Value);
                    if (refElement is SubmodelElementCollection)
                        smec = refElement as SubmodelElementCollection;
                    if (refElement is Submodel)
                        sm = refElement as Submodel;
                }

                switch (inputRef.IdShort.ToLower())
                {
                    case "authentication":
                        if (smec != null)
                            authentication = smec;
                        break;
                    case "endpoint":
                        if (p != null)
                            endPoint = p;
                        break;
                    case "path":
                        if (p != null)
                            path = p;
                        break;
                    case "element":
                        if (smec != null)
                            elementCollection = smec;
                        if (sm != null)
                            elementSubmodel = sm;
                        break;
                    case "mode":
                        if (p != null)
                            mode = p;
                        break;
                    case "steps":
                        if (p != null)
                            steps = p;
                        break;
                    case "loop":
                        if (p != null)
                            loop = p;
                        break;
                }
            }

            foreach (var output in op.OutputVariables)
            {
                smec = null;
                sm = null;
                p = null;
                var outputRef = output.Value;
                if (outputRef is Property)
                {
                    p = (outputRef as Property);
                }

                if (outputRef is ReferenceElement)
                {
                    var refElement = Program.env[ envIndex ].AasEnv.FindReferableByReference((outputRef as ReferenceElement).Value);
                    if (refElement is SubmodelElementCollection)
                        smec = refElement as SubmodelElementCollection;
                    if (refElement is Submodel)
                        sm = refElement as Submodel;
                }

                switch (outputRef.IdShort.ToLower())
                {
                    case "lastdiff":
                        if (p != null)
                            lastDiff = p;
                        break;
                    case "status":
                        if (p != null)
                            status = p;
                        break;
                    case "duration":
                        if (p != null)
                            duration = p;
                        break;
                }
            }

            if (authentication != null)
            {
                smec = authentication;
                int countSmec = smec.Value.Count;
                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                {
                    var sme2 = smec.Value[ iSmec ];
                    var idShort = sme2.IdShort.ToLower();

                    switch (idShort)
                    {
                        case "authtype":
                            if (sme2 is Property)
                            {
                                authType = sme2 as Property;
                            }

                            break;

                        case "accesstoken":
                            if (sme2 is Property)
                            {
                                accessToken = sme2 as Property;
                            }

                            break;

                        case "clienttoken":
                            if (sme2 is Property)
                            {
                                clientToken = sme2 as Property;
                            }

                            break;

                        case "username":
                            if (sme2 is Property)
                            {
                                userName = sme2 as Property;
                            }

                            break;

                        case "password":
                            if (sme2 is Property)
                            {
                                passWord = sme2 as Property;
                            }

                            break;

                        case "authservercertificate":
                            if (sme2 is AasCore.Aas3_0.File)
                            {
                                authServerCertificate = sme2 as AasCore.Aas3_0.File;
                            }

                            break;

                        case "authserverendpoint":
                            if (sme2 is Property)
                            {
                                authServerEndPoint = sme2 as Property;
                            }

                            break;

                        case "clientcertificate":
                            if (sme2 is AasCore.Aas3_0.File)
                            {
                                clientCertificate = sme2 as AasCore.Aas3_0.File;
                            }

                            break;

                        case "clientcertificatepassword":
                            if (sme2 is Property)
                            {
                                clientCertificatePassWord = sme2 as Property;
                            }

                            break;
                    }
                }
            }

            int loopCount = 1;
            if (loop != null)
            {
                loopCount = Convert.ToInt32(loop.Value);
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();

            for (int l = 0; l < loopCount; l++)
            {
                if (loop != null)
                {
                    loop.Value = l + 1 + "";
                    loop.SetTimeStamp(timeStamp);
                }

                if (duration != null)
                {
                    duration.Value = watch.ElapsedMilliseconds + " ms";
                    duration.TimeStamp = timeStamp;
                }

                Program.signalNewData(0);

                string requestPath = endPoint.Value;
                if (path.Value != "")
                    requestPath += "/" + path.Value;
                HttpResponseMessage response = null;
                Task task = null;
                string diffPath = "";
                var splitPath = path.Value.Split('/');
                string aasPath = splitPath[ 0 ];
                string subPath = "";
                int i = 1;
                string pre = "";
                while (i < splitPath.Length)
                {
                    subPath += pre + splitPath[ i ];
                    pre = ".";
                    i++;
                }

                if (status != null)
                {
                    status.Value = "OK";
                    status.TimeStamp = timeStamp;
                }

                if (opName == "get" || opName == "getdiff")
                {
                    // if (splitPath.Length < 2)
                    // return;

                    DateTime last = new DateTime();
                    if (opName == "getdiff")
                    {
                        if (lastDiff == null)
                            continue;
                        if (elementCollection == null)
                            continue;

                        if (lastDiff.Value == "")
                        {
                            opName = "get";
                            requestPath = endPoint.Value + "/aas/" + aasPath +
                                          "/submodels/" + splitPath[ 1 ] + "/elements";
                            i = 2;
                            while (i < splitPath.Length)
                            {
                                requestPath += "/" + splitPath[ i ];
                                i++;
                            }
                            requestPath += "/complete";
                        }
                        else
                        {
                            last = DateTime.Parse(lastDiff.Value).ToUniversalTime();
                            requestPath = endPoint.Value +
                                          "/diffjson/aas/" + splitPath[ 0 ] +
                                          "?path=" + subPath;
                            requestPath += "."; // to avoid wrong data by prefix only
                            requestPath += "&time=" + lastDiff.Value;
                        }
                    }

                    handler = new HttpClientHandler();

                    if (!requestPath.Contains("localhost"))
                    {
                        if (proxy != null)
                            handler.Proxy = proxy;
                        else
                            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    }

                    client = new HttpClient(handler);
                    client.Timeout = TimeSpan.FromSeconds(20);

                    string policy = "";
                    string policyRequestedResource = "";
                    // test, if usage policy is needed
                    if (steps != null && steps.Value != "")
                    {
                        if (steps.Value.Contains("11") || (accessToken != null && accessToken.Value == ""))
                        {
                            accessToken.Value = "";

                            if (!createAccessToken(envIndex, authServerEndPoint, authServerCertificate,
                                    clientCertificate, clientCertificatePassWord,
                                    accessToken, clientToken,
                                    "", ""))
                                continue;
                            accessToken.TimeStamp = timeStamp;
                            Console.WriteLine("Create Token1");
                        }

                        if (steps.Value.Contains("1"))
                        {
                            client.SetBearerToken(accessToken.Value);
                        }

                        if (steps.Value.Contains("H"))
                        {
                            try
                            {
                                task = Task.Run(async () => { response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, requestPath)); });
                                task.Wait();
                                if (!response.IsSuccessStatusCode)
                                {
                                    if (status != null)
                                    {
                                        status.Value = response.StatusCode.ToString() + " ; " +
                                                       response.Content.ReadAsStringAsync().Result + " ; " +
                                                       "HEAD " + requestPath;
                                        status.TimeStamp = timeStamp;
                                        Program.signalNewData(0);
                                    }

                                    continue;
                                }

                                foreach (var kvp in response.Headers)
                                {
                                    if (kvp.Key == "policy")
                                        policy = kvp.Value.FirstOrDefault();
                                    if (kvp.Key == "policyRequestedResource")
                                        policyRequestedResource = kvp.Value.FirstOrDefault();
                                }

                                if (policy == "" || policyRequestedResource == "")
                                {
                                    Console.WriteLine("HEAD: No policy!");
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        if ((steps.Value.Contains("22") && steps.Value.Contains("1H"))
                            || (clientToken != null && clientToken.Value == ""))
                        {
                            clientToken.Value = "";
                            if (!createAccessToken(envIndex, authServerEndPoint, authServerCertificate,
                                    clientCertificate, clientCertificatePassWord,
                                    accessToken, clientToken,
                                    policy, policyRequestedResource))
                                continue;
                            clientToken.TimeStamp = timeStamp;
                            Console.WriteLine("Create Token2");
                        }

                        if (steps.Value.Contains("2"))
                        {
                            client.SetBearerToken(clientToken.Value);
                        }
                    }

                    if (steps.Value.Contains("G"))
                    {
                        try
                        {
                            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestPath))
                            {
                                task = Task.Run(async () => { response = await client.SendAsync(requestMessage); });
                                task.Wait();
                                if (!response.IsSuccessStatusCode)
                                {
                                    if (status != null)
                                    {
                                        status.Value = response.StatusCode.ToString() + " ; " +
                                                       response.Content.ReadAsStringAsync().Result + " ; " +
                                                       "GET " + requestPath;
                                        status.TimeStamp = timeStamp;
                                        Program.signalNewData(0);
                                    }

                                    continue;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }

                        string json = response.Content.ReadAsStringAsync().Result;
                        SubmodelElementCollection receiveCollection = null;
                        Submodel receiveSubmodel = null;
                        try
                        {
                            if (opName == "get")
                            {
                                if (elementCollection != null)
                                {
                                    JObject parsed = JObject.Parse(json);
                                    foreach (JProperty jp1 in (JToken) parsed)
                                    {
                                        if (jp1.Name == "elem")
                                        {
                                            string text = jp1.Value.ToString();
                                            receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<SubmodelElementCollection>(
                                                text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                            elementCollection.Value = receiveCollection.Value;
                                            elementCollection.SetAllParentsAndTimestamps(elementCollection, timeStamp, elementCollection.TimeStampCreate);
                                            elementCollection.SetTimeStamp(timeStamp);
                                        }
                                    }
                                }

                                if (elementSubmodel != null)
                                {
                                    // receiveSubmodel = Newtonsoft.Json.JsonConvert.DeserializeObject<Submodel>(
                                    //    json, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                    JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                    receiveSubmodel = Jsonization.Deserialize.SubmodelFrom(node);

                                    receiveSubmodel.SetTimeStamp(timeStamp);
                                    receiveSubmodel.SetAllParents(timeStamp);

                                    // need id for idempotent behaviour
                                    if (receiveSubmodel.Id == null /*|| receiveSubmodel.Id != elementSubmodel.Id*/)
                                        continue;
                                    receiveSubmodel.Id = elementSubmodel.Id;

                                    var aas = Program.env[ envIndex ].AasEnv.FindAasWithSubmodelId(elementSubmodel.Id);

                                    // datastructure update
                                    if (Program.env == null || Program.env[ envIndex ].AasEnv == null /*|| Program.env[ envIndex ].AasEnv.Assets == null*/)
                                        continue;

                                    // add Submodel
                                    var existingSm = Program.env[ envIndex ].AasEnv.FindSubmodelById(elementSubmodel.Id);
                                    if (existingSm != null)
                                        Program.env[ envIndex ].AasEnv.Submodels.Remove(existingSm);
                                    Program.env[ envIndex ].AasEnv.Submodels.Add(receiveSubmodel);
                                    for (int s = 0; s < aas.Submodels.Count; s++)
                                    {
                                        if (aas.Submodels[ s ].Keys[ 0 ].Value == existingSm.Id)
                                        {
                                            aas.Submodels.RemoveAt(s);
                                            break;
                                        }
                                    }

                                    aas.Submodels.Add(receiveSubmodel.GetModelReference());

                                    continue;
                                }
                            }

                            if (opName == "getdiff")
                            {
                                // lastDiff.Value = "" + timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                                List<AasxRestServer.TestResource.diffEntry> diffList = new List<AasxRestServer.TestResource.diffEntry>();
                                diffList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AasxRestServer.TestResource.diffEntry>>(json);
                                foreach (var d in diffList)
                                {
                                    if (d.type == "SMEC")
                                    {
                                        if (d.path.Length > subPath.Length && subPath == d.path.Substring(0, subPath.Length))
                                        {
                                            switch (d.mode)
                                            {
                                                case "CREATE":
                                                case "UPDATE":
                                                    splitPath = d.path.Split('.');
                                                    if (splitPath.Length < 2)
                                                        continue;
                                                    requestPath = endPoint.Value + "/aas/" + aasPath +
                                                                  "/submodels/" + splitPath[ 0 ] + "/elements";
                                                    i = 1;
                                                    while (i < splitPath.Length)
                                                    {
                                                        requestPath += "/" + splitPath[ i ];
                                                        i++;
                                                    }

                                                    requestPath += "/complete";
                                                    try
                                                    {
                                                        task = Task.Run(async () => { response = await client.GetAsync(requestPath, HttpCompletionOption.ResponseHeadersRead); });
                                                        task.Wait();
                                                        if (!response.IsSuccessStatusCode)
                                                        {
                                                            if (status != null)
                                                            {
                                                                status.Value = response.StatusCode.ToString() + " ; " +
                                                                               response.Content.ReadAsStringAsync().Result + " ; " +
                                                                               "GET " + requestPath;
                                                                Program.signalNewData(0);
                                                            }

                                                            continue;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        continue;
                                                    }

                                                    json = response.Content.ReadAsStringAsync().Result;
                                                    JObject parsed = JObject.Parse(json);
                                                    foreach (JProperty jp1 in (JToken) parsed)
                                                    {
                                                        if (jp1.Name == "elem")
                                                        {
                                                            string text = jp1.Value.ToString();
                                                            receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<SubmodelElementCollection>(
                                                                text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                                            break;
                                                        }
                                                    }

                                                    bool found = false;
                                                    foreach (var smew in elementCollection.Value)
                                                    {
                                                        var sme = smew;
                                                        if (sme.IdShort == receiveCollection.IdShort)
                                                        {
                                                            if (sme is SubmodelElementCollection smc)
                                                            {
                                                                if (d.mode == "UPDATE")
                                                                {
                                                                    smc.Value = receiveCollection.Value;
                                                                    smc.SetAllParentsAndTimestamps(elementCollection, timeStamp, elementCollection.TimeStampCreate);
                                                                    smc.SetTimeStamp(timeStamp);
                                                                }
                                                                found = true;
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    if (!found && d.mode == "CREATE")
                                                    {
                                                        elementCollection.Value.Add(receiveCollection);
                                                        receiveCollection.SetAllParentsAndTimestamps(elementCollection, timeStamp, timeStamp);
                                                        receiveCollection.SetTimeStamp(timeStamp);
                                                    }

                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (lastDiff != null)
                        lastDiff.Value = "" + timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    Program.signalNewData(0);
                }

                handler = new HttpClientHandler();

                if (!requestPath.Contains("localhost") && !requestPath.Contains("192.168."))
                {
                    if (proxy != null)
                        handler.Proxy = proxy;
                    else
                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                }

                client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(20);
                if (accessToken != null)
                    client.SetBearerToken(accessToken.Value);

                if (opName == "put" || opName == "putdiff")
                {
                    SubmodelElementCollection diffCollection = null;
                    DateTime last = new DateTime();
                    int count = 1;
                    if (mode != null && mode.Value == "clear")
                        opName = "put";
                    if (opName == "putdiff")
                    {
                        if (lastDiff == null)
                            continue;
                        if (elementCollection == null)
                            continue;
                        diffCollection = elementCollection;
                        count = elementCollection.Value.Count;
                        if (lastDiff.Value == "")
                        {
                            // get "latestData" from server
                            bool error = false;
                            splitPath = path.Value.Split('/');
                            /*
                            requestPath = endPoint.Value + "/aas/" + splitPath[ 1 ] +
                                "/submodels/" + splitPath[3];
                            requestPath += "/elements/" + elementCollection.IdShort;
                            requestPath += "/latestData/complete";
                            */
                            try
                            {
                                task = Task.Run(async () => { response = await client.GetAsync(requestPath + ".latestData", HttpCompletionOption.ResponseHeadersRead); });
                                task.Wait();
                                if (!response.IsSuccessStatusCode)
                                {
                                    if (status != null)
                                    {
                                        status.Value = response.StatusCode.ToString() + " ; " +
                                                       response.Content.ReadAsStringAsync().Result + " ; " +
                                                       "GET " + requestPath;
                                    }

                                    error = true;
                                }
                            }
                            catch
                            {
                                error = true;
                            }

                            int highDataIndex = -1;
                            int lowDataIndex = 0;
                            int totalSamples = 0;
                            if (!error)
                            {
                                try
                                {
                                    string json = response.Content.ReadAsStringAsync().Result;
                                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                    JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                    var receiveCollection = Jsonization.Deserialize.SubmodelElementCollectionFrom(node);

                                    // JObject parsed = JObject.Parse(json);
                                    // foreach (JProperty jp1 in (JToken) parsed)
                                    {
                                        // if (jp1.Name == "elem")
                                        {
                                            // string text = jp1.Value.ToString();
                                            // var receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<SubmodelElementCollection>(
                                            //    text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                            foreach (var sme in receiveCollection.Value)
                                            {
                                                var e = sme;
                                                if (e is Property ep)
                                                {
                                                    if (ep.IdShort == "highDataIndex")
                                                    {
                                                        highDataIndex = Convert.ToInt32(ep.Value);
                                                        lowDataIndex = highDataIndex + 1;
                                                    }

                                                    if (ep.IdShort == "totalSamples")
                                                    {
                                                        totalSamples = Convert.ToInt32(ep.Value);
                                                    }
                                                }
                                            }

                                            if (elementCollection.Value.Count == 1)
                                            {
                                                if (elementCollection.Value[ 0 ] is SubmodelElementCollection smc)
                                                {
                                                    if (smc.IdShort == "latestData")
                                                    {
                                                        if (smc.Value.Count == 0)
                                                            continue;

                                                        foreach (var sme in smc.Value)
                                                        {
                                                            var e = sme;
                                                            if (e is Property ep)
                                                            {
                                                                if (ep.IdShort == "highDataIndex")
                                                                {
                                                                    ep.Value = highDataIndex.ToString();
                                                                }

                                                                if (ep.IdShort == "lowDataIndex")
                                                                {
                                                                    ep.Value = lowDataIndex.ToString();
                                                                }

                                                                if (ep.IdShort == "totalSamples")
                                                                {
                                                                    ep.Value = totalSamples.ToString();
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    error = true;
                                }
                            }

                            if (error || highDataIndex == -1)
                            {
                                opName = "put";
                            }
                            else
                            {
                                if (lastDiff != null)
                                    lastDiff.Value = "" + timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                            }
                        }
                        else
                        {
                            last = DateTime.Parse(lastDiff.Value).ToUniversalTime();
                        }
                    }

                    for (i = 0; i < count; i++)
                    {
                        bool error = false;
                        string statusValue = "";
                        string json = "";
                        try
                        {
                            if (opName == "putdiff")
                            {
                                var sme = diffCollection.Value[ i ];
                                if (!(sme is SubmodelElementCollection))
                                    continue;
                                elementCollection = sme as SubmodelElementCollection;
                                diffPath = "." + elementCollection.IdShort;
                                if (elementCollection.TimeStamp <= last)
                                    elementCollection = null;
                            }

                            if (elementCollection != null)
                            {
                                var j = Jsonization.Serialize.ToJsonObject(elementCollection);
                                json = j.ToJsonString();
                            }

                            if (elementSubmodel != null)
                            {
                                var j = Jsonization.Serialize.ToJsonObject(elementSubmodel);
                                json = j.ToJsonString();
                            }
                        }
                        catch
                        {
                            statusValue = "error PUTDIFF index: " + i + " old " + count +
                                          " new " + diffCollection.Value.Count;
                            Console.WriteLine(statusValue);
                            error = true;
                        }

                        if (json != "")
                        {
                            try
                            {
                                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                                if (opName == "put" || (elementCollection != null && elementCollection.IdShort == "latestData"))
                                {
                                    task = Task.Run(async () =>
                                    {
                                        response = await client.PutAsync(
                                            requestPath + diffPath, content);
                                    });
                                }
                                else if (opName == "putdiff")
                                {
                                    task = Task.Run(async () =>
                                    {
                                        response = await client.PostAsync(
                                            requestPath, content);
                                    });
                                }

                                task.Wait();
                            }
                            catch
                            {
                                error = true;
                            }

                            if (error || !response.IsSuccessStatusCode)
                            {
                                if (response != null)
                                {
                                    statusValue = response.StatusCode.ToString() + " ; " +
                                                  response.Content.ReadAsStringAsync().Result + " ; " +
                                                  "PUT " + requestPath;
                                }
                                error = true;
                            }
                        }

                        if (error)
                        {
                            if (status != null)
                            {
                                status.Value = statusValue;
                                Program.signalNewData(0);
                            }

                            continue;
                        }
                    }

                    if (lastDiff != null)
                        lastDiff.Value = "" + timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                }
            }

            if (loop != null)
            {
                loop.Value = loopCount + "";
                loop.TimeStamp = timeStamp;
            }

            watch.Stop();
            if (duration != null)
            {
                duration.Value = watch.ElapsedMilliseconds + " ms";
                duration.TimeStamp = timeStamp;
            }

            Program.signalNewData(2); // new tree, nodes opened
        }

        static void operation_limitCount(Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference collection: collection
            // inputVariable limit: property
            // inputVariable prefix: property

            if (op.InputVariables.Count != 3)
            {
                return;
            }

            SubmodelElementCollection collection = null;
            Property limit = null;
            Property prefix = null;
            SubmodelElementCollection smec = null;
            Property p = null;

            foreach (var input in op.InputVariables)
            {
                smec = null;
                p = null;
                var inputRef = input.Value;
                if (inputRef is Property)
                {
                    p = (inputRef as Property);
                }

                if (inputRef is ReferenceElement)
                {
                    var refElement = Program.env[ envIndex ].AasEnv.FindReferableByReference((inputRef as ReferenceElement).Value);
                    if (refElement is SubmodelElementCollection)
                        smec = refElement as SubmodelElementCollection;
                }

                switch (inputRef.IdShort.ToLower())
                {
                    case "collection":
                        if (smec != null)
                            collection = smec;
                        break;
                    case "limit":
                        if (p != null)
                            limit = p;
                        break;
                    case "prefix":
                        if (p != null)
                            prefix = p;
                        break;
                }
            }

            if (collection == null || limit == null || prefix == null)
                return;

            try
            {
                int count = Convert.ToInt32(limit.Value);
                string pre = prefix.Value;
                int preCount = 0;
                int i = 0;
                while (i < collection.Value.Count)
                {
                    if (pre == collection.Value[ i ].IdShort.Substring(0, pre.Length))
                        preCount++;
                    i++;
                }

                i = 0;
                while (preCount > count && i < collection.Value.Count)
                {
                    if (pre == collection.Value[ i ].IdShort.Substring(0, pre.Length))
                    {
                        IReferable r = collection.Value[ i ];
                        var sm = r.GetParentSubmodel();
                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                            r, "Remove", sm, (ulong)timeStamp.Ticks);
                        collection.Value.RemoveAt(i);
                        preCount--;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch
            {

            }

            Program.signalNewData(1);
        }

        public class cfpNode
        {
            public int envIndex = -1;
            public string asset = null;
            public AssetAdministrationShell aas = null;
            public Property cradleToGateModule = null;
            public Property productionModule = null;
            public Property distributionModule = null;
            public Property cradleToGateCombination = null;
            public Property productionCombination = null;
            public Property distributionCombination = null;
            public Property weightModule = null;
            public Property weightCombination = null;
            public AasCore.Aas3_0.File manufacturerLogo = null;
            public AasCore.Aas3_0.File productImage = null;
            public string productDesignation = "";
            public List<string> bom = new List<string>();
            public DateTime bomTimestamp = new DateTime();
            public List<cfpNode> children = new List<cfpNode>();
            public int iChild = 0;
        }

        public static cfpNode root = null;
        public static string asbuilt_total = null;
        public static bool cfpValid = false;
        public static DateTime lastCreateTimestamp = new DateTime();
        public static bool credentialsChanged = false;

        public static void resetTimeStamp()
        {
            lastCreateTimestamp = new DateTime();
            credentialsChanged = true;
        }

        static string cleanupIdShort(String text)
        {
            if (text.Contains(" - EXTERNAL"))
                text = text.Replace(" - EXTERNAL", "");
            if (text.Contains(" - NO ACCESS"))
                text = text.Replace(" - NO ACCESS", "");
            if (text.Contains(" - COPY"))
                text = text.Replace(" - COPY", "");
            return text;
        }

        public static string hashBOM = "";
        public static long logCount = 0;
        public static long logCountModulo = 30;

        public static bool createCfpTree(int envIndex, DateTime timeStamp)
        {
            bool changed = false;
            string digest = "";
            cfpValid = true;

            if (logCount % logCountModulo == 0)
            {
                Console.WriteLine();
            }
            else
            {
                Console.Write(logCount % logCountModulo + " ");
            }

            // GET actual BOM
            AdminShellPackageEnv env = null;
            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                env = AasxServer.Program.env[ i ];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[ 0 ];
                    // if (aas.IdShort != "ZveiControlCabinetAas - EXTERNAL")
                    //    continue;

                    Submodel newsm = null;
                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        // foreach (var smr in aas.Submodels)
                        for (int j = 0; j < aas.Submodels.Count; j++)
                        {
                            var smr = aas.Submodels[ j ];
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null)
                            {
                                if (sm.IdShort.Contains("BillOfMaterial"))
                                {
                                    if (sm.Extensions != null && sm.Extensions.Count != 0 && sm.Extensions[ 0 ].Name == "endpoint")
                                    {
                                        var requestPath = sm.Extensions[0].Value;

                                        string queryPara = "";
                                        string userPW = "";
                                        string urlEdcWrapper = "";
                                        string replace = "";
                                        if (AasxCredentials.get(cs.credentials, requestPath, out queryPara, out userPW, out urlEdcWrapper, out replace))
                                        {
                                            if (replace != "")
                                                requestPath = replace;
                                            if (queryPara != "")
                                                queryPara = "?" + queryPara;
                                            if (urlEdcWrapper != "")
                                                requestPath = urlEdcWrapper;
                                        }

                                        var handler = new HttpClientHandler()
                                        {
                                            ServerCertificateCustomValidationCallback = delegate { return true; },
                                        };

                                        if (!requestPath.Contains("localhost"))
                                        {
                                            if (AasxServer.AasxTask.proxy != null)
                                                handler.Proxy = AasxServer.AasxTask.proxy;
                                            else
                                                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                                        }

                                        var client = new HttpClient(handler);

                                        if (userPW != "")
                                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", userPW);
                                        string clientToken = "";
                                        if (sm.Extensions != null && sm.Extensions.Count > 1 && sm.Extensions[ 1 ].Name == "clientToken")
                                            clientToken = sm.Extensions[ 1 ].Value;
                                        if (clientToken != "")
                                            client.SetBearerToken(clientToken);

                                        client.DefaultRequestHeaders.Clear();

                                        bool success = false;
                                        HttpResponseMessage response = new HttpResponseMessage();
                                        try
                                        {
                                            requestPath += queryPara;
                                            if (logCount % logCountModulo == 0)
                                                Console.WriteLine("GET Submodel " + requestPath);
                                            client.Timeout = TimeSpan.FromSeconds(3);
                                            var task1 = Task.Run(async () => { response = await client.GetAsync(requestPath); });
                                            task1.Wait();
                                            if (response.IsSuccessStatusCode)
                                            {
                                                var json = response.Content.ReadAsStringAsync().Result;
                                                byte[] buffer = Encoding.UTF8.GetBytes(json);
                                                digest += Convert.ToBase64String(SHA256.HashData(buffer));
                                                // if (digest != hashBOM)
                                                //    changed= true;
                                                // hashBOM = digest;
                                                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(json));
                                                JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                                newsm = new Submodel("");
                                                newsm = Jsonization.Deserialize.SubmodelFrom(node);
                                                newsm.IdShort += " - COPY";
                                                newsm.Extensions = sm.Extensions;
                                                newsm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp);
                                                env.AasEnv.Submodels.Remove(sm);
                                                env.AasEnv.Submodels.Add(newsm);
                                                success = true;
                                            }
                                        }
                                        catch
                                        {
                                            success = false;
                                        }

                                        if (!success)
                                        {
                                            if (sm.IdShort != "BillOfMaterial - NO ACCESS")
                                            {
                                                if (hashBOM != "")
                                                    changed = true;
                                                hashBOM = "";
                                                newsm = new Submodel(sm.Id);
                                                newsm.IdShort = "BillOfMaterial - NO ACCESS";
                                                newsm.Extensions = sm.Extensions;
                                                newsm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp);
                                                env.AasEnv.Submodels.Remove(sm);
                                                env.AasEnv.Submodels.Add(newsm);
                                            }

                                            Console.WriteLine("NO ACCESS: aas " + aas.IdShort + " sm " + sm.IdShort);
                                            cfpValid = false;
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<string, cfpNode> assetCfp = new Dictionary<string, cfpNode>();
            // cfpNode root = new cfpNode();
            aascount = AasxServer.Program.env.Length;
            root = null;

            // Collect data from all AAS into cfpNode(s)
            for (int i = 0; i < aascount; i++)
            {
                env = AasxServer.Program.env[ i ];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[ 0 ];

                    //var assetId = aas.assetRef.Keys[ 0 ].Value;
                    var assetId = aas.AssetInformation.GlobalAssetId;
                    var cfp = new cfpNode();
                    cfp.envIndex = i;
                    cfp.aas = aas as AssetAdministrationShell;
                    cfp.asset = assetId;
                    cfp.productDesignation = aas.IdShort;

                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var smr in aas.Submodels)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null)
                            {
                                // ZVEI Level 1
                                if (sm.IdShort.Contains("ProductCarbonFootprint"))
                                {
                                    if (sm.IdShort.Contains(" - NO ACCESS"))
                                    {
                                        Console.WriteLine("NO ACCESS: aas " + aas.IdShort + " sm " + sm.IdShort);
                                        cfpValid = false;
                                    }

                                    if (sm.SubmodelElements != null)
                                    {
                                        foreach (var v in sm.SubmodelElements)
                                        {
                                            if (v is SubmodelElementCollection c)
                                            {
                                                if (c.IdShort.Contains("FootprintInformationModule")
                                                    || c.IdShort.Contains("FootprintInformationCombination"))
                                                {
                                                    string lifeCyclePhase = "";
                                                    Property co2eq = null;
                                                    foreach (var v2 in c.Value)
                                                    {
                                                        switch (v2.IdShort)
                                                        {
                                                            case "LifeCyclePhase":
                                                                lifeCyclePhase = v2.ValueAsText();
                                                                break;
                                                            case "CO2eq":
                                                                co2eq = v2 as Property;
                                                                break;
                                                        }
                                                    }

                                                    switch (lifeCyclePhase)
                                                    {
                                                        case "Cradle-to-gate":
                                                            if (c.IdShort.Contains("FootprintInformationModule"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.cradleToGateModule = co2eq;
                                                            }

                                                            if (c.IdShort.Contains("FootprintInformationCombination"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.cradleToGateCombination = co2eq;
                                                            }

                                                            break;
                                                        case "Production":
                                                            if (c.IdShort.Contains("FootprintInformationModule"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.productionModule = co2eq;
                                                            }

                                                            if (c.IdShort.Contains("FootprintInformationCombination"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.productionCombination = co2eq;
                                                            }

                                                            break;
                                                        case "Distribution":
                                                            if (c.IdShort.Contains("FootprintInformationModule"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.distributionModule = co2eq;
                                                            }

                                                            if (c.IdShort.Contains("FootprintInformationCombination"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.distributionCombination = co2eq;
                                                            }

                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // ZVEI Level 2
                                if (sm.IdShort.Contains("CarbonFootprint"))
                                {
                                    if (sm.IdShort.Contains(" - NO ACCESS"))
                                    {
                                        Console.WriteLine("NO ACCESS: aas " + aas.IdShort + " sm " + sm.IdShort);
                                        cfpValid = false;
                                    }

                                    if (sm.SubmodelElements != null)
                                    {
                                        foreach (var v in sm.SubmodelElements)
                                        {
                                            if (v is SubmodelElementCollection c)
                                            {
                                                if (c.IdShort.Contains("ProductCarbonFootprint"))
                                                {
                                                    string lifeCyclePhase = "";
                                                    Property co2eq = null;
                                                    foreach (var v2 in c.Value)
                                                    {
                                                        switch (v2.IdShort)
                                                        {
                                                            case "PCFLifeCyclePhase":
                                                            case "PCFLiveCyclePhase":
                                                                lifeCyclePhase = v2.ValueAsText();
                                                                break;
                                                            case "PCFCO2eq":
                                                                co2eq = v2 as Property;
                                                                break;
                                                        }
                                                    }

                                                    switch (lifeCyclePhase)
                                                    {
                                                        case "Cradle-to-gate":
                                                        case "A1-A3":
                                                            if (c.IdShort.Contains("ProductCarbonFootprint"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.cradleToGateModule = co2eq;
                                                            }

                                                            if (c.IdShort.Contains("FootprintInformationCombination"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.cradleToGateCombination = co2eq;
                                                            }

                                                            break;
                                                        case "Production":
                                                        case "A3":
                                                        case "A3 - production":
                                                            if (c.IdShort.Contains("ProductCarbonFootprint"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.productionModule = co2eq;
                                                            }

                                                            if (c.IdShort.Contains("FootprintInformationCombination"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.productionCombination = co2eq;
                                                            }

                                                            break;
                                                        case "Distribution":
                                                        case "A2":
                                                        case "A1 – raw material supply (and upstream production)":
                                                            if (c.IdShort.Contains("ProductCarbonFootprint"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.distributionModule = co2eq;
                                                            }

                                                            if (c.IdShort.Contains("FootprintInformationCombination"))
                                                            {
                                                                co2eq.Value = co2eq.Value.Replace(",", ".");
                                                                cfp.distributionCombination = co2eq;
                                                            }

                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (sm.IdShort.Contains("BillOfMaterial"))
                                {
                                    if (sm.IdShort.Contains(" - NO ACCESS"))
                                    {
                                        Console.WriteLine("NO ACCESS: aas " + aas.IdShort + " sm " + sm.IdShort);
                                        cfpValid = false;
                                    }

                                    if (sm.SubmodelElements != null)
                                    {
                                        cfp.bomTimestamp = sm.TimeStampTree;
                                        List<string> bom = new List<string>();
                                        foreach (var v in sm.SubmodelElements)
                                        {
                                            string s = "";
                                            if (v is Entity e)
                                            {
                                                s = e?.GlobalAssetId;
                                                if (s != "")
                                                {
                                                    // check if first entity is newer than last cfp creation
                                                    //TODO jtikekar:Whether to use GlobalAssetId or SpecificAssetId
                                                    //s = e?.assetRef?.Keys?[ 0 ].Value;
                                                    s = e?.GlobalAssetId;
                                                    if (s != "")
                                                    {
                                                        bom.Add(s);
                                                    }
                                                }
                                            }
                                        }

                                        // assetBOM.Add(assetId, bom);
                                        cfp.bom = bom;
                                    }
                                }

                                if (sm.IdShort.Contains("TechnicalData") && sm.SubmodelElements != null)
                                {
                                    foreach (var v in sm.SubmodelElements)
                                    {
                                        if (v is SubmodelElementCollection c)
                                        {
                                            if (c.IdShort == "GeneralInformation")
                                            {
                                                foreach (var sme in c.Value)
                                                {
                                                    if (sme is AasCore.Aas3_0.File f)
                                                    {
                                                        if (f.IdShort == "ManufacturerLogo")
                                                            cfp.manufacturerLogo = f;
                                                        if (f.IdShort == "ProductImage")
                                                            cfp.productImage = f;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (sm.IdShort.Contains("Nameplate") && sm.SubmodelElements != null)
                                {
                                    foreach (var v in sm.SubmodelElements)
                                    {
                                        if (v is MultiLanguageProperty p)
                                        {
                                            if (p.IdShort == "ManufacturerProductDesignation")
                                            {
                                                if (p.Value != null)
                                                {
                                                    string s = null;
                                                    foreach (var ls in p.Value)
                                                    {
                                                        if (ls.Language.ToLower() == "en")
                                                        {
                                                            s = ls.Text;
                                                            break; //english has priority over German
                                                        }

                                                        if (ls.Language.ToLower() == "de")
                                                        {
                                                            if (s != null)
                                                                s = ls.Text;
                                                        }
                                                    }

                                                    if (s != null)
                                                        cfp.productDesignation = s;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Weight
                                if (sm.IdShort.Contains("WeightInformation"))
                                {
                                    if (sm.IdShort.Contains(" - NO ACCESS"))
                                    {
                                        Console.WriteLine("NO ACCESS: aas " + aas.IdShort + " sm " + sm.IdShort);
                                        cfpValid = false;
                                    }

                                    if (sm.SubmodelElements != null)
                                    {
                                        foreach (var v in sm.SubmodelElements)
                                        {
                                            if (v is SubmodelElementCollection c)
                                            {
                                                if (c.IdShort.Contains("WeightInformationModule")
                                                    || c.IdShort.Contains("WeightInformationCombination"))
                                                {
                                                    foreach (var v2 in c.Value)
                                                    {
                                                        switch (v2.IdShort)
                                                        {
                                                            case "ProductWeight":
                                                                if (c.IdShort.Contains("WeightInformationModule"))
                                                                {
                                                                    cfp.weightModule = v2 as Property;
                                                                    cfp.weightModule.Value = cfp.weightModule.Value.Replace(",", ".");
                                                                }

                                                                if (c.IdShort.Contains("WeightInformationCombination"))
                                                                {
                                                                    cfp.weightCombination = v2 as Property;
                                                                    cfp.weightCombination.Value = cfp.weightCombination.Value.Replace(",", ".");
                                                                }

                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!assetCfp.ContainsKey(assetId))
                    {
                        assetCfp.Add(assetId, cfp);
                    }

                    /*
                    if (i == envIndex)
                    {
                        root = cfp;
                    }
                    */
                    if (aas.IdShort == "ZveiControlCabinetAas - EXTERNAL")
                    {
                        root = cfp;
                        if (!Program.showWeight && root.cradleToGateCombination != null)
                        {
                            //TODO: elements need proper deep clone method implemented within AAS metamodel classes
                            if (asbuilt_total == null)
                                asbuilt_total = new String(root.cradleToGateCombination.Value);
                        }

                        if (Program.showWeight && root.weightCombination != null)
                        {
                            //TODO: elements need proper deep clone method implemented within AAS metamodel classes
                            if (asbuilt_total == null)
                                asbuilt_total = new String(root.weightCombination.Value);
                        }
                    }
                }
            }

            // create children from BOM
            foreach (var d in assetCfp)
            {
                var cfp = d.Value;
                if (cfp.bom.Count != 0)
                {
                    foreach (var asset in cfp.bom)
                    {
                        cfpNode child = null;
                        if (assetCfp.TryGetValue(asset, out child))
                        {
                            cfp.children.Add(child);
                        }
                    }
                }
            }

            logCount++;

            if (digest != hashBOM)
            {
                changed = true;
                hashBOM = digest;
            }

            return changed;
        }

        public static bool once = false;

        public static void operation_calculate_cfp(Operation op, int envIndex, DateTime timeStamp)
        {
            if (AasxServer.Program.initializingRegistry)
            {
                // once = false; // one more again
                return;
            }

            if (once)
                return;

            // Iterate tree and calculate CFP values
            bool changed = createCfpTree(envIndex, timeStamp);

            cfpNode node = root;
            cfpNode parent = null;
            List<cfpNode> stack = new List<cfpNode>();
            int sp = -1;

            while (node != null)
            {
                // create cfp combination only once at first child
                if (node.iChild == 0)
                {
                    if (node.cradleToGateCombination != null)
                    {
                        node.cradleToGateCombination.Value = "0.0";
                        if (node.cradleToGateModule != null)
                        {
                            node.cradleToGateCombination.Value = node.cradleToGateModule.Value;
                        }

                        node.cradleToGateCombination.SetTimeStamp(timeStamp);
                    }

                    if (node.productionCombination != null)
                    {
                        node.productionCombination.Value = "0.0";
                        if (node.productionModule != null)
                        {
                            node.productionCombination.Value = node.productionModule.Value;
                        }

                        node.productionCombination.SetTimeStamp(timeStamp);
                    }
                    if (node.distributionCombination != null)
                    {
                        node.distributionCombination.Value = "0.0";
                        if (node.distributionModule != null)
                        {
                            node.distributionCombination.Value = node.distributionModule.Value;
                        }

                        node.distributionCombination.SetTimeStamp(timeStamp);
                    }
                    if (node.weightCombination != null)
                    {
                        node.weightCombination.Value = "0.0";
                        if (node.weightModule != null)
                        {
                            node.weightCombination.Value = node.weightModule.Value;
                        }

                        node.weightCombination.SetTimeStamp(timeStamp);
                    }
                }
                // move up, if all children iterated
                if (node.iChild == node.children.Count)
                {
                    node.iChild = 0;
                    if (sp == -1)
                    {
                        node = null;
                    }
                    else
                    {
                        parent = stack[ sp ];
                        if (parent.cradleToGateCombination != null)
                        {
                            Property p = node.cradleToGateModule;
                            if (node.cradleToGateCombination != null)
                                p = node.cradleToGateCombination;

                            if (p != null)
                            {
                                double value1 = 0.0;
                                double value2 = 0.0;
                                try
                                {
                                    value1 = Convert.ToDouble(parent.cradleToGateCombination.Value, CultureInfo.InvariantCulture);
                                    value2 = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture);
                                    value1 = Math.Round(value1 + value2, 8);
                                    parent.cradleToGateCombination.Value = value1.ToString(CultureInfo.InvariantCulture);
                                    parent.cradleToGateCombination.SetTimeStamp(timeStamp);
                                }
                                catch
                                {

                                }
                            }
                        }
                        if (parent.productionCombination != null)
                        {
                            Property p = node.productionModule;
                            if (node.productionCombination != null)
                                p = node.productionCombination;
                            if (p != null)
                            {
                                double value1 = 0.0;
                                double value2 = 0.0;
                                try
                                {
                                    value1 = Convert.ToDouble(parent.productionCombination.Value, CultureInfo.InvariantCulture);
                                    value2 = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture);
                                    value1 = Math.Round(value1 + value2, 8);
                                    parent.productionCombination.Value = value1.ToString(CultureInfo.InvariantCulture);
                                    parent.productionCombination.SetTimeStamp(timeStamp);
                                }
                                catch
                                {

                                }
                            }
                        }
                        if (parent.distributionCombination != null)
                        {
                            Property p = node.distributionModule;
                            if (node.distributionCombination != null)
                                p = node.distributionCombination;
                            if (p != null)
                            {
                                double value1 = 0.0;
                                double value2 = 0.0;
                                try
                                {
                                    value1 = Convert.ToDouble(parent.distributionCombination.Value, CultureInfo.InvariantCulture);
                                    value2 = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture);
                                    value1 = Math.Round(value1 + value2, 8);
                                    parent.distributionCombination.Value = value1.ToString(CultureInfo.InvariantCulture);
                                    parent.distributionCombination.SetTimeStamp(timeStamp);
                                }
                                catch
                                {

                                }
                            }
                        }
                        if (parent.weightCombination != null)
                        {
                            Property p = node.weightModule;
                            if (node.weightCombination != null)
                                p = node.weightCombination;
                            if (p != null)
                            {
                                double value1 = 0.0;
                                double value2 = 0.0;
                                try
                                {
                                    value1 = Convert.ToDouble(parent.weightCombination.Value, CultureInfo.InvariantCulture);
                                    value2 = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture);
                                    value1 = Math.Round(value1 + value2, 8);
                                    parent.weightCombination.Value = value1.ToString(CultureInfo.InvariantCulture);
                                    parent.weightCombination.SetTimeStamp(timeStamp);
                                }
                                catch
                                {

                                }
                            }
                        }
                        parent = null;
                        node = stack[ sp ];
                        stack.RemoveAt(sp);
                        sp--;
                    }
                }
                else
                {
                    // Interate children
                    stack.Add(node);
                    sp++;
                    node = node.children[ node.iChild++ ];
                }
            }

            // once = true;
            // if (root != null && root.bomTimestamp > lastCreateTimestamp)
            if (changed || credentialsChanged)
            {
                Program.signalNewData(1);
                lastCreateTimestamp = timeStamp;
                credentialsChanged = false;
            }
        }

        static void saveAASXtoTemp()
        {
            bool newData = false;
            int envi = 0;
            while (envi < Program.env.Length)
            {
                if (!Program.withDb)
                {
                    string fn = Program.envFileName[ envi ];

                    if (fn != null && fn != "")
                    {
                        fn = Path.GetFileName(fn);
                        if (fn.ToLower().Contains("--save-temp"))
                        {
                            lock (Program.changeAasxFile)
                            {
                                Console.WriteLine("SAVE TEMP: " + fn);
                                Program.env[ envi ].SaveAs("./temp/" + fn, true);
                                DateTime timeStamp = DateTime.Now;
                                foreach (var submodel in Program.env[ envi ].AasEnv.Submodels)
                                {
                                    submodel.TimeStampCreate = timeStamp;
                                    submodel.SetTimeStamp(timeStamp);
                                    submodel.SetAllParents(timeStamp);
                                }

                                newData = true;
                            }
                        }
                    }
                }
                else
                {
                    if (Program.env[ envi ] != null && Program.env[ envi ].getWrite())
                    {
                        lock (Program.changeAasxFile)
                        {
                            Edit.Update(Program.env[ envi ]);
                            newData = true;
                        }
                    }
                    // delete of aas handeling
                }
                envi++;
            }

            if (newData)
                Program.signalNewData(0);
        }

        static Thread tasksThread;

        public static void tasksSamplingLoop()
        {
            while (true)
            {
                tasksCyclic();
                Thread.Sleep(100);
            }
        }

        public static void tasksCyclic()
        {
            if (Program.isLoading)
                return;

            DateTime timeStamp = DateTime.UtcNow;

            // check save AASX to temp
            if (Program.saveTemp > 0)
            {
                if (Program.saveTempDt.AddSeconds(Program.saveTemp) < timeStamp)
                {
                    Program.saveTempDt = timeStamp;
                    saveAASXtoTemp();
                }
            }

            // foreach (var t in taskList)
            bool taskRun = false;
            for (int i = 0; i < taskList.Count; i++)
            {
                var t = taskList[ i ];
                if (t.taskType?.Value.ToLower() == "cyclic")
                {
                    if (t.nextExecution > timeStamp)
                        continue;

                    if (t.cycleCount != null)
                    {
                        if (t.cycleCount.Value == "")
                            t.cycleCount.Value = "0";

                        t.cycleCount.Value = (Convert.ToInt32(t.cycleCount.Value) + 1).ToString();
                        t.cycleCount.SetTimeStamp(timeStamp);
                    }

                    t.nextExecution = timeStamp.AddMilliseconds(Convert.ToInt32(t.cycleTime.Value));
                    if (t.nextCycle != null)
                    {
                        t.nextCycle.Value = t.nextExecution.ToString();
                        t.nextCycle.SetTimeStamp(timeStamp);
                    }

                    Program.signalNewData(0);

                    runOperations(t.def, t.envIndex, timeStamp);
                    taskRun = true;
                }
            }

            if (taskRun)
                System.GC.Collect();
        }
    }
}
