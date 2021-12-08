using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AasxServer;
using AdminShellNS;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AasxServer
{
    public class AasxTask
    {
        public AdminShell.SubmodelElementCollection def = null;
        public AdminShell.Property taskType = null;
        public AdminShell.Property cycleTime = null;
        public AdminShell.Property cycleCount = null;
        public AdminShell.Property nextCycle = null;
        public DateTime nextExecution = new DateTime();
        public int envIndex = -1;

        public static List<AasxTask> taskList = null;

        public static WebProxy proxy = null;

        public static void taskInit()
        {
            // Test for proxy
            Console.WriteLine("Test: ../proxy.dat");

            if (File.Exists("../proxy.dat"))
            {
                bool error = false;
                Console.WriteLine("Found: ../proxy.dat");
                string proxyAddress = "";
                string username = "";
                string password = "";
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader("../proxy.dat"))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("The file ../proxy.dat could not be read:");
                    Console.WriteLine(e.Message);
                    error = true;
                }
                if (!error)
                {
                    Console.WriteLine("Proxy: " + proxyAddress);
                    Console.WriteLine("Username: " + username);
                    Console.WriteLine("Password: " + password);
                    proxy = new WebProxy();
                    Uri newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    if (username != "" && password != "")
                        proxy.Credentials = new NetworkCredential(username, password);
                }
            }


            DateTime timeStamp = DateTime.Now;
            taskList = new List<AasxTask>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AdministrationShells[0];
                    if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                    {
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null && sm.idShort.ToLower().Contains("tasks"))
                            {
                                sm.setTimeStamp(timeStamp);
                                int countSme = sm.submodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.submodelElements[iSme].submodelElement;
                                    if (sme is AdminShell.SubmodelElementCollection smec)
                                    {
                                        var nextTask = new AasxTask();
                                        AasxTask.taskList.Add(nextTask);
                                        nextTask.def = smec;
                                        nextTask.envIndex = i;

                                        int countSmec = smec.value.Count;
                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.value[iSmec].submodelElement;
                                            var idShort = sme2.idShort.ToLower();

                                            switch (idShort)
                                            {
                                                case "tasktype":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.taskType = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "cycletime":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.cycleTime = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "cyclecount":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.cycleCount = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "nextcycle":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.nextCycle = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                            }
                                        }

                                        if (nextTask.taskType?.value.ToLower() == "init")
                                            runOperations(nextTask.def, nextTask.envIndex, timeStamp);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            tasksThread = new Thread(new ThreadStart(tasksSamplingLoop));
            tasksThread.Start();
        }

        static void runOperations(AdminShell.SubmodelElementCollection smec, int envIndex, DateTime timeStamp)
        {
            int countSmec = smec.value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.value[iSmec].submodelElement;

                if (sme2 is AdminShell.Operation op)
                {
                    var idShort = sme2.idShort.ToLower();
                    switch (idShort)
                    {
                        case "authenticate":
                            operation_authenticate(op, envIndex, timeStamp);
                            break;
                        case "get":
                        case "put":
                            operation_get_put(op, envIndex, timeStamp);
                            break;
                    }
                }
            }
        }

        static void operation_authenticate(AdminShell.Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection

            if (op.inputVariable.Count != 1)
            {
                return;
            }

            AdminShell.Property authType = null;
            AdminShell.Property authServerEndPoint = null;
            AdminShell.File authServerCertificate = null;
            AdminShell.File clientCertificate = null;
            AdminShell.Property clientCertificatePassWord = null;
            AdminShell.Property accessToken = null;
            AdminShell.Property userName = null;
            AdminShell.Property passWord = null;

            var smec = new AdminShell.SubmodelElementCollection();
            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (!(inputRef is AdminShell.ReferenceElement))
                    return;
                var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement is AdminShell.SubmodelElementCollection re)
                    smec = re;
            }

            int countSmec = smec.value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.value[iSmec].submodelElement;
                var idShort = sme2.idShort.ToLower();

                switch (idShort)
                {
                    case "authtype":
                        if (sme2 is AdminShell.Property)
                        {
                            authType = sme2 as AdminShell.Property;
                        }
                        break;
                    case "authserverendpoint":
                        if (sme2 is AdminShell.Property)
                        {
                            authServerEndPoint = sme2 as AdminShell.Property;
                        }
                        break;
                    case "accesstoken":
                        if (sme2 is AdminShell.Property)
                        {
                            accessToken = sme2 as AdminShell.Property;
                        }
                        break;
                    case "username":
                        if (sme2 is AdminShell.Property)
                        {
                            userName = sme2 as AdminShell.Property;
                        }
                        break;
                    case "password":
                        if (sme2 is AdminShell.Property)
                        {
                            passWord = sme2 as AdminShell.Property;
                        }
                        break;
                    case "authservercertificate":
                        if (sme2 is AdminShell.File)
                        {
                            authServerCertificate = sme2 as AdminShell.File;
                        }
                        break;
                    case "clientcertificate":
                        if (sme2 is AdminShell.File)
                        {
                            clientCertificate = sme2 as AdminShell.File;
                        }
                        break;
                    case "clientcertificatepassword":
                        if (sme2 is AdminShell.Property)
                        {
                            clientCertificatePassWord = sme2 as AdminShell.Property;
                        }
                        break;
                }
            }

            if (authType != null)
            {
                switch (authType.value.ToLower())
                {
                    case "openid":
                        if (authServerEndPoint != null && authServerCertificate != null && clientCertificate != null
                            && accessToken != null)
                        {
                            if (accessToken.value != "")
                            {
                                bool valid = true;
                                var jwtToken = new JwtSecurityToken(accessToken.value);
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

                            var task = Task.Run(async () => { disco = await client.GetDiscoveryDocumentAsync(authServerEndPoint.value); });
                            task.Wait();
                            if (disco.IsError) return;
                            Console.WriteLine("OpenID Discovery JSON:");
                            Console.WriteLine(disco.Raw);

                            var serverCert = new X509Certificate2();
                            Stream s = null;
                            try
                            {
                                s = AasxServer.Program.env[envIndex].GetLocalStreamFromPackage(authServerCertificate.value);
                            }
                            catch { }
                            if (s == null) return;

                            using (var m = new System.IO.MemoryStream())
                            {
                                s.CopyTo(m);
                                var b = m.GetBuffer();
                                serverCert = new X509Certificate2(b);
                                Console.WriteLine("Auth server certificate: " + authServerCertificate.value);
                            }

                            string[] x5c = null;
                            X509Certificate2 certificate = null;
                            string certificatePassword = clientCertificatePassWord.value;
                            Stream s2 = null;
                            try
                            {
                                s2 = AasxServer.Program.env[envIndex].GetLocalStreamFromPackage(clientCertificate.value);
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
                                    Console.WriteLine("Client certificate: " + clientCertificate.value);
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

                                accessToken.value = response.AccessToken;
                                accessToken.setTimeStamp(timeStamp);
                            }
                        }
                        break;
                }
            }
        }

        static void operation_get_put(AdminShell.Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection
            // inputVariable sourceEndPoint: property
            // inputVariable reference sourcePath: property
            // inputVariable reference destinationElement: collection

            if (op.inputVariable.Count < 3 || op.inputVariable.Count > 4)
            {
                return;
            }
            string opName = op.idShort.ToLower();

            AdminShell.SubmodelElementCollection authentication = null;
            AdminShell.Property authType = null;
            AdminShell.Property accessToken = null;
            AdminShell.Property userName = null;
            AdminShell.Property passWord = null;
            AdminShell.File authServerCertificate = null;
            AdminShell.Property endPoint = null;
            AdminShell.Property path = null;
            AdminShell.SubmodelElementCollection elementCollection = null;
            AdminShell.Submodel elementSubmodel = null;

            AdminShell.SubmodelElementCollection smec = null;
            AdminShell.Submodel sm = null;
            AdminShell.Property p = null;
            foreach (var input in op.inputVariable)
            {
                smec = null;
                sm = null;
                p = null;
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    p = (inputRef as AdminShell.Property);
                }
                if (inputRef is AdminShell.ReferenceElement)
                {
                    var refElement = Program.env[0].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                    if (refElement is AdminShell.SubmodelElementCollection)
                        smec = refElement as AdminShell.SubmodelElementCollection;
                    if (refElement is AdminShell.Submodel)
                        sm = refElement as AdminShell.Submodel;
                }
                switch (inputRef.idShort.ToLower())
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
                }
            }

            if (authentication != null)
            {
                smec = authentication;
                int countSmec = smec.value.Count;
                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                {
                    var sme2 = smec.value[iSmec].submodelElement;
                    var idShort = sme2.idShort.ToLower();

                    switch (idShort)
                    {
                        case "authtype":
                            if (sme2 is AdminShell.Property)
                            {
                                authType = sme2 as AdminShell.Property;
                            }
                            break;
                        case "accesstoken":
                            if (sme2 is AdminShell.Property)
                            {
                                accessToken = sme2 as AdminShell.Property;
                            }
                            break;
                        case "username":
                            if (sme2 is AdminShell.Property)
                            {
                                userName = sme2 as AdminShell.Property;
                            }
                            break;
                        case "password":
                            if (sme2 is AdminShell.Property)
                            {
                                passWord = sme2 as AdminShell.Property;
                            }
                            break;
                        case "authservercertificate":
                            if (sme2 is AdminShell.File)
                            {
                                authServerCertificate = sme2 as AdminShell.File;
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
                client.SetBearerToken(accessToken.value);

            string requestPath = endPoint.value + "/" + path.value;
            HttpResponseMessage response = null;
            Task task = null;

            if (opName == "get")
            {
                try
                {
                    task = Task.Run(async () => { response = await client.GetAsync(requestPath, HttpCompletionOption.ResponseHeadersRead); });
                    task.Wait();
                    if (!response.IsSuccessStatusCode) return;
                }
                catch
                {
                    return;
                }

                string json = response.Content.ReadAsStringAsync().Result;
                AdminShell.SubmodelElementCollection receiveCollection = null;
                AdminShell.Submodel receiveSubmodel = null;
                try
                {
                    if (elementCollection != null)
                    {
                        JObject parsed = JObject.Parse(json);
                        foreach (JProperty jp1 in (JToken)parsed)
                        {
                            if (jp1.Name == "elem")
                            {
                                string text = jp1.Value.ToString();
                                receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.SubmodelElementCollection>(
                                    text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                elementCollection.value = receiveCollection.value;
                                elementCollection.setTimeStamp(timeStamp);
                            }
                        }
                    }
                    if (elementSubmodel != null)
                    {
                        receiveSubmodel = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.Submodel>(
                            json, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        receiveSubmodel.setTimeStamp(timeStamp);
                        receiveSubmodel.SetAllParents(timeStamp);

                        // need id for idempotent behaviour
                        if (receiveSubmodel.identification == null || receiveSubmodel.identification.id != elementSubmodel.identification.id)
                            return;

                        var aas = Program.env[0].AasEnv.FindAASwithSubmodel(receiveSubmodel.identification);

                        // datastructure update
                        if (Program.env == null || Program.env[0].AasEnv == null || Program.env[0].AasEnv.Assets == null)
                            return;

                        // add Submodel
                        var existingSm = Program.env[0].AasEnv.FindSubmodel(receiveSubmodel.identification);
                        if (existingSm != null)
                            Program.env[0].AasEnv.Submodels.Remove(existingSm);
                        Program.env[0].AasEnv.Submodels.Add(receiveSubmodel);
                    }
                }
                catch
                {
                    return;
                }
                Program.signalNewData(1);
            }
            if (opName == "put")
            {
                string json = "";
                if (elementCollection != null)
                    json = JsonConvert.SerializeObject(elementCollection, Formatting.Indented);
                if (elementSubmodel != null)
                    json = JsonConvert.SerializeObject(elementSubmodel, Formatting.Indented);
                if (json != "")
                {
                    try
                    {
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        task = Task.Run(async () => { response = await client.PutAsync(requestPath, content); });
                        task.Wait();
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }

        static Thread tasksThread;
        public static void tasksSamplingLoop()
        {
            while (true)
            {
                tasksCyclic();
            }
        }

        public static void tasksCyclic()
        {
            if (Program.isLoading)
                return;

            DateTime timeStamp = DateTime.Now;

            foreach (var t in taskList)
            {
                if (t.taskType?.value.ToLower() == "cyclic")
                {
                    if (t.nextExecution > timeStamp)
                        continue;
                    if (t.cycleCount != null)
                    {
                        if (t.cycleCount.value == "")
                            t.cycleCount.value = "0";
                        t.cycleCount.value = (Convert.ToInt32(t.cycleCount.value) + 1).ToString();
                        t.cycleCount.setTimeStamp(timeStamp);
                    }
                    t.nextExecution = timeStamp.AddMilliseconds(Convert.ToInt32(t.cycleTime.value));
                    if (t.nextCycle != null)
                    {
                        t.nextCycle.value = t.nextExecution.ToString();
                        t.nextCycle.setTimeStamp(timeStamp);
                    }
                    Program.signalNewData(0);

                    runOperations(t.def, t.envIndex, timeStamp);
                }
            }
        }
    }
}
