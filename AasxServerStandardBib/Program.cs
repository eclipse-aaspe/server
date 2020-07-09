using AasOpcUaServer;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Opc.Ua.Client;
using System;
using System.IO;
using System.IO.Packaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AasxRestServerLibrary;
using System.Timers;
using Newtonsoft.Json;
using Grapevine;
using Grapevine.Client;
using Grapevine.Shared;
using Formatting = Newtonsoft.Json.Formatting;
using AasxMqttServer;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net;
using System.Dynamic;
using Jose;
// using AASXLoader;

namespace Net46ConsoleServer
{
    static public class Program
    {
        public static int envimax = 100;
        public static AdminShellPackageEnv[] env = new AdminShellPackageEnv[100]
            {
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null
            };
        public static string[] envFileName = new string[100]
            {
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null
            };

        public static string[] envSymbols = new string[100]
            {
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null
            };

        public static string[] envSubjectIssuer = new string[100]
            {
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null
            };

        public static string hostPort = "";
        public static ulong dataVersion = 0;

        public static void changeDataVersion() { dataVersion++; }
        public static ulong getDataVersion() { return (dataVersion); }

        static Dictionary<string, SampleClient.UASampleClient> OPCClients = new Dictionary<string, SampleClient.UASampleClient>();
        static Boolean opcclientActive;
        static readonly object opcclientAddLock = new object(); // object for lock around connecting to an external opc server

        static MqttServer AASMqttServer = new MqttServer();

        static bool runREST = false;
        static bool runOPC = false;
        static bool runMQTT = false;

        public static string connectServer = "";
        static string connectNodeName = "";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop = false;

        public static WebProxy proxy = null;
        public static HttpClientHandler clientHandler = null;

        public static bool noSecurity = false;
        public static bool edit = false;

