
using AasxRestServerLibrary;
using AdminShellNS;
using Extensions;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

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
                {   // Open the text file using a stream reader.
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
                {   // Open the text file using a stream reader.
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
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[0];
                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var smr in aas.Submodels)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null && sm.IdShort.ToLower().Contains("tasks"))
                            {
                                sm.SetTimeStamp(timeStamp);
                                int countSme = sm.SubmodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.SubmodelElements[iSme];
                                    if (sme is SubmodelElementCollection smec)
                                    {
                                        var nextTask = new AasxTask();
                                        AasxTask.taskList.Add(nextTask);
                                        nextTask.def = smec;
                                        nextTask.envIndex = i;

                                        int countSmec = smec.Value.Count;
                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.Value[iSmec];
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
                var sme2 = smec.Value[iSmec];

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

            var smec = new SubmodelElementCollection();
            foreach (var input in op.InputVariables)
            {
                var inputRef = input.Value;
                if (!(inputRef is ReferenceElement))
                    return;
                var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
                if (refElement is SubmodelElementCollection re)
                    smec = re;
            }

            int countSmec = smec.Value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.Value[iSmec];
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
                            if (accessToken.Value != "")
                            {
                                bool valid = true;
                                var jwtToken = new JwtSecurityToken(accessToken.Value);
                                if ((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow))
                                    valid = false;
                                if (valid) return;
                            }

                            var handler = new HttpClientHandler();
                            if (proxy != null)
                                handler.Proxy = proxy;
                            else
                                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                            var client = new HttpClient(handler);
                            DiscoveryDocumentResponse disco = null;

                            var task = Task.Run(async () => { disco = await client.GetDiscoveryDocumentAsync(authServerEndPoint.Value); });
                            task.Wait();
                            if (disco.IsError) return;
                            Console.WriteLine("OpenID Discovery JSON:");
                            Console.WriteLine(disco.Raw);

                            var serverCert = new X509Certificate2();
                            Stream s = null;
                            try
                            {
                                s = AasxServer.Program.env[envIndex].GetLocalStreamFromPackage(authServerCertificate.Value);
                            }
                            catch { }
                            if (s == null) return;

                            using (var m = new System.IO.MemoryStream())
                            {
                                s.CopyTo(m);
                                var b = m.GetBuffer();
                                serverCert = new X509Certificate2(b);
                                Console.WriteLine("Auth server certificate: " + authServerCertificate.Value);
                            }

                            string[] x5c = null;
                            X509Certificate2 certificate = null;
                            string certificatePassword = clientCertificatePassWord.Value;
                            Stream s2 = null;
                            try
                            {
                                s2 = AasxServer.Program.env[envIndex].GetLocalStreamFromPackage(clientCertificate.Value);
                            }
                            catch { }
                            if (s2 == null) return;
                            if (s2 != null)
                            {
                                X509Certificate2Collection xc = new X509Certificate2Collection();
                                using (var m = new System.IO.MemoryStream())
                                {
                                    s2.CopyTo(m);
                                    var b = m.GetBuffer();
                                    xc.Import(b, certificatePassword, X509KeyStorageFlags.PersistKeySet);
                                    certificate = new X509Certificate2(b, certificatePassword);
                                    Console.WriteLine("Client certificate: " + clientCertificate.Value);
                                }

                                string[] X509Base64 = new string[xc.Count];

                                int j = xc.Count;
                                var xce = xc.GetEnumerator();
                                for (int i = 0; i < xc.Count; i++)
                                {
                                    xce.MoveNext();
                                    X509Base64[--j] = Convert.ToBase64String(xce.Current.GetRawCertData());
                                }
                                x5c = X509Base64;

                                var credential = new X509SigningCredentials(certificate);
                                string clientId = "client.jwt";
                                string email = "";
                                string subject = certificate.Subject;
                                var split = subject.Split(new Char[] { ',' });
                                if (split[0] != "")
                                {
                                    var split2 = split[0].Split(new Char[] { '=' });
                                    if (split2[0] == "E")
                                    {
                                        email = split2[1];
                                    }
                                }
                                Console.WriteLine("email: " + email);

                                var now = DateTime.UtcNow;
                                var token = new JwtSecurityToken(
                                        clientId,
                                        disco.TokenEndpoint,
                                        new List<Claim>()
                                        {
                                                        new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                                                        new Claim(JwtClaimTypes.Subject, clientId),
                                                        new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                                                        // OZ
                                                        new Claim(JwtClaimTypes.Email, email)
                                        },
                                        now,
                                        now.AddMinutes(1),
                                        credential)
                                ;

                                token.Header.Add("x5c", x5c);
                                var tokenHandler = new JwtSecurityTokenHandler();
                                string clientToken = tokenHandler.WriteToken(token);

                                TokenResponse response = null;
                                task = Task.Run(async () =>
                                {
                                    response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                                    {
                                        Address = disco.TokenEndpoint,
                                        Scope = "resource1.scope1",

                                        ClientAssertion =
                                        {
                                            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                                            Value = clientToken
                                        }
                                    });
                                });
                                task.Wait();

                                if (response.IsError) return;

                                accessToken.Value = response.AccessToken;
                                accessToken.SetTimeStamp(timeStamp);
                            }
                        }
                        break;
                }
            }
        }

        static void operation_get_put(Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection
            // inputVariable sourceEndPoint: property
            // inputVariable reference sourcePath: property
            // inputVariable reference destinationElement: collection

            string opName = op.IdShort.ToLower();

            SubmodelElementCollection authentication = null;
            Property authType = null;
            Property accessToken = null;
            Property userName = null;
            Property passWord = null;
            AasCore.Aas3_0.File authServerCertificate = null;
            Property endPoint = null;
            Property path = null;
            SubmodelElementCollection elementCollection = null;
            Submodel elementSubmodel = null;
            Property lastDiff = null;
            Property status = null;
            Property mode = null;

            SubmodelElementCollection smec = null;
            Submodel sm = null;
            Property p = null;
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
                    var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
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
                    var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((outputRef as ReferenceElement).GetModelReference());
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
                }
            }

            if (authentication != null)
            {
                smec = authentication;
                int countSmec = smec.Value.Count;
                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                {
                    var sme2 = smec.Value[iSmec];
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
                    }
                }
            }

            var handler = new HttpClientHandler();
            if (proxy != null)
                handler.Proxy = proxy;
            else
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);
            if (accessToken != null)
                client.SetBearerToken(accessToken.Value);

            string requestPath = endPoint.Value + "/" + path.Value;
            HttpResponseMessage response = null;
            Task task = null;
            string diffPath = "";
            var splitPath = path.Value.Split('.');
            string aasPath = splitPath[0];
            string subPath = "";
            int i = 1;
            string pre = "";
            while (i < splitPath.Length)
            {
                subPath += pre + splitPath[i];
                pre = ".";
                i++;
            }

            if (status != null)
                status.Value = "OK";
            if (opName == "get" || opName == "getdiff")
            {
                if (splitPath.Length < 2)
                    return;

                DateTime last = new DateTime();
                if (opName == "getdiff")
                {
                    if (lastDiff == null)
                        return;
                    if (elementCollection == null)
                        return;

                    if (lastDiff.Value == "")
                    {
                        opName = "get";
                        requestPath = endPoint.Value + "/aas/" + aasPath +
                            "/submodels/" + splitPath[1] + "/elements";
                        i = 2;
                        while (i < splitPath.Length)
                        {
                            requestPath += "/" + splitPath[i];
                            i++;
                        }
                        requestPath += "/complete";
                    }
                    else
                    {
                        last = DateTime.Parse(lastDiff.Value).ToUniversalTime();
                        requestPath = endPoint.Value +
                            "/diffjson/aas/" + splitPath[0] +
                            "?path=" + subPath;
                        requestPath += "."; // to avoid wrong data by prefix only
                        requestPath += "&time=" + lastDiff.Value;
                    }
                }

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
                            Program.signalNewData(1);
                        }
                        return;
                    }
                }
                catch
                {
                    return;
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
                            foreach (JProperty jp1 in (JToken)parsed)
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
                            receiveSubmodel = Newtonsoft.Json.JsonConvert.DeserializeObject<Submodel>(
                                json, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                            receiveSubmodel.SetTimeStamp(timeStamp);
                            receiveSubmodel.SetAllParents(timeStamp);

                            // need id for idempotent behaviour
                            if (receiveSubmodel.Id == null || receiveSubmodel.Id != elementSubmodel.Id)
                                return;

                            var aas = Program.env[envIndex].AasEnv.FindAasWithSubmodelId(receiveSubmodel.Id);

                            // datastructure update
                            if (Program.env == null || Program.env[envIndex].AasEnv == null /*|| Program.env[envIndex].AasEnv.Assets == null*/)
                                return;

                            // add Submodel
                            var existingSm = Program.env[envIndex].AasEnv.FindSubmodelById(receiveSubmodel.Id);
                            if (existingSm != null)
                                Program.env[envIndex].AasEnv.Submodels.Remove(existingSm);
                            Program.env[envIndex].AasEnv.Submodels.Add(receiveSubmodel);
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
                                                return;
                                            requestPath = endPoint.Value + "/aas/" + aasPath +
                                                "/submodels/" + splitPath[0] + "/elements";
                                            i = 1;
                                            while (i < splitPath.Length)
                                            {
                                                requestPath += "/" + splitPath[i];
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
                                                        Program.signalNewData(1);
                                                    }
                                                    return;
                                                }
                                            }
                                            catch
                                            {
                                                return;
                                            }

                                            json = response.Content.ReadAsStringAsync().Result;
                                            JObject parsed = JObject.Parse(json);
                                            foreach (JProperty jp1 in (JToken)parsed)
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
                                                Program.signalNewData(2);
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
                    return;
                }
                if (lastDiff != null)
                    lastDiff.Value = "" + timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                Program.signalNewData(1);
            }
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
                        return;
                    if (elementCollection == null)
                        return;
                    diffCollection = elementCollection;
                    count = elementCollection.Value.Count;
                    if (lastDiff.Value == "")
                    {
                        // get "latestData" from server
                        bool error = false;
                        splitPath = path.Value.Split('/');
                        requestPath = endPoint.Value + "/aas/" + splitPath[1] +
                            "/submodels/" + splitPath[3];
                        requestPath += "/elements/" + elementCollection.IdShort;
                        requestPath += "/latestData/complete";
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
                                JObject parsed = JObject.Parse(json);
                                foreach (JProperty jp1 in (JToken)parsed)
                                {
                                    if (jp1.Name == "elem")
                                    {
                                        string text = jp1.Value.ToString();
                                        var receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<SubmodelElementCollection>(
                                            text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
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
                                            if (elementCollection.Value[0] is SubmodelElementCollection smc)
                                            {
                                                if (smc.IdShort == "latestData")
                                                {
                                                    if (smc.Value.Count == 0)
                                                        return;
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
                            var sme = diffCollection.Value[i];
                            if (!(sme is SubmodelElementCollection))
                                return;
                            elementCollection = sme as SubmodelElementCollection;
                            diffPath = "/" + diffCollection.IdShort;
                            if (elementCollection.TimeStamp <= last)
                                elementCollection = null;
                        }
                        if (elementCollection != null)
                            json = JsonConvert.SerializeObject(elementCollection, Formatting.Indented);
                        if (elementSubmodel != null)
                            json = JsonConvert.SerializeObject(elementSubmodel, Formatting.Indented);
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
                            task = Task.Run(async () =>
                            {
                                response = await client.PutAsync(
                                    requestPath + diffPath, content);
                            });
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
                            Program.signalNewData(1);
                        }
                        return;
                    }
                }

                if (lastDiff != null)
                    lastDiff.Value = "" + timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
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
                    var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((inputRef as ReferenceElement).GetModelReference());
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
                    if (pre == collection.Value[i].IdShort.Substring(0, pre.Length))
                        preCount++;
                    i++;
                }
                i = 0;
                while (preCount > count && i < collection.Value.Count)
                {
                    if (pre == collection.Value[i].IdShort.Substring(0, pre.Length))
                    {
                        IReferable r = collection.Value[i];
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
            public AasCore.Aas3_0.File manufacturerLogo = null;
            public AasCore.Aas3_0.File productImage = null;
            public string productDesignation = "";
            public List<string> bom = new List<string>();
            public List<cfpNode> children = new List<cfpNode>();
            public int iChild = 0;
        }

        public static cfpNode root = null;

        public static void createCfpTree(int envIndex, DateTime timeStamp)
        {
            Dictionary<string, cfpNode> assetCfp = new Dictionary<string, cfpNode>();
            // cfpNode root = new cfpNode();
            AdminShellPackageEnv env = null;
            int aascount = AasxServer.Program.env.Length;
            root = null;

            // Collect data from all AAS into cfpNode(s)
            for (int i = 0; i < aascount; i++)
            {
                env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[0];
                    //var assetId = aas.assetRef.Keys[0].Value;
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
                                if (sm.IdShort.Contains("ProductCarbonFootprint") && sm.SubmodelElements != null)
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
                                // ZVEI Level 2
                                if (sm.IdShort.Contains("CarbonFootprint") && sm.SubmodelElements != null)
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
                                if (sm.IdShort.Contains("BillOfMaterial") && sm.SubmodelElements != null)
                                {
                                    List<string> bom = new List<string>();
                                    foreach (var v in sm.SubmodelElements)
                                    {
                                        if (v is Entity e)
                                        {
                                            string s = "";
                                            s = e?.GlobalAssetId;
                                            if (s != "")
                                            {
                                                bom.Add(s);
                                            }
                                        }
                                    }
                                    // assetBOM.Add(assetId, bom);
                                    cfp.bom = bom;
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
                            }
                        }
                    }
                    if (!assetCfp.ContainsKey(assetId))
                    {
                        assetCfp.Add(assetId, cfp);

                    }
                    if (i == envIndex)
                        root = cfp;
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

            // return root;
        }

        static bool once = false;
        public static void operation_calculate_cfp(Operation op, int envIndex, DateTime timeStamp)
        {
            if (once)
                return;

            if (AasxServer.Program.initializingRegistry)
                return;

            // Dictionary<string, cfpNode> assetCfp = new Dictionary<string, cfpNode>();
            // cfpNode root = null;

            // Iterate tree and calculate CFP values
            // cfpNode node = createCfpTree(envIndex, timeStamp);
            createCfpTree(envIndex, timeStamp);
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
                        parent = stack[sp];
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
                                catch { }
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
                                catch { }
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
                                catch { }
                            }
                        }
                        parent = null;
                        node = stack[sp];
                        stack.RemoveAt(sp);
                        sp--;
                    }
                }
                else
                {
                    // Interate children
                    stack.Add(node);
                    sp++;
                    node = node.children[node.iChild++];
                }
            }

            // once = true;
            Program.signalNewData(1);
        }

        static void saveAASXtoTemp()
        {
            int envi = 0;
            while (envi < Program.envFileName.Length)
            {
                string fn = Program.envFileName[envi];

                if (fn != null && fn != "")
                {
                    fn = Path.GetFileName(fn);
                    if (fn.ToLower().Contains("--save-temp"))
                    {
                        lock (Program.changeAasxFile)
                        {
                            Console.WriteLine("SAVE TEMP: " + fn);
                            Program.env[envi].SaveAs("./temp/" + fn, true);
                        }
                    }
                }
                envi++;
            }
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
                var t = taskList[i];
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
