using AasxRestServerLibrary;
using AdminShellNS;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using static AasxDemonstration.EnergyModel;
using Formatting = Newtonsoft.Json.Formatting;

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    /// <summary>
    /// Checks whether the console will persist after the program exits.
    /// This should run only on Windows as it depends on kernel32.dll.
    ///
    /// The code has been adapted from: https://stackoverflow.com/a/63135555/1600678
    /// </summary>
    static class WindowsConsoleWillBeDestroyedAtTheEnd
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

        public static bool Check()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);

            return processCount == 1;
        }
    }

    public static class Program
    {
        public static int envimax = 100;
        public static AdminShellPackageEnv[] env = Enumerable.Repeat<AdminShellPackageEnv>(null, 100).ToArray();
        public static string[] envFileName = Enumerable.Repeat<string>(null, 100).ToArray();
        public static string[] envSymbols = Enumerable.Repeat<string>(null, 100).ToArray();
        public static string[] envSubjectIssuer = Enumerable.Repeat<string>(null, 100).ToArray();


        public static string hostPort = "";
        public static string blazorHostPort = "";

        public static string connectServer = "";
        static string connectNodeName = "";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop = false;

        public static WebProxy proxy = null;
        public static HttpClientHandler clientHandler = null;

        public static bool noSecurity = false;
        public static bool edit = false;
        public static string externalRest = "";

        public static HashSet<object> submodelsToPublish = new HashSet<object>();
        public static HashSet<object> submodelsToSubscribe = new HashSet<object>();

        public static Dictionary<object, string> generatedQrCodes = new Dictionary<object, string>();

        public static string redirectServer = "";
        public static string authType = "";

        public static bool isLoading = true;

        public static object changeAasxFile = new object();

        private class CommandLineArguments

        {
            // ReSharper disable UnusedAutoPropertyAccessor.Local
#pragma warning disable 8618
            public string Host { get; set; }
            public string Port { get; set; }
            public bool Https { get; set; }
            public string DataPath { get; set; }
            public bool Rest { get; set; }
            public bool Opc { get; set; }
            public bool Mqtt { get; set; }
            public bool DebugWait { get; set; }
            public int? OpcClientRate { get; set; }
            public string[] Connect { get; set; }
            public string ProxyFile { get; set; }
            public bool NoSecurity { get; set; }
            public bool Edit { get; set; }
            public string Name { get; set; }
            public string ExternalRest { get; set; }
#pragma warning restore 8618
            // ReSharper enable UnusedAutoPropertyAccessor.Local
        }

        private static int Run(CommandLineArguments a)
        {
            if (a.Connect != null)
            {
                if (a.Connect.Length == 0)
                {
                    connectServer = "http://admin-shell-io.com:52000";
                    Byte[] barray = new byte[10];
                    RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                    rngCsp.GetBytes(barray);
                    connectNodeName = "AasxServer_" + Convert.ToBase64String(barray);
                    connectUpdateRate = 2000;
                    if (a.Name != null && a.Name != "")
                        connectNodeName = a.Name;
                }
                else if (a.Connect.Length == 1)
                {
                    bool parsable = true;

                    string[] c = a.Connect[0].Split(',');
                    if (c.Length == 3)
                    {
                        int rate = 0;
                        try
                        {
                            rate = Convert.ToInt32(c[2]);
                        }
                        catch (FormatException)
                        {
                            parsable = false;
                        }

                        if (parsable)
                        {
                            if (c[0].Length == 0 || c[1].Length == 0 || c[2].Length == 0 || rate <= 0)
                            {
                                parsable = false;
                            }
                            else
                            {
                                connectServer = c[0];
                                connectNodeName = c[1];
                                connectUpdateRate = Convert.ToInt32(c[2]);
                            }
                        }
                    }
                    else
                    {
                        parsable = false;
                    }

                    if (!parsable)
                    {
                        Console.Error.WriteLine(
                            "Invalid --connect. " +
                            "Expected a comma-separated values (server, node name, period in milliseconds), " +
                            $"but got: {a.Connect[0]}");
                        return 1;
                    }
                }

                Console.WriteLine(
                    $"--connect: " +
                    $"ConnectServer {connectServer}, " +
                    $"NodeName {connectNodeName}, " +
                    $"UpdateRate {connectUpdateRate}");
            }

            /*
             * Set the global variables at this point inferred from the command-line arguments
             */

            if (a.DataPath != null)
            {
                Console.WriteLine($"Serving the AASXs from: {a.DataPath}");
                AasxHttpContextHelper.DataPath = a.DataPath;
            }

            noSecurity = a.NoSecurity;
            edit = a.Edit;

            // Wait for Debugger
            if (a.DebugWait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", a.Host);
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            if (a.OpcClientRate != null && a.OpcClientRate < 200)
            {
                Console.WriteLine("Recommend an OPC client update rate > 200 ms.");
            }

            // Proxy
            string proxyAddress = "";
            string username = "";
            string password = "";

            if (a.ProxyFile != null)
            {
                if (!File.Exists(a.ProxyFile))
                {
                    Console.Error.WriteLine($"Proxy file not found: {a.ProxyFile}");
                    return 1;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(a.ProxyFile))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(a.ProxyFile + " not found!");
                }

                if (proxyAddress != "")
                {
                    proxy = new WebProxy();
                    Uri newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    proxy.Credentials = new NetworkCredential(username, password);
                    Console.WriteLine("Using proxy: " + proxyAddress);

                    clientHandler = new HttpClientHandler
                    {
                        Proxy = proxy,
                        UseProxy = true
                    };
                }
            };

            hostPort = a.Host + ":" + a.Port;
            blazorHostPort = a.Host + ":" + blazorHostPort;

            if (a.ExternalRest != null)
            {
                externalRest = a.ExternalRest;
            }
            else
            {
                externalRest = "http://" + hostPort;
            }

            // Read root cert from root subdirectory
            Console.WriteLine("Security 1 Startup - Server");
            Console.WriteLine("Security 1.1 Load X509 Root Certificates into X509 Store Root");

            X509Store root = new X509Store("Root", StoreLocation.CurrentUser);
            root.Open(OpenFlags.ReadWrite);

            System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(".");

            if (Directory.Exists("./root"))
            {
                foreach (System.IO.FileInfo f in ParentDirectory.GetFiles("./root/*.cer"))
                {
                    X509Certificate2 cert = new X509Certificate2("./root/" + f.Name);

                    root.Add(cert);
                    Console.WriteLine("Security 1.1 Add " + f.Name);
                }
            }

            Directory.CreateDirectory("./temp");

            string fn = null;

            if (a.Opc)
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


            int envi = 0;

            string[] fileNames = null;
            if (Directory.Exists(AasxHttpContextHelper.DataPath))
            {
                fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx");
                Array.Sort(fileNames);

                while (envi < fileNames.Length)
                {
                    fn = fileNames[envi];

                    if (fn != "" && envi < envimax)
                    {
                        Console.WriteLine("Loading {0}...", fn);
                        envFileName[envi] = fn;
                        env[envi] = new AdminShellPackageEnv(fn, true);
                        if (env[envi] == null)
                        {
                            Console.Error.WriteLine($"Cannot open {fn}. Aborting..");
                            return 1;
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

                for (int j = 0; j < fileNames.Length; j++)
                {
                    fn = fileNames[j];

                    if (fn != "" && envi < envimax)
                    {
                        envFileName[envi] = fn;
                        envSymbols[envi] = "L"; // Show lock
                    }
                    envi++;
                }
            }

            AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
            AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers

            i40LanguageRuntime.initialize();

            // MICHA MICHA
            AasxTimeSeries.TimeSeries.timeSeriesInit();

            var _energyModelInstances = new List<EnergyModelInstance>();
            foreach (var penv in AasxServer.Program.env)
            {
                if (penv != null)
                {
                    EnergyModelInstance.TagAllAasAndSm(penv?.AasEnv, DateTime.UtcNow);
                    _energyModelInstances.AddRange(
                        EnergyModelInstance.FindAllSmInstances(penv?.AasEnv));
                }
            }
            EnergyModelInstance.StartAllAsOneThread(_energyModelInstances);

            AasxTask.taskInit();

            RunScript(true);

            isLoading = false;

            SetScriptTimer(1000); // also updates balzor view

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

                //
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
                //
                // string content = "OK";
                if (content == "OK")
                {
                    connectThread = new Thread(new ThreadStart(connectThreadLoop));
                    // MICHA
                    // connectThread.Start();
                    connectLoop = true;
                }
                else
                {
                    Console.WriteLine("********** Can not connect to: " + connectServer);
                }
            }

            Console.WriteLine("Servers successfully started. Press Ctrl-C to exit...");
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

            // wait for RETURN

            if (connectServer != "")
            {
                if (connectLoop)
                {
                    connectLoop = false;
                }
            }

            return 0;
        }

        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            }

            string nl = System.Environment.NewLine;

            var rootCommand = new RootCommand("serve AASX packages over different interfaces")
            {
                new Option<string>(
                    new[] {"--host", "-h"},
                    () => "localhost",
                    "Host which the server listens on"),

                new Option<string>(
                    new[] {"--port", "-p"},
                    ()=>"51310",
                    "Port which the server listens on"),

                new Option<bool>(
                    new[] {"--https"},
                    "If set, opens SSL connections. " +
                    "Make sure you bind a certificate to the port before."),

                new Option<string>(
                    new[] {"--data-path"},
                    "Path to where the AASXs reside"),

                new Option<bool>(
                    new[] {"--rest"},
                    "If set, starts the REST server"),

                new Option<bool>(
                    new[] {"--opc"},
                    "If set, starts the OPC server"),

                new Option<bool>(
                    new[] {"--mqtt"},
                    "If set, starts a MQTT publisher"),

                new Option<bool>(
                    new[] {"--debug-wait"},
                    "If set, waits for Debugger to attach"),

                new Option<int>(
                    new[] {"--opc-client-rate"},
                    "If set, starts an OPC client and refreshes on the given period " +
                    "(in milliseconds)"),

                new Option<string[]>(
                    new[] {"--connect"},
                    "If set, connects to AAS connect server. " +
                    "Given as a comma-separated-values (server, node name, period in milliseconds) or " +
                    "as a flag (in which case it connects to a default server)."),

                new Option<string>(
                    new[] {"--proxy-file"},
                    "If set, parses the proxy information from the given proxy file"),

                new Option<bool>(
                    new[] {"--no-security"},
                    "If set, no authentication is required"),

                new Option<bool>(
                    new[] {"--edit"},
                    "If set, allows edits in the user interface"),

                new Option<string>(
                    new[] {"--name"},
                    "Name of the server"),

                new Option<string>(
                    new[] {"--external-rest"},
                    "exeternal name of the server"),           };

            if (args.Length == 0)
            {
                new HelpBuilder(new SystemConsole()).Write(rootCommand);

                // set default arguments
                List<string> defaults = new List<string>();

                defaults.Add("--rest");
                defaults.Add("--port");
                defaults.Add("51310");
                defaults.Add("--data-path");
                defaults.Add(".");
                defaults.Add("--edit");
                defaults.Add("--no-security");

                args = defaults.ToArray();
            }

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(
                (CommandLineArguments a) =>
                {
                    if (!(a.Rest || a.Opc || a.Mqtt))
                    {
                        Console.Error.WriteLine($"Please specify --rest and/or --opc and/or --mqtt{nl}");
                        new HelpBuilder(new SystemConsole()).Write(rootCommand);
                        return 1;
                    }

                    return Run(a);
                });

            int exitCode = rootCommand.InvokeAsync(args).Result;
            System.Environment.ExitCode = exitCode;
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
            public string assetId;
            public string humanEndPoint;
            public string restEndPoint;
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

        /* AAS Detail Part 2 Descriptor Definitions BEGIN*/
        /* End Point Definition */
        public class AASxEndpoint
        {
            [XmlElement(ElementName = "address")]
            public string address = "";
            [XmlElement(ElementName = "type")]
            public string type = "";
        }

        /* Submodel Descriptor Definition */
        public class SubmodelDescriptors
        {
            [XmlElement(ElementName = "administration")]
            [JsonIgnore]
            public AdminShell.Administration administration = null;

            [XmlElement(ElementName = "description")]
            [JsonIgnore]
            public AdminShell.Description description = null;

            [XmlElement(ElementName = "idShort")]
            [JsonIgnore]
            public string idShort = "";

            [XmlElement(ElementName = "identification")]
            [JsonIgnore]
            public AdminShell.Identification identification = null;

            [XmlElement(ElementName = "semanticId")]
            public AdminShell.SemanticId semanticId = null;

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();
        }
        /* AAS Descriptor Definiton */
        public class aasDescriptor
        {

            [XmlElement(ElementName = "administration")]
            [JsonIgnore]
            public AdminShell.Administration administration = null;

            [XmlElement(ElementName = "description")]
            [JsonIgnore]
            public AdminShell.Description description = new AdminShell.Description();

            [XmlElement(ElementName = "idShort")]
            public string idShort = "";

            [XmlElement(ElementName = "identification")]
            [JsonIgnore]
            public AdminShell.Identification identification = null;

            [XmlElement(ElementName = "assets")]
            public List<AdminShell.Asset> assets = new List<AdminShell.Asset>();

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();

            [XmlElement(ElementName = "submodelDescriptors")]
            public List<SubmodelDescriptors> submodelDescriptors = new List<SubmodelDescriptors>();
        }

        /* AAS Detail Part 2 Descriptor Definitions END*/

        /* Creation of AAS Descriptor */
        public static aasDescriptor creatAASDescriptor(AdminShellPackageEnv adminShell)
        {
            aasDescriptor aasD = new aasDescriptor();
            string endpointAddress = "http://" + hostPort;

            aasD.idShort = adminShell.AasEnv.AdministrationShells[0].idShort;
            aasD.identification = adminShell.AasEnv.AdministrationShells[0].identification;
            aasD.description = adminShell.AasEnv.AdministrationShells[0].description;

            AASxEndpoint endp = new AASxEndpoint();
            endp.address = endpointAddress + "/aas/" + adminShell.AasEnv.AdministrationShells[0].idShort;
            aasD.endpoints.Add(endp);

            int submodelCount = adminShell.AasEnv.Submodels.Count;
            for (int i = 0; i < submodelCount; i++)
            {
                SubmodelDescriptors sdc = new SubmodelDescriptors();

                sdc.administration = adminShell.AasEnv.Submodels[i].administration;
                sdc.description = adminShell.AasEnv.Submodels[i].description;
                sdc.identification = adminShell.AasEnv.Submodels[i].identification;
                sdc.idShort = adminShell.AasEnv.Submodels[i].idShort;
                sdc.semanticId = adminShell.AasEnv.Submodels[i].semanticId;

                AASxEndpoint endpSub = new AASxEndpoint();
                endpSub.address = endpointAddress + "/aas/" + adminShell.AasEnv.AdministrationShells[0].idShort +
                                   "/submodels/" + adminShell.AasEnv.Submodels[i].idShort;
                endpSub.type = "http";
                sdc.endpoints.Add(endpSub);

                aasD.submodelDescriptors.Add(sdc);
            }

            int assetCount = adminShell.AasEnv.Assets.Count;
            for (int i = 0; i < assetCount; i++)
            {
                aasD.assets.Add(adminShell.AasEnv.Assets[i]);
            }
            return aasD;
        }

        /*Publishing the AAS Descriptor*/
        public static void publishDescriptorData(string descriptorData)
        {
            HttpClient httpClient;
            if (clientHandler != null)
            {
                httpClient = new HttpClient(clientHandler);
            }
            else
            {
                httpClient = new HttpClient();
            }
            var descriptorJson = new StringContent(descriptorData, System.Text.Encoding.UTF8, "application/json");
            try
            {
                var result = httpClient.PostAsync(connectServer + "/publish", descriptorJson).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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

        static bool getDirectory = true;
        static string getDirectoryDestination = "";

        static string getaasxFile_destination = "";
        static string getaasxFile_fileName = "";
        static string getaasxFile_fileData = "";
        static string getaasxFile_fileType = "";
        static int getaasxFile_fileLenBase64 = 0;
        static int getaasxFile_fileLenBinary = 0;
        static int getaasxFile_fileTransmitted = 0;
        static int blockSize = 1500000;

        static List<TransmitData> tdPending = new List<TransmitData> { };

        public static void connectPublish(string type, string json)
        {
            if (connectServer == "")
                return;

            TransmitData tdp = new TransmitData();

            tdp.source = connectNodeName;
            tdp.type = type;
            tdp.publish.Add(json);
            tdPending.Add(tdp);
        }

        public static void connectThreadLoop()
        {
            bool newConnectData = false;

            while (connectLoop)
            {
                TransmitFrame tf = new TransmitFrame
                {
                    source = connectNodeName
                };
                TransmitData td = null;

                if (getDirectory)
                {
                    Console.WriteLine("if getDirectory");

                    // AAAS Detail part 2 Descriptor
                    TransmitFrame descriptortf = new TransmitFrame
                    {
                        source = connectNodeName
                    };

                    aasDirectoryParameters adp = new aasDirectoryParameters();

                    adp.source = connectNodeName;

                    int aascount = env.Length;

                    for (int j = 0; j < aascount; j++)
                    {
                        aasListParameters alp = new aasListParameters();

                        if (env[j] != null)
                        {
                            alp.index = j;

                            /* Create Detail part 2 Descriptor Start */
                            aasDescriptor aasDsecritpor = creatAASDescriptor(env[j]);
                            TransmitData aasDsecritporTData = new TransmitData
                            {
                                source = connectNodeName
                            };
                            aasDsecritporTData.type = "register";
                            aasDsecritporTData.destination = "VWS_AAS_Registry";
                            var aasDescriptorJsonData = JsonConvert.SerializeObject(aasDsecritpor, Newtonsoft.Json.Formatting.Indented,
                                                                        new JsonSerializerSettings
                                                                        {
                                                                            NullValueHandling = NullValueHandling.Ignore
                                                                        });
                            aasDsecritporTData.publish.Add(aasDescriptorJsonData);
                            descriptortf.data.Add(aasDsecritporTData);
                            /* Create Detail part 2 Descriptor END */


                            alp.idShort = env[j].AasEnv.AdministrationShells[0].idShort;
                            alp.identification = env[j].AasEnv.AdministrationShells[0].identification.ToString();
                            alp.fileName = envFileName[j];
                            alp.assetId = "";
                            var asset = env[j].AasEnv.FindAsset(env[j].AasEnv.AdministrationShells[0].assetRef);
                            if (asset != null)
                                alp.assetId = asset.identification.id;
                            alp.humanEndPoint = blazorHostPort;
                            alp.restEndPoint = hostPort;

                            adp.aasList.Add(alp);
                        }
                    }

                    string decriptorData = JsonConvert.SerializeObject(descriptortf, Formatting.Indented);
                    publishDescriptorData(decriptorData);

                    td = new TransmitData
                    {
                        source = connectNodeName
                    };

                    var json = JsonConvert.SerializeObject(adp, Newtonsoft.Json.Formatting.Indented);
                    td.type = "directory";
                    td.destination = getDirectoryDestination;
                    td.publish.Add(json);
                    tf.data.Add(td);
                    Console.WriteLine("Send directory");

                    getDirectory = false;
                    getDirectoryDestination = "";
                }

                if (getaasxFile_destination != "") // block transfer
                {
                    dynamic res = new System.Dynamic.ExpandoObject();

                    td = new TransmitData
                    {
                        source = connectNodeName
                    };

                    int len = 0;
                    if ((getaasxFile_fileLenBase64 - getaasxFile_fileTransmitted) > blockSize)
                    {
                        len = blockSize;
                    }
                    else
                    {
                        len = getaasxFile_fileLenBase64 - getaasxFile_fileTransmitted;
                    }

                    res.fileData = getaasxFile_fileData.Substring(getaasxFile_fileTransmitted, len);
                    res.fileName = getaasxFile_fileName;
                    res.fileLenBase64 = getaasxFile_fileLenBase64;
                    res.fileLenBinary = getaasxFile_fileLenBinary;
                    res.fileType = getaasxFile_fileType;
                    res.fileTransmitted = getaasxFile_fileTransmitted;

                    string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                    td.destination = getaasxFile_destination;
                    td.type = "getaasxBlock";
                    td.publish.Add(responseJson);
                    tf.data.Add(td);

                    getaasxFile_fileTransmitted += len;

                    if (getaasxFile_fileTransmitted == getaasxFile_fileLenBase64)
                    {
                        getaasxFile_destination = "";
                        getaasxFile_fileName = "";
                        getaasxFile_fileData = "";
                        getaasxFile_fileType = "";
                        res.fileLenBase64 = 0;
                        res.fileLenBinary = 0;
                        getaasxFile_fileTransmitted = 0;
                    }
                }

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
                            bool toPublish = submodelsToPublish.Contains(sm);
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
                                td = new TransmitData
                                {
                                    source = connectNodeName
                                };

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

                // i40language
                if (i40LanguageRuntime.isRequester && i40LanguageRuntime.sendFrameJSONRequester.Count != 0)
                {
                    foreach (string s in i40LanguageRuntime.sendFrameJSONRequester)
                    {
                        td = new TransmitData
                        {
                            source = connectNodeName
                        };

                        td.type = "i40LanguageRuntime.sendFrameJSONRequester";
                        var json = JsonConvert.SerializeObject(s, Newtonsoft.Json.Formatting.Indented);
                        td.publish.Add(json);
                        tf.data.Add(td);
                    }
                    i40LanguageRuntime.sendFrameJSONRequester.Clear();
                }
                if (i40LanguageRuntime.isProvider && i40LanguageRuntime.sendFrameJSONProvider.Count != 0)
                {
                    td = new TransmitData
                    {
                        source = connectNodeName
                    };

                    foreach (string s in i40LanguageRuntime.sendFrameJSONProvider)
                    {
                        td.type = "i40LanguageRuntime.sendFrameJSONProvider";
                        var json = JsonConvert.SerializeObject(s, Newtonsoft.Json.Formatting.Indented);
                        td.publish.Add(json);
                        tf.data.Add(td);
                    }
                    i40LanguageRuntime.sendFrameJSONProvider.Clear();
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
                    newConnectData = false;
                    string node = "";

                    try
                    {
                        TransmitFrame tf2 = new TransmitFrame();
                        tf2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TransmitFrame>(content);

                        node = tf2.source;
                        foreach (TransmitData td2 in tf2.data)
                        {
                            if (td2.type == "getDirectory")
                            {
                                Console.WriteLine("received getDirectory");
                                getDirectory = true;
                                getDirectoryDestination = td2.source;
                            }

                            if (td2.type == "getaasx" && td2.destination == connectNodeName)
                            {
                                int aasIndex = Convert.ToInt32(td2.extensions);

                                dynamic res = new System.Dynamic.ExpandoObject();

                                Byte[] binaryFile = File.ReadAllBytes(envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                                string fileToken = Jose.JWT.Encode(payload, enc.GetBytes(AasxRestServerLibrary.AasxHttpContextHelper.secretString), JwsAlgorithm.HS256);

                                if (fileToken.Length <= blockSize)
                                {
                                    res.fileName = Path.GetFileName(envFileName[aasIndex]);
                                    res.fileData = fileToken;

                                    string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                                    TransmitData tdp = new TransmitData();

                                    tdp.source = connectNodeName;
                                    tdp.destination = td2.source;
                                    tdp.type = "getaasxFile";
                                    tdp.publish.Add(responseJson);
                                    tdPending.Add(tdp);
                                }
                                else
                                {
                                    getaasxFile_destination = td2.source;
                                    getaasxFile_fileName = Path.GetFileName(envFileName[aasIndex]);
                                    getaasxFile_fileData = fileToken;
                                    getaasxFile_fileType = "getaasxFileStream";
                                    getaasxFile_fileLenBase64 = getaasxFile_fileData.Length;
                                    getaasxFile_fileLenBinary = binaryFile.Length;
                                    getaasxFile_fileTransmitted = 0;
                                }
                            }

                            if (td2.type == "getaasxstream" && td2.destination == connectNodeName)
                            {
                                int aasIndex = Convert.ToInt32(td2.extensions);

                                dynamic res = new System.Dynamic.ExpandoObject();

                                Byte[] binaryFile = File.ReadAllBytes(envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                if (binaryBase64.Length <= blockSize)
                                {
                                    res.fileName = Path.GetFileName(envFileName[aasIndex]);
                                    res.fileData = binaryBase64;
                                    Byte[] fileBytes = Convert.FromBase64String(binaryBase64);

                                    string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                                    TransmitData tdp = new TransmitData();

                                    tdp.source = connectNodeName;
                                    tdp.destination = td2.source;
                                    tdp.type = "getaasxFile";
                                    tdp.publish.Add(responseJson);
                                    tdPending.Add(tdp);
                                }
                                else
                                {
                                    getaasxFile_destination = td2.source;
                                    getaasxFile_fileName = Path.GetFileName(envFileName[aasIndex]);
                                    getaasxFile_fileData = binaryBase64;
                                    getaasxFile_fileType = "getaasxFile";
                                    getaasxFile_fileLenBase64 = getaasxFile_fileData.Length;
                                    getaasxFile_fileLenBinary = binaryFile.Length;
                                    getaasxFile_fileTransmitted = 0;
                                }
                            }

                            if (td2.type.ToLower().Contains("timeseries"))
                            {
                                string[] split = td2.type.Split('.');
                                foreach (var smc in AasxTimeSeries.TimeSeries.timeSeriesSubscribe)
                                {
                                    if (smc.idShort == split[0])
                                    {
                                        foreach (var tsb in AasxTimeSeries.TimeSeries.timeSeriesBlockList)
                                        {
                                            if (tsb.sampleStatus.value == "stop")
                                            {
                                                tsb.sampleStatus.value = "stopped";
                                            }
                                            if (tsb.sampleStatus.value != "start")
                                                continue;

                                            if (tsb.block == smc)
                                            {
                                                foreach (string data in td2.publish)
                                                {
                                                    using (TextReader reader = new StringReader(data))
                                                    {
                                                        JsonSerializer serializer = new JsonSerializer();
                                                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                                        var smcData = (AdminShell.SubmodelElementCollection)serializer.Deserialize(reader,
                                                            typeof(AdminShell.SubmodelElementCollection));
                                                        if (smcData != null && smc.value.Count < 100)
                                                        {
                                                            if (tsb.data != null)
                                                            {
                                                                int maxCollections = Convert.ToInt32(tsb.maxCollections.value);
                                                                int actualCollections = tsb.data.value.Count;
                                                                if (actualCollections < maxCollections ||
                                                                    (tsb.sampleMode.value == "continuous" && actualCollections == maxCollections))
                                                                {
                                                                    tsb.data.Add(smcData);
                                                                    actualCollections++;
                                                                }
                                                                if (actualCollections > maxCollections)
                                                                {
                                                                    tsb.data.value.RemoveAt(0);
                                                                    actualCollections--;
                                                                }
                                                                tsb.actualCollections.value = actualCollections.ToString();
                                                                tsb.lowDataIndex.value =
                                                                    tsb.data.value[0].submodelElement.idShort.Substring("data".Length);
                                                                tsb.highDataIndex.value =
                                                                    tsb.data.value[tsb.data.value.Count - 1].submodelElement.idShort.Substring("data".Length);

                                                                SignalNewData(TreeUpdateMode.Rebuild);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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
                                    envi = 0;
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
                                            bool toSubscribe = submodelsToSubscribe.Contains(existingSm);
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
                                                newConnectData = true;
                                            }
                                        }
                                    }
                                }
                            }

                            // i40language
                            if (i40LanguageRuntime.isRequester && td2.type == "i40LanguageRuntime.sendFrameJSONProvider")
                            {
                                foreach (string s in td2.publish)
                                {
                                    i40LanguageRuntime.receivedFrameJSONRequester.Add(JsonConvert.DeserializeObject<string>(s));
                                }
                            }
                            if (i40LanguageRuntime.isProvider && td2.type == "i40LanguageRuntime.sendFrameJSONRequester")
                            {
                                foreach (string s in td2.publish)
                                {
                                    i40LanguageRuntime.receivedFrameJSONProvider.Add(JsonConvert.DeserializeObject<string>(s));
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }

                    if (newConnectData)
                    {
                        SignalNewData(TreeUpdateMode.Rebuild);
                    }
                }

                if (getaasxFile_destination != "") // block transfer
                {
                    Thread.Sleep(500);
                }
                else
                    Thread.Sleep(connectUpdateRate);
            }
        }

        public static event EventHandler NewDataAvailable;

        public enum TreeUpdateMode
        {
            ValuesOnly = 0,
            Rebuild,
            RebuildAndCollapse
        }

        public static void SignalNewData(TreeUpdateMode mode)
        {
            NewDataAvailable?.Invoke(mode, EventArgs.Empty);
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
            RunScript(false);
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

        static long countGetPut = 0;

        private static void OnRestTimedEvent(Object source, ElapsedEventArgs e)
        {
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
        }

        static void RunScript(bool init)
        {
            if (env == null)
                return;

            lock (changeAasxFile)
            {
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

                                        if (qq.type == "GetJSON")
                                        {
                                            if (init)
                                                return;

                                            if (isLoading)
                                                return;

                                            if (!(sme1 is AdminShell.ReferenceElement))
                                            {
                                                continue;
                                            }

                                            string url = qq.value;
                                            string username = "";
                                            string password = "";

                                            if (sme1.qualifiers.Count == 3)
                                            {
                                                qq = sme1.qualifiers[1] as AdminShell.Qualifier;
                                                if (qq.type != "Username")
                                                    continue;
                                                username = qq.value;
                                                qq = sme1.qualifiers[2] as AdminShell.Qualifier;
                                                if (qq.type != "Password")
                                                    continue;
                                                password = qq.value;
                                            }

                                            var handler = new HttpClientHandler();
                                            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                                            var client = new HttpClient(handler);

                                            if (username != "" && password != "")
                                            {
                                                var authToken = System.Text.Encoding.ASCII.GetBytes(username + ":" + password);
                                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                        Convert.ToBase64String(authToken));
                                            }

                                            Console.WriteLine("GetJSON: " + url);
                                            string response = client.GetStringAsync(url).Result;
                                            Console.WriteLine(response);

                                            if (response != "")
                                            {
                                                var r12 = sme1 as AdminShell.ReferenceElement;
                                                var ref12 = env[i].AasEnv.FindReferableByReference(r12.value);
                                                if (ref12 is AdminShell.SubmodelElementCollection)
                                                {
                                                    var c1 = ref12 as AdminShell.SubmodelElementCollection;
                                                    // if (c1.value.Count == 0)
                                                    {
                                                        // dynamic model = JObject.Parse(response);
                                                        JObject parsed = JObject.Parse(response);
                                                        parseJson(c1, parsed);
                                                    }
                                                }
                                            }
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
            }

            return;
        }

        public static void parseJson(AdminShell.SubmodelElementCollection c, JObject o)
        {
            TreeUpdateMode newMode = TreeUpdateMode.ValuesOnly;
            DateTime timeStamp = DateTime.UtcNow;

            foreach (JProperty jp1 in (JToken)o)
            {
                AdminShell.SubmodelElementCollection c2;
                switch (jp1.Value.Type)
                {
                    case JTokenType.Array:
                        c2 = c.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>(jp1.Name);
                        if (c2 == null)
                        {
                            c2 = AdminShell.SubmodelElementCollection.CreateNew(jp1.Name);
                            c.Add(c2);
                            c2.TimeStampCreate = timeStamp;
                            c2.setTimeStamp(timeStamp);
                            newMode = TreeUpdateMode.Rebuild;
                        }
                        int count = 1;
                        foreach (JObject el in jp1.Value)
                        {
                            string n = jp1.Name + "_array_" + count++;
                            AdminShell.SubmodelElementCollection c3 =
                                c2.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>(n);
                            if (c3 == null)
                            {
                                c3 = AdminShell.SubmodelElementCollection.CreateNew(n);
                                c2.Add(c3);
                                c3.TimeStampCreate = timeStamp;
                                c3.setTimeStamp(timeStamp);
                                newMode = TreeUpdateMode.Rebuild;
                            }
                            parseJson(c3, el);
                        }
                        break;
                    case JTokenType.Object:
                        c2 = c.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>(jp1.Name);
                        if (c2 == null)
                        {
                            c2 = AdminShell.SubmodelElementCollection.CreateNew(jp1.Name);
                            c.Add(c2);
                            c2.TimeStampCreate = timeStamp;
                            c2.setTimeStamp(timeStamp);
                            newMode = TreeUpdateMode.Rebuild;
                        }
                        foreach (JObject el in jp1.Value)
                        {
                            parseJson(c2, el);
                        }
                        break;
                    default:
                        AdminShell.Property p = c.value.FindFirstIdShortAs<AdminShell.Property>(jp1.Name);
                        if (p == null)
                        {
                            p = AdminShell.Property.CreateNew(jp1.Name);
                            c.Add(p);
                            p.TimeStampCreate = timeStamp;
                            p.setTimeStamp(timeStamp);
                            newMode = TreeUpdateMode.Rebuild;
                        }
                        // see https://github.com/JamesNK/Newtonsoft.Json/issues/874
                        p.value = (jp1.Value as JValue).ToString(CultureInfo.InvariantCulture);
                        p.setTimeStamp(timeStamp);
                        break;
                }
            }

            SignalNewData(newMode);
        }
    }
}

