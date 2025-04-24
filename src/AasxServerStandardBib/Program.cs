/********************************************************************************
* Copyright (c) {2018 - 2025} Contributors to the Eclipse Foundation
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

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    using AasxServerDB;
    using AasxRestServerLibrary;
    using AdminShellNS;
    using Extensions;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Help;
    using System.CommandLine.IO;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text.Json;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Extensions.DependencyInjection;
    using Contracts;

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
            var processList  = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);

            return processCount == 1;
        }
    }

    public static class Program
    {
        public static string conditionSM = "";
        public static string conditionSME = "";

        public static IConfiguration con { get; set; }

        /*
        public static string getBetween(AdminShellPackageEnv env, string strStart, string strEnd)
        {
            string strSource = env.getEnvXml();
            if (strSource != null && strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End   = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }
        */

        public static void saveEnv(int envIndex)
        {
            Console.WriteLine("SAVE: " + envFileName[envIndex]);
            string requestedFileName = envFileName[envIndex];
            string copyFileName      = Path.GetTempFileName().Replace(".tmp", ".aasx");
            System.IO.File.Copy(requestedFileName, copyFileName, true);
            AasxServer.Program.env[envIndex].SaveAs(copyFileName);
            System.IO.File.Copy(copyFileName, requestedFileName, true);
            System.IO.File.Delete(copyFileName);
        }

        static int oldest = 0;

        public static bool isLoadingDB = false;
        static bool isLoaded = false;

        public static void LoadAllPackages()
        {
            if (!withDb || isLoadingDB || isLoaded)
                return;

            Program.isLoadingDB = true;
            var aasIDDBList = new AasContext().AASSets.Select(aas => aas.Identifier).ToList();

            foreach (var aasIDDB in aasIDDBList)
                LoadPackage<IAssetAdministrationShell>(success: out _, packageIndex: out _, aasIdentifier: aasIDDB);

            isLoaded            = true;
            Program.isLoadingDB = false;
            Program.signalNewData(2);
        }

        public static T? LoadPackage<T>(out bool success, out int packageIndex, string aasIdentifier = "", string smIdentifier = "", string cdIdentifier = "")
        {
            // default values
            success = false;
            packageIndex = -1;
            var output = default(T);

            if (!withDb || isLoading)
            {
                return output;
            }

            // search in memory
            // var i = envimin;
            var i = 0;
            while (i < env.Length)
            {
                if (env[i] == null)
                {
                    break;
                }

                if (!cdIdentifier.IsNullOrEmpty())
                {
                    var cd = env[i].AasEnv?.ConceptDescriptions?.Where(cdV => cdV.Id != null && cdV.Id.Equals(cdIdentifier));
                    if (cd != null && cd.Any())
                    {
                        output = (T)cd.First();
                    }
                }
                else if (!aasIdentifier.IsNullOrEmpty())
                {
                    var aas = env[i].AasEnv?.AssetAdministrationShells?.Where(predicate: aasV => aasV.Id != null && aasV.Id.Equals(aasIdentifier));
                    if (aas != null && aas.Any())
                    {
                        output = (T)aas.First();
                    }
                }
                else if (!smIdentifier.IsNullOrEmpty())
                {
                    var sm = env[i].AasEnv?.Submodels?.Where(smV => smV.Id != null && smV.Id.Equals(smIdentifier));
                    if (sm != null && sm.Any())
                    {
                        output = (T)sm.First();
                    }
                }

                if (!EqualityComparer<T>.Default.Equals(output, default))
                {
                    success = true;
                    packageIndex = i;
                    return output;
                }

                i++;
            }

            // env full, change to the one that should be replaced
            if (i == env.Length)
            {
                i = oldest++;
                if (oldest == env.Length)
                {
                    oldest = envimin;
                }
            }

            // not found in memory
            lock (changeAasxFile)
            {
                // get filename
                envFileName[i] = Converter.GetAASXPath(cdId: cdIdentifier, aasId: aasIdentifier, smId: smIdentifier);
                if (envFileName[i].IsNullOrEmpty())
                {
                    return output;
                }

                // unload
                if (env[i] != null)
                {
                    Console.WriteLine("UNLOAD: " + envFileName[i]);
                    if (env[i].getWrite())
                    {
                        saveEnv(i);
                        env[i].setWrite(false);
                    }
                    env[i].Close();
                }

                // load
                // ... from file
                if (!withDbFiles)
                {
                    Console.WriteLine("LOAD: " + envFileName[i]);
                    env[i] = new AdminShellPackageEnv(envFileName[i]);
                    if (env[i].AasEnv == null)
                    {
                        return output;
                    }

                    // set all timestamps and find the searched element
                    var timeStamp = DateTime.Now;
                    if (env[i].AasEnv?.ConceptDescriptions != null)
                    {
                        foreach (var cd in env[i].AasEnv.ConceptDescriptions)
                        {
                            cd.TimeStampCreate = timeStamp;
                            cd.SetTimeStamp(timeStamp);
                            if (!cdIdentifier.IsNullOrEmpty() && cd.Id != null && cd.Id.Equals(cdIdentifier))
                            {
                                output = (T)cd;
                            }
                        }
                    }
                    if (env[i].AasEnv?.AssetAdministrationShells != null)
                    {
                        foreach (var aas in env[i].AasEnv.AssetAdministrationShells)
                        {
                            aas.TimeStampCreate = timeStamp;
                            aas.SetTimeStamp(timeStamp);
                            if (!aasIdentifier.IsNullOrEmpty() && aas.Id != null && aas.Id.Equals(aasIdentifier))
                            {
                                output = (T)aas;
                            }
                        }
                    }
                    if (env[i].AasEnv?.Submodels != null)
                    {
                        foreach (var sm in env[i].AasEnv.Submodels)
                        {
                            sm.TimeStampCreate = timeStamp;
                            sm.SetTimeStamp(timeStamp);
                            sm.SetAllParents(timeStamp);
                            if (!smIdentifier.IsNullOrEmpty() && sm.Id != null && sm.Id.Equals(smIdentifier))
                            {
                                output = (T)sm;
                            }
                        }
                    }
                }

                // ... from db
                else
                {
                    Console.WriteLine("LOAD: " + cdIdentifier + aasIdentifier + smIdentifier);

                    // get envId
                    var db = new AasContext();
                    var envId = 0;
                    if (!cdIdentifier.IsNullOrEmpty())
                    {
                        envId = db.CDSets.Where(cd => cd.Identifier == cdIdentifier).Join(db.EnvCDSets, cd => cd.Id, envcd => envcd.CDId, (cd, envcd) => envcd.EnvId).FirstOrDefault();
                    }
                    else if (!aasIdentifier.IsNullOrEmpty())
                    {
                        envId = db.AASSets.Where(aas => aas.Identifier == aasIdentifier).Select(aas => aas.EnvId.Value).FirstOrDefault();
                    }
                    else if (!smIdentifier.IsNullOrEmpty())
                    {
                        var e = db.SMSets.Where(sm => sm.Identifier == smIdentifier).Select(sm => sm.EnvId).FirstOrDefault();
                        if (e != null)
                        {
                            envId = (int)e;
                        }
                    }
                    if (envId == 0)
                    {
                        return output;
                    }

                    // create package env
                    env[i] = Converter.GetPackageEnv(envId);
                    if (env[i] == null || env[i].AasEnv == null)
                    {
                        return output;
                    }

                    // set output
                    if (!cdIdentifier.IsNullOrEmpty() && env[i].AasEnv?.ConceptDescriptions != null)
                    {
                        output = (T)env[i].AasEnv.ConceptDescriptions.FirstOrDefault(cd => cd.Id != null && cd.Id.Equals(cdIdentifier));
                    }
                    else if (!aasIdentifier.IsNullOrEmpty() && env[i].AasEnv?.AssetAdministrationShells != null)
                    {
                        output = (T)env[i].AasEnv.AssetAdministrationShells.FirstOrDefault(aas => aas.Id != null && aas.Id.Equals(aasIdentifier));
                    }
                    else if (!smIdentifier.IsNullOrEmpty() && env[i].AasEnv?.Submodels != null)
                    {
                        output = (T)env[i].AasEnv.Submodels.FirstOrDefault(sm => sm.Id != null && sm.Id.Equals(smIdentifier));
                    }
                }

                // not found
                if (EqualityComparer<T>.Default.Equals(output, default))
                {
                    return output;
                }

                // found
                success = true;
                packageIndex = i;
                signalNewData(2);
                return output;
            }
        }

        public static int envimin = 0;
        public static int envimax = 200;
        public static AdminShellPackageEnv[] env = null;
        public static string[] envFileName = null;
        public static string[] envSymbols = null;

        public static string[] envSubjectIssuer = null;


        public static string hostPort = "";
        public static string blazorPort = "";
        public static string blazorHostPort = "";
        public static ulong dataVersion = 0;

        public static void  changeDataVersion() { dataVersion++; }
        public static ulong getDataVersion()    { return (dataVersion); }

        static Dictionary<string, SampleClient.UASampleClient> OPCClients = new Dictionary<string, SampleClient.UASampleClient>();
        static readonly object opcclientAddLock = new object(); // object for lock around connecting to an external opc server

        // static MqttServer AASMqttServer = new MqttServer();

        static bool runOPC = false;

        public static string connectServer = "";
        /*
        public static string connectNodeName = "";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop = false;
        */

        public static WebProxy proxy = null;
        public static HttpClientHandler clientHandler = null;

        public static bool noSecurity = false;
        public static bool edit = false;
        public static string externalRest = "";
        public static string externalBlazor = "";
        public static string externalRepository = "";
        public static bool readTemp = false;
        public static int saveTemp = 0;
        public static DateTime saveTempDt = new DateTime();
        public static string secretStringAPI = null;

        public static bool htmlId = false;

        // public static string Email = "";
        public static long submodelAPIcount = 0;

        public static HashSet<object> submodelsToPublish = new HashSet<object>();
        public static HashSet<object> submodelsToSubscribe = new HashSet<object>();

        public static Dictionary<object, string> generatedQrCodes = new Dictionary<object, string>();

        public static string redirectServer = "";
        public static string authType = "";
        public static string getUrl = "";
        public static string getSecret = "";

        public static bool isLoading = true;
        public static int count = 0;

        public static bool initializingRegistry = false;

        public static object changeAasxFile = new object();

        public static Dictionary<string, string> envVariables = new Dictionary<string, string>();

        public static bool withDb = false;
        public static bool withDbFiles = false;
        public static int startIndex = 0;

        public static bool withPolicy = false;

        public static bool showWeight = false;

        private class CommandLineArguments
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Local
#pragma warning disable 8618
            public string   Host            { get; set; }
            public string   Port            { get; set; }
            public bool     Https           { get; set; }
            public string   DataPath        { get; set; }
            public bool     Rest            { get; set; }
            public bool     Opc             { get; set; }
            public bool     Mqtt            { get; set; }
            public bool     DebugWait       { get; set; }
            public int?     OpcClientRate   { get; set; }
            public string[] Connect         { get; set; }
            public string   ProxyFile       { get; set; }
            public bool     NoSecurity      { get; set; }
            public bool     Edit            { get; set; }
            public string   Name            { get; set; }
            public string   ExternalRest    { get; set; }
            public string   ExternalBlazor  { get; set; }
            public bool     ReadTemp        { get; set; }
            public int      SaveTemp        { get; set; }
            public string   SecretStringAPI { get; set; }
            public string   Tag             { get; set; }
            public bool     HtmlId          { get; set; }
            public int      AasxInMemory    { get; set; }
            public bool     WithDb          { get; set; }
            public bool     NoDbFiles       { get; set; }
            public int      StartIndex      { get; set; }
