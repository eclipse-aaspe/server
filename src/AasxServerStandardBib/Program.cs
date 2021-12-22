using AasxRestServerLibrary;
using AdminShellNS;
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
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Timers;
using static AasxDemonstration.EnergyModel;

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    public static class Program
    {
        public static int envimax = 100;
        public static AdminShellPackageEnv[] env = Enumerable.Repeat<AdminShellPackageEnv>(null, 100).ToArray();
        public static string[] envFileName = Enumerable.Repeat<string>(null, 100).ToArray();
        public static string[] envSymbols = Enumerable.Repeat<string>(null, 100).ToArray();
        public static string[] envSubjectIssuer = Enumerable.Repeat<string>(null, 100).ToArray();


        public static string hostPort = "";
        public static string blazorHostPort = "";

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
            if (a.DataPath != null)
            {
                Console.WriteLine($"Serving the AASXs from: {a.DataPath}");
                AasxHttpContextHelper.DataPath = a.DataPath;
            }

            noSecurity = a.NoSecurity;
            edit = a.Edit;

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
                    _energyModelInstances.AddRange(EnergyModelInstance.FindAllSmInstances(penv?.AasEnv));
                }
            }
            EnergyModelInstance.StartAllAsOneThread(_energyModelInstances);

            RunScript(true);

            isLoading = false;

            SetScriptTimer(1000); // also updates balzor view

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

           return 0;
        }

        public static void Main(string[] args)
        {
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

