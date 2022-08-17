using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using AasxServer;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Org.Webpki.JsonCanonicalizer;
using SampleClient;

namespace AasxTimeSeries
{
    public enum TimeSeriesDestFormat { Plain, TimeSeries10 }

    public static class TimeSeries
    {
        public class TimeSeriesBlock
        {
            public AdminShell.Submodel submodel = null;
            public AdminShell.SubmodelElementCollection block = null;
            public AdminShell.SubmodelElementCollection data = null;
            public AdminShell.SubmodelElementCollection latestData = null;
            public AdminShell.Property sampleStatus = null;
            public AdminShell.Property sampleMode = null;
            public AdminShell.Property sampleRate = null;
            public AdminShell.Property maxSamples = null;
            public AdminShell.Property actualSamples = null;
            public AdminShell.Property maxSamplesInCollection = null;
            public AdminShell.Property actualSamplesInCollection = null;
            public AdminShell.Property maxCollections = null;
            public AdminShell.Property actualCollections = null;
            public AdminShell.Property minDiffAbsolute = null;
            public AdminShell.Property minDiffPercent = null;

            public TimeSeriesDestFormat destFormat;

            public int threadCounter = 0;
            public string sourceType = "";
            public string sourceAddress = "";
            public List<string> sourceNames = new List<string>();
            public string username = "";
            public string password = "";
            public int plotRowOffset = 0;
            public int samplesCollectionsCount = 0;
            public List<AdminShell.Property> samplesProperties = null;
            public List<string> samplesValues = null;
            public string samplesTimeStamp = "";
            public int samplesValuesCount = 0;
            // public int totalSamples = 0;
            public AdminShell.Property totalSamples = null;
            // public int lowDataIndex = 0;
            public AdminShell.Property lowDataIndex = null;
            // public int highDataIndex = -1;
            public AdminShell.Property highDataIndex = null;

            public List<string> opcNodes = null;
            public List<string> modbusNodes = null;
            public DateTime opcLastTimeStamp;
            public int correctionMinutes = 0;
        }