#pragma warning restore 8618
            // ReSharper enable UnusedAutoPropertyAccessor.Local
        }

        private static async Task<int> Run(CommandLineArguments a, IServiceProvider serviceProvider)
        {
            var persistenceService = serviceProvider.GetService<IPersistenceService>();
            var dbRequestService = serviceProvider.GetService<IDbRequestHandlerService>();

            // Wait for Debugger
            if (a.DebugWait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", a.Host);
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            // Read environment variables
            string[] evlist = {"PLCNEXTTARGET", "WITHPOLICY", "SHOWWEIGHT", "AASREPOSITORY"};
            foreach (var ev in evlist)
            {
                string v = System.Environment.GetEnvironmentVariable(ev);
                if (v != null)
                {
                    v = v.Replace("\r", "");
                    v = v.Replace("\n", "");
                    Console.WriteLine("Variable: " + ev + " = " + v);
                    envVariables.Add(ev, v);
                }
            }

            string w;
            if (envVariables.TryGetValue("WITHPOLICY", out w))
            {
                if (w.ToLower() == "true" || w.ToLower() == "on")
                {
                    withPolicy = true;
                }

                if (w.ToLower() == "false" || w.ToLower() == "off")
                {
                    withPolicy = false;
                }

                Console.WriteLine("withPolicy: " + withPolicy);
            }

            if (envVariables.TryGetValue("SHOWWEIGHT", out w))
            {
                if (w.ToLower() == "true" || w.ToLower() == "on")
                {
                    showWeight = true;
                }

                if (w.ToLower() == "false" || w.ToLower() == "off")
                {
                    showWeight = false;
                }

                Console.WriteLine("showWeight: " + showWeight);
            }

            envVariables.TryGetValue("AASREPOSITORY", out externalRepository);

            /*
            if (a.Connect != null)
            {
                if (a.Connect.Length == 0)
                {
                    Program.connectServer = "http://admin-shell-io.com:52000";
                    Byte[]                barray = new byte[10];
                    RandomNumberGenerator rngCsp = RandomNumberGenerator.Create();
                    rngCsp.GetBytes(barray);
                    Program.connectNodeName   = "AasxServer_" + Convert.ToBase64String(barray);
                    Program.connectUpdateRate = 2000;
                    if (a.Name != null && a.Name != "")
                        Program.connectNodeName = a.Name;
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
                                Program.connectServer     = c[0];
                                Program.connectNodeName   = c[1];
                                Program.connectUpdateRate = Convert.ToInt32(c[2]);
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
            */

            /*
             * Set the global variables at this point inferred from the command-line arguments
             */

            if (a.DataPath != null)
            {
                Console.WriteLine($"Serving the AASXs from: {a.DataPath}");
                AasxHttpContextHelper.DataPath = a.DataPath;
            }

            Program.runOPC     = a.Opc;
            Program.noSecurity = a.NoSecurity;
            Program.edit       = a.Edit;
            Program.readTemp   = a.ReadTemp;
            // if (a.SaveTemp > 0)
            saveTemp            = a.SaveTemp;
            Program.htmlId      = a.HtmlId;
            Program.withDb      = a.WithDb;
            Program.withDbFiles = a.WithDb;
            if (a.NoDbFiles)
                Program.withDbFiles = false;
            if (a.StartIndex > 0)
                startIndex = a.StartIndex;
            if (a.AasxInMemory > 0)
                envimax = a.AasxInMemory;
            if (a.SecretStringAPI != null && a.SecretStringAPI != "")
            {
                secretStringAPI = a.SecretStringAPI;
                Console.WriteLine("secretStringAPI = " + secretStringAPI);
            }

            if (a.OpcClientRate != null && a.OpcClientRate < 200)
            {
                Console.WriteLine("Recommend an OPC client update rate > 200 ms.");
            }

            // allocate memory
            env              = new AdminShellPackageEnv[envimax];
            envFileName      = new string[envimax];
            envSymbols       = new string[envimax];
            envSubjectIssuer = new string[envimax];

            // Proxy
            string proxyAddress = "";
            string username     = "";
            string password     = "";

            if (a.ProxyFile != null)
            {
                if (!System.IO.File.Exists(a.ProxyFile))
                {
                    Console.Error.WriteLine($"Proxy file not found: {a.ProxyFile}");
                    return 1;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(a.ProxyFile))
                    {
                        proxyAddress = sr.ReadLine();
                        username     = sr.ReadLine();
                        password     = sr.ReadLine();
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
                    proxy.Address     = newUri;
                    proxy.Credentials = new NetworkCredential(username, password);
                    Console.WriteLine("Using proxy: " + proxyAddress);

                    clientHandler = new HttpClientHandler {Proxy = proxy, UseProxy = true};
                }
            }

            ;

            hostPort       = a.Host + ":" + a.Port;
            blazorHostPort = a.Host + ":" + blazorPort;

            if (a.ExternalRest != null)
            {
                externalRest = a.ExternalRest;
            }
            else
            {
                externalRest = "http://" + hostPort;
            }

            if (a.ExternalBlazor != null)
            {
                externalBlazor = a.ExternalBlazor;
            }
            else
            {
                externalBlazor = "http://" + blazorHostPort;
            }

            externalBlazor = externalBlazor.Replace("\r", "");
            externalBlazor = externalBlazor.Replace("\n", "");

            if (string.IsNullOrEmpty(externalRepository))
            {
                externalRepository = externalBlazor;
            }

            Query.ExternalBlazor = externalBlazor;

            /*
            if (File.Exists("redirect.dat"))
            {
                try
                {
                    using (StreamReader sr = new StreamReader("redirect.dat"))
                    {
                        redirectServer = sr.ReadLine();
                        authType = sr.ReadLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("redirect.dat " + " can not be read!");
                }
            }
            */

            // Pass global options to subprojects
            AdminShellNS.AdminShellPackageEnv.setGlobalOptions(withDb, withDbFiles, a.DataPath);

            // Read root cert from root subdirectory
            Console.WriteLine("Security 1 Startup - Server");
            Console.WriteLine("Security 1.1 Load X509 Root Certificates into X509 Store Root");

            try
            {
                X509Store root = new X509Store("Root", StoreLocation.CurrentUser);
                root.Open(OpenFlags.ReadWrite);

                DirectoryInfo ParentDirectory = new DirectoryInfo(".");

                if (Directory.Exists("./root"))
                {
                    foreach (FileInfo f in ParentDirectory.GetFiles("./root/*.cer"))
                    {
                        X509Certificate2 cert = new X509Certificate2("./root/" + f.Name);

                        root.Add(cert);
                        Console.WriteLine("Security 1.1 Add " + f.Name);
                    }
                }
            }
            catch (CryptographicException cryptographicException)
            {
                Console.WriteLine($"Cannot initialise cryptography: {cryptographicException.Message}");
            }

            if (!Directory.Exists("./temp"))
                Directory.CreateDirectory("./temp");

            string fn = null;

            /*
            if (a.Opc)
            {
                Boolean       is_BaseAddresses = false;
                Boolean       is_uaString      = false;
                XmlTextReader reader           = new XmlTextReader("Opc.Ua.SampleServer.Config.xml");
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
                                is_uaString      = false;
                            }

                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            break;
                    }
                }
            }
            */

            bool createFilesOnly = false;
            if (System.IO.File.Exists(AasxHttpContextHelper.DataPath + "/FILES.ONLY"))
                createFilesOnly = true;

            int envi = 0;

            // Init DB
            if (withDb)
            {
                persistenceService.InitDB(startIndex == 0 && !createFilesOnly, a.DataPath);
            }

            string[] fileNames = null;
            if (Directory.Exists(AasxHttpContextHelper.DataPath))
            {
                if (!Directory.Exists(AasxHttpContextHelper.DataPath + "/xml"))
                    Directory.CreateDirectory(AasxHttpContextHelper.DataPath + "/xml");
                if (!Directory.Exists(AasxHttpContextHelper.DataPath + "/files"))
                    Directory.CreateDirectory(AasxHttpContextHelper.DataPath + "/files");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx");
                Array.Sort(fileNames);

                var fi = 0;
                while (fi < fileNames.Length)
                {
                    // try
                    {
                        fn = fileNames[fi];
                        if (fn.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains("globalsecurity", StringComparison.InvariantCulture) ||
                            fn.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains("registry", StringComparison.InvariantCulture))
                        {
                            envFileName[envi] = fn;
                            env[envi]         = new AdminShellPackageEnv(fn, true, false);
                            //TODO:jtikekar
                            //AasxHttpContextHelper.securityInit(); // read users and access rights from AASX Security
                            //AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                            envi++;
                            envimin = envi;
                            oldest  = envi;
                            fi++;
                            continue;
                        }

                        if (fi < startIndex)
                        {
                            fi++;
                            continue;
                        }


                        if (fn != "" && envi < envimax)
                        {
                            string name     = Path.GetFileName(fn);
                            string tempName = "./temp/" + Path.GetFileName(fn);

                            // Convert to newest version only
                            if (saveTemp == -1)
                            {
                                env[envi] = new AdminShellPackageEnv(fn, true, false);
                                if (env[envi] == null)
                                {
                                    Console.Error.WriteLine($"Cannot open {fn}. Aborting..");
                                    return 1;
                                }

                                Console.WriteLine((fi + 1) + "/" + fileNames.Length + " " + watch.ElapsedMilliseconds / 1000 + "s " + "SAVE TO TEMP: " + fn);
                                Program.env[envi].SaveAs(tempName);
                                fi++;
                                continue;
                            }

                            if (readTemp && System.IO.File.Exists(tempName))
                            {
                                fn = tempName;
                            }

                            Console.WriteLine((fi + 1) + "/" + fileNames.Length + " " + watch.ElapsedMilliseconds / 1000 + "s" + " Loading {0}...", fn);
                            envFileName[envi] = fn;
                            if (!withDb)
                            {
                                env[envi] = new AdminShellPackageEnv(fn, true, false);
                                if (env[envi] == null)
                                {
                                    Console.Error.WriteLine($"Cannot open {fn}. Aborting..");
                                    return 1;
                                }
                            }
                            else
                            {
                                persistenceService.ImportAASXIntoDB(fn, createFilesOnly, withDbFiles);
                                envFileName[envi] = null;
                                env[envi]         = null;
                            }

                            // check if signed
                            string fileCert = "./user/" + name + ".cer";
                            if (System.IO.File.Exists(fileCert))
                            {
                                X509Certificate2 x509 = new X509Certificate2(fileCert);
                                envSymbols[envi]       = "S";
                                envSubjectIssuer[envi] = x509.Subject;

                                X509Chain chain = new X509Chain();
                                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                bool isValid = chain.Build(x509);
                                if (isValid)
                                {
                                    envSymbols[envi]       += ";V";
                                    envSubjectIssuer[envi] += ";" + x509.Issuer;
                                }
                            }
                        }

                        fi++;
                        if (withDb)
                        {
                            if (fi % 500 == 0) // every 500
                            {
                                /*
                                Console.WriteLine("DB Save Changes");
                                db.SaveChanges();
                                db.ChangeTracker.Clear();
                                System.GC.Collect();
                                */
                            }
                        }
                        else
                        {
                            envi++;
                        }
                    }
                    /*
                    catch
                    {
                        Console.WriteLine("Error with " + fileNames[fi]);
                        fi++;
                    }
                    */
                }

                if (saveTemp == -1)
                    return (0);

                if (withDb)
                {
                    // preload AASX from DB and keep in memory
                    var packages = new List<AdminShellPackageEnv>();
                    var paths = persistenceService.ReadFilteredPackages("--memory", packages);

                    for (var p = 0; p < packages.Count && p < paths.Count; p++)
                    {
                        envFileName[envi] = paths[p];
                        env[envi] = packages[p];
                        envi++;
                    }
                    envimin = envi;
                    oldest = envi;

                    /*
                    Console.WriteLine("DB Save Changes");
                    db.SaveChanges();
                    db.ChangeTracker.Clear();
                    System.GC.Collect();
                    */
                }

                watch.Stop();
                Console.WriteLine(fi + " AASX loaded in " + watch.ElapsedMilliseconds / 1000 + "s");

                fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx2");
                Array.Sort(fileNames);

                for (int j = 0; j < fileNames.Length; j++)
                {
                    fn = fileNames[j];

                    if (fn != "" && envi < envimax)
                    {
                        envFileName[envi] = fn;
                        envSymbols[envi]  = "L"; // Show lock
                    }

                    envi++;
                }
            }

            if (!withDb)
            {
                // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
            }

            Console.WriteLine();
            Console.WriteLine("Please wait for the servers to start...");

            if (a.Rest)
            {
                Console.WriteLine("--rest argument is not supported anymore, as the old V2 related REST APIs are deprecated. Please find the new REST APIs on the port 5001.");
                //Console.WriteLine("Connect to REST by: {0}:{1}", a.Host, a.Port);

                //AasxRestServer.Start(env, a.Host, a.Port, a.Https); // without Logger

                //Console.WriteLine("REST Server started.");
            }

            //i40LanguageRuntime.initialize();

            // MICHA MICHA
            AasxTimeSeries.TimeSeries.timeSeriesInit();

            /* OZOZ
            var _energyModelInstances = new List<EnergyModelInstance>();
            foreach (var penv in AasxServer.Program.env)
            {
                EnergyModelInstance.TagAllAasAndSm(penv?.AasEnv, DateTime.UtcNow);
                _energyModelInstances.AddRange(
                    EnergyModelInstance.FindAllSmInstances(penv?.AasEnv));
            }
            EnergyModelInstance.StartAllAsOneThread(_energyModelInstances);
            */

            AasxTask.taskInit(serviceProvider.GetService<IEventService>());

            // RunScript(true);
            //// Initialize            NewDataAvailable?.Invoke(null, EventArgs.Empty);

            // Disable, because of Linux Segementation Fault
            //// ProductChange.pcn.pcnInit();

            isLoading = false;

            /*
            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStartAsync().Wait();
                Console.WriteLine("MQTT Publisher started.");
            }
            */

            /*
            MySampleServer server = null;
            if (a.Opc)
            {
                server = new MySampleServer(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env);
                Console.WriteLine("OPC UA Server started..");
            }
            */

            if (a.OpcClientRate != null) // read data by OPC UA
            {
                // Initial read of OPC values, will quit the program if it returns false
                if (!ReadOPCClient(true))
                {
                    Console.Error.WriteLine("Failed to read from the OPC client.");
                    return 1;
                }

                Console.WriteLine($"OPC client will be updating every: {a.OpcClientRate} milliseconds");
                SetOPCClientTimer((double)a.OpcClientRate); // read again everytime timer expires
            }

            // SetScriptTimer(1000); // also updates balzor view

            /*
            if (connectServer != "")
            {
                HttpClient httpClient;
                if (clientHandler == null)
                {
                    clientHandler                         = new HttpClientHandler();
                    clientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    httpClient                            = new HttpClient(clientHandler);
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
                    // connectThread = new Thread(new ThreadStart(connectThreadLoop));
                    // MICHA
                    // connectThread.Start();
                    // connectLoop = true;
                }
                else
                {
                    Console.WriteLine("********** Can not connect to: " + connectServer);
                }
            }
            */

            Program.signalNewData(3);

            /*
            if (a.Opc && server != null)
            {
                server.Run(); // wait for CTRL-C
            }
            else
            */
            {
                // no OPC UA: wait only for CTRL-C
                Console.WriteLine("Servers successfully started. Press Ctrl-C to exit...");

                //jtikekar moved this code to Blazor's Program.cs
                //ManualResetEvent quitEvent = new ManualResetEvent(false);
                //try
                //{
                //    Console.CancelKeyPress += (sender, eArgs) =>
                //    {
                //        quitEvent.Set();
                //        //eArgs.Cancel = true;
                //    };
                //}
                //catch
                //{
                //}

                //// wait for timeout or Ctrl-C
                //quitEvent.WaitOne(Timeout.Infinite);
            }

            // wait for RETURN

            /*
            if (connectServer != "")
            {
                **
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
                **

                if (connectLoop)
                {
                    connectLoop = false;
                }
            }
            */

            /*
            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStopAsync().Wait();
            }
            */

            // AasxRestServer.Stop();

            return 0;
        }

        public static void Main(string[] args, IServiceProvider serviceProvider)
        {
            Console.WriteLine("args:");
            foreach (var a in args)
            {
                Console.WriteLine(a);
            }

            Console.WriteLine();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            }

            // AasContext.Config = con;
            // AasContext.IsPostgres = AasContext.GetConnectionString().ToLower().Contains("host");

            string nl = System.Environment.NewLine;

            var rootCommand = new RootCommand("serve AASX packages over different interfaces")
                              {
                                  new Option<string>(
                                                     new[] {"--host"},
                                                     () => "localhost",
                                                     "Host which the server listens on"),
                                  new Option<string>(
                                                     new[] {"--data-path"},
                                                     "Path to where the AASXs reside"),
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
                                                     new[] {"--external-blazor"},
                                                     "external name of the server blazor UI"),
                                  new Option<bool>(
                                                   new[] {"--read-temp"},
                                                   "If set, reads existing AASX from temp at startup"),
                                  new Option<int>(
                                                  new[] {"--save-temp"},
                                                  "If set, writes AASX every given seconds"),
                                  new Option<string>(
                                                     new[] {"--secret-string-api"},
                                                     "If set, allows UPDATE access by query parameter s="),
                                  new Option<bool>(
                                                   new[] {"--html-id"},
                                                   "If set, creates id for HTML objects in blazor tree for testing"),
                                  new Option<string>(
                                                     new[] {"--tag"},
                                                     "Only used to differ servers in task list"),
                                  new Option<int>(
                                                  new[] {"--aasx-in-memory"},
                                                  "If set, size of array of AASX files in memory"),
                                  new Option<bool>(
                                                   new[] {"--with-db"},
                                                   "If set, will use DB by Entity Framework"),
                                  new Option<bool>(
                                                   new[] {"--no-db-files"},
                                                   "If set, do not export files from AASX into ZIP"),
                                  new Option<int>(
                                                  new[] {"--start-index"},
                                                  "If set, start index in list of AASX files")
                              };

            if (args.Length == 0)
            {
                new HelpBuilder(new SystemConsole()).Write(rootCommand);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    WindowsConsoleWillBeDestroyedAtTheEnd.Check())
                {
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

                return;
            }

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create((CommandLineArguments a) =>
                                                                                      {
                                                                                          var task = Run(a, serviceProvider);
                                                                                          task.Wait();
                                                                                          var op = task.Result;
                                                                                          return Task.FromResult(op);
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
        /*
        public class AASxEndpoint
        {
            [XmlElement(ElementName = "address")] public string address = "";

            [XmlElement(ElementName = "type")] public string type = "";
        }
        */

        /* Submodel Descriptor Definition */
        /*
        public class SubmodelDescriptors
        {
            [XmlElement(ElementName = "administration")] [JsonIgnore]
            //public AdminShell.Administration administration = null;
            public AdministrativeInformation administration = null;

            [XmlElement(ElementName = "description")] [JsonIgnore]
            //public AdminShell.Description description = null;
            public List<ILangStringTextType> description = null;

            [XmlElement(ElementName = "idShort")] [JsonIgnore]
            public string idShort = "";

            [XmlElement(ElementName = "identification")] [JsonIgnore]
            public string identification = null;

            [XmlElement(ElementName = "semanticId")]
            public Reference semanticId = null;

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();
        }
        */

        /* AAS Descriptor Definiton */
        /*
        public class aasDescriptor
        {
            [XmlElement(ElementName = "administration")] [JsonIgnore]
            public AdministrativeInformation administration = null;

            [XmlElement(ElementName = "description")] [JsonIgnore]
            public List<ILangStringTextType> description = new(new List<ILangStringTextType>());

            [XmlElement(ElementName = "idShort")] public string idShort = "";

            [XmlElement(ElementName = "identification")] [JsonIgnore]
            public string identification = null;

            [XmlElement(ElementName = "assets")] public List<AssetInformation> assets = new List<AssetInformation>();

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();

            [XmlElement(ElementName = "submodelDescriptors")]
            public List<SubmodelDescriptors> submodelDescriptors = new List<SubmodelDescriptors>();
        }
        */

        /* AAS Detail Part 2 Descriptor Definitions END*/

        /* Creation of AAS Descriptor */
        // TODO (jtikekar, 2023-09-04): Remove for now
        /*
        public static aasDescriptor creatAASDescriptor(AdminShellPackageEnv adminShell)
        {
            aasDescriptor aasD            = new aasDescriptor();
            string        endpointAddress = "http://" + hostPort;

            aasD.idShort        = adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
            aasD.identification = adminShell.AasEnv.AssetAdministrationShells[0].Id;
            aasD.description    = adminShell.AasEnv.AssetAdministrationShells[0].Description;

            AASxEndpoint endp = new AASxEndpoint();
            endp.address = endpointAddress + "/aas/" + adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
            aasD.endpoints.Add(endp);

            int submodelCount = adminShell.AasEnv.Submodels.Count;
            for (int i = 0; i < submodelCount; i++)
            {
                SubmodelDescriptors sdc = new SubmodelDescriptors();

                sdc.administration = adminShell.AasEnv.Submodels[i].Administration as AdministrativeInformation;
                sdc.description    = adminShell.AasEnv.Submodels[i].Description;
                sdc.identification = adminShell.AasEnv.Submodels[i].Id;
                sdc.idShort        = adminShell.AasEnv.Submodels[i].IdShort;
                sdc.semanticId     = adminShell.AasEnv.Submodels[i].SemanticId as Reference;

                AASxEndpoint endpSub = new AASxEndpoint();
                endpSub.address = endpointAddress + "/aas/" + adminShell.AasEnv.AssetAdministrationShells[0].IdShort +
                                  "/submodels/" + adminShell.AasEnv.Submodels[i].IdShort;
                endpSub.type = "http";
                sdc.endpoints.Add(endpSub);

                aasD.submodelDescriptors.Add(sdc);
            }

            //Commented wrt new specifications, Environment does not contain AssetInformatiom
            //int assetCount = adminShell.AasEnv.Assets.Count;
            //for (int i = 0; i < assetCount; i++)
            //{
            //    aasD.assets.Add(adminShell.AasEnv.Assets[i]);
            //}
            return aasD;
        }
        */

        /*Publishing the AAS Descriptor*/
        /*
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
        */

        /*
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
        */

        // static List<TransmitData> tdPending = new List<TransmitData> { };

        static bool timerSet = false;

        private static void SetOPCClientTimer(double value)
        {
            if (timerSet)
            {
                return;
            }

            timerSet = true;

            AasxTimeSeries.TimeSeries.SetOPCClientThread(value);
        }

        public static event EventHandler NewDataAvailable;

        public class NewDataAvailableArgs : EventArgs
        {
            public int signalNewDataMode;

            public NewDataAvailableArgs(int mode = 2)
            {
                signalNewDataMode = mode;
            }
        }

        // 0 == same tree, only values changed
        // 1 == same tree, structure may change
        // 2 == build new tree, keep open nodes
        // 3 == build new tree, all nodes closed
        // public static int signalNewDataMode = 2;
        public static void signalNewData(int mode)
        {
            // signalNewDataMode = mode;
            // NewDataAvailable?.Invoke(null, EventArgs.Empty);
            NewDataAvailable?.Invoke(null, new NewDataAvailableArgs(mode));
        }

        /*
        public static int getSignalNewDataMode()
        {
            int mode = signalNewDataMode;
            signalNewDataMode = 0;
            return (mode);
        }
        */

        public static void OnOPCClientNextTimedEvent()
        {
            ReadOPCClient(false);
            // RunScript(false);
            NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }

        /*
        private static System.Timers.Timer scriptTimer;

        private static void SetScriptTimer(double value)
        {
            // Create a timer with a two second interval.
            scriptTimer = new System.Timers.Timer(value);
            // Hook up the Elapsed event for the timer. 
            scriptTimer.Elapsed   += OnScriptTimedEvent;
            scriptTimer.AutoReset =  true;
            scriptTimer.Enabled   =  true;
        }

        private static void OnScriptTimedEvent(Object source, ElapsedEventArgs e)
        {
            RunScript(false);
            // NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }
        */

        private static Boolean OPCWrite(string nodeId, object value)
            /// <summary>
            /// Writes to (i.e. updates values of) Nodes in the AAS OPC Server
            /// </summary>
        {
            return true;

            /*
            if (!runOPC)
            {
                return true;
            }

            AasOpcUaServer.AasModeManager nodeMgr = AasOpcUaServer.AasEntityBuilder.nodeMgr;

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
                return false;
            }

            var convertedValue = Convert.ChangeType(value, bvs.Value.GetType());
            if (!object.Equals(bvs.Value, convertedValue))
            {
                bvs.Value = convertedValue;
                // TODO: timestamp UtcNow okay or get this internally from the Server?
                bvs.Timestamp = DateTime.UtcNow;
                bvs.ClearChangeMasks(null, false);
            }

            return true;
            */
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

            lock (Program.changeAasxFile)
            {
                int i = 0;
                while (env[i] != null)
                {
                    foreach (var sm in env[i].AasEnv.Submodels)
                    {
                        if (sm != null && sm.IdShort != null)
                        {
                            int count = sm.Qualifiers.Count;
                            if (count != 0)
                            {
                                int  stopTimeout = Timeout.Infinite;
                                bool autoAccept  = true;
                                // Variablen aus AAS Qualifiern
                                string Username  = "";
                                string Password  = "";
                                string URL       = "";
                                int    Namespace = 0;
                                string Path      = "";

                                int j = 0;

                                while (j < count) // URL, Username, Password, Namespace, Path
                                {
                                    var p = sm.Qualifiers[j] as Qualifier;

                                    switch (p.Type)
                                    {
                                        case "OPCURL": // URL
                                            URL = p.Value;
                                            break;
                                        case "OPCUsername": // Username
                                            Username = p.Value;
                                            break;
                                        case "OPCPassword": // Password
                                            Password = p.Value;
                                            break;
                                        case "OPCNamespace": // Namespace
                                            // TODO: if not int, currently throws nondescriptive error
                                            if (int.TryParse(p.Value, out int tmpI))
                                                Namespace = tmpI;
                                            break;
                                        case "OPCPath": // Path
                                            Path = p.Value;
                                            break;
                                        case "OPCEnvVar": // Only if enviroment variable ist set
                                            // VARIABLE=VALUE
                                            string[] split = p.Value.Split('=');
                                            if (split.Length == 2)
                                            {
                                                string value = "";
                                                if (envVariables.TryGetValue(split[0], out value))
                                                {
                                                    if (split[1] != value)
                                                        URL = ""; // continue
                                                }
                                            }

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
                                }

                                // try to get the client from dictionary, else create and add it
                                SampleClient.UASampleClient client;
                                lock (Program.opcclientAddLock)
                                {
                                    if (!OPCClients.TryGetValue(URL, out client))
                                    {
                                        try
                                        {
                                            // make OPC UA client
                                            client = new SampleClient.UASampleClient(URL, autoAccept, stopTimeout, Username, Password);
                                            Console.WriteLine("Connecting to external OPC UA Server at {0} with {1} ...", URL, sm.IdShort);
                                            client.ConsoleSampleClient().Wait();
                                            // add it to the dictionary under this submodels idShort
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
                                                Console.WriteLine("Could not connect to {0} with {1} ...", URL, sm.IdShort);
                                                return true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Already connected to OPC UA Server at {0} with {1} ...", URL, sm.IdShort);
                                    }
                                }

                                Console.WriteLine("==================================================");
                                Console.WriteLine("Read values for {0} from {1} ...", sm.IdShort, URL);
                                Console.WriteLine("==================================================");

                                // over all SMEs
                                count = sm.SubmodelElements.Count;
                                for (j = 0; j < count; j++)
                                {
                                    var sme = sm.SubmodelElements[j];
                                    // some preparations for multiple AAS below
                                    int serverNamespaceIdx = 3; //could be gotten directly from the nodeMgr in OPCWrite instead, only pass the string part of the Id

                                    string AASSubmodel
                                        = env[i].AasEnv.AssetAdministrationShells[0].IdShort + "." +
                                          sm.IdShort; // for multiple AAS, use something like env.AasEnv.AssetAdministrationShells[i].IdShort;
                                    string serverNodePrefix = string.Format("ns={0};s=AASROOT.{1}", serverNamespaceIdx, AASSubmodel);
                                    string nodePath         = Path; // generally starts with Submodel idShort
                                    WalkSubmodelElement(sme, nodePath, serverNodePrefix, client, Namespace);
                                }
                            }
                        }
                    }

                    i++;
                }
            }

            if (!initial)
            {
                changeDataVersion();
            }

            return true;
        }

        /*
        static void RunScript(bool init)
        {
            if (env == null)
                return;

            // if (countRunScript++ > 1)
            //    return;

            lock (Program.changeAasxFile)
            {
                int i = 0;
                while (i < env.Length && env[i] != null)
                {
                    if (env[i].AasEnv.Submodels != null)
                    {
                        foreach (var sm in env[i].AasEnv.Submodels)
                        {
                            if (sm != null && sm.IdShort != null)
                            {
                                int count = sm.Qualifiers != null ? sm.Qualifiers.Count : 0;
                                if (count != 0)
                                {
                                    var q = sm.Qualifiers[0] as Qualifier;
                                    if (q.Type == "SCRIPT")
                                    {
                                        // Triple
                                        // Reference to property with Number
                                        // Reference to submodel with numbers/strings
                                        // Reference to property to store found text
                                        count = sm.SubmodelElements.Count;
                                        int smi = 0;
                                        while (smi < count)
                                        {
                                            var sme1 = sm.SubmodelElements[smi++];
                                            if (sme1.Qualifiers == null || sme1.Qualifiers.Count == 0)
                                            {
                                                continue;
                                            }

                                            var qq = sme1.Qualifiers[0] as Qualifier;

                                            if (qq.Type == "Add")
                                            {
                                                int v = Convert.ToInt32((sme1 as Property).Value);
                                                v                        += Convert.ToInt32(qq.Value);
                                                (sme1 as Property).Value =  v.ToString();
                                                continue;
                                            }

                                            if (qq.Type == "GetValue")
                                            {
                                                **
                                                if (!(sme1 is ReferenceElement))
                                                {
                                                    continue;
                                                }

                                                string url = qq.Value;
                                                string username = "";
                                                string password = "";

                                                if (sme1.qualifiers.Count == 3)
                                                {
                                                    qq = sme1.qualifiers[1] as Qualifier;
                                                    if (qq.type != "Username")
                                                        continue;
                                                    username = qq.Value;
                                                    qq = sme1.qualifiers[2] as Qualifier;
                                                    if (qq.type != "Password")
                                                        continue;
                                                    password = qq.Value;
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

                                                string response = await client.GetStringAsync(url);

                                                var r12 = sme1 as ReferenceElement;
                                                var ref12 = env[i].AasEnv.FindReferableByReference(r12.Value);
                                                if (ref12 is Property)
                                                {
                                                    var p1 = ref12 as Property;
                                                    p1.Value = response;
                                                }
                                                continue;
                                                **
                                            }

                                            if (qq.Type == "GetJSON")
                                            {
                                                if (init)
                                                    return;

                                                if (Program.isLoading)
                                                    return;

                                                if (!(sme1 is ReferenceElement))
                                                {
                                                    continue;
                                                }

                                                string url      = qq.Value;
                                                string username = "";
                                                string password = "";

                                                if (sme1.Qualifiers.Count == 3)
                                                {
                                                    qq = sme1.Qualifiers[1] as Qualifier;
                                                    if (qq.Type != "Username")
                                                        continue;
                                                    username = qq.Value;
                                                    qq       = sme1.Qualifiers[2] as Qualifier;
                                                    if (qq.Type != "Password")
                                                        continue;
                                                    password = qq.Value;
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
                                                    var r12   = sme1 as ReferenceElement;
                                                    var ref12 = env[i].AasEnv.FindReferableByReference(r12.GetModelReference());
                                                    if (ref12 is SubmodelElementCollection)
                                                    {
                                                        var c1     = ref12 as SubmodelElementCollection;
                                                        var parsed = JsonDocument.Parse(response);
                                                        ParseJson(c1, parsed, null);
                                                    }
                                                }

                                                continue;
                                            }

                                            if (qq.Type != "SearchNumber" || smi >= count)
                                            {
                                                continue;
                                            }

                                            var sme2 = sm.SubmodelElements[smi++];
                                            if (sme2.Qualifiers.Count == 0)
                                            {
                                                continue;
                                            }

                                            qq = sme2.Qualifiers[0] as Qualifier;
                                            if (qq.Type != "SearchList" || smi >= count)
                                            {
                                                continue;
                                            }

                                            var sme3 = sm.SubmodelElements[smi++];
                                            if (sme3.Qualifiers.Count == 0)
                                            {
                                                continue;
                                            }

                                            qq = sme3.Qualifiers[0] as Qualifier;
                                            if (qq.Type != "SearchResult")
                                            {
                                                break;
                                            }

                                            if (sme1 is ReferenceElement &&
                                                sme2 is ReferenceElement &&
                                                sme3 is ReferenceElement)
                                            {
                                                var r1   = sme1 as ReferenceElement;
                                                var r2   = sme2 as ReferenceElement;
                                                var r3   = sme3 as ReferenceElement;
                                                var ref1 = env[i].AasEnv.FindReferableByReference(r1.GetModelReference());
                                                var ref2 = env[i].AasEnv.FindReferableByReference(r2.GetModelReference());
                                                var ref3 = env[i].AasEnv.FindReferableByReference(r3.GetModelReference());
                                                if (ref1 is Property && ref2 is Submodel && ref3 is Property)
                                                {
                                                    var p1 = ref1 as Property;
                                                    // Simulate changes
                                                    var sm2    = ref2 as Submodel;
                                                    var p3     = ref3 as Property;
                                                    int count2 = sm2.SubmodelElements.Count;
                                                    for (int j = 0; j < count2; j++)
                                                    {
                                                        var sme = sm2.SubmodelElements[j];
                                                        if (sme.IdShort == p1.Value)
                                                        {
                                                            p3.Value = (sme as Property).Value;
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

                    i++;
                }
            }

            return;
        }
        */

        public static bool ParseJson(SubmodelElementCollection c, object o, List<string> filter,
                                     Property minDiffAbsolute = null, Property minDiffPercent = null,
                                     AdminShellPackageEnv envaas = null)
        {
            var newMode   = 0;
            var timeStamp = DateTime.UtcNow;
            var ok        = false;

            var iMinDiffAbsolute = 1;
            var iMinDiffPercent  = 0;
            if (minDiffAbsolute != null)
                iMinDiffAbsolute = Convert.ToInt32(minDiffAbsolute.Value);
            if (minDiffPercent != null)
                iMinDiffPercent = Convert.ToInt32(minDiffPercent.Value);

            switch (o)
            {
                case JsonDocument doc:
                    ok |= ParseJson(c, doc.RootElement, filter, minDiffAbsolute, minDiffPercent, envaas);
                    break;
                case JsonElement el:
                    foreach (JsonProperty jp1 in el.EnumerateObject())
                    {
                        if (filter != null && filter.Count != 0)
                        {
                            if (!filter.Contains(jp1.Name))
                                continue;
                        }

                        SubmodelElementCollection c2;
                        switch (jp1.Value.ValueKind)
                        {
                            case JsonValueKind.Array:
                                c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                                if (c2 == null)
                                {
                                    c2 = new SubmodelElementCollection(idShort: jp1.Name);
                                    c.Value.Add(c2);
                                    c2.TimeStampCreate = timeStamp;
                                    c2.SetTimeStamp(timeStamp);
                                    newMode = 1;
                                }

                                var count = 1;
                                foreach (var subEl in jp1.Value.EnumerateArray())
                                {
                                    var n = $"{jp1.Name}_array_{count++}";
                                    var c3 =
                                        c2.FindFirstIdShortAs<SubmodelElementCollection>(n);
                                    if (c3 == null)
                                    {
                                        c3 = new SubmodelElementCollection(idShort: n);
                                        c2.Value.Add(c3);
                                        c3.TimeStampCreate = timeStamp;
                                        c3.SetTimeStamp(timeStamp);
                                        newMode = 1;
                                    }

                                    ok |= ParseJson(c3, subEl, filter, minDiffAbsolute, minDiffPercent, envaas);
                                }

                                break;
                            case JsonValueKind.Object:
                                c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                                if (c2 == null)
                                {
                                    c2 = new SubmodelElementCollection(idShort: jp1.Name);
                                    c.Value.Add(c2);
                                    c2.TimeStampCreate = timeStamp;
                                    c2.SetTimeStamp(timeStamp);
                                    newMode = 1;
                                }

                                ok |= ParseJson(c2, jp1.Value, filter, minDiffAbsolute, minDiffPercent, envaas);
                                break;
                        }
                    }

                    break;
                default:
                    throw new ArgumentException("Unsupported argument type for JSON parsing.");
            }

            envaas?.setWrite(true);
            Program.signalNewData(newMode);
            return ok;
        }

        private static void WalkSubmodelElement(ISubmodelElement sme, string nodePath, string serverNodePrefix, SampleClient.UASampleClient client, int clientNamespace)
        {
            if (sme is Property)
            {
                var p              = sme as Property;
                var clientNodeName = nodePath + p.IdShort;
                var serverNodeId   = $"{serverNodePrefix}.{p.IdShort}.Value";
                var clientNode     = new NodeId(clientNodeName, (ushort)clientNamespace);
                UpdatePropertyFromOPCClient(p, serverNodeId, client, clientNode);
            }
            else if (sme is SubmodelElementCollection)
            {
                var collection = sme as SubmodelElementCollection;
                foreach (var t in collection.Value)
                {
                    var newNodeIdBase = $"{nodePath}.{collection.IdShort}";
                    WalkSubmodelElement(t, newNodeIdBase, serverNodePrefix, client, clientNamespace);
                }
            }
        }

        private static void UpdatePropertyFromOPCClient(Property p, string serverNodeId, SampleClient.UASampleClient client, NodeId clientNodeId)
        {
            string value = "";

            bool write = (p.FindQualifierOfType("OPCWRITE") != null);
            if (write)
                value = p.Value;

            try
            {
                // ns=#;i=#
                string[] split = (clientNodeId.ToString()).Split('#');
                if (split.Length == 2)
                {
                    uint i = Convert.ToUInt16(split[1]);
                    split = clientNodeId.ToString().Split('=');
                    split = split[1].Split(';');
                    ushort ns = Convert.ToUInt16(split[0]);
                    clientNodeId = new NodeId(i, ns);
                    Console.WriteLine("New node id: ", clientNodeId.ToString());
                }

                Console.WriteLine(string.Format("{0} <= {1}", serverNodeId, value));
                if (write)
                {
                    short i = Convert.ToInt16(value);
                    client.WriteSubmodelElementValue(clientNodeId, i);
                }
                else
                    value = client.ReadSubmodelElementValue(clientNodeId);
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine(string.Format("OPC ServiceResultException ({0}) trying to read {1}", ex.Message, clientNodeId.ToString()));
                return;
            }

            // update in AAS env
            if (!write)
            {
                p.Value = value;
                //p.Set(p.ValueType, value);
                signalNewData(0);

                // update in OPC
                if (!OPCWrite(serverNodeId, value))
                    Console.WriteLine("OPC write not successful.");
            }
        }
    }

    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask = false;

        public override void Message(string text, bool ask)
        {
            this.message = text;
            this.ask     = ask;
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

    /*
    public class MySampleServer
    {
        SampleServer server;
        Task status;
        DateTime lastEventTime;
        int serverRunTime = Timeout.Infinite;
        static bool autoAccept = false;
        static ExitCode exitCode;

        static AdminShellPackageEnv[] aasxEnv = null;

        // OZ
        public static ManualResetEvent quitEvent;

        public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShellPackageEnv[] _aasxEnv)
        {
            autoAccept    = _autoAccept;
            aasxEnv       = _aasxEnv;
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
                }
                else
                {
                }
            }
        }

        private async Task ConsoleSampleServer()
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance();

            application.ApplicationName   = "UA Core Sample Server";
            application.ApplicationType   = ApplicationType.Server;
            application.ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleServer" : "Opc.Ua.SampleServer";

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(true);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(true, 0);

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
            server.CurrentInstance.SessionManager.SessionClosing   += EventStatus;
            server.CurrentInstance.SessionManager.SessionCreated   += EventStatus;
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
    */
}