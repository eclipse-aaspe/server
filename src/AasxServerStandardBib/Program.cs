using AasOpcUaServer;
using AasxMqttServer;
using AasxRestServerLibrary;
//using AasxServerStandardBib.Migrations;
using AdminShellNS;
using Extensions;
using Jose;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
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
        public static IConfiguration con { get; set; }
        public static string getBetween(AdminShellPackageEnv env, string strStart, string strEnd)
        {
            string strSource = env.getEnvXml();
            if (strSource != null && strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }
        public static void createDbFiles(List<SubmodelSet> slist, AdminShellPackageEnv env, string dataPath, string fileName)
        {
            string envXml = env.getEnvXml();

            Console.WriteLine("Length XML env: " + envXml.Length);
            string fnXml = dataPath + "/xml/" + fileName + ".xml";
            System.IO.File.WriteAllText(fnXml, envXml);

            // extract semanticIds
            int pos = 0;
            int found1 = -1;
            int found2 = -1;
            string semanticId = "";
            int countSemanticId = 0;
            int countIdShort = 0;
            HashSet<string> hsSubSemanticId = null;
            HashSet<string> hsSubIdShort = null;
            string subId = null;

            string search = "";
            string text = "";
            XmlReader reader = XmlReader.Create(new StringReader(envXml));
            while (!reader.EOF)
            {
                reader.Read();
                if (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "submodel" || reader.Name == "aas:submodel")
                        {
                            search = "id aas:identification";
                            hsSubSemanticId = new HashSet<string>();
                            hsSubIdShort = new HashSet<string>();
                            subId = null;
                            countSemanticId = 0;
                            countIdShort = 0;
                        }
                        else if (search.Contains(reader.Name))
                        {
                            switch (reader.Name)
                            {
                                case "aas:semanticId":
                                case "semanticId":
                                    search = "aas:key key";
                                    break;
                                case "key":
                                    search = "value";
                                    break;
                            }
                        }
                    }
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        text = reader.Value;
                    }
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "id":
                            case "aas:identification":
                                if (search.Contains(reader.Name))
                                {
                                    subId = text;
                                    search = "aas:semanticId semanticId";
                                }
                                break;
                            case "aas:semanticId":
                            case "semanticId":
                            case "key":
                                search = "aas:semanticId semanticId";
                                break;
                            case "aas:key":
                            case "value":
                                if (subId != null && search.Contains(reader.Name))
                                {
                                    hsSubSemanticId.Add(text);
                                    countSemanticId++;
                                    search = "aas:semanticId semanticId";
                                }
                                break;
                            case "idShort":
                            case "aas:idShort":
                                if (subId != null)
                                {
                                    hsSubIdShort.Add(text);
                                    countIdShort++;
                                }
                                break;
                            case "submodel":
                            case "aas:submodel":
                                // Submodel complete
                                search = "";
                                if (subId != null)
                                {
                                    SubmodelSet submodelDB = null;
                                    var submodelDBList = slist.Where(s => s.SubmodelId == subId);
                                    if (submodelDBList.Any())
                                    {
                                        submodelDB = submodelDBList.First();
                                    }

                                    Console.WriteLine("Found " + hsSubSemanticId.Count + " different semanticIds in total of " + countSemanticId + " in submodel " + subId);
                                    string subId64 = Base64UrlEncoder.Encode(subId);
                                    string fnSemanticId = dataPath + "/xml/semanticid--" + subId64 + ".txt";

                                    string lines = "";
                                    foreach (var h in hsSubSemanticId)
                                    {
                                        lines += h + "\r\n";
                                    }
                                    System.IO.File.WriteAllText(fnSemanticId, lines);
                                    /*
                                    if (submodelDB != null)
                                        submodelDB.AllSemanticId = lines;
                                    */

                                    Console.WriteLine("Found " + hsSubIdShort.Count + " different idShorts in total of " + countIdShort + " in submodel " + subId);
                                    subId64 = Base64UrlEncoder.Encode(subId);
                                    string fnIdShort = dataPath + "/xml/idshort--" + subId64 + ".txt";

                                    lines = "";
                                    foreach (var h in hsSubIdShort)
                                    {
                                        lines += h + "\r\n";
                                    }
                                    System.IO.File.WriteAllText(fnIdShort, lines);
                                    /*
                                    if (submodelDB != null)
                                        submodelDB.AllIdshort = lines;
                                    */

                                    subId = null;
                                }
                                break;
                        }
                    }
                }
            }

            /*
            // Pattern 1
            do
            {
                found1 = envXml.IndexOf("<aas:semanticId>", pos);
                if (found1 != -1)
                {
                    found1 = envXml.IndexOf("<aas:key ", found1);
                    if (found1 != -1)
                    {
                        found1 = envXml.IndexOf(">", found1);
                        if (found1 != -1)
                        {
                            found2 = envXml.IndexOf("</aas:key>", found1);
                            if (found2 != -1)
                            {
                                found1 += 1;
                                semanticId = envXml.Substring(found1, found2-found1);
                                hs.Add(semanticId);
                                pos = found2;
                                count++;
                            }
                        }
                    }
                }
            }
            while (found1 != -1);
            // Pattern 2
            do
            {
                found1 = envXml.IndexOf("<semanticId>", pos);
                if (found1 != -1)
                {
                    found1 = envXml.IndexOf("<key>", found1);
                    if (found1 != -1)
                    {
                        found1 = envXml.IndexOf("<value>", found1);
                        if (found1 != -1)
                        {
                            found2 = envXml.IndexOf("</value>", found1);
                            if (found2 != -1)
                            {
                                found1 += "<value>".Length;
                                semanticId = envXml.Substring(found1, found2 - found1);
                                hs.Add(semanticId);
                                pos = found2;
                                count++;
                            }
                        }
                    }
                }
            }
            while (found1 != -1);
            */
        }
        public static void saveEnv(int envIndex)
        {
            Console.WriteLine("SAVE: " + envFileName[envIndex]);
            string requestedFileName = envFileName[envIndex];
            string copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
            System.IO.File.Copy(requestedFileName, copyFileName, true);
            AasxServer.Program.env[envIndex].SaveAs(copyFileName);
            System.IO.File.Copy(copyFileName, requestedFileName, true);
            System.IO.File.Delete(copyFileName);
        }

        static int oldest = 0;
        public static bool loadPackageForAas(string aasIdentifier, out IAssetAdministrationShell output, out int packageIndex)
        {
            output = null; packageIndex = -1;
            if (!withDb)
                return false;
            if (Program.isLoading)
                return false;

            int i = envimin;
            while (i < env.Length)
            {
                if (env[i] == null)
                    break;

                var aas = env[i].AasEnv.AssetAdministrationShells.Where(a => a.Id.Equals(aasIdentifier));
                if (aas.Any())
                {
                    output = aas.First();
                    packageIndex = i;
                    return true;
                }
                i++;
            }

            // not found in memory
            if (i == env.Length)
            {
                i = oldest++;
                if (oldest == env.Length)
                    oldest = envimin;
            }

            using (AasContext db = new AasContext())
            {
                var aasDBList = db.AasSets.Where(a => a.AasId == aasIdentifier);
                if (aasDBList.Any())
                {
                    lock (Program.changeAasxFile)
                    {
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

                        var aasDB = aasDBList.First();
                        long aasxNum = aasDB.AASXNum;
                        var aasxDBList = db.AASXSets.Where(a => a.AASXNum == aasxNum);
                        var aasxDB = aasxDBList.First();
                        string fn = aasxDB.AASX;
                        envFileName[i] = fn;

                        if (!withDbFiles)
                        {
                            Console.WriteLine("LOAD: " + fn);
                            env[i] = new AdminShellPackageEnv(fn);

                            DateTime timeStamp = DateTime.Now;
                            var a = env[i].AasEnv.AssetAdministrationShells[0];
                            a.TimeStampCreate = timeStamp;
                            a.SetTimeStamp(timeStamp);
                            foreach (var submodel in env[i].AasEnv.Submodels)
                            {
                                submodel.TimeStampCreate = timeStamp;
                                submodel.SetTimeStamp(timeStamp);
                                submodel.SetAllParents(timeStamp);
                            }
                            output = a;
                        }
                        else
                        {
                            Console.WriteLine("LOAD: " + aasDB.Idshort);
                            env[i] = new AdminShellPackageEnv();
                            env[i].SetFilename(fn);

                            AssetAdministrationShell aas = new AssetAdministrationShell(
                                id: aasDB.AasId,
                                idShort: aasDB.Idshort,
                                assetInformation: new AssetInformation(AssetKind.Type, aasDB.AssetId)
                                /* new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                                    new List<Key>() { new Key(KeyTypes.GlobalReference, aasDB.AssetId) }) */
                                );
                            env[i].AasEnv.AssetAdministrationShells.Add(aas);

                            var submodelDBList = db.SubmodelSets
                                .OrderBy(sm => sm.SubmodelNum)
                                .Where(sm => sm.AasNum == aasDB.AasNum)
                                .ToList();

                            aas.Submodels = new List<AasCore.Aas3_0.IReference>();
                            foreach (var submodelDB in submodelDBList)
                            {
                                var sm = DBRead.getSubmodel(submodelDB.SubmodelId);
                                aas.Submodels.Add(sm.GetReference());
                                env[i].AasEnv.Submodels.Add(sm);
                            }
                            output = aas;
                        }

                        packageIndex = i;
                        AasxServer.Program.signalNewData(2);
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool loadPackageForSubmodel(string submodelIdentifier, out ISubmodel output, out int packageIndex)
        {
            output = null; packageIndex = -1;
            if (!withDb)
                return false;
            if (Program.isLoading)
                return false;

            int i = envimin;
            while (i < env.Length)
            {
                if (env[i] == null)
                    break;

                var submodels = env[i].AasEnv.Submodels.Where(s => s.Id.Equals(submodelIdentifier));
                if (submodels.Any())
                {
                    output = submodels.First();
                    packageIndex = i;
                    return true;
                }
                i++;
            }

            // not found in memory
            if (i == env.Length)
            {
                i = oldest++;
                if (oldest == env.Length)
                    oldest = envimin;
            }

            using (AasContext db = new AasContext())
            {
                var submodelDBList = db.SubmodelSets.Where(s => s.SubmodelId == submodelIdentifier);
                if (submodelDBList.Any())
                {
                    lock (Program.changeAasxFile)
                    {
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

                        var submodelDB = submodelDBList.First();
                        long aasxNum = submodelDB.AASXNum;
                        var aasxDBList = db.AASXSets.Where(a => a.AASXNum == aasxNum);
                        var aasxDB = aasxDBList.First();
                        string fn = aasxDB.AASX;
                        envFileName[i] = fn;

                        if (!withDbFiles)
                        {
                            Console.WriteLine("LOAD: " + fn);
                            env[i] = new AdminShellPackageEnv(fn);

                            DateTime timeStamp = DateTime.Now;
                            var a = env[i].AasEnv.AssetAdministrationShells[0];
                            a.TimeStampCreate = timeStamp;
                            a.SetTimeStamp(timeStamp);
                            foreach (var submodel in env[i].AasEnv.Submodels)
                            {
                                submodel.TimeStampCreate = timeStamp;
                                submodel.SetTimeStamp(timeStamp);
                                submodel.SetAllParents(timeStamp);
                            }

                            var submodels = env[i].AasEnv.Submodels.Where(s => s.Id.Equals(submodelIdentifier));
                            if (submodels.Any())
                            {
                                output = submodels.First();
                            }
                        }
                        else
                        {
                            Console.WriteLine("LOAD Submodel: " + submodelDB.Idshort);
                            env[i] = new AdminShellPackageEnv();
                            env[i].SetFilename(fn);

                            var aasDBList = db.AasSets.Where(a => a.AASXNum == submodelDB.AASXNum);
                            var aasDB = aasDBList.First();

                            AssetAdministrationShell aas = new AssetAdministrationShell(
                                id: aasDB.AasId,
                                idShort: aasDB.Idshort,
                                assetInformation: new AssetInformation(AssetKind.Type, aasDB.AssetId)
                                /* new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                                    new List<IKey>() { new Key(KeyTypes.GlobalReference, aasDB.AssetId) })
                                ) */
                                );
                            env[i].AasEnv.AssetAdministrationShells.Add(aas);

                            var submodelDBList2 = db.SubmodelSets
                                .OrderBy(sm => sm.SubmodelNum)
                                .Where(sm => sm.AasNum == aasDB.AasNum)
                                .ToList();

                            aas.Submodels = new List<AasCore.Aas3_0.IReference>();
                            foreach (var sDB in submodelDBList2)
                            {
                                var sm = DBRead.getSubmodel(sDB.SubmodelId);
                                aas.Submodels.Add(sm.GetReference());
                                env[i].AasEnv.Submodels.Add(sm);
                                if (sDB.SubmodelId == submodelIdentifier)
                                    output = sm;
                            }
                        }

                        packageIndex = i;
                        AasxServer.Program.signalNewData(2);
                        return true;
                    }
                }
            }

            return false;
        }

        public static int envimin = 0;
        public static int envimax = 200;
        public static AdminShellPackageEnv[] env = null;
        public static string[] envFileName = null;
        public static string[] envSymbols = null;
        public static string[] envSubjectIssuer = null;
        /*
        public static AdminShellPackageEnv[] env = new AdminShellPackageEnv[envimax];
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
                null, null, null, null, null, null, null, null, null, null,
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
        public static string[] envFileName = new string[envimax];
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
                null, null, null, null, null, null, null, null, null, null,
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
        public static string[] envSymbols = new string[envimax];
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
                null, null, null, null, null, null, null, null, null, null,
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
        public static string[] envSubjectIssuer = new string[envimax];
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
                null, null, null, null, null, null, null, null, null, null,
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
        */

        public static string hostPort = "";
        public static string blazorPort = "";
        public static string blazorHostPort = "";
        public static ulong dataVersion = 0;

        public static void changeDataVersion() { dataVersion++; }
        public static ulong getDataVersion() { return (dataVersion); }

        static Dictionary<string, SampleClient.UASampleClient> OPCClients = new Dictionary<string, SampleClient.UASampleClient>();
        static readonly object opcclientAddLock = new object(); // object for lock around connecting to an external opc server

        static MqttServer AASMqttServer = new MqttServer();

        static bool runOPC = false;

        public static string connectServer = "";
        public static string connectNodeName = "";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop = false;

        public static WebProxy proxy = null;
        public static HttpClientHandler clientHandler = null;

        public static bool noSecurity = false;
        public static bool edit = false;
        public static string externalRest = "";
        public static string externalBlazor = "";
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
        public static bool isPostgres = false;
        public static bool withDbFiles = false;
        public static int startIndex = 0;

        public static bool withPolicy = false;
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
            public string ExternalBlazor { get; set; }
            public bool ReadTemp { get; set; }
            public int SaveTemp { get; set; }
            public string SecretStringAPI { get; set; }
            public string Tag { get; set; }
            public bool HtmlId { get; set; }
            public int AasxInMemory { get; set; }
            public bool WithDb { get; set; }
            public bool NoDbFiles { get; set; }
            public int StartIndex { get; set; }
#pragma warning restore 8618
            // ReSharper enable UnusedAutoPropertyAccessor.Local
        }

        private static async Task<int> Run(CommandLineArguments a)
        {

            // Wait for Debugger
            if (a.DebugWait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", a.Host);
                while (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            // Read environment variables
            string[] evlist = { "PLCNEXTTARGET", "WITHPOLICY" };
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
                if (w.ToLower() == "true" || w.ToLower() =="on")
                {
                    withPolicy = true;
                }
                if (w.ToLower() == "false" || w.ToLower() == "off")
                {
                    withPolicy = false;
                }
                Console.WriteLine("withPolicy: " + withPolicy);
            }

            if (a.Connect != null)
            {
                if (a.Connect.Length == 0)
                {
                    Program.connectServer = "http://admin-shell-io.com:52000";
                    Byte[] barray = new byte[10];
                    RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                    rngCsp.GetBytes(barray);
                    Program.connectNodeName = "AasxServer_" + Convert.ToBase64String(barray);
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
                                Program.connectServer = c[0];
                                Program.connectNodeName = c[1];
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

            /*
             * Set the global variables at this point inferred from the command-line arguments
             */

            if (a.DataPath != null)
            {
                Console.WriteLine($"Serving the AASXs from: {a.DataPath}");
                AasxHttpContextHelper.DataPath = a.DataPath;
            }
            Program.runOPC = a.Opc;
            Program.noSecurity = a.NoSecurity;
            Program.edit = a.Edit;
            Program.readTemp = a.ReadTemp;
            // if (a.SaveTemp > 0)
            saveTemp = a.SaveTemp;
            Program.htmlId = a.HtmlId;
            Program.withDb = a.WithDb;
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
            env = new AdminShellPackageEnv[envimax];
            envFileName = new string[envimax];
            envSymbols = new string[envimax];
            envSubjectIssuer = new string[envimax];

            // Proxy
            string proxyAddress = "";
            string username = "";
            string password = "";

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
                externalBlazor = blazorHostPort;
            }

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

            if (!Directory.Exists("./temp"))
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

            bool createFilesOnly = false;
            if (System.IO.File.Exists(AasxHttpContextHelper.DataPath + "/FILES.ONLY"))
                createFilesOnly = true;

            int envi = 0;
            int count = 0;

            // Migrate always
            if (withDb)
            {
                if (isPostgres)
                {
                    Console.WriteLine("Use POSTGRES");
                    using (PostgreAasContext db = new PostgreAasContext())
                    {
                        db.Database.Migrate();
                    }
                }
                else
                {
                    Console.WriteLine("Use SQLITE");
                    using (SqliteAasContext db = new SqliteAasContext())
                    {
                        db.Database.Migrate();
                    }
                }
            }

            // Clear DB
            if (withDb && startIndex == 0 && !createFilesOnly)
            {
                /*
                if (isPostgres)
                {
                    Console.WriteLine("Use POSTGRES");
                    using (PostgreAasContext db = new PostgreAasContext())
                    {
                        db.Database.Migrate();
                    }
                }
                else
                {
                    Console.WriteLine("Use SQLITE");
                    using (SqliteAasContext db = new SqliteAasContext())
                    {
                        db.Database.Migrate();
                    }
                }
                */

                using (AasContext db = new AasContext())
                {
                    var task = Task.Run(async () => count = await db.DbConfigSets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.AASXSets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.AasSets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.SubmodelSets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.SMESets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.IValueSets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.SValueSets.ExecuteDeleteAsync());
                    task.Wait();
                    task = Task.Run(async () => count = await db.DValueSets.ExecuteDeleteAsync());
                    task.Wait();

                    DbConfigSet dbConfig = null;
                    dbConfig = new DbConfigSet
                    {
                        Id = 1,
                        AasCount = 0,
                        SubmodelCount = 0,
                        AASXCount = 0,
                        SMECount = 0
                    };
                    db.Add(dbConfig);
                    db.SaveChanges();
                }
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

                List<Task> saveTasks = new List<Task>();
                int maxTasks = 1;
                int taskIndex = 0;

                int fi = 0;
                while (fi < fileNames.Length)
                {
                    // try
                    {
                        fn = fileNames[fi];
                        if (fn.ToLower().Contains("globalsecurity"))
                        {
                            envFileName[envi] = fn;
                            env[envi] = new AdminShellPackageEnv(fn, true, false);
                            //TODO:jtikekar
                            //AasxHttpContextHelper.securityInit(); // read users and access rights from AASX Security
                            //AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                            envi++;
                            envimin = envi;
                            oldest = envi;
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
                            string name = Path.GetFileName(fn);
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
                                envFileName[envi] = null;
                                env[envi] = null;

                                using (var asp = new AdminShellPackageEnv(fn, false, true))
                                {
                                    if (!createFilesOnly)
                                    {
                                        using (AasContext db = new AasContext())
                                        {
                                            var configDBList = db.DbConfigSets.Where(d => true);
                                            var dbConfig = configDBList.FirstOrDefault();

                                            long aasxNum = ++dbConfig.AASXCount;
                                            var aasxDB = new AASXSet
                                            {
                                                AASXNum = aasxNum,
                                                AASX = fn
                                            };
                                            db.Add(aasxDB);

                                            var aas = asp.AasEnv.AssetAdministrationShells[0];
                                            var aasId = aas.Id;
                                            var assetId = aas.AssetInformation.GlobalAssetId;

                                            // Check security
                                            if (aas.IdShort.ToLower().Contains("globalsecurity"))
                                            {
                                                // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                                                // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                                            }
                                            else
                                            {
                                                if (aasId != null && aasId != "" && assetId != null && assetId != "")
                                                {
                                                    VisitorAASX.LoadAASInDB(db, aas, aasxNum, asp, dbConfig);
                                                }
                                            }
                                            db.SaveChanges();
                                            /*
                                            Task t = db.SaveChangesAsync();
                                            if (saveTasks.Count == maxTasks)
                                            {
                                                // search for completed task
                                                int i = 0;
                                                while (!saveTasks[i].IsCompleted && i < maxTasks)
                                                {
                                                    i++;
                                                }
                                                if (i < maxTasks)
                                                {
                                                    taskIndex = i;
                                                }
                                                else
                                                {
                                                    await saveTasks[taskIndex];
                                                }
                                            }
                                            if (taskIndex <= saveTasks.Count)
                                            {
                                                saveTasks.Add(t);
                                            }
                                            else
                                            {
                                                saveTasks[taskIndex] = t;
                                            }
                                            taskIndex++;
                                            if (taskIndex >= maxTasks)
                                                taskIndex = 0;
                                            */
                                        }
                                    }

                                    if (withDbFiles)
                                    {
                                        try
                                        {
                                            string fcopyt = name + "__thumbnail";
                                            fcopyt = fcopyt.Replace("/", "_");
                                            fcopyt = fcopyt.Replace(".", "_");
                                            Uri dummy = null;
                                            using (var st = asp.GetLocalThumbnailStream(ref dummy, init: true))
                                            {
                                                Console.WriteLine("Copy " + AasxHttpContextHelper.DataPath + "/files/" + fcopyt + ".dat");
                                                var fst = System.IO.File.Create(AasxHttpContextHelper.DataPath + "/files/" + fcopyt + ".dat");
                                                st.CopyTo(fst);
                                            }
                                        }
                                        catch { }

                                        using (var fileStream = new FileStream(AasxHttpContextHelper.DataPath + "/files/" + name + ".zip", FileMode.Create))
                                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                                        {
                                            var files = asp.GetListOfSupplementaryFiles();
                                            foreach (var f in files)
                                            {
                                                try
                                                {
                                                    // string fcopy = name + "__" + f.Uri.OriginalString;
                                                    // fcopy = fcopy.Replace("/", "_");
                                                    // fcopy = fcopy.Replace(".", "_");
                                                    using (var s = asp.GetLocalStreamFromPackage(f.Uri.OriginalString, init: true))
                                                    {
                                                        var archiveFile = archive.CreateEntry(f.Uri.OriginalString);
                                                        Console.WriteLine("Copy " + AasxHttpContextHelper.DataPath + "/" + name + "/" + f.Uri.OriginalString);

                                                        using (var archiveStream = archiveFile.Open())
                                                        {
                                                            // Console.WriteLine("Copy " + AasxHttpContextHelper.DataPath + "/files/" + fcopy + ".dat");
                                                            // var fs = System.IO.File.Create(AasxHttpContextHelper.DataPath + "/files/" + fcopy + ".dat");
                                                            s.CopyTo(archiveStream);
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                            }

                            // check if signed
                            string fileCert = "./user/" + name + ".cer";
                            if (System.IO.File.Exists(fileCert))
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
                        envSymbols[envi] = "L"; // Show lock
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

            AasxTask.taskInit();

            RunScript(true);
            //// Initialize            NewDataAvailable?.Invoke(null, EventArgs.Empty);

            // Disable, because of Linux Segementation Fault
            //// ProductChange.pcn.pcnInit();

            isLoading = false;

            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStartAsync().Wait();
                Console.WriteLine("MQTT Publisher started.");
            }

            MySampleServer server = null;
            if (a.Opc)
            {
                server = new MySampleServer(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env);
                Console.WriteLine("OPC UA Server started..");
            }

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
            Program.signalNewData(3);

            if (a.Opc && server != null)
            {
                server.Run(); // wait for CTRL-C
            }
            else
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
                }
            }

            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStopAsync().Wait();
            }

            AasxRestServer.Stop();

            return 0;
        }

        public static void Main(string[] args)
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

            AasContext._con = con;
            if (con != null)
            {
                if (con["DatabaseConnection:ConnectionString"] != null)
                {
                    isPostgres = con["DatabaseConnection:ConnectionString"].ToLower().Contains("host");
                }
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
                    "as a flag (in which case it connects to a default server).")
                {
                    Argument = new Argument<string[]>{ Arity = ArgumentArity.ZeroOrOne }
                },

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
                    "external name of the server"),

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

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(
                async (CommandLineArguments a) =>
                {
                    /*
                    if (!(a.Rest || a.Opc || a.Mqtt))
                    {
                        Console.Error.WriteLine($"Please specify --rest and/or --opc and/or --mqtt{nl}");
                        new HelpBuilder(new SystemConsole()).Write(rootCommand);
                        return 1;
                    }
                    */

                    //return Run(a);
                    var task = Run(a);
                    task.Wait();
                    var op = task.Result;
                    return op;
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
            //public AdminShell.Administration administration = null;
            public AdministrativeInformation administration = null;

            [XmlElement(ElementName = "description")]
            [JsonIgnore]
            //public AdminShell.Description description = null;
            public List<ILangStringTextType> description = null;

            [XmlElement(ElementName = "idShort")]
            [JsonIgnore]
            public string idShort = "";

            [XmlElement(ElementName = "identification")]
            [JsonIgnore]
            public string identification = null;

            [XmlElement(ElementName = "semanticId")]
            public Reference semanticId = null;

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();
        }
        /* AAS Descriptor Definiton */
        public class aasDescriptor
        {

            [XmlElement(ElementName = "administration")]
            [JsonIgnore]
            public AdministrativeInformation administration = null;

            [XmlElement(ElementName = "description")]
            [JsonIgnore]
            public List<ILangStringTextType> description = new(new List<ILangStringTextType>());

            [XmlElement(ElementName = "idShort")]
            public string idShort = "";

            [XmlElement(ElementName = "identification")]
            [JsonIgnore]
            public string identification = null;

            [XmlElement(ElementName = "assets")]
            public List<AssetInformation> assets = new List<AssetInformation>();

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();

            [XmlElement(ElementName = "submodelDescriptors")]
            public List<SubmodelDescriptors> submodelDescriptors = new List<SubmodelDescriptors>();
        }

        /* AAS Detail Part 2 Descriptor Definitions END*/

        /* Creation of AAS Descriptor */
        // TODO (jtikekar, 2023-09-04): Remove for now
        public static aasDescriptor creatAASDescriptor(AdminShellPackageEnv adminShell)
        {
            aasDescriptor aasD = new aasDescriptor();
            string endpointAddress = "http://" + hostPort;

            aasD.idShort = adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
            aasD.identification = adminShell.AasEnv.AssetAdministrationShells[0].Id;
            aasD.description = adminShell.AasEnv.AssetAdministrationShells[0].Description;

            AASxEndpoint endp = new AASxEndpoint();
            endp.address = endpointAddress + "/aas/" + adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
            aasD.endpoints.Add(endp);

            int submodelCount = adminShell.AasEnv.Submodels.Count;
            for (int i = 0; i < submodelCount; i++)
            {
                SubmodelDescriptors sdc = new SubmodelDescriptors();

                sdc.administration = adminShell.AasEnv.Submodels[i].Administration as AdministrativeInformation;
                sdc.description = adminShell.AasEnv.Submodels[i].Description;
                sdc.identification = adminShell.AasEnv.Submodels[i].Id;
                sdc.idShort = adminShell.AasEnv.Submodels[i].IdShort;
                sdc.semanticId = adminShell.AasEnv.Submodels[i].SemanticId as Reference;

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

                    int aascount = Program.env.Length;

                    for (int j = 0; j < aascount; j++)
                    {
                        aasListParameters alp = new aasListParameters();

                        if (Program.env[j] != null)
                        {
                            alp.index = j;

                            /* Create Detail part 2 Descriptor Start */
                            aasDescriptor aasDsecritpor = Program.creatAASDescriptor(Program.env[j]);
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


                            alp.idShort = Program.env[j].AasEnv.AssetAdministrationShells[0].IdShort;
                            alp.identification = Program.env[j].AasEnv.AssetAdministrationShells[0].Id;
                            alp.fileName = Program.envFileName[j];
                            alp.assetId = "";
                            //var asset = Program.env[j].AasEnv.FindAsset(Program.env[j].AasEnv.AssetAdministrationShells[0].assetRef);
                            var asset = Program.env[j].AasEnv.AssetAdministrationShells[0].AssetInformation;
                            if (asset != null)
                                alp.humanEndPoint = blazorHostPort;
                            alp.restEndPoint = hostPort;

                            adp.aasList.Add(alp);
                        }
                    }

                    string decriptorData = JsonConvert.SerializeObject(descriptortf, Formatting.Indented);
                    Program.publishDescriptorData(decriptorData);

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
                        if (sm != null && sm.IdShort != null)
                        {
                            bool toPublish = Program.submodelsToPublish.Contains(sm);
                            if (!toPublish)
                            {
                                int count = sm.Qualifiers.Count;
                                if (count != 0)
                                {
                                    int j = 0;

                                    while (j < count) // Scan qualifiers
                                    {
                                        var p = sm.Qualifiers[j] as Qualifier;

                                        if (p.Type == "PUBLISH")
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
                                Console.WriteLine("Publish Submodel " + sm.IdShort);
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

                                Byte[] binaryFile = System.IO.File.ReadAllBytes(Program.envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                                string fileToken = Jose.JWT.Encode(payload, enc.GetBytes(AasxRestServerLibrary.AasxHttpContextHelper.secretString), JwsAlgorithm.HS256);

                                if (fileToken.Length <= blockSize)
                                {
                                    res.fileName = Path.GetFileName(Program.envFileName[aasIndex]);
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
                                    getaasxFile_fileName = Path.GetFileName(Program.envFileName[aasIndex]);
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

                                Byte[] binaryFile = System.IO.File.ReadAllBytes(Program.envFileName[aasIndex]);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                if (binaryBase64.Length <= blockSize)
                                {
                                    res.fileName = Path.GetFileName(Program.envFileName[aasIndex]);
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
                                    getaasxFile_fileName = Path.GetFileName(Program.envFileName[aasIndex]);
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
                                    if (smc.IdShort == split[0])
                                    {
                                        foreach (var tsb in AasxTimeSeries.TimeSeries.timeSeriesBlockList)
                                        {
                                            if (tsb.sampleStatus.Value == "stop")
                                            {
                                                tsb.sampleStatus.Value = "stopped";
                                            }
                                            if (tsb.sampleStatus.Value != "start")
                                                continue;

                                            if (tsb.block == smc)
                                            {
                                                foreach (string data in td2.publish)
                                                {
                                                    using (TextReader reader = new StringReader(data))
                                                    {
                                                        JsonSerializer serializer = new JsonSerializer();
                                                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                                        var smcData = (SubmodelElementCollection)serializer.Deserialize(reader,
                                                            typeof(SubmodelElementCollection));
                                                        if (smcData != null && smc.Value.Count < 100)
                                                        {
                                                            if (tsb.data != null)
                                                            {
                                                                int maxCollections = Convert.ToInt32(tsb.maxCollections.Value);
                                                                int actualCollections = tsb.data.Value.Count;
                                                                if (actualCollections < maxCollections ||
                                                                    (tsb.sampleMode.Value == "continuous" && actualCollections == maxCollections))
                                                                {
                                                                    tsb.data.Value.Add(smcData);
                                                                    actualCollections++;
                                                                }
                                                                if (actualCollections > maxCollections)
                                                                {
                                                                    tsb.data.Value.RemoveAt(0);
                                                                    actualCollections--;
                                                                }
                                                                tsb.actualCollections.Value = actualCollections.ToString();
                                                                /*
                                                                tsb.lowDataIndex =
                                                                    Convert.ToInt32(tsb.data.Value[0].submodelElement.IdShort.Substring("data".Length));
                                                                tsb.highDataIndex =
                                                                    Convert.ToInt32(tsb.data.Value[tsb.data.Value.Count - 1].submodelElement.IdShort.Substring("data".Length));
                                                                */
                                                                signalNewData(1);
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
                                    Submodel submodel = null;
                                    try
                                    {
                                        using (TextReader reader = new StringReader(sm))
                                        {
                                            JsonSerializer serializer = new JsonSerializer();
                                            serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                            submodel = (Submodel)serializer.Deserialize(reader, typeof(Submodel));
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Can not read SubModel!");
                                        return;
                                    }

                                    // need id for idempotent behaviour
                                    if (submodel.Id == null)
                                    {
                                        Console.WriteLine("Identification of SubModel is (null)!");
                                        return;
                                    }

                                    IAssetAdministrationShell aas = null;
                                    envi = 0;
                                    while (env[envi] != null)
                                    {
                                        aas = env[envi].AasEnv.FindAasWithSubmodelId(submodel.Id);
                                        if (aas != null)
                                            break;
                                        envi++;
                                    }


                                    if (aas != null)
                                    {
                                        // datastructure update
                                        if (env == null || env[envi].AasEnv == null /*|| env[envi].AasEnv.Assets == null*/)
                                        {
                                            Console.WriteLine("Error accessing internal data structures.");
                                            return;
                                        }

                                        var existingSm = env[envi].AasEnv.FindSubmodelById(submodel.Id);
                                        if (existingSm != null)
                                        {
                                            bool toSubscribe = Program.submodelsToSubscribe.Contains(existingSm);
                                            if (!toSubscribe)
                                            {
                                                int eqcount = existingSm.Qualifiers.Count;
                                                if (eqcount != 0)
                                                {
                                                    int j = 0;

                                                    while (j < eqcount) // Scan qualifiers
                                                    {
                                                        var p = existingSm.Qualifiers[j] as Qualifier;

                                                        if (p.Type == "SUBSCRIBE")
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
                                                Console.WriteLine("Subscribe Submodel " + submodel.IdShort);

                                                int c2 = submodel.Qualifiers.Count;
                                                if (c2 != 0)
                                                {
                                                    int k = 0;

                                                    while (k < c2) // Scan qualifiers
                                                    {
                                                        var q = submodel.Qualifiers[k] as Qualifier;

                                                        if (q.Type == "PUBLISH")
                                                        {
                                                            q.Type = "SUBSCRIBE";
                                                        }

                                                        k++;
                                                    }
                                                }

                                                bool overwrite = true;
                                                int escount = existingSm.SubmodelElements.Count;
                                                int count2 = submodel.SubmodelElements.Count;
                                                if (escount == count2)
                                                {
                                                    int smi = 0;
                                                    while (smi < escount)
                                                    {
                                                        var sme1 = submodel.SubmodelElements[smi];
                                                        var sme2 = existingSm.SubmodelElements[smi];

                                                        if (sme1 is Property)
                                                        {
                                                            if (sme2 is Property)
                                                            {
                                                                (sme2 as Property).Value = (sme1 as Property).Value;
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
                                                    var key = new Key(KeyTypes.Submodel, submodel.Id);
                                                    var keyList = new List<IKey>() { key };
                                                    var newsmr = new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, keyList);
                                                    //var newsmr = SubmodelRef.CreateNew("Submodel", submodel.id);
                                                    var existsmr = aas.HasSubmodelReference(newsmr);
                                                    if (!existsmr)
                                                    {
                                                        aas.Submodels.Add(newsmr);
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
                    catch
                    {
                    }
                    if (newConnectData)
                    {
                        NewDataAvailable?.Invoke(null, EventArgs.Empty);
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

        private static System.Timers.Timer OPCClientTimer;
        static bool timerSet = false;
        private static void SetOPCClientTimer(double value)
        {
            if (!timerSet)
            {
                /*
                // Create a timer with an specified interval.
                OPCClientTimer = new System.Timers.Timer(value);
                // Hook up the Elapsed event for the timer. 
                OPCClientTimer.Elapsed += OnOPCClientNextTimedEvent;
                OPCClientTimer.AutoReset = true;
                OPCClientTimer.Enabled = true;
                */

                timerSet = true;

                AasxTimeSeries.TimeSeries.SetOPCClientThread(value);
            }
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
        private static void OnOPCClientNextTimedEvent(Object source, ElapsedEventArgs e)
        {
            ReadOPCClient(false);
            // RunScript(false);
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
            RunScript(false);
            // NewDataAvailable?.Invoke(null, EventArgs.Empty);
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

            RESTalreadyRunning = true;

            string GETSUBMODEL = "";
            string GETURL = "";
            string PUTSUBMODEL = "";
            string PUTURL = "";

            // Search for submodel REST and scan qualifiers for GET and PUT commands
            foreach (var sm in env[0].AasEnv.Submodels)
            {
                if (sm != null && sm.IdShort != null && sm.IdShort == "REST")
                {
                    int count = sm.Qualifiers.Count;
                    if (count != 0)
                    {
                        int j = 0;

                        while (j < count) // Scan qualifiers
                        {
                            var p = sm.Qualifiers[j] as Qualifier;

                            if (p.Type == "GETSUBMODEL")
                            {
                                GETSUBMODEL = p.Value;
                            }
                            if (p.Type == "GETURL")
                            {
                                GETURL = p.Value;
                            }
                            if (p.Type == "PUTSUBMODEL")
                            {
                                PUTSUBMODEL = p.Value;
                            }
                            if (p.Type == "PUTURL")
                            {
                                PUTURL = p.Value;
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

                Submodel submodel = null;
                try
                {
                    using (TextReader reader = new StringReader(sm))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        submodel = (Submodel)serializer.Deserialize(reader, typeof(Submodel));
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Can not read SubModel {0}.", GETSUBMODEL);
                    return;
                }

                // need id for idempotent behaviour
                if (submodel.Id == null)
                {
                    Console.WriteLine("Identification of SubModel {0} is (null).", GETSUBMODEL);
                    return;
                }

                var aas = env[0].AasEnv.FindAasWithSubmodelId(submodel.Id);

                // datastructure update
                if (env == null || env[0].AasEnv == null /*|| env[0].AasEnv.Assets == null*/)
                {
                    Console.WriteLine("Error accessing internal data structures.");
                    return;
                }

                // add Submodel
                var existingSm = env[0].AasEnv.FindSubmodelById(submodel.Id);
                if (existingSm != null)
                    env[0].AasEnv.Submodels.Remove(existingSm);
                env[0].AasEnv.Submodels.Add(submodel);

                // add SubmodelRef to AAS            
                // access the AAS
                var keyList = new List<IKey>() { new Key(KeyTypes.Submodel, submodel.Id) };
                var newsmr = new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, keyList);
                var existsmr = aas.HasSubmodelReference(newsmr);
                if (!existsmr)
                {
                    aas.AddSubmodelReference(newsmr);
                }
            }

            if (PUTSUBMODEL != "" && PUTURL != "") // PUT
            {
                Console.WriteLine("{0} PUT Submodel {1} from URL {2}.", countGetPut++, PUTSUBMODEL, PUTURL);

                {
                    foreach (var sm in env[0].AasEnv.Submodels)
                    {
                        if (sm != null && sm.IdShort != null && sm.IdShort == PUTSUBMODEL)
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

            RESTalreadyRunning = false;

            // start MQTT Client as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.DoWork += async (s1, e1) =>
            {

                try
                {
                    await AasxMqttClient.MqttClient.StartAsync(env);
                }
                catch (Exception)
                {
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {

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

                                    string AASSubmodel = env[i].AasEnv.AssetAdministrationShells[0].IdShort + "." + sm.IdShort; // for multiple AAS, use something like env.AasEnv.AssetAdministrationShells[i].IdShort;
                                    string serverNodePrefix = string.Format("ns={0};s=AASROOT.{1}", serverNamespaceIdx, AASSubmodel);
                                    string nodePath = Path; // generally starts with Submodel idShort
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

        static int countRunScript = 0;

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
                                                v += Convert.ToInt32(qq.Value);
                                                (sme1 as Property).Value = v.ToString();
                                                continue;
                                            }

                                            if (qq.Type == "GetValue")
                                            {
                                                /*
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
                                                */
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

                                                string url = qq.Value;
                                                string username = "";
                                                string password = "";

                                                if (sme1.Qualifiers.Count == 3)
                                                {
                                                    qq = sme1.Qualifiers[1] as Qualifier;
                                                    if (qq.Type != "Username")
                                                        continue;
                                                    username = qq.Value;
                                                    qq = sme1.Qualifiers[2] as Qualifier;
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
                                                    var r12 = sme1 as ReferenceElement;
                                                    var ref12 = env[i].AasEnv.FindReferableByReference(r12.GetModelReference());
                                                    if (ref12 is SubmodelElementCollection)
                                                    {
                                                        var c1 = ref12 as SubmodelElementCollection;
                                                        // if (c1.Value.Count == 0)
                                                        {
                                                            // dynamic model = JObject.Parse(response);
                                                            JObject parsed = JObject.Parse(response);
                                                            parseJson(c1, parsed, null);
                                                        }
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
                                                var r1 = sme1 as ReferenceElement;
                                                var r2 = sme2 as ReferenceElement;
                                                var r3 = sme3 as ReferenceElement;
                                                var ref1 = env[i].AasEnv.FindReferableByReference(r1.GetModelReference());
                                                var ref2 = env[i].AasEnv.FindReferableByReference(r2.GetModelReference());
                                                var ref3 = env[i].AasEnv.FindReferableByReference(r3.GetModelReference());
                                                if (ref1 is Property && ref2 is Submodel && ref3 is Property)
                                                {
                                                    var p1 = ref1 as Property;
                                                    // Simulate changes
                                                    var sm2 = ref2 as Submodel;
                                                    var p3 = ref3 as Property;
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

        public static bool parseJson(SubmodelElementCollection c, JObject o, List<string> filter,
            Property minDiffAbsolute = null, Property minDiffPercent = null)
        {
            int newMode = 0;
            DateTime timeStamp = DateTime.UtcNow;
            bool ok = false;

            int iMinDiffAbsolute = 1;
            int iMinDiffPercent = 0;
            if (minDiffAbsolute != null)
                iMinDiffAbsolute = Convert.ToInt32(minDiffAbsolute.Value);
            if (minDiffPercent != null)
                iMinDiffPercent = Convert.ToInt32(minDiffPercent.Value);

            foreach (JProperty jp1 in (JToken)o)
            {
                if (filter != null && filter.Count != 0)
                {
                    if (!filter.Contains(jp1.Name))
                        continue;
                }

                SubmodelElementCollection c2;
                switch (jp1.Value.Type)
                {
                    case JTokenType.Array:
                        c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                        if (c2 == null)
                        {
                            //c2 = SubmodelElementCollection.CreateNew(jp1.Name);
                            c2 = new SubmodelElementCollection(idShort: jp1.Name);
                            c.Value.Add(c2);
                            c2.TimeStampCreate = timeStamp;
                            c2.SetTimeStamp(timeStamp);
                            newMode = 1;
                        }
                        int count = 1;
                        foreach (JObject el in jp1.Value)
                        {
                            string n = jp1.Name + "_array_" + count++;
                            SubmodelElementCollection c3 =
                                c2.FindFirstIdShortAs<SubmodelElementCollection>(n);
                            if (c3 == null)
                            {
                                c3 = new SubmodelElementCollection(idShort: n);
                                c2.Value.Add(c3);
                                c3.TimeStampCreate = timeStamp;
                                c3.SetTimeStamp(timeStamp);
                                newMode = 1;
                            }
                            ok |= parseJson(c3, el, filter);
                        }
                        break;
                    case JTokenType.Object:
                        c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                        if (c2 == null)
                        {
                            c2 = new SubmodelElementCollection(idShort: jp1.Name);
                            c.Value.Add(c2);
                            c2.TimeStampCreate = timeStamp;
                            c2.SetTimeStamp(timeStamp);
                            newMode = 1;
                        }
                        foreach (JObject el in jp1.Value)
                        {
                            ok |= parseJson(c2, el, filter);
                        }
                        break;
                    default:
                        Property p = c.FindFirstIdShortAs<Property>(jp1.Name);
                        if (p == null)
                        {
                            p = new Property(DataTypeDefXsd.String, idShort: jp1.Name);
                            c.Value.Add(p);
                            p.TimeStampCreate = timeStamp;
                            p.SetTimeStamp(timeStamp);
                            newMode = 1;
                        }
                        // see https://github.com/JamesNK/Newtonsoft.Json/issues/874    
                        try
                        {
                            if (p.Value == "")
                                p.Value = "0";
                            string value = (jp1.Value as JValue).ToString(CultureInfo.InvariantCulture);
                            if (!value.Contains("."))
                            {
                                int v = Convert.ToInt32(value);
                                int lastv = Convert.ToInt32(p.Value);
                                int delta = Math.Abs(v - lastv);
                                if (delta >= iMinDiffAbsolute && delta >= lastv * iMinDiffPercent / 100)
                                {
                                    p.Value = value;
                                    p.SetTimeStamp(timeStamp);
                                    ok = true;
                                }
                            }
                            else
                            {
                                /*
                                double v = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                                double lastv = Convert.ToDouble(p.Value, CultureInfo.InvariantCulture);
                                double delta = Math.Abs(v - lastv);
                                if (delta >= iMinDiffAbsolute && delta >= lastv * iMinDiffPercent / 100)
                                {
                                    p.Value = value;
                                    p.setTimeStamp(timeStamp);
                                    ok = true;
                                }
                                */
                                p.Value = value;
                                p.SetTimeStamp(timeStamp);
                                ok = true;
                            }
                        }
                        catch
                        {
                        }
                        break;
                }
            }

            Program.signalNewData(newMode);
            return ok;
        }

        private static void WalkSubmodelElement(ISubmodelElement sme, string nodePath, string serverNodePrefix, SampleClient.UASampleClient client, int clientNamespace)
        {
            if (sme is Property)
            {
                var p = sme as Property;
                string clientNodeName = nodePath + p.IdShort;
                string serverNodeId = string.Format("{0}.{1}.Value", serverNodePrefix, p.IdShort);
                NodeId clientNode = new NodeId(clientNodeName, (ushort)clientNamespace);
                UpdatePropertyFromOPCClient(p, serverNodeId, client, clientNode);
            }
            else if (sme is SubmodelElementCollection)
            {
                var collection = sme as SubmodelElementCollection;
                for (int i = 0; i < collection.Value.Count; i++)
                {
                    string newNodeIdBase = nodePath + "." + collection.IdShort;
                    WalkSubmodelElement(collection.Value[i], newNodeIdBase, serverNodePrefix, client, clientNamespace);
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
        static AdminShellPackageEnv[] aasxEnv = null;
        // OZ
        public static ManualResetEvent quitEvent;

        public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShellPackageEnv[] _aasxEnv)
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

            application.ApplicationName = "UA Core Sample Server";
            application.ApplicationType = ApplicationType.Server;
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