        static public List<TimeSeriesBlock> timeSeriesBlockList = null;
        static public List<AdminShell.SubmodelElementCollection> timeSeriesSubscribe = null;
        public static void timeSeriesInit()
        {
            DateTime timeStamp = DateTime.UtcNow;

            timeSeriesBlockList = new List<TimeSeriesBlock>();
            timeSeriesSubscribe = new List<AdminShellV20.SubmodelElementCollection>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AdministrationShells[0];
                    aas.TimeStampCreate = timeStamp;
                    aas.setTimeStamp(timeStamp);
                    if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                    {
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null)
                            {
                                sm.TimeStampCreate = timeStamp;
                                sm.setTimeStamp(timeStamp);
                                sm.SetAllParents(timeStamp);
                                int countSme = sm.submodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.submodelElements[iSme].submodelElement;
                                    if (sme is AdminShell.SubmodelElementCollection && sme.idShort.Contains("TimeSeries"))
                                    {
                                        bool nextSme = false;
                                        if (sme.qualifiers.Count > 0)
                                        {
                                            int j = 0;
                                            while (j < sme.qualifiers.Count)
                                            {
                                                var q = sme.qualifiers[j] as AdminShell.Qualifier;
                                                if (q.type == "SUBSCRIBE")
                                                {
                                                    timeSeriesSubscribe.Add(sme as AdminShell.SubmodelElementCollection);
                                                    // nextSme = true;
                                                    break;
                                                }
                                                j++;
                                            }
                                        }
                                        if (nextSme)
                                            continue;

                                        var smec = sme as AdminShell.SubmodelElementCollection;
                                        int countSmec = smec.value.Count;

                                        var tsb = new TimeSeriesBlock();
                                        tsb.submodel = sm;
                                        tsb.block = smec;
                                        tsb.data = tsb.block;
                                        tsb.samplesProperties = new List<AdminShell.Property>();
                                        tsb.samplesValues = new List<string>();

                                        for (int dataSections = 0; dataSections < 2; dataSections++)
                                        {
                                            for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                            {
                                                var sme2 = smec.value[iSmec].submodelElement;
                                                var idShort = sme2.idShort;
                                                if (idShort.Contains("opcNode"))
                                                    idShort = "opcNode";
                                                if (idShort.Contains("modbusNode"))
                                                    idShort = "modbusNode";
                                                switch (idShort)
                                                {
                                                    case "sourceType":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.sourceType = (sme2 as AdminShell.Property).value;
                                                        }
                                                        break;
                                                    case "sourceAddress":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.sourceAddress = (sme2 as AdminShell.Property).value;
                                                        }
                                                        break;
                                                    case "sourceNames":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            string[] split = (sme2 as AdminShell.Property).value.Split(',');
                                                            if (split.Length != 0)
                                                            {
                                                                foreach (string s in split)
                                                                    tsb.sourceNames.Add(s);
                                                            }
                                                        }
                                                        break;
                                                    case "destFormat":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            var xx = (sme2 as AdminShell.Property).value.Trim().ToLower();
                                                            switch (xx)
                                                            {
                                                                case "plain":
                                                                    tsb.destFormat = TimeSeriesDestFormat.Plain;
                                                                    break;
                                                                case "timeseries/1/0":
                                                                    tsb.destFormat = TimeSeriesDestFormat.TimeSeries10;
                                                                    break;
                                                            }
                                                        }
                                                        break;
                                                    case "username":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.username = (sme2 as AdminShell.Property).value;
                                                        }
                                                        break;
                                                    case "password":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.password = (sme2 as AdminShell.Property).value;
                                                        }
                                                        break;
                                                    case "plotRowOffset":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.plotRowOffset = Convert.ToInt32((sme2 as AdminShell.Property).value);
                                                        }
                                                        break;
                                                    case "correctionMinutes":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.correctionMinutes = Convert.ToInt32((sme2 as AdminShell.Property).value);
                                                        }
                                                        break;
                                                    case "data":
                                                        if (sme2 is AdminShell.SubmodelElementCollection)
                                                        {
                                                            tsb.data = sme2 as AdminShell.SubmodelElementCollection;
                                                        }
                                                        if (sme2 is AdminShell.ReferenceElement)
                                                        {
                                                            var refElement = Program.env[0].AasEnv.FindReferableByReference((sme2 as AdminShell.ReferenceElement).value);
                                                            if (refElement is AdminShell.SubmodelElementCollection)
                                                                tsb.data = refElement as AdminShell.SubmodelElementCollection;
                                                        }
                                                        break;
                                                    case "minDiffPercent":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.minDiffPercent = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "minDiffAbsolute":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.minDiffAbsolute = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "sampleStatus":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.sampleStatus = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "sampleMode":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.sampleMode = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "sampleRate":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.sampleRate = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "maxSamples":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.maxSamples = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "actualSamples":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.actualSamples = sme2 as AdminShell.Property;
                                                            tsb.actualSamples.value = "0";
                                                        }
                                                        break;
                                                    case "maxSamplesInCollection":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.maxSamplesInCollection = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "actualSamplesInCollection":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.actualSamplesInCollection = sme2 as AdminShell.Property;
                                                            tsb.actualSamplesInCollection.value = "0";
                                                        }
                                                        break;
                                                    case "maxCollections":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.maxCollections = sme2 as AdminShell.Property;
                                                        }
                                                        break;
                                                    case "actualCollections":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            tsb.actualCollections = sme2 as AdminShell.Property;
                                                            tsb.actualCollections.value = "0";
                                                        }
                                                        break;
                                                    case "opcNode":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            string node = (sme2 as AdminShell.Property).value;
                                                            string[] split = node.Split(',');
                                                            if (tsb.opcNodes == null)
                                                                tsb.opcNodes = new List<string>();
                                                            tsb.opcNodes.Add(split[1] + "," + split[2]);
                                                            var p = AdminShell.Property.CreateNew(split[0]);
                                                            tsb.samplesProperties.Add(p);
                                                            p.TimeStampCreate = timeStamp;
                                                            p.setTimeStamp(timeStamp);
                                                            tsb.samplesValues.Add("");
                                                        }
                                                        break;
                                                    case "modbusNode":
                                                        if (sme2 is AdminShell.Property)
                                                        {
                                                            string node = (sme2 as AdminShell.Property).value;
                                                            string[] split = node.Split(',');
                                                            if (tsb.modbusNodes == null)
                                                                tsb.modbusNodes = new List<string>();
                                                            tsb.modbusNodes.Add(split[1] + "," + split[2] + "," + split[3] + "," + split[4]);
                                                            var p = AdminShell.Property.CreateNew(split[0]);
                                                            tsb.samplesProperties.Add(p);
                                                            p.TimeStampCreate = timeStamp;
                                                            p.setTimeStamp(timeStamp);
                                                            tsb.samplesValues.Add("");
                                                        }
                                                        break;
                                                }
                                                if (tsb.sourceType == "aas" && sme2 is AdminShell.ReferenceElement r)
                                                {
                                                    var el = env.AasEnv.FindReferableByReference(r.value);
                                                    if (el is AdminShell.Property p)
                                                    {
                                                        tsb.samplesProperties.Add(p);
                                                        tsb.samplesValues.Add("");
                                                    }
                                                }
                                            }
                                            if (dataSections == 0)
                                            {
                                                if (tsb.data != null)
                                                    smec = tsb.data;
                                                countSmec = smec.value.Count;
                                            }
                                        }
                                        tsb.opcLastTimeStamp = DateTime.UtcNow + TimeSpan.FromMinutes(tsb.correctionMinutes) - TimeSpan.FromMinutes(2);

                                        if (tsb.data != null)
                                        {
                                            tsb.latestData = AdminShell.SubmodelElementCollection.CreateNew("latestData");
                                            tsb.latestData.setTimeStamp(timeStamp);
                                            tsb.latestData.TimeStampCreate = timeStamp;
                                            tsb.data.Add(tsb.latestData);

                                            AdminShell.SubmodelElement latestDataProperty = null;
                                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("lowDataIndex");
                                            if (latestDataProperty == null)
                                            {
                                                latestDataProperty = AdminShell.Property.CreateNew("lowDataIndex");
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Add(latestDataProperty);
                                                tsb.lowDataIndex = latestDataProperty as AdminShell.Property;
                                                tsb.lowDataIndex.value = "0";
                                            }
                                            latestDataProperty.setTimeStamp(timeStamp);

                                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("highDataIndex");
                                            if (latestDataProperty == null)
                                            {
                                                latestDataProperty = AdminShell.Property.CreateNew("highDataIndex");
                                                (latestDataProperty as AdminShell.Property).value = "-1";
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Add(latestDataProperty);
                                                tsb.highDataIndex = latestDataProperty as AdminShell.Property;
                                            }
                                            latestDataProperty.setTimeStamp(timeStamp);

                                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("totalSamples");
                                            if (latestDataProperty == null)
                                            {
                                                latestDataProperty = AdminShell.Property.CreateNew("totalSamples");
                                                (latestDataProperty as AdminShell.Property).value = "0";
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Add(latestDataProperty);
                                                tsb.totalSamples = latestDataProperty as AdminShell.Property;
                                            }
                                            latestDataProperty.setTimeStamp(timeStamp);

                                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("timeStamp");
                                            if (latestDataProperty == null)
                                            {
                                                latestDataProperty = AdminShell.Property.CreateNew("timeStamp");
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Add(latestDataProperty);
                                            }
                                            latestDataProperty.setTimeStamp(timeStamp);
                                        }
                                        if (tsb.sampleRate != null)
                                            tsb.threadCounter = Convert.ToInt32(tsb.sampleRate.value);
                                        timeSeriesBlockList.Add(tsb);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // test
            if (test)
            {
                dummy = 0;
                for (int i = 0; i < 500; i++)
                {
                    timeSeriesSampling(false);
                }
                timeSeriesSampling(true);
            }
            else
            {
                timeSeriesThread = new Thread(new ThreadStart(timeSeriesSamplingLoop));
                timeSeriesThread.Start();
            }
        }

        static bool test = false;

        static Thread timeSeriesThread;

        static int dummy = 0;

        public static void timeSeriesSamplingLoop()
        {
            /*
            while(timeSeriesSampling(false));
            timeSeriesSampling(true);
            */
            while (true)
            {
                timeSeriesSampling(false);
            }
        }

        private static long opcClientRate = 0;
        private static long opcClientCount = 0;
        public static void SetOPCClientThread(double value)
        {
            opcClientRate = (long)value;
        }

        private static void OnOPCClientNextTimedEvent(long ms)
        {
            if (opcClientRate != 0)
            {
                opcClientCount += ms;
                if (opcClientCount >= opcClientRate)
                {
                    AasxServer.Program.OnOPCClientNextTimedEvent();
                    opcClientCount = 0;
                }
            }
        }

        /*
        static ulong ChangeNumber = 0;

        static bool setChangeNumber(AdminShell.Referable r, ulong changeNumber)
        {
            do
            {
                r.ChangeNumber = changeNumber;
                if (r != r.parent)
                {
                    r = r.parent;
                }
                else
                    r = null;
            }
            while (r != null);

            return true;
        }
        */

        private static T AddToSMC<T>(
            DateTime timestamp,
            AdminShell.SubmodelElementCollection smc,
            string idShort,
            AdminShell.Key semanticIdKey,
            string smeValue = null) where T : AdminShell.SubmodelElement
        {
            var newElem = AdminShell.SubmodelElementWrapper.CreateAdequateType(typeof(T));
            newElem.idShort = idShort;
            newElem.semanticId = new AdminShell.SemanticId(semanticIdKey);
            newElem.setTimeStamp(timestamp);
            newElem.TimeStampCreate = timestamp;
            if (smc?.value != null)
                smc.value.Add(newElem);
            if (smeValue != null && newElem is AdminShell.Property newP)
                newP.value = smeValue;
            if (smeValue != null && newElem is AdminShell.Blob newB)
                newB.value = smeValue;
            newElem.qualifiers = new AdminShellV20.QualifierCollection();
            newElem.hasDataSpecification = new AdminShellV20.HasDataSpecification();
            return newElem as T;
        }

        /*
        static public bool AcceptAllCertifications(
            object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        */

        private static void Sign(AdminShell.SubmodelElementCollection smc, DateTime timestamp)
        {
            Console.WriteLine("Sign");
            //
            string certFile = "Andreas_Orzelski_Chain.pfx";
            string certPW = "i40";
            if (System.IO.File.Exists(certFile))
            {
                // ServicePointManager.ServerCertificateValidationCallback =
                //    new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

                Console.WriteLine("X509");
                using (var certificate = new X509Certificate2(certFile, certPW))
                {
                    if (certificate == null)
                        return;

                    AdminShell.SubmodelElementCollection smec = AdminShell.SubmodelElementCollection.CreateNew("signature");
                    smec.setTimeStamp(timestamp);
                    smec.TimeStampCreate = timestamp;
                    AdminShell.Property json = AdminShellV20.Property.CreateNew("submodelJson");
                    json.setTimeStamp(timestamp);
                    json.TimeStampCreate = timestamp;
                    AdminShell.Property canonical = AdminShellV20.Property.CreateNew("submodelJsonCanonical");
                    canonical.setTimeStamp(timestamp);
                    canonical.TimeStampCreate = timestamp;
                    AdminShell.Property subject = AdminShellV20.Property.CreateNew("subject");
                    subject.setTimeStamp(timestamp);
                    subject.TimeStampCreate = timestamp;
                    AdminShell.SubmodelElementCollection x5c = AdminShell.SubmodelElementCollection.CreateNew("x5c");
                    x5c.setTimeStamp(timestamp);
                    x5c.TimeStampCreate = timestamp;
                    AdminShell.Property algorithm = AdminShellV20.Property.CreateNew("algorithm");
                    algorithm.setTimeStamp(timestamp);
                    algorithm.TimeStampCreate = timestamp;
                    AdminShell.Property sigT = AdminShellV20.Property.CreateNew("sigT");
                    sigT.setTimeStamp(timestamp);
                    sigT.TimeStampCreate = timestamp;
                    AdminShell.Property signature = AdminShellV20.Property.CreateNew("signature");
                    signature.setTimeStamp(timestamp);
                    signature.TimeStampCreate = timestamp;
                    smec.Add(json);
                    smec.Add(canonical);
                    smec.Add(subject);
                    smec.Add(x5c);
                    smec.Add(algorithm);
                    smec.Add(sigT);
                    smec.Add(signature);
                    string s = null;
                    s = JsonConvert.SerializeObject(smc, Formatting.Indented);
                    json.value = s;

                    Console.WriteLine("Canonicalize");
                    JsonCanonicalizer jsonCanonicalizer = new JsonCanonicalizer(s);
                    string result = jsonCanonicalizer.GetEncodedString();
                    canonical.value = result;
                    subject.value = certificate.Subject;

                    X509Certificate2Collection xc = new X509Certificate2Collection();
                    xc.Import(certFile, certPW, X509KeyStorageFlags.PersistKeySet);

                    for (int j = xc.Count - 1; j >= 0; j--)
                    {
                        Console.WriteLine("Add certificate_" + (j + 1));
                        AdminShell.Property c = AdminShellV20.Property.CreateNew("certificate_" + (j + 1));
                        c.setTimeStamp(timestamp);
                        c.TimeStampCreate = timestamp;
                        c.value = Convert.ToBase64String(xc[j].GetRawCertData());
                        x5c.Add(c);
                    }

                    Console.WriteLine("RSA");
                    try
                    {
                        using (RSA rsa = certificate.GetRSAPrivateKey())
                        {
                            if (rsa == null)
                                return;

                            algorithm.value = "RS256";
                            byte[] data = Encoding.UTF8.GetBytes(result);
                            byte[] signed = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                            signature.value = Convert.ToBase64String(signed);
                            sigT.value = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
                        }
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch
                    {
                    }
                    // ReSharper enable EmptyGeneralCatchClause

                    Console.WriteLine("Add smc");
                    smc.Add(smec); // add signature
                }
            }
            //
        }

        static void modbusByteSwap(Byte[] bytes)
        {
            int len = bytes.Length;
            int i = 0;
            while (i < len - 1)
            {
                byte b = bytes[i + 1];
                bytes[i + 1] = bytes[i];
                bytes[i] = b;
                i += 2;
            }
        }

        public static bool timeSeriesSampling(bool final)
        {
            if (Program.isLoading)
                return true;

            OnOPCClientNextTimedEvent(100);

            // ulong newChangeNumber = ChangeNumber + 1;
            // bool useNewChangeNumber = false;
            DateTime timeStamp = DateTime.UtcNow;

            foreach (var tsb in timeSeriesBlockList)
            {
                if (tsb.sampleStatus == null)
                    continue;

                if (tsb.sampleStatus.value == "stop")
                {
                    tsb.sampleStatus.value = "stopped";
                    final = true;
                }
                else
                {
                    if (tsb.sampleStatus.value != "start")
                        continue;
                }

                if (tsb.sampleRate == null)
                    continue;

                tsb.threadCounter -= 100;
                if (tsb.threadCounter > 0)
                    continue;

                tsb.threadCounter = Convert.ToInt32(tsb.sampleRate.value);

                int actualSamples = Convert.ToInt32(tsb.actualSamples.value);
                int maxSamples = Convert.ToInt32(tsb.maxSamples.value);
                int actualSamplesInCollection = Convert.ToInt32(tsb.actualSamplesInCollection.value);
                int maxSamplesInCollection = Convert.ToInt32(tsb.maxSamplesInCollection.value);

                if (final || actualSamples < maxSamples)
                {
                    int updateMode = 0;
                    if (!final)
                    {
                        int valueCount = 1;
                        if (tsb.sourceType == "json" && tsb.sourceAddress != "")
                        {
                            AdminShell.SubmodelElementCollection c =
                                tsb.block.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("jsonData");
                            if (c == null)
                            {
                                c = new AdminShellV20.SubmodelElementCollection();
                                c.idShort = "jsonData";
                                c.TimeStampCreate = timeStamp;
                                tsb.block.Add(c);
                                c.setTimeStamp(timeStamp);
                            }
                            if (parseJSON(tsb.sourceAddress, "", "", c, tsb.sourceNames, tsb.minDiffAbsolute, tsb.minDiffPercent))
                            {
                                foreach (var el in c.value)
                                {
                                    if (el.submodelElement is AdminShell.Property p)
                                    {
                                        if (!tsb.samplesProperties.Contains(p))
                                        {
                                            tsb.samplesProperties.Add(p);
                                            tsb.samplesValues.Add("");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                valueCount = 0;
                            }
                        }
                        if (tsb.sourceType == "opchd" && tsb.sourceAddress != "")
                        {
                            GetHistory(tsb);
                            valueCount = 0;
                            if (table != null)
                                valueCount = table.Count;
                        }
                        if (tsb.sourceType == "opcda" && tsb.sourceAddress != "")
                        {
                            valueCount = GetDAData(tsb);
                        }
                        if (tsb.sourceType == "modbus" && tsb.sourceAddress != "")
                        {
                            valueCount = GetModbus(tsb);
                        }

                        DateTime dt;
                        int valueIndex = 0;
                        while (valueIndex < valueCount)
                        {
                            if (tsb.sourceType == "opchd" && tsb.sourceAddress != "")
                            {
                                dt = (DateTime)table[valueIndex][0];
                                Console.WriteLine(valueIndex + " " + dt + " " + table[valueIndex][1] + " " + table[valueIndex][2]);
                            }
                            else
                            {
                                dt = DateTime.UtcNow;
                            }

                            string latestTimeStamp = "";
                            if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                            {
                                var t = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                                latestTimeStamp = t;
                                if (tsb.samplesTimeStamp != "")
                                    tsb.samplesTimeStamp += ", ";
                                tsb.samplesTimeStamp += $"[{tsb.totalSamples.value}, {t}]";
                            }
                            else
                            {
                                if (tsb.samplesTimeStamp == "")
                                {
                                    latestTimeStamp = dt.ToString("yy-MM-dd HH:mm:ss.fff");
                                    tsb.samplesTimeStamp += latestTimeStamp;
                                }
                                else
                                {
                                    latestTimeStamp = dt.ToString("HH:mm:ss.fff");
                                    tsb.samplesTimeStamp += "," + latestTimeStamp;
                                }
                            }

                            // tsb.latestData.value.Clear();
                            // tsb.latestData.setTimeStamp(timeStamp);
                            AdminShell.SubmodelElement latestDataProperty = null;
                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("lowDataIndex");
                            if (latestDataProperty == null)
                            {
                                latestDataProperty = AdminShell.Property.CreateNew("lowDataIndex");
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Add(latestDataProperty);
                                tsb.lowDataIndex = latestDataProperty as AdminShell.Property;
                                tsb.lowDataIndex.value = "0";
                            }
                            (latestDataProperty as AdminShell.Property).value = "" + tsb.lowDataIndex.value;
                            latestDataProperty.setTimeStamp(timeStamp);

                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("highDataIndex");
                            if (latestDataProperty == null)
                            {
                                latestDataProperty = AdminShell.Property.CreateNew("highDataIndex");
                                (latestDataProperty as AdminShell.Property).value = "-1";
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Add(latestDataProperty);
                                tsb.highDataIndex = latestDataProperty as AdminShell.Property;
                            }
                            // (latestDataProperty as AdminShell.Property).value = "" + tsb.highDataIndex;
                            latestDataProperty.setTimeStamp(timeStamp);

                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("totalSamples");
                            if (latestDataProperty == null)
                            {
                                latestDataProperty = AdminShell.Property.CreateNew("totalSamples");
                                (latestDataProperty as AdminShell.Property).value = "0";
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Add(latestDataProperty);
                                tsb.totalSamples = latestDataProperty as AdminShell.Property;
                            }
                            // (latestDataProperty as AdminShell.Property).value = "" + tsb.highDataIndex;
                            latestDataProperty.setTimeStamp(timeStamp);

                            latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>("timeStamp");
                            if (latestDataProperty == null)
                            {
                                latestDataProperty = AdminShell.Property.CreateNew("timeStamp");
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Add(latestDataProperty);
                            }
                            (latestDataProperty as AdminShell.Property).value = dt.ToString("yy-MM-dd HH:mm:ss.fff");
                            latestDataProperty.setTimeStamp(timeStamp);

                            updateMode = 1;
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                string latestDataName = tsb.samplesProperties[i].idShort;
                                string latestDataValue = "";

                                if (tsb.samplesValues[i] != "")
                                {
                                    tsb.samplesValues[i] += ",";
                                }

                                if ((tsb.sourceType == "opchd" || tsb.sourceType == "opcda" || tsb.sourceType == "modbus")
                                    && tsb.sourceAddress != "")
                                {
                                    if (tsb.sourceType == "opchd")
                                    {
                                        if (table[valueIndex] != null && table[valueIndex][i + 1] != null)
                                            latestDataValue = table[valueIndex][i + 1].ToString();
                                        switch (latestDataValue.ToLower())
                                        {
                                            case "true":
                                                latestDataValue = "1";
                                                break;
                                            case "false":
                                                latestDataValue = "0";
                                                break;
                                        }
                                        if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        {
                                            tsb.samplesValues[i] += $"[{tsb.totalSamples.value}, {latestDataValue}]";
                                        }
                                        else
                                        {
                                            tsb.samplesValues[i] += latestDataValue;
                                        }
                                    }
                                    if (tsb.sourceType == "opcda")
                                    {
                                        latestDataValue = opcDAValues[i];
                                        switch (latestDataValue.ToLower())
                                        {
                                            case "true":
                                                latestDataValue = "1";
                                                break;
                                            case "false":
                                                latestDataValue = "0";
                                                break;
                                        }
                                        if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        {
                                            tsb.samplesValues[i] += $"[{tsb.totalSamples.value}, {latestDataValue}]";
                                        }
                                        else
                                        {
                                            tsb.samplesValues[i] += latestDataValue;
                                        }
                                        Console.WriteLine(tsb.opcNodes[i] + " " + opcDAValues[i]);
                                    }
                                    if (tsb.sourceType == "modbus")
                                    {
                                        latestDataValue = modbusValues[i];
                                        if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        {
                                            tsb.samplesValues[i] += $"[{tsb.totalSamples.value}, {latestDataValue}]";
                                        }
                                        else
                                        {
                                            tsb.samplesValues[i] += latestDataValue;
                                        }
                                        Console.WriteLine(tsb.modbusNodes[i] + " " + modbusValues[i]);
                                    }
                                }
                                else
                                {
                                    var p = tsb.samplesProperties[i];
                                    latestDataValue = p.value;

                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                    {
                                        tsb.samplesValues[i] += $"[{tsb.totalSamples.value}, {latestDataValue}]";
                                    }
                                    else
                                    {
                                        tsb.samplesValues[i] += latestDataValue;
                                    }
                                }

                                latestDataProperty = tsb.latestData.value.FindFirstIdShortAs<AdminShell.Property>(latestDataName);
                                if (latestDataProperty == null)
                                {
                                    latestDataProperty = AdminShell.Property.CreateNew(latestDataName);
                                    latestDataProperty.TimeStampCreate = timeStamp;
                                    AdminShell.Qualifier q = new AdminShell.Qualifier();
                                    q.type = "Plotting.Args";
                                    q.value =
                                        "{ grp:1, src: \"Event\"," +
                                        "title: \"" + latestDataName + "\"," +
                                        "fmt: \"F0\"," +
                                        "row: " + (tsb.plotRowOffset + i) + "," +
                                        "col: 0, rowspan: 1, colspan:1, unit: \"\", linewidth: 1.0 }";
                                    if (latestDataProperty.qualifiers == null)
                                        latestDataProperty.qualifiers = new AdminShellV20.QualifierCollection();
                                    latestDataProperty.qualifiers.Add(q);
                                    tsb.latestData.Add(latestDataProperty);
                                }
                                (latestDataProperty as AdminShell.Property).value = latestDataValue;
                                latestDataProperty.setTimeStamp(timeStamp);
                            }
                            tsb.samplesValuesCount++;
                            actualSamples++;
                            // tsb.totalSamples++;
                            int totalSamples = Convert.ToInt32(tsb.totalSamples.value);
                            totalSamples++;
                            tsb.totalSamples.value = totalSamples.ToString();
                            tsb.actualSamples.value = "" + actualSamples;
                            tsb.actualSamples.setTimeStamp(timeStamp);
                            actualSamplesInCollection++;
                            tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                            if (actualSamples >= maxSamples)
                            {
                                if (tsb.sampleMode.value == "continuous")
                                {
                                    var firstName = "data" + tsb.lowDataIndex.value;
                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        firstName = "Segment_" + tsb.lowDataIndex.value;

                                    var first =
                                        tsb.data.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>(
                                            firstName);
                                    if (first != null)
                                    {
                                        actualSamples -= maxSamplesInCollection;
                                        tsb.actualSamples.value = "" + actualSamples;
                                        tsb.actualSamples.setTimeStamp(timeStamp);
                                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                            first, "Remove", tsb.submodel, (ulong)timeStamp.Ticks);
                                        tsb.data.Remove(first);
                                        tsb.data.setTimeStamp(timeStamp);
                                        // tsb.lowDataIndex++;
                                        int index = Convert.ToInt32(tsb.lowDataIndex.value);
                                        tsb.lowDataIndex.value = (index + 1).ToString();
                                        updateMode = 1;
                                    }
                                }
                            }
                            if (actualSamplesInCollection >= maxSamplesInCollection)
                            {
                                if (actualSamplesInCollection > 0)
                                {
                                    // tsb.highDataIndex = tsb.samplesCollectionsCount;
                                    int index = Convert.ToInt32(tsb.highDataIndex.value);
                                    tsb.highDataIndex.value = (index + 1).ToString();

                                    AdminShell.SubmodelElementCollection nextCollection = null;
                                    // decide
                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                    {
                                        nextCollection = AddToSMC<AdminShell.SubmodelElementCollection>(
                                            timeStamp, null,
                                            // "Segment_" + tsb.samplesCollectionsCount++,
                                            "Segment_" + tsb.highDataIndex.value,
                                            semanticIdKey: PrefTimeSeries10.CD_TimeSeriesSegment);

                                        var smcvar = AddToSMC<AdminShell.SubmodelElementCollection>(
                                            timeStamp, nextCollection,
                                            "TSvariable_timeStamp", semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable);

                                        AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                            "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId,
                                            smeValue: "timeStamp");

                                        AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                            "UtcTime", semanticIdKey: PrefTimeSeries10.CD_UtcTime);

                                        AddToSMC<AdminShell.Blob>(timeStamp, smcvar,
                                            "timeStamp", semanticIdKey: PrefTimeSeries10.CD_ValueArray,
                                            smeValue: tsb.samplesTimeStamp);
                                    }
                                    else
                                    {
                                        // nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                                        nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.highDataIndex.value);
                                        var p = AdminShell.Property.CreateNew("timeStamp");
                                        p.value = tsb.samplesTimeStamp;
                                        p.setTimeStamp(timeStamp);
                                        p.TimeStampCreate = timeStamp;

                                        nextCollection.Add(p);
                                        nextCollection.setTimeStamp(timeStamp);
                                        nextCollection.TimeStampCreate = timeStamp;
                                    }

                                    tsb.samplesTimeStamp = "";
                                    for (int i = 0; i < tsb.samplesProperties.Count; i++)
                                    {
                                        if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        {
                                            var smcvar = AddToSMC<AdminShell.SubmodelElementCollection>(
                                                timeStamp, nextCollection,
                                                "TSvariable_" + tsb.samplesProperties[i].idShort,
                                                semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable);

                                            // MICHA: bad hack
                                            // if (tsb.samplesProperties[i].idShort.ToLower().Contains("int2"))
                                            //    smcvar.AddQualifier("TimeSeries.Args", "{ type: \"Bars\" }");

                                            AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                                "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId,
                                                smeValue: "" + tsb.samplesProperties[i].idShort);

                                            if (tsb.samplesProperties[i].idShort.ToLower().Contains("float"))
                                                AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                                    "" + tsb.samplesProperties[i].idShort,
                                                    semanticIdKey: PrefTimeSeries10.CD_GeneratedFloat);
                                            else
                                                AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                                    "" + tsb.samplesProperties[i].idShort,
                                                    semanticIdKey: PrefTimeSeries10.CD_GeneratedInteger);

                                            AddToSMC<AdminShell.Blob>(timeStamp, smcvar,
                                                "ValueArray", semanticIdKey: PrefTimeSeries10.CD_ValueArray,
                                                smeValue: tsb.samplesValues[i]);
                                        }
                                        else
                                        {
                                            var p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                                            nextCollection.Add(p);
                                            p.value = tsb.samplesValues[i];
                                            p.setTimeStamp(timeStamp);
                                            p.TimeStampCreate = timeStamp;
                                        }

                                        tsb.samplesValues[i] = "";
                                    }
                                    Sign(nextCollection, timeStamp);
                                    tsb.data.Add(nextCollection);
                                    tsb.data.setTimeStamp(timeStamp);
                                    AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                        nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                                    tsb.samplesValuesCount = 0;
                                    actualSamplesInCollection = 0;
                                    tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                                    tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                                    updateMode = 1;
                                    var json = JsonConvert.SerializeObject(nextCollection, Newtonsoft.Json.Formatting.Indented,
                                                                        new JsonSerializerSettings
                                                                        {
                                                                            NullValueHandling = NullValueHandling.Ignore
                                                                        });
                                    Program.connectPublish(tsb.block.idShort + "." + nextCollection.idShort, json);
                                }
                            }
                            valueIndex++;
                        }
                    }
                    if (final || actualSamplesInCollection >= maxSamplesInCollection)
                    {
                        if (actualSamplesInCollection > 0)
                        {
                            // tsb.highDataIndex = tsb.samplesCollectionsCount;
                            int index = Convert.ToInt32(tsb.highDataIndex.value);
                            tsb.highDataIndex.value = (index + 1).ToString();

                            AdminShell.SubmodelElementCollection nextCollection = null;
                            if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                            {
                                nextCollection = AddToSMC<AdminShell.SubmodelElementCollection>(
                                    timeStamp, null,
                                    // "Segment_" + tsb.samplesCollectionsCount++,
                                    "Segment_" + tsb.highDataIndex.value,
                                    semanticIdKey: PrefTimeSeries10.CD_TimeSeriesSegment);

                                var smcvar = AddToSMC<AdminShell.SubmodelElementCollection>(
                                    timeStamp, nextCollection,
                                    "TSvariable_timeStamp", semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable);

                                AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                    "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId,
                                    smeValue: "timeStamp");

                                AddToSMC<AdminShell.Property>(timeStamp, smcvar,
                                    "UtcTime", semanticIdKey: PrefTimeSeries10.CD_UtcTime);

                                AddToSMC<AdminShell.Blob>(timeStamp, smcvar,
                                    "timeStamp", semanticIdKey: PrefTimeSeries10.CD_ValueArray,
                                    smeValue: tsb.samplesTimeStamp);
                            }
                            else
                            {
                                // nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                                nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.highDataIndex.value);
                                var p = AdminShell.Property.CreateNew("timeStamp");
                                p.value = tsb.samplesTimeStamp;
                                p.setTimeStamp(timeStamp);
                                p.TimeStampCreate = timeStamp;
                                tsb.samplesTimeStamp = "";
                                nextCollection.Add(p);
                                nextCollection.setTimeStamp(timeStamp);
                                nextCollection.TimeStampCreate = timeStamp;
                            }
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                var p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                                p.value = tsb.samplesValues[i];
                                p.setTimeStamp(timeStamp);
                                p.TimeStampCreate = timeStamp;
                                tsb.samplesValues[i] = "";
                                nextCollection.Add(p);
                            }
                            Sign(nextCollection, timeStamp);
                            tsb.data.Add(nextCollection);
                            tsb.data.setTimeStamp(timeStamp);
                            AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                            tsb.samplesValuesCount = 0;
                            actualSamplesInCollection = 0;
                            tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                            updateMode = 1;
                        }
                    }
                    //// if (updateMode != 0)
                    Program.signalNewData(updateMode);
                }
            }

            if (!test)
                Thread.Sleep(100);

            return !final;
        }

        static bool parseJSON(string url, string username, string password, AdminShell.SubmodelElementCollection c,
            List<string> filter, AdminShell.Property minDiffAbsolute, AdminShell.Property minDiffPercent)
        {
            if (url == "posttimeseries")
            {
                string payload = AasxRestServerLibrary.AasxRestServer.TestResource.posttimeseriesPayload;
                if (payload != "")
                {
                    AasxRestServerLibrary.AasxRestServer.TestResource.posttimeseriesPayload = "";
                    try
                    {
                        JObject parsed = JObject.Parse(payload);
                        if (Program.parseJson(c, parsed, filter, minDiffAbsolute, minDiffPercent))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetJSON() expection: " + ex.Message);
                    }
                    return false;
                }
                return false;
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
            try
            {
                string response = client.GetStringAsync(url).Result;
                Console.WriteLine(response);

                if (response != "")
                {
                    JObject parsed = JObject.Parse(response);
                    if (Program.parseJson(c, parsed, filter, minDiffAbsolute, minDiffPercent))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetJSON() expection: " + ex.Message);
            }
            return false;
        }

        static List<List<object>> table = null;
        static string ErrorMessage { get; set; }
        static UASampleClient opc = null;
        static Opc.Ua.Client.Session session = null;
        static DateTime startTime;
        static DateTime endTime;
        static List<string> opcDAValues = null;
        static List<string> modbusValues = null;
        static List<string> lastModbusValues = null;
        static Modbus.ModbusTCPClient mbClient = null;

        public static int GetModbus(TimeSeriesBlock tsb)
        {
            int minDiffAbsolute = 1;
            int minDiffPercent = 0;
            if (tsb.minDiffAbsolute != null)
                minDiffAbsolute = Convert.ToInt32(tsb.minDiffAbsolute.value);
            if (tsb.minDiffPercent != null)
                minDiffPercent = Convert.ToInt32(tsb.minDiffPercent.value);

            // Console.WriteLine("Read Modbus Data:");
            try
            {
                ErrorMessage = "";
                if (mbClient == null)
                {
                    mbClient = new Modbus.ModbusTCPClient();
                    string[] hostPort = tsb.sourceAddress.Split(':');
                    mbClient.Connect(hostPort[0], Convert.ToInt32(hostPort[1]));
                }

                if (mbClient != null)
                {
                    modbusValues = new List<string>();
                    for (int i = 0; i < tsb.modbusNodes.Count; i++)
                    {
                        string[] split = tsb.modbusNodes[i].Split(',');
                        byte[] modbusValue = mbClient.Read(
                            (byte)Convert.ToInt32(split[0]),
                            Modbus.ModbusTCPClient.FunctionCode.ReadHoldingRegisters,
                            (ushort)Convert.ToInt32(split[1]),
                            (ushort)Convert.ToInt32(split[2]));
                        modbusByteSwap(modbusValue);
                        switch (split[3])
                        {
                            case "float":
                                float f = BitConverter.ToSingle(modbusValue, 0);
                                string value = Convert.ToInt32(f).ToString();
                                modbusValues.Add(value);
                                break;
                        }
                    }
                    if (lastModbusValues == null)
                    {
                        lastModbusValues = new List<string>(modbusValues);
                    }
                    else
                    {
                        // only collect changes if at least one value changed at least 1%
                        bool keep = false;
                        for (int i = 0; i < modbusValues.Count; i++)
                        {
                            int v = Convert.ToInt32(modbusValues[i]);
                            int lastv = Convert.ToInt32(lastModbusValues[i]);
                            int delta = Math.Abs(v - lastv);
                            if (delta >= minDiffAbsolute && delta >= lastv * minDiffPercent / 100)
                                keep = true;
                        }
                        if (keep)
                        {
                            lastModbusValues = new List<string>(modbusValues);
                        }
                        else
                        {
                            modbusValues = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return 0;
            }

            if (modbusValues == null)
                return 0;
            return 1;
        }

        public static int GetDAData(TimeSeriesBlock tsb)
        {
            Console.WriteLine("Read OPC DA Data:");
            try
            {
                ErrorMessage = "";
                if (session == null)
                    Connect(tsb);
                if (session != null)
                {
                    opcDAValues = new List<string>();
                    for (int i = 0; i < tsb.opcNodes.Count; i++)
                    {
                        string[] split = tsb.opcNodes[i].Split(',');
                        string value = opc.ReadSubmodelElementValue(split[1], (ushort)Convert.ToInt32(split[0]));
                        opcDAValues.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return 0;
            }
            /*
            session?.Close();
            session?.Dispose();
            session = null;
            */

            return 1;
        }

        public static void GetHistory(TimeSeriesBlock tsb)
        {
            Console.WriteLine("Read OPC UA Historical Data:");
            try
            {
                ErrorMessage = "";
                if (session == null)
                    Connect(tsb);
                startTime = tsb.opcLastTimeStamp;
                // get current time on server
                if (session != null)
                    endTime = (DateTime)session.ReadValue(new NodeId(2258, 0)).Value;
                GetData(tsb);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Console.WriteLine(ErrorMessage);
                session?.Close();
                session?.Dispose();
                session = null;
                opc = null;
            }
            /*
            session?.Close();
            session?.Dispose();
            session = null;
            */
        }
        public static void Connect(TimeSeriesBlock tsb)
        {
            Console.WriteLine("Connect OPC UA");
            if (opc == null)
                opc = new UASampleClient(tsb.sourceAddress, true, 10000, tsb.username, tsb.password);
            opc.ConsoleSampleClient().Wait();
            session = opc.session;
            if (session == null)
            {
                Console.WriteLine("ERROR: Session not connected "
                    + tsb.sourceAddress + " " + tsb.username + " " + tsb.password);
            }
            else
            {
                // get current time on server
                tsb.opcLastTimeStamp = (DateTime)session.ReadValue(new NodeId(2258, 0)).Value;
                tsb.opcLastTimeStamp -= TimeSpan.FromMinutes(1);
            }
        }
        public static void GetData(TimeSeriesBlock tsb)
        {
            if (session != null)
            {
                ReadRawModifiedDetails details = new ReadRawModifiedDetails();
                details.StartTime = startTime;
                details.EndTime = endTime;
                details.NumValuesPerNode = 0;
                details.IsReadModified = false;
                details.ReturnBounds = true;

                var nodesToRead = new HistoryReadValueIdCollection();
                for (int i = 0; i < tsb.opcNodes.Count; i++)
                {
                    var nodeToRead = new HistoryReadValueId();
                    string[] split = tsb.opcNodes[i].Split(',');
                    nodeToRead.NodeId = new NodeId(split[1], (ushort)Convert.ToInt32(split[0]));
                    nodesToRead.Add(nodeToRead);
                }

                table = new List<List<object>>();

                HistoryReadResultCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                bool loop = true;
                while (loop)
                {
                    session.HistoryRead(
                        null,
                        new ExtensionObject(details),
                        TimestampsToReturn.Both,
                        false,
                        nodesToRead,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToRead);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

                    foreach (var res in results)
                    {
                        if (StatusCode.IsBad(res.StatusCode))
                        {
                            throw new ServiceResultException(res.StatusCode);
                        }
                    }

                    if (results.Count > 0)
                    {
                        HistoryData[] historyDatas = new HistoryData[results.Count];
                        for (int i = 0; i < results.Count; i++)
                        {
                            historyDatas[i] = ExtensionObject.ToEncodeable(results[i].HistoryData) as HistoryData;
                        }

                        int dataValuesCount = historyDatas[0].DataValues.Count;
                        for (int i = 0; i < dataValuesCount; i++)
                        {
                            var sourceTimeStamp = historyDatas[0].DataValues[i].SourceTimestamp;
                            if (sourceTimeStamp != null && sourceTimeStamp >= startTime)
                            {
                                bool isValid = true;
                                var row = new List<object>();
                                row.Add(sourceTimeStamp);

                                foreach (HistoryData historyData in historyDatas)
                                {
                                    var value = historyData.DataValues[i].Value?.ToString();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        row.Add(historyData.DataValues[i].Value);
                                    }
                                    else
                                    {
                                        isValid = false;
                                        break;
                                    }
                                }
                                if (isValid)
                                {
                                    table.Add(row);
                                    tsb.opcLastTimeStamp = sourceTimeStamp + TimeSpan.FromMilliseconds(1);
                                }
                            }
                        }
                    }

                    for (int i = 0; i < results.Count; i++)
                    {
                        if (results[i].ContinuationPoint == null || results[i].ContinuationPoint.Length == 0)
                        {
                            loop = false;
                            break;
                        }
                        nodesToRead[i].ContinuationPoint = results[i].ContinuationPoint;
                    }
                }
            }
        }
    }
}
