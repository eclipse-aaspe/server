using AasOpcUaServer;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Opc.Ua.Client;
using System;
using System.IO;
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

namespace Net46ConsoleServer
{
    class Program
    {
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

        private static void OnOPCClientNextTimedEvent(Object source, ElapsedEventArgs e)
        {
            ReadOPCClient(false);
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

                            if (p.qualifierType == "GETSUBMODEL")
                            {
                                GETSUBMODEL = p.qualifierValue;
                            }
                            if (p.qualifierType == "GETURL")
                            {
                                GETURL = p.qualifierValue;
                            }
                            if (p.qualifierType == "PUTSUBMODEL")
                            {
                                PUTSUBMODEL = p.qualifierValue;
                            }
                            if (p.qualifierType == "PUTURL")
                            {
                                PUTURL = p.qualifierValue;
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
                        serializer.Converters.Add(new AdminShell.JsonAasxConverter("modelType", "name"));
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

        public static int envimax = 100;
        public static AdminShell.PackageEnv[] env = new AdminShellV10.PackageEnv[100]
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

        static Dictionary<string, SampleClient.UASampleClient> OPCClients = new Dictionary<string, SampleClient.UASampleClient>();
        static Boolean opcclientActive;
        static readonly object opcclientAddLock = new object(); // object for lock around connecting to an external opc server

        static MqttServer AASMqttServer = new MqttServer();

        static void Main(string[] args)
        {
            // default command line options
            var host = "localhost";
            var port = "51310";
            bool debugwait = false;
            opcclientActive = false;
            int opcclient_rate = 5000;  // 5 seconds

            // parse options
            Console.WriteLine("--help for options and help");
            Console.WriteLine("Copyright (c) 2018-2019 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski");
            Console.WriteLine("Copyright (c) 2018-2019 Festo AG & Co. KG");
            Console.WriteLine("Fraunhofer IOSB-INA Lemgo, eine rechtlich nicht selbstaendige Einrichtung der Fraunhofer-Gesellschaft zur Foerderung der angewandten Forschung e.V.");
            Console.WriteLine("This software is licensed under the Eclipse Public License 2.0 (EPL-2.0)");
            Console.WriteLine("The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)");
            Console.WriteLine("The Grapevine REST server framework is licensed under Apache License 2.0 (Apache-2.0)");
            Console.WriteLine("The MQTT server and client is licensed under the MIT license (MIT) (see below)");
            Console.WriteLine("Portions copyright(c) by OPC Foundation, Inc. and licensed under the Reciprocal Community License (RCL)");
            Console.WriteLine("For further details see LICENSE.TXT");
            Console.WriteLine("");

            Boolean help = false;
            if (args.Length == 0)
                help = true;
            if (args.Length == 1)
            {
                var x = args[0].Trim().ToLower();
                if (x == "--help")
                {
                    help = true;
                }
            }

            int i = 0;
            while (i < args.Length - 1)
            {
                var x = args[i].Trim().ToLower();

                if (x == "-host")
                {
                    host = args[i + 1];
                    i += 2;
                    continue;
                }

                if (x == "-port")
                {
                    port = args[i + 1];
                    i += 2;
                    continue;
                }

                if (x == "-datapath")
                {
                    AasxHttpContextHelper.DataPath = args[i + 1];
                    i += 2;
                    continue;
                }

                if (x == "-debugwait")
                {
                    debugwait = true;
                    i++;
                    continue;
                }

                if (x == "-opcclient")
                {
                    opcclientActive = true;
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

                if (x == "--help")
                {
                    help = true;
                    break;
                }
            }

            if (help)
            {
                Console.WriteLine("-host HOSTIP");
                Console.WriteLine("-port HOSTPORT");
                Console.WriteLine("-datapath PATH_TO_AASX_FILES");
                Console.WriteLine("-debugwait = Wait for Debugger to attach");
                Console.WriteLine("-opclient UPDATERATE = time in ms between getting new values");
                Console.WriteLine("FILENAME.AASX");
                return;
            }

            // auf Debugger warten
            if (debugwait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", host);
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            Console.WriteLine("Connect to REST by: {0}:{1}", host, port);

            string fn = null; //  "Festo-USB-stick-sample-admin-shell.aasx";
            /*
            if (args.Length < 1)
                return;
            fn = args[args.Length - 1];
            if (fn == null)
                return;
            */

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

            System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(AasxHttpContextHelper.DataPath);

            int envi = 0;

            foreach (System.IO.FileInfo f in ParentDirectory.GetFiles("*.aasx"))
            {
                fn = f.Name;

                if (fn != "" && envi < envimax)
                {
                    fn = AasxHttpContextHelper.DataPath + "/" + fn;
                    Console.WriteLine("Loading {0}...", fn);
                    env[envi] = new AdminShell.PackageEnv(fn);
                    if (env[envi] == null)
                    {
                        Console.Out.WriteLine($"Cannot open {fn}. Aborting..");
                        return;
                    }
                }
                envi++;
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

            Console.WriteLine("Please wait for servers starting...");

            // REST vor OPC starten
            // AasxRestServer.Start(env, host, port, new GrapevineLoggerToConsole());
            AasxRestServer.Start(env, host, port); // without Logger

            SetRestTimer(10000); // GET and PUT every 10 seconds
                                 // OnRestTimedEvent(null, null);

            AASMqttServer.MqttSeverStartAsync().Wait();

            //
            MySampleServer server = new MySampleServer(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env);

            server.Run(); // wait for CTRL-C
            //

            /*
            // no OPC UA: wait only for CTRL-C
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
            */

            // wait for RETURN

            AASMqttServer.MqttSeverStopAsync().Wait();

            AasxRestServer.Stop();
        }

        private static Boolean OPCWrite(string nodeId, object value)
        /// <summary>
        /// Writes to (i.e. updates values of) Nodes in the AAS OPC Server
        /// </summary>
        {
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

            foreach (var sm in env[0].AasEnv.Submodels)
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

                            switch (p.qualifierType)
                            {
                                case "OPCURL": // URL
                                    URL = p.qualifierValue;
                                    break;
                                case "OPCUsername": // Username
                                    Username = p.qualifierValue;
                                    break;
                                case "OPCPassword": // Password
                                    Password = p.qualifierValue;
                                    break;
                                case "OPCNamespace": // Namespace
                                    // TODO: if not int, currently throws nondescriptive error
                                    Namespace = int.Parse(p.qualifierValue);
                                    break;
                                case "OPCPath": // Path
                                    Path = p.qualifierValue;
                                    break;
                            }
                            j++;
                        }

                        if (URL == "" || Username == "" || Password == "" || Namespace == 0 || Path == "")
                        {
                            Console.WriteLine("Incorrent or missing qualifier. Aborting ...");
                            return false;
                        }

                        // try to get the client from dictionary, else create and add it
                        SampleClient.UASampleClient client;
                        lock (Program.opcclientAddLock)
                        {
                            if (!OPCClients.TryGetValue(sm.idShort, out client))
                            {
                                try
                                {
                                    // make OPC UA client
                                    client = new SampleClient.UASampleClient(URL, autoAccept, stopTimeout, Username, Password);
                                    Console.WriteLine("Connecting to external OPC UA Server at {0} ...", URL);
                                    client.ConsoleSampleClient().Wait();
                                    // add it to the dictionary under this submodels idShort
                                    OPCClients.Add(sm.idShort, client);
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
                                });
                                    if (cantconnect)
                                    {
                                        // stop processing OPC read because we couldnt connect
                                        // but return true as this shouldn't stop the main loop
                                        Console.WriteLine("Could not connect to {0} ...", URL);
                                        return true;
                                    }
                                }
                            }
                        }
                        Console.WriteLine("==================================================");
                        Console.WriteLine("Read values for {0} from {1} ...", sm.idShort, URL);
                        Console.WriteLine("==================================================");

                        // over all SMEs
                        count = sm.submodelElements.Count;
                        for(j = 0; j < count; j++)
                        {
                            var sme = sm.submodelElements[j].submodelElement;
                            //Console.WriteLine("{0} contains {1}", sm.idShort, sme.idShort);
                            // some preparations for multiple AAS below
                            int serverNamespaceIdx = 3; //could be gotten directly from the nodeMgr in OPCWrite instead, only pass the string part of the Id
                            string AASIdShort = "AAS"; // for multiple AAS, use something like env.AasEnv.AdministrationShells[i].idShort;
                            string serverNodePrefix = string.Format("ns={0};s=AASROOT.{1}", serverNamespaceIdx, AASIdShort);
                            string nodePath = Path; // generally starts with Submodel idShort
                            WalkSubmodelElement(sme, nodePath, serverNodePrefix, client, Namespace);
                        }
                    }
                }
            }
            return true;
        }

        private static void WalkSubmodelElement(AdminShell.SubmodelElement sme, string nodePath, string serverNodePrefix, SampleClient.UASampleClient client, int clientNamespace )
        {
            if (sme is AdminShell.Property)
            {
                //Console.WriteLine("{0} is a Property ", sme);
                var p = sme as AdminShell.Property;
                string clientNodeName = nodePath + "." + p.idShort;
                string serverNodeId = string.Format("{0}.{1}.{2}.Value", serverNodePrefix, nodePath, p.idShort);
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
        static AdminShell.PackageEnv [] aasxEnv = null;
        // OZ
        public static ManualResetEvent quitEvent;

        public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShell.PackageEnv [] _aasxEnv)
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