        public static HashSet<object> submodelsToPublish = new HashSet<object>();
        public static HashSet<object> submodelsToSubscribe = new HashSet<object>();
        static public void Main(string[] args)
        {
            // default command line options
            string host = "localhost";
            string port = "51310";
            bool https = false;
            bool debugwait = false;
            opcclientActive = false;
            int opcclient_rate = 5000;  // 5 seconds
            string registry = null;
            string proxyFile = "";

            Console.WriteLine("--help for options and help");
            /*
            Console.WriteLine("AASX Server Version 0.9.10");
            Console.WriteLine("Copyright (c) 2019 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski");
            Console.WriteLine("Copyright (c) 2018-2019 Festo AG & Co. KG");
            Console.WriteLine("Copyright (c) 2019 Fraunhofer IOSB-INA Lemgo, eine rechtlich nicht selbstaendige Einrichtung der Fraunhofer-Gesellschaft zur Foerderung der angewandten Forschung e.V.");
            Console.WriteLine("This software is licensed under the Eclipse Public License 2.0 (EPL-2.0)");
            Console.WriteLine("The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)");
            Console.WriteLine("The Grapevine REST server framework is licensed under Apache License 2.0 (Apache-2.0)");
            Console.WriteLine("The MQTT server and client is licensed under the MIT license (MIT) (see below)");
            Console.WriteLine("Portions copyright(c) by OPC Foundation, Inc. and licensed under the Reciprocal Community License (RCL)");
            */
            Console.WriteLine(
            "Copyright(c) 2019-2020 PHOENIX CONTACT GmbH & Co.KG <opensource@phoenixcontact.com>, author: Andreas Orzelski\n" +
            "Copyright(c) 2018-2020 Festo SE & Co.KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister\n" +
            "Copyright(c) 2019-2020 Fraunhofer IOSB-INA Lemgo, eine rechtlich nicht selbstaendige Einrichtung der Fraunhofer - Gesellschaft\n" +
            "zur Foerderung der angewandten Forschung e.V.\n" +
            "This software is licensed under the Eclipse Public License 2.0 (EPL - 2.0)\n" +
            "The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)\n" +
            "The Grapevine REST server framework is licensed under Apache License 2.0 (Apache - 2.0)\n" +
            "The MQTT server and client is licensed under the MIT license (MIT)\n" +
            "Portions copyright(c) by OPC Foundation, Inc.and licensed under the Reciprocal Community License (RCL)\n" +
            "Jose-JWT is licensed under the MIT license (MIT)\n" +
            "Font Awesome is licensed under the Font Awesome Free License\n" +
            "This application is a sample application for demonstration of the features of the Administration Shell.\n" +
            "It is not allowed for productive use. The implementation uses the concepts of the document Details of the Asset\n" +
            "Administration Shell published on www.plattform-i40.de which is licensed under Creative Commons CC BY-ND 3.0 DE."
            );
            Console.WriteLine("For further details see LICENSE.TXT");
            Console.WriteLine("");

            Boolean help = false;

            int i = 0;
            while (i < args.Length)
            {
                var x = args[i].Trim().ToLower();

                if (x == "-proxy")
                {
                    proxyFile = args[i + 1];
                    Console.WriteLine(args[i] + " " + args[i + 1]);
                    i += 2;
                    continue;
                }

                if (x == "-host")
                {
                    host = args[i + 1];
                    Console.WriteLine(args[i] + " " + args[i + 1]);
                    i += 2;
                    continue;
                }

                if (x == "-port")
                {
                    port = args[i + 1];
                    Console.WriteLine(args[i] + " " + args[i + 1]);
                    i += 2;
                    continue;
                }

                if (x == "-https")
                {
                    https = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "-datapath")
                {
                    AasxHttpContextHelper.DataPath = args[i + 1];
                    Console.WriteLine(args[i] + " " + args[i + 1]);
                    i += 2;
                    continue;
                }

                if (x == "-debugwait")
                {
                    debugwait = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "-opcclient")
                {
                    opcclientActive = true;
                    Console.WriteLine(args[i] + " " + args[i + 1]);
                    int rate = 0;
                    if (Int32.TryParse(args[i + 1], out rate))
                    {
                        if (rate < 200)
                        {
                            Console.WriteLine("Recommend an OPC client update rate > 200 ms.");
                        }
                        opcclient_rate = rate;
                    }
                    i += 2;
                    continue;
                }

                if (x == "-registry")
                {
                    registry = args[i + 1];
                    Console.WriteLine(args[i] + " " + args[i + 1]);
                    i += 2;
                    continue;
                }

                if (x == "-rest")
                {
                    runREST = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "-opc")
                {
                    runOPC = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "-mqtt")
                {
                    runMQTT = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "-connect")
                {
                    string[] c = null;
                    if (i+1 < args.Length)
                    {
                        string connect = args[i + 1];
                        c = connect.Split(',');
                    }
                    if (c != null && c.Length == 3)
                    {
                        connectServer = c[0];
                        connectNodeName = c[1];
                        connectUpdateRate = Convert.ToInt32(c[2]);
                        i += 2;
                    }
                    else
                    {
                        connectServer = "http://admin-shell-io.com:52000";
                        Byte[] barray = new byte[10];
                        RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                        rngCsp.GetBytes(barray);
                        connectNodeName = "AasxServer_" + Convert.ToBase64String(barray);
                        connectUpdateRate = 2000;
                        i++;
                    }
                    Console.WriteLine("-connect: ConnectServer " + connectServer + ", NodeName " + connectNodeName + ", UpdateRate " + connectUpdateRate);
                    continue;
                }

                if (x == "-nosecurity")
                {
                    noSecurity = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "-edit")
                {
                    edit = true;
                    Console.WriteLine(args[i]);
                    i++;
                    continue;
                }

                if (x == "--help")
                {
                    help = true;
                    break;
                }
            }

            if(!(runREST || runOPC || runMQTT))
            {
                Console.WriteLine();
                Console.WriteLine("Please specifiy -REST and/or -OPC and/or -MQTT");
                Console.WriteLine();
                help = true;
            }

            if (help)
            {
                Console.WriteLine("-host HOSTIP");
                Console.WriteLine("-port HOSTPORT");
                Console.WriteLine("-https = SSL connection, you must bind certificate to port before");
                Console.WriteLine("-datapath PATH_TO_AASX_FILES");
                Console.WriteLine("-REST = start REST server");
                Console.WriteLine("-OPC = start OPC server");
                Console.WriteLine("-MQTT = start MQTT publisher");
                Console.WriteLine("-debugwait = wait for Debugger to attach");
                Console.WriteLine("-opcclient UPDATERATE = time in ms between getting new values");
                Console.WriteLine("-connect SERVER,NAME,UPDATERATE = AAS Connect Server, Node Name, time in ms between publishing/subscribing new values");
                Console.WriteLine("-registry = server IP of BaSyx registry");
                // Console.WriteLine("FILENAME.AASX");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("");

            // auf Debugger warten
            if (debugwait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", host);
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            // Proxy
            string proxyAddress = "";
            string username = "";
            string password = "";

            if (proxyFile != "")
            {
                if (!File.Exists(proxyFile))
                {
                    Console.WriteLine(proxyFile + " not found!");
                    Console.ReadLine();
                    return;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(proxyFile))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(proxyFile + " not found!");
                }

                if (proxyAddress != "")
                {
                    proxy = new WebProxy();
                    Uri newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    proxy.Credentials = new NetworkCredential(username, password);
                    // proxy.BypassProxyOnLocal = true;
                    Console.WriteLine("Using proxy: " + proxyAddress);

                    clientHandler = new HttpClientHandler
                    {
                        Proxy = proxy,
                        UseProxy = true
                    };
                }
            };

            hostPort = host + ":" + port;

            // Read root cert from root subdirectory
            Console.WriteLine("Security 1 Startup - Server");
            Console.WriteLine("Security 1.1 Load X509 Root Certificates into X509 Store Root");

            X509Store root = new X509Store("Root", StoreLocation.CurrentUser);
            root.Open(OpenFlags.ReadWrite);

            System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(".");

            foreach (System.IO.FileInfo f in ParentDirectory.GetFiles("./root/*.cer"))
            {
                X509Certificate2 cert = new X509Certificate2("./root/" + f.Name);

                root.Add(cert);
                Console.WriteLine("Security 1.1 Add " + f.Name);
            }

            Directory.CreateDirectory("./temp");

            // Register AAS to registry server
            if (registry != null)
            {
                // AASXLoader.Registry.RegisterAASX(registry, host + ":" + port, AasxHttpContextHelper.DataPath);
                /*
                Console.WriteLine();
                Console.WriteLine("*** Include #210 in Program.cs and AASXLoader in solution ***");
                Console.WriteLine();
                */
            }

            string fn = null;

            if (runOPC)
            {
                Boolean is_BaseAddresses = false;
                Boolean is_uaString = false;
                XmlTextReader reader = new XmlTextReader("Opc.Ua.SampleServer.Config.xml");
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            if (reader.Name == "BaseAddresses")
                                is_BaseAddresses = true;
                            if (reader.Name == "ua:String")
                                is_uaString = true;
                            break;
                        case XmlNodeType.Text: //Display the text in each element.
                            if (is_BaseAddresses && is_uaString)
                            {
                                Console.WriteLine("Connect to OPC UA by: {0}", reader.Value);
                                is_BaseAddresses = false;
                                is_uaString = false;
                            }
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            break;
                    }
                }
            }

            // ParentDirectory = new System.IO.DirectoryInfo(AasxHttpContextHelper.DataPath);

            int envi = 0;

            string[] fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx");
            Array.Sort(fileNames);

            // foreach (System.IO.FileInfo f in ParentDirectory.GetFiles("*.aasx"))
            while (envi < fileNames.Length)
            {
                // fn = f.Name;
                fn = fileNames[envi];

                if (fn != "" && envi < envimax)
                {
                    // fn = AasxHttpContextHelper.DataPath + "/" + fn;
                    Console.WriteLine("Loading {0}...", fn);
                    envFileName[envi] = fn;
                    env[envi] = new AdminShellPackageEnv(fn);
                    if (env[envi] == null)
                    {
                        Console.Out.WriteLine($"Cannot open {fn}. Aborting..");
                        return;
                    }
                    // check if signed
                    string name = Path.GetFileName(fn);
                    string fileCert = "./user/" + name + ".cer";
                    if (File.Exists(fileCert))
                    {
                        X509Certificate2 x509 = new X509Certificate2(fileCert);
                        envSymbols[envi] = "S";
                        envSubjectIssuer[envi] = x509.Subject;

                        X509Chain chain = new X509Chain();
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        bool isValid = chain.Build(x509);
                        if (isValid)
                        {
                            envSymbols[envi] += ";V";
                            envSubjectIssuer[envi] += ";" + x509.Issuer;
                        }
                    }

                }
                envi++;
            }

            fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx2");
            Array.Sort(fileNames);

            // foreach (System.IO.FileInfo f in ParentDirectory.GetFiles("*.aasx"))
            for (int j = 0; j < fileNames.Length; j++)
            {
                // fn = f.Name;
                fn = fileNames[j];

                if (fn != "" && envi < envimax)
                {
                    envFileName[envi] = fn;
                    envSymbols[envi] = "L"; // Show lock
                }
                envi++;
            }

            AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
            AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers

            Console.WriteLine();
            Console.WriteLine("Please wait for servers starting...");

            RunScript(); // Initialize

            if (runREST)
            {
                Console.WriteLine("Connect to REST by: {0}:{1}", host, port);

                // AasxRestServer.Start(env, host, port, new GrapevineLoggerToConsole());
                AasxRestServer.Start(env, host, port, https); // without Logger

                // SetRestTimer(10000); // GET and PUT every 10 seconds
                                     // OnRestTimedEvent(null, null);

                Console.WriteLine("REST Server started..");
            }

            if (runMQTT)
            {
                AASMqttServer.MqttSeverStartAsync().Wait();
                Console.WriteLine("MQTT Publisher started..");
            }

            MySampleServer server = null;
            if (runOPC)
            {
                server = new MySampleServer(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env);
                Console.WriteLine("OPC UA Server started..");
            }

            if (opcclientActive) // read data by OPC UA
            {
                // Initial read of OPC values, will quit the program if it return false
                if (!ReadOPCClient(true))
                {
                    return;
                }

                Console.WriteLine("OPC client updating every {0} ms.", opcclient_rate);
                SetOPCClientTimer(opcclient_rate); // read again everytime timer expires
            }

            SetScriptTimer(2000);

            if (connectServer != "")
            {
                HttpClient httpClient;
                if (clientHandler == null)
                {
                    clientHandler = new HttpClientHandler();
                    clientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    httpClient = new HttpClient(clientHandler);
                }
                httpClient = new HttpClient(clientHandler);

                string payload = "{ \"source\" : \"" + connectNodeName + "\" }";

                /*
                string content = "";
                try
                {
                    var contentJson = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                    // httpClient.PostAsync("http://" + connectServer + "/connect", contentJson).Wait();
                    var result = httpClient.PostAsync(connectServer + "/connect", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch
                {

                }
                */
                string content = "OK";
                if (content == "OK")
                {
                    connectThread = new Thread(new ThreadStart(connectThreadLoop));
                    connectThread.Start();
                    connectLoop = true;
                }
                else
                {
                    Console.WriteLine("Can not connect to: " + connectServer);
                }
            }

            if (runOPC && server != null)
            {
                server.Run(); // wait for CTRL-C
            }
            else
            {
                // no OPC UA: wait only for CTRL-C
                Console.WriteLine("Servers succesfully started. Press Ctrl-C to exit...");
                ManualResetEvent quitEvent = new ManualResetEvent(false);
                try
                {
                    Console.CancelKeyPress += (sender, eArgs) =>
                    {
                        quitEvent.Set();
                        eArgs.Cancel = true;
                    };
                }
                catch
                {
                }

                // wait for timeout or Ctrl-C
                quitEvent.WaitOne(Timeout.Infinite);
            }

            // wait for RETURN

            if (connectServer != "")
            {
                /*
                HttpClient httpClient;
                if (clientHandler != null)
                {
                    httpClient = new HttpClient(clientHandler);
                }
                else
                {
                    httpClient = new HttpClient();
                }

                string payload = "{ \"source\" : \"" + connectNodeName + "\" }";

                string content = "";
                try
                {
                    var contentJson = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                    // httpClient.PostAsync("http://" + connectServer + "/disconnect", contentJson).Wait();
                    var result = httpClient.PostAsync(connectServer + "/disconnect", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch
                {

                }
                */

                if (connectLoop)
                {
                    connectLoop = false;
                    // connectThread.Abort();
                }
            }

            if (runMQTT)
            {
                AASMqttServer.MqttSeverStopAsync().Wait();
            }

            AasxRestServer.Stop();
        }

        public static string ContentToString(this HttpContent httpContent)
        {
            var readAsStringAsync = httpContent.ReadAsStringAsync();
            return readAsStringAsync.Result;
        }

        public class aasListParameters
        {
            public int index;
            public string idShort;
            public string identification;
            public string fileName;
        }
        public class aasDirectoryParameters
        {
            public string source;
            public List<aasListParameters> aasList;
            public aasDirectoryParameters()
            {
                aasList = new List<aasListParameters> { };
            }
        }

        public class TransmitData
        {
            public string source;
            public string destination;
            public string type;
            public string encrypt;
            public string extensions;
            public List<string> publish;
            public TransmitData()
            {
                publish = new List<string> { };
            }
        }

        public class TransmitFrame
        {
            public string source;
            public List<TransmitData> data;
            public TransmitFrame()
            {
                data = new List<TransmitData> { };
            }
        }

        static bool connectInit = true;

        static List<TransmitData> tdPending = new List<TransmitData> { };

        public static void connectThreadLoop()
        {
            while (connectLoop)
            {
                TransmitFrame tf = new TransmitFrame
                {
                    source = connectNodeName
                };
                TransmitData td = new TransmitData
                {
                    source = connectNodeName
                };

                if (connectInit)
                {
                    aasDirectoryParameters adp = new aasDirectoryParameters();

                    adp.source = connectNodeName;

                    // var aaslist = new List<string>();
                    int aascount = Net46ConsoleServer.Program.env.Length;

                    for (int j = 0; j < aascount; j++)
                    {
                        aasListParameters alp = new aasListParameters();

                        if (Net46ConsoleServer.Program.env[j] != null)
                        {
                            alp.index = j;
                            alp.idShort = Net46ConsoleServer.Program.env[j].AasEnv.AdministrationShells[0].idShort;
                            alp.identification = Net46ConsoleServer.Program.env[j].AasEnv.AdministrationShells[0].identification.ToString();
                            alp.fileName = Net46ConsoleServer.Program.envFileName[j];

                            adp.aasList.Add(alp);
                        }
                    }

                    var json = JsonConvert.SerializeObject(adp, Newtonsoft.Json.Formatting.Indented);
                    td.type = "directory";
                    td.publish.Add(json);
                    tf.data.Add(td);

                    connectInit = false;
                }
                else
                {
                    if (tdPending.Count != 0)
                    {
                        foreach (TransmitData tdp in tdPending)
                        {
                            tf.data.Add(tdp);
                        }
                        tdPending.Clear();
                    }
                    int envi = 0;
                    while (env[envi] != null)
                    {
                        foreach (var sm in env[envi].AasEnv.Submodels)
                        {
                            if (sm != null && sm.idShort != null)
                            {
                                bool toPublish = Program.submodelsToPublish.Contains(sm);
                                if (!toPublish)
                                {
                                    int count = sm.qualifiers.Count;
                                    if (count != 0)
                                    {
                                        int j = 0;

                                        while (j < count) // Scan qualifiers
                                        {
                                            var p = sm.qualifiers[j] as AdminShell.Qualifier;

                                            if (p.type == "PUBLISH")
                                            {
                                                toPublish = true;
                                            }
                                            j++;
                                        }
                                    }
                                }
                                if (toPublish)
                                {
                                    var json = JsonConvert.SerializeObject(sm, Newtonsoft.Json.Formatting.Indented);
                                    td.type = "submodel";
                                    td.publish.Add(json);
                                    tf.data.Add(td);
                                    Console.WriteLine("Publish Submodel " + sm.idShort);
                                }
                            }
                        }
                        envi++;
                    }
                }

                string publish = JsonConvert.SerializeObject(tf, Formatting.Indented);

                HttpClient httpClient;
                if (clientHandler != null)
                {
                    httpClient = new HttpClient(clientHandler);
                }
                else
                {
                    httpClient = new HttpClient();
                }
                var contentJson = new StringContent(publish, System.Text.Encoding.UTF8, "application/json");

                string content = "";
                try
                {
                    var result = httpClient.PostAsync(connectServer + "/publish", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch
                {

                }

                if (content != "")
                {
                    string node = "";

                    try
                    {
                        TransmitFrame tf2 = new TransmitFrame();
                        tf2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TransmitFrame>(content);

                        node = tf2.source;
                        foreach (TransmitData td2 in tf2.data)
                        {
                            if (td2.type == "getaasx" && td2.destination == connectNodeName)
                            {
                                int aasIndex = Convert.ToInt32(td2.extensions);

                                dynamic res = new ExpandoObject();

                                Byte[] binaryFile = File.ReadAllBytes(Net46ConsoleServer.Program.envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                                string fileToken = Jose.JWT.Encode(payload, enc.GetBytes(AasxRestServerLibrary.AasxHttpContextHelper.secretString), JwsAlgorithm.HS256);

                                res.fileName = Path.GetFileName(Net46ConsoleServer.Program.envFileName[aasIndex]);
                                res.fileData = fileToken;

                                string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                                TransmitData tdp = new TransmitData();

                                tdp.source = connectNodeName;
                                tdp.destination = td2.source;
                                tdp.type = "getaasxFile";
                                tdp.publish.Add(responseJson);
                                tdPending.Add(tdp);
                            }

                            if (td2.type == "submodel")
                            {
                                foreach (string sm in td2.publish)
                                {
                                    AdminShell.Submodel submodel = null;
                                    try
                                    {
                                        using (TextReader reader = new StringReader(sm))
                                        {
                                            JsonSerializer serializer = new JsonSerializer();
                                            serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                            submodel = (AdminShell.Submodel)serializer.Deserialize(reader, typeof(AdminShell.Submodel));
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Can not read SubModel!");
                                        return;
                                    }

                                    // need id for idempotent behaviour
                                    if (submodel.identification == null)
                                    {
                                        Console.WriteLine("Identification of SubModel is (null)!");
                                        return;
                                    }

                                    AdminShell.AdministrationShell aas = null;
                                    int envi = 0;
                                    while (env[envi] != null)
                                    {
                                        aas = env[envi].AasEnv.FindAASwithSubmodel(submodel.identification);
                                        if (aas != null)
                                            break;
                                        envi++;
                                    }


                                    if (aas != null)
                                    {
                                        // datastructure update
                                        if (env == null || env[envi].AasEnv == null || env[envi].AasEnv.Assets == null)
                                        {
                                            Console.WriteLine("Error accessing internal data structures.");
                                            return;
                                        }

                                        var existingSm = env[envi].AasEnv.FindSubmodel(submodel.identification);
                                        if (existingSm != null)
                                        {
                                            bool toSubscribe = Program.submodelsToSubscribe.Contains(existingSm);
                                            if (!toSubscribe)
                                            {
                                                int eqcount = existingSm.qualifiers.Count;
                                                if (eqcount != 0)
                                                {
                                                    int j = 0;

                                                    while (j < eqcount) // Scan qualifiers
                                                    {
                                                        var p = existingSm.qualifiers[j] as AdminShell.Qualifier;

                                                        if (p.type == "SUBSCRIBE")
                                                        {
                                                            toSubscribe = true;
                                                            break;
                                                        }
                                                        j++;
                                                    }
                                                }


                                            }

                                            if (toSubscribe)
                                            {
                                                Console.WriteLine("Subscribe Submodel " + submodel.idShort);

                                                int c2 = submodel.qualifiers.Count;
                                                if (c2 != 0)
                                                {
                                                    int k = 0;

                                                    while (k < c2) // Scan qualifiers
                                                    {
                                                        var q = submodel.qualifiers[k] as AdminShell.Qualifier;

                                                        if (q.type == "PUBLISH")
                                                        {
                                                            q.type = "SUBSCRIBE";
                                                        }

                                                        k++;
                                                    }
                                                }

                                                bool overwrite = true;
                                                int escount = existingSm.submodelElements.Count;
                                                int count2 = submodel.submodelElements.Count;
                                                if (escount == count2)
                                                {
                                                    int smi = 0;
                                                    while (smi < escount)
                                                    {
                                                        var sme1 = submodel.submodelElements[smi].submodelElement;
                                                        var sme2 = existingSm.submodelElements[smi].submodelElement;

                                                        if (sme1 is AdminShell.Property)
                                                        {
                                                            if (sme2 is AdminShell.Property)
                                                            {
                                                                (sme2 as AdminShell.Property).value = (sme1 as AdminShell.Property).value;
                                                            }
                                                            else
                                                            {
                                                                overwrite = false;
                                                                break;
                                                            }
                                                        }

                                                        smi++;
                                                    }
                                                }

                                                if (!overwrite)
                                                {
                                                    env[envi].AasEnv.Submodels.Remove(existingSm);
                                                    env[envi].AasEnv.Submodels.Add(submodel);

                                                    // add SubmodelRef to AAS            
                                                    // access the AAS
                                                    var newsmr = AdminShell.SubmodelRef.CreateNew("Submodel", true, submodel.identification.idType, submodel.identification.id);
                                                    var existsmr = aas.HasSubmodelRef(newsmr);
                                                    if (!existsmr)
                                                    {
                                                        aas.AddSubmodelRef(newsmr);
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
                    }
                }

                NewDataAvailable?.Invoke(null, EventArgs.Empty);
                Thread.Sleep(connectUpdateRate);
            }
        }

        private static System.Timers.Timer OPCClientTimer;
        static bool timerSet = false;
        private static void SetOPCClientTimer(double value)
        {
            if (!timerSet)
            {
                // Create a timer with an specified interval.
                OPCClientTimer = new System.Timers.Timer(value);
                // Hook up the Elapsed event for the timer. 
                OPCClientTimer.Elapsed += OnOPCClientNextTimedEvent;
                OPCClientTimer.AutoReset = true;
                OPCClientTimer.Enabled = true;

                timerSet = true;
            }
        }

        public static event EventHandler NewDataAvailable; 
        private static void OnOPCClientNextTimedEvent(Object source, ElapsedEventArgs e)
        {
            ReadOPCClient(false);
            // RunScript();
            NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }

        private static System.Timers.Timer scriptTimer;
        private static void SetScriptTimer(double value)
        {
            // Create a timer with a two second interval.
            scriptTimer = new System.Timers.Timer(value);
            // Hook up the Elapsed event for the timer. 
            scriptTimer.Elapsed += OnScriptTimedEvent;
            scriptTimer.AutoReset = true;
            scriptTimer.Enabled = true;
        }

        private static void OnScriptTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Console.WriteLine("RunScript");
            RunScript();
            NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }

        private static System.Timers.Timer restTimer;
        private static void SetRestTimer(double value)
        {
            // Create a timer with a two second interval.
            restTimer = new System.Timers.Timer(value);
            // Hook up the Elapsed event for the timer. 
            restTimer.Elapsed += OnRestTimedEvent;
            restTimer.AutoReset = true;
            restTimer.Enabled = true;
        }

        static bool RESTalreadyRunning = false;
        static long countGetPut = 0;

        private static void OnRestTimedEvent(Object source, ElapsedEventArgs e)
        {
            //            if (RESTalreadyRunning)
            //                return;
            // return;

            RESTalreadyRunning = true;

            // string GETSUBMODEL = "OPC3";
            // string GETURL = "http://192.168.1.10:51310";
            // string PUTSUBMODEL = "OPC3";
            // string PUTURL = "http://lin-eu-tsdvc03.europe.phoenixcontact.com:51310";
            string GETSUBMODEL = "";
            string GETURL = "";
            string PUTSUBMODEL = "";
            string PUTURL = "";

            // Search for submodel REST and scan qualifiers for GET and PUT commands
            foreach (var sm in env[0].AasEnv.Submodels)
            {
                if (sm != null && sm.idShort != null && sm.idShort == "REST")
                {
                    int count = sm.qualifiers.Count;
                    if (count != 0)
                    {
                        int j = 0;

                        while (j < count) // Scan qualifiers
                        {
                            var p = sm.qualifiers[j] as AdminShell.Qualifier;

                            if (p.type == "GETSUBMODEL")
                            {
                                GETSUBMODEL = p.value;
                            }
                            if (p.type == "GETURL")
                            {
                                GETURL = p.value;
                            }
                            if (p.type == "PUTSUBMODEL")
                            {
                                PUTSUBMODEL = p.value;
                            }
                            if (p.type == "PUTURL")
                            {
                                PUTURL = p.value;
                            }

                            j++;
                        }
                    }
                }
            }

            if (GETSUBMODEL != "" && GETURL != "") // GET
            {
                Console.WriteLine("{0} GET Submodel {1} from URL {2}.", countGetPut++, GETSUBMODEL, GETURL);

                var sm = "";
                try
                {
                    var client = new AasxRestServerLibrary.AasxRestClient(GETURL);
                    sm = client.GetSubmodel(GETSUBMODEL);
                }
                catch (Exception)
                {
                    Console.WriteLine("Can not connect to REST server {0}.", GETURL);
                }

                AdminShell.Submodel submodel = null;
                try
                {
                    using (TextReader reader = new StringReader(sm))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        submodel = (AdminShell.Submodel)serializer.Deserialize(reader, typeof(AdminShell.Submodel));
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Can not read SubModel {0}.", GETSUBMODEL);
                    return;
                }

                // need id for idempotent behaviour
                if (submodel.identification == null)
                {
                    Console.WriteLine("Identification of SubModel {0} is (null).", GETSUBMODEL);
                    return;
                }

                var aas = env[0].AasEnv.FindAASwithSubmodel(submodel.identification);

                // datastructure update
                if (env == null || env[0].AasEnv == null || env[0].AasEnv.Assets == null)
                {
                    Console.WriteLine("Error accessing internal data structures.");
                    return;
                }

                // add Submodel
                var existingSm = env[0].AasEnv.FindSubmodel(submodel.identification);
                if (existingSm != null)
                    env[0].AasEnv.Submodels.Remove(existingSm);
                env[0].AasEnv.Submodels.Add(submodel);

                // add SubmodelRef to AAS            
                // access the AAS
                var newsmr = AdminShell.SubmodelRef.CreateNew("Submodel", true, submodel.identification.idType, submodel.identification.id);
                var existsmr = aas.HasSubmodelRef(newsmr);
                if (!existsmr)
                {
                    aas.AddSubmodelRef(newsmr);
                }
            }

            if (PUTSUBMODEL != "" && PUTURL != "") // PUT
            {
                Console.WriteLine("{0} PUT Submodel {1} from URL {2}.", countGetPut++, PUTSUBMODEL, PUTURL);

                {
                    foreach (var sm in env[0].AasEnv.Submodels)
                    {
                        if (sm != null && sm.idShort != null && sm.idShort == PUTSUBMODEL)
                        {
                            var json = JsonConvert.SerializeObject(sm, Newtonsoft.Json.Formatting.Indented);

                            try
                            {
                                var client = new AasxRestServerLibrary.AasxRestClient(PUTURL);
                                // theOnlineConnection = client;
                                string result = client.PutSubmodel(json);
                            }
                            catch
                            {
                                Console.WriteLine("Can not connect to REST server {0}", PUTURL);
                            }
                        }
                    }
                }
            }

            RESTalreadyRunning = false;

            // start MQTT Client as a worker (will start in the background)
            // Console.WriteLine("Publish MQTT");
            var worker = new BackgroundWorker();
            worker.DoWork += async (s1, e1) =>
            {

                //AasxRestServerLibrary.AasxRestServer.Start(this.thePackageEnv, Options.RestServerHost, Options.RestServerPort, logger);
                try
                {
                    // await AasxMqttClient.MqttClient.StartAsync(this.thePackageEnv, logger);
                    await AasxMqttClient.MqttClient.StartAsync(env);
                }
                catch (Exception)
                {
                    // logger.Error(e);
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
                // in any case, close flyover
                // CloseFlyover();
            };
            worker.RunWorkerAsync();
        }

        private static Boolean OPCWrite(string nodeId, object value)
        /// <summary>
        /// Writes to (i.e. updates values of) Nodes in the AAS OPC Server
        /// </summary>
        {
            if (!runOPC)
            {
                return true;
            }

            AasOpcUaServer.AasNodeManager nodeMgr = AasOpcUaServer.AasEntityBuilder.nodeMgr;

            if (nodeMgr == null)
            {
                // if Server has not started yet, the AasNodeManager is null
                Console.WriteLine("OPC NodeManager not initialized.");
                return false;
            }

            // Find node in Core3OPC Server to update it
            BaseVariableState bvs = nodeMgr.Find(nodeId) as BaseVariableState;

            if (bvs == null)
            {
                Console.WriteLine("node {0} does not exist in server!", nodeId);
                //throw new ArgumentNullException("Error on OPCWrite. Node does not exist?");
                return false;
            }
            var convertedValue = Convert.ChangeType(value, bvs.Value.GetType()) ;
            if (!object.Equals(bvs.Value, convertedValue))
            {
                bvs.Value = convertedValue;
                // TODO: timestamp UtcNow okay or get this internally from the Server?
                bvs.Timestamp = DateTime.UtcNow;
                bvs.ClearChangeMasks(null, false);
            }
            return true;
        }

        static Boolean ReadOPCClient(bool initial)
        /// <summary>
        /// Update AAS property values from external OPC servers.
        /// Only submodels which have the appropriate qualifier are affected.
        /// However, this will attempt to get values for all properties of the submodel.
        /// TODO: Possilby add a qualifier to specifiy which values to get? Or NodeIds per alue?
        /// </summary>
        {
            if (env == null)
                return false;

            int i = 0;
            while (env[i] != null)
            {
                foreach (var sm in env[i].AasEnv.Submodels)
                {
                    if (sm != null && sm.idShort != null)
                    {
                        int count = sm.qualifiers.Count;
                        if (count != 0)
                        {
                            int stopTimeout = Timeout.Infinite;
                            bool autoAccept = true;
                            // Variablen aus AAS Qualifiern
                            string Username = "";
                            string Password = "";
                            string URL = "";
                            int Namespace = 0;
                            string Path = "";

                            int j = 0;

                            while (j < count) // URL, Username, Password, Namespace, Path
                            {
                                var p = sm.qualifiers[j] as AdminShell.Qualifier;

                                switch (p.type)
                                {
                                    case "OPCURL": // URL
                                        URL = p.value;
                                        break;
                                    case "OPCUsername": // Username
                                        Username = p.value;
                                        break;
                                    case "OPCPassword": // Password
                                        Password = p.value;
                                        break;
                                    case "OPCNamespace": // Namespace
                                                         // TODO: if not int, currently throws nondescriptive error
                                        Namespace = int.Parse(p.value);
                                        break;
                                    case "OPCPath": // Path
                                        Path = p.value;
                                        break;
                                }
                                j++;
                            }

                            if (URL == "")
                            {
                                continue;
                            }

                            if (URL == "" || Namespace == 0 || Path == "" || (Username == "" && Password != "") || (Username != "" && Password == ""))
                            {
                                Console.WriteLine("Incorrent or missing qualifier. Aborting ...");
                                return false;
                            }
                            if (Username == "" && Password == "")
                            {
                                Console.WriteLine("Using Anonymous to login ...");
                                // return false;
                            }

                            // try to get the client from dictionary, else create and add it
                            SampleClient.UASampleClient client;
                            lock (Program.opcclientAddLock)
                            {
                                if (!OPCClients.TryGetValue(URL, out client))
                                // if (!OPCClients.TryGetValue(sm.idShort, out client))
                                {
                                    try
                                    {
                                        // make OPC UA client
                                        client = new SampleClient.UASampleClient(URL, autoAccept, stopTimeout, Username, Password);
                                        Console.WriteLine("Connecting to external OPC UA Server at {0} with {1} ...", URL, sm.idShort);
                                        client.ConsoleSampleClient().Wait();
                                        // add it to the dictionary under this submodels idShort
                                        // OPCClients.Add(sm.idShort, client);
                                        OPCClients.Add(URL, client);
                                    }
                                    catch (AggregateException ae)
                                    {
                                        bool cantconnect = false;
                                        ae.Handle((x) =>
                                        {
                                            if (x is ServiceResultException)
                                            {
                                                cantconnect = true;
                                                return true; // this exception handled
                                            }
                                            return false; // others not handled, will cause unhandled exception
                                        }
                                        );
                                        if (cantconnect)
                                        {
                                            // stop processing OPC read because we couldnt connect
                                            // but return true as this shouldn't stop the main loop
                                            Console.WriteLine(ae.Message);
                                            Console.WriteLine("Could not connect to {0} with {1} ...", URL, sm.idShort);
                                            return true;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Already connected to OPC UA Server at {0} with {1} ...", URL, sm.idShort);
                                }
                            }
                            Console.WriteLine("==================================================");
                            Console.WriteLine("Read values for {0} from {1} ...", sm.idShort, URL);
                            Console.WriteLine("==================================================");

                            // over all SMEs
                            count = sm.submodelElements.Count;
                            for (j = 0; j < count; j++)
                            {
                                var sme = sm.submodelElements[j].submodelElement;
                                //Console.WriteLine("{0} contains {1}", sm.idShort, sme.idShort);
                                // some preparations for multiple AAS below
                                int serverNamespaceIdx = 3; //could be gotten directly from the nodeMgr in OPCWrite instead, only pass the string part of the Id
                                // string AASIdShort = "AAS"; // for multiple AAS, use something like env.AasEnv.AdministrationShells[i].idShort;
                                string AASSubmodel = env[i].AasEnv.AdministrationShells[0].idShort + "." + sm.idShort; // for multiple AAS, use something like env.AasEnv.AdministrationShells[i].idShort;
                                string serverNodePrefix = string.Format("ns={0};s=AASROOT.{1}", serverNamespaceIdx, AASSubmodel);
                                string nodePath = Path; // generally starts with Submodel idShort
                                WalkSubmodelElement(sme, nodePath, serverNodePrefix, client, Namespace);
                            }
                        }
                    }
                }
                i++;
            }
            if (!initial)
            {
                // StateHasChanged();
                // dataVersion++;
                changeDataVersion();
            }
            return true;
        }

        static void RunScript()
        {
            if (env == null)
                return;

            int i = 0;
            while (env[i] != null)
            {
                foreach (var sm in env[i].AasEnv.Submodels)
                {
                    if (sm != null && sm.idShort != null)
                    {
                        int count = sm.qualifiers != null ? sm.qualifiers.Count : 0;
                        if (count != 0)
                        {
                            var q = sm.qualifiers[0] as AdminShell.Qualifier;
                            if (q.type == "SCRIPT")
                            {
                                // Triple
                                // Reference to property with Number
                                // Reference to submodel with numbers/strings
                                // Reference to property to store found text
                                count = sm.submodelElements.Count;
                                int smi = 0;
                                while (smi < count)
                                {
                                    var sme1 = sm.submodelElements[smi++].submodelElement;
                                    if (sme1.qualifiers.Count == 0)
                                    {
                                        continue;
                                    }
                                    var qq = sme1.qualifiers[0] as AdminShell.Qualifier;

                                    if (qq.type == "Add")
                                    {
                                        int v = Convert.ToInt32((sme1 as AdminShell.Property).value);
                                        v += Convert.ToInt32(qq.value);
                                        (sme1 as AdminShell.Property).value = v.ToString();
                                        continue;
                                    }

                                    if (qq.type != "SearchNumber" || smi >= count)
                                    {
                                        continue;
                                    }
                                    var sme2 = sm.submodelElements[smi++].submodelElement;
                                    if (sme2.qualifiers.Count == 0)
                                    {
                                        continue;
                                    }
                                    qq = sme2.qualifiers[0] as AdminShell.Qualifier;
                                    if (qq.type != "SearchList" || smi >= count)
                                    {
                                        continue;
                                    }
                                    var sme3 = sm.submodelElements[smi++].submodelElement;
                                    if (sme3.qualifiers.Count == 0)
                                    {
                                        continue;
                                    }
                                    qq = sme3.qualifiers[0] as AdminShell.Qualifier;
                                    if (qq.type != "SearchResult")
                                    {
                                        break;
                                    }
                                    if (sme1 is AdminShell.ReferenceElement &&
                                        sme2 is AdminShell.ReferenceElement &&
                                        sme3 is AdminShell.ReferenceElement)
                                    {
                                        var r1 = sme1 as AdminShell.ReferenceElement;
                                        var r2 = sme2 as AdminShell.ReferenceElement;
                                        var r3 = sme3 as AdminShell.ReferenceElement;
                                        var ref1 = env[i].AasEnv.FindReferableByReference(r1.value);
                                        var ref2 = env[i].AasEnv.FindReferableByReference(r2.value);
                                        var ref3 = env[i].AasEnv.FindReferableByReference(r3.value);
                                        if (ref1 is AdminShell.Property && ref2 is AdminShell.Submodel && ref3 is AdminShell.Property)
                                        {
                                            var p1 = ref1 as AdminShell.Property;
                                            // Simulate changes
                                            // p1.value = Convert.ToString(Convert.ToInt32(p1.value) + 5);
                                            var sm2 = ref2 as AdminShell.Submodel;
                                            var p3 = ref3 as AdminShell.Property;
                                            int count2 = sm2.submodelElements.Count;
                                            for (int j = 0; j < count2; j++)
                                            {
                                                var sme = sm2.submodelElements[j].submodelElement;
                                                if (sme.idShort == p1.value)
                                                {
                                                    p3.value = (sme as AdminShell.Property).value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                i++;
            }
            return;
        }

        private static void WalkSubmodelElement(AdminShell.SubmodelElement sme, string nodePath, string serverNodePrefix, SampleClient.UASampleClient client, int clientNamespace )
        {
            if (sme is AdminShell.Property)
            {
                // Console.WriteLine("{0} is a Property ", sme);
                var p = sme as AdminShell.Property;
                // string clientNodeName = nodePath + "." + p.idShort;
                string clientNodeName = nodePath + p.idShort;
                string serverNodeId = string.Format("{0}.{1}.Value", serverNodePrefix, p.idShort);
                // string serverNodeId = string.Format("{0}.{1}{2}.Value", serverNodePrefix, nodePath, p.idShort);
                // string serverNodeId = string.Format("{0}.{1}.Value", nodePath, p.idShort);
                // string serverNodeId = string.Format("{0}{1}.Value", nodePath, p.idShort);
                NodeId clientNode = new NodeId(clientNodeName, (ushort)clientNamespace);
                UpdatePropertyFromOPCClient(p, serverNodeId, client, clientNode);
            }
            else if (sme is AdminShell.SubmodelElementCollection)
            {
                var collection = sme as AdminShell.SubmodelElementCollection;
                for (int i = 0; i < collection.value.Count; i++)
                {
                    //Console.WriteLine("Collection {0} contains {1}", collection.idShort, collection.value[i].submodelElement);
                    string newNodeIdBase = nodePath + "." + collection.idShort;
                    WalkSubmodelElement(collection.value[i].submodelElement, newNodeIdBase, serverNodePrefix, client, clientNamespace);
                }
            }
        }

    private static void UpdatePropertyFromOPCClient(AdminShell.Property p, string serverNodeId, SampleClient.UASampleClient client, NodeId clientNodeId)
        {
        string value;
        try
        {
            //Console.WriteLine(string.Format("Trying to read {0}", clientNodeId.ToString()));
            value = client.ReadSubmodelElementValue(clientNodeId);
            Console.WriteLine(string.Format("{0} <= {1}", serverNodeId, value));
        }
        catch (ServiceResultException ex)
        {
            Console.WriteLine(string.Format("OPC ServiceResultException ({0}) trying to read {1}", ex.Message, clientNodeId.ToString()));
            return;
        }

        // update in AAS env
        p.Set(p.valueType, value);
        // update in OPC
        
        if (!OPCWrite(serverNodeId, value))
            Console.WriteLine("OPC write not successful.");
        
    }
}

    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask = false;

        public override void Message(string text, bool ask)
        {
            this.message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            if (ask)
            {
                try
                {
                    ConsoleKeyInfo result = Console.ReadKey();
                    Console.WriteLine();
                    return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y') || (result.KeyChar == '\r'));
                }
                catch
                {
                    // intentionally fall through
                }
            }
            return await Task.FromResult(true);
        }
    }

    public enum ExitCode : int
    {
        Ok = 0,
        ErrorServerNotStarted = 0x80,
        ErrorServerRunning = 0x81,
        ErrorServerException = 0x82,
        ErrorInvalidCommandLine = 0x100
    };

    public class MySampleServer
    {
        SampleServer server;
        Task status;
        DateTime lastEventTime;
        int serverRunTime = Timeout.Infinite;
        static bool autoAccept = false;
        static ExitCode exitCode;
        static AdminShellPackageEnv [] aasxEnv = null;
        // OZ
        public static ManualResetEvent quitEvent;

        public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShellPackageEnv [] _aasxEnv)
        {
            autoAccept = _autoAccept;
            aasxEnv = _aasxEnv;
            serverRunTime = _stopTimeout == 0 ? Timeout.Infinite : _stopTimeout * 1000;
        }

        public void Run()
        {

            try
            {
                exitCode = ExitCode.ErrorServerNotStarted;
                ConsoleSampleServer().Wait();
                Console.WriteLine("Servers succesfully started. Press Ctrl-C to exit...");
                exitCode = ExitCode.ErrorServerRunning;
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                exitCode = ExitCode.ErrorServerException;
                return;
            }

            quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) => {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(serverRunTime);

            if (server != null)
            {
                Console.WriteLine("Server stopped. Waiting for exit...");
                
                using (SampleServer _server = server)
                {
                    // Stop status thread
                    server = null;
                    status.Wait();
                    // Stop server and dispose
                    _server.Stop();
                }
            }

            exitCode = ExitCode.Ok;
        }

        public static ExitCode ExitCode { get => exitCode; }

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = autoAccept;
                if (autoAccept)
                {
                    // Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
                }
                else
                {
                    // Console.WriteLine("Rejected Certificate: {0}", e.Certificate.Subject);
                }
            }
        }

        private async Task ConsoleSampleServer()
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance();

            application.ApplicationName = "UA Core Sample Server";
            application.ApplicationType = ApplicationType.Server;
            application.ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleServer" : "Opc.Ua.SampleServer";

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            // start the server.
            server = new SampleServer(aasxEnv);
            await application.Start(server);

            // start the status thread
            status = Task.Run(new Action(StatusThread));

            // print notification on session events
            server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
            server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
            server.CurrentInstance.SessionManager.SessionCreated += EventStatus;

        }

        private void EventStatus(Opc.Ua.Server.Session session, SessionEventReason reason)
        {
            lastEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }

        void PrintSessionStatus(Opc.Ua.Server.Session session, string reason, bool lastContact = false)
        {
            lock (session.DiagnosticsLock)
            {
                string item = String.Format("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
                if (lastContact)
                {
                    item += String.Format("Last Event:{0:HH:mm:ss}", session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
                }
                else
                {
                    if (session.Identity != null)
                    {
                        item += String.Format(":{0,20}", session.Identity.DisplayName);
                    }
                    item += String.Format(":{0}", session.Id);
                }
                // Console.WriteLine(item);
            }
        }

        private async void StatusThread()
        {
            while (server != null)
            {
                if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(6000))
                {
                    IList<Opc.Ua.Server.Session> sessions = server.CurrentInstance.SessionManager.GetSessions();
                    for (int ii = 0; ii < sessions.Count; ii++)
                    {
                        Opc.Ua.Server.Session session = sessions[ii];
                        PrintSessionStatus(session, "-Status-", true);
                    }
                    lastEventTime = DateTime.UtcNow;
                }
                await Task.Delay(1000);
            }
        }
    }
}
