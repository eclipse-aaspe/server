using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using AasCore.Aas3_0_RC02;
using AasxServer;
using AdminShellNS;
using Extenstions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using SampleClient;

namespace AasxTimeSeries
{
    public enum TimeSeriesDestFormat { Plain, TimeSeries10 }

    public static class TimeSeries
    {
        public class TimeSeriesBlock
        {
            public Submodel submodel = null;
            public SubmodelElementCollection block = null;
            public SubmodelElementCollection data = null;
            public SubmodelElementCollection latestData = null;
            public Property sampleStatus = null;
            public Property sampleMode = null;
            public Property sampleRate = null;
            public Property maxSamples = null;
            public Property actualSamples = null;
            public Property maxSamplesInCollection = null;
            public Property actualSamplesInCollection = null;
            public Property maxCollections = null;
            public Property actualCollections = null;
            public Property minDiffAbsolute = null;
            public Property minDiffPercent = null;

            public TimeSeriesDestFormat destFormat;

            public int threadCounter = 0;
            public string sourceType = "";
            public string sourceAddress = "";
            public List<string> sourceNames = new List<string>();
            public string username = "";
            public string password = "";
            public int plotRowOffset = 0;
            public int samplesCollectionsCount = 0;
            public List<Property> samplesProperties = null;
            public List<string> samplesValues = null;
            public string samplesTimeStamp = "";
            public int samplesValuesCount = 0;
            // public int totalSamples = 0;
            public Property totalSamples = null;
            // public int lowDataIndex = 0;
            public Property lowDataIndex = null;
            // public int highDataIndex = -1;
            public Property highDataIndex = null;

            public List<string> opcNodes = null;
            public List<string> modbusNodes = null;
            public DateTime opcLastTimeStamp;
            public int correctionMinutes = 0;
        }

        static public List<TimeSeriesBlock> timeSeriesBlockList = null;
        static public List<SubmodelElementCollection> timeSeriesSubscribe = null;

        public static void timeSeriesInit()
        {
            DateTime timeStamp = DateTime.UtcNow;

            timeSeriesBlockList = new List<TimeSeriesBlock>();
            timeSeriesSubscribe = new List<SubmodelElementCollection>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AssetAdministrationShells[0];
                    AasxCompatibilityModels.AdminShellV20.AdministrationShell aasV2 = new AasxCompatibilityModels.AdminShellV20.AdministrationShell();
                    aasV2.TimeStampCreate = timeStamp;

                    aas.TimeStampCreate = timeStamp;
                    aas.SetTimeStamp(timeStamp);
                    if (aas.Submodels != null && aas.Submodels.Count > 0)
                    {
                        foreach (var smr in aas.Submodels)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.IdShort != null)
                            {
                                sm.TimeStampCreate = timeStamp;
                                sm.SetTimeStamp(timeStamp);
                                sm.SetAllParents(timeStamp);
                                int countSme = sm.SubmodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.SubmodelElements[iSme];
                                    if (sme is SubmodelElementCollection && sme.IdShort.Contains("TimeSeries"))
                                    {
                                        bool nextSme = false;
                                        if (sme.Qualifiers.Count > 0)
                                        {
                                            int j = 0;
                                            while (j < sme.Qualifiers.Count)
                                            {
                                                var q = sme.Qualifiers[j] as Qualifier;
                                                if (q.Type == "SUBSCRIBE")
                                                {
                                                    timeSeriesSubscribe.Add(sme as SubmodelElementCollection);
                                                    // nextSme = true;
                                                    break;
                                                }
                                                j++;
                                            }
                                        }
                                        if (nextSme)
                                            continue;

                                        var smec = sme as SubmodelElementCollection;
                                        int countSmec = smec.Value.Count;

                                        var tsb = new TimeSeriesBlock();
                                        tsb.submodel = sm;
                                        tsb.block = smec;
                                        tsb.data = tsb.block;
                                        tsb.samplesProperties = new List<Property>();
                                        tsb.samplesValues = new List<string>();

                                        for (int dataSections = 0; dataSections < 2; dataSections++)
                                        {
                                            for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                            {
                                                var sme2 = smec.Value[iSmec];
                                                var idShort = sme2.IdShort;
                                                if (idShort.Contains("opcNode"))
                                                    idShort = "opcNode";
                                                if (idShort.Contains("modbusNode"))
                                                    idShort = "modbusNode";
                                                switch (idShort)
                                                {
                                                    case "sourceType":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.sourceType = (sme2 as Property).Value;
                                                        }
                                                        break;
                                                    case "sourceAddress":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.sourceAddress = (sme2 as Property).Value;
                                                        }
                                                        break;
                                                    case "sourceNames":
                                                        if (sme2 is Property)
                                                        {
                                                            string[] split = (sme2 as Property).Value.Split(',');
                                                            if (split.Length != 0)
                                                            {
                                                                foreach (string s in split)
                                                                    tsb.sourceNames.Add(s);
                                                            }
                                                        }
                                                        break;
                                                    case "destFormat":
                                                        if (sme2 is Property)
                                                        {
                                                            var xx = (sme2 as Property).Value.Trim().ToLower();
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
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.username = (sme2 as Property).Value;
                                                        }
                                                        break;
                                                    case "password":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.password = (sme2 as Property).Value;
                                                        }
                                                        break;
                                                    case "plotRowOffset":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.plotRowOffset = Convert.ToInt32((sme2 as Property).Value);
                                                        }
                                                        break;
                                                    case "correctionMinutes":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.correctionMinutes = Convert.ToInt32((sme2 as Property).Value);
                                                        }
                                                        break;
                                                    case "data":
                                                        if (sme2 is SubmodelElementCollection)
                                                        {
                                                            tsb.data = sme2 as SubmodelElementCollection;
                                                        }
                                                        if (sme2 is ReferenceElement)
                                                        {
                                                            var refElement = Program.env[0].AasEnv.FindReferableByReference((sme2 as ReferenceElement).GetModelReference());
                                                            if (refElement is SubmodelElementCollection)
                                                                tsb.data = refElement as SubmodelElementCollection;
                                                        }
                                                        break;
                                                    case "minDiffPercent":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.minDiffPercent = sme2 as Property;
                                                        }
                                                        break;
                                                    case "minDiffAbsolute":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.minDiffAbsolute = sme2 as Property;
                                                        }
                                                        break;
                                                    case "sampleStatus":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.sampleStatus = sme2 as Property;
                                                        }
                                                        break;
                                                    case "sampleMode":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.sampleMode = sme2 as Property;
                                                        }
                                                        break;
                                                    case "sampleRate":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.sampleRate = sme2 as Property;
                                                        }
                                                        break;
                                                    case "maxSamples":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.maxSamples = sme2 as Property;
                                                        }
                                                        break;
                                                    case "actualSamples":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.actualSamples = sme2 as Property;
                                                            tsb.actualSamples.Value = "0";
                                                        }
                                                        break;
                                                    case "maxSamplesInCollection":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.maxSamplesInCollection = sme2 as Property;
                                                        }
                                                        break;
                                                    case "actualSamplesInCollection":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.actualSamplesInCollection = sme2 as Property;
                                                            tsb.actualSamplesInCollection.Value = "0";
                                                        }
                                                        break;
                                                    case "maxCollections":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.maxCollections = sme2 as Property;
                                                        }
                                                        break;
                                                    case "actualCollections":
                                                        if (sme2 is Property)
                                                        {
                                                            tsb.actualCollections = sme2 as Property;
                                                            tsb.actualCollections.Value = "0";
                                                        }
                                                        break;
                                                    case "opcNode":
                                                        if (sme2 is Property)
                                                        {
                                                            string node = (sme2 as Property).Value;
                                                            string[] split = node.Split(',');
                                                            if (tsb.opcNodes == null)
                                                                tsb.opcNodes = new List<string>();
                                                            tsb.opcNodes.Add(split[1] + "," + split[2]);
                                                            var p = new Property(DataTypeDefXsd.String, idShort: split[0]);
                                                            //var p = Property.CreateNew(split[0]);
                                                            tsb.samplesProperties.Add(p);
                                                            p.TimeStampCreate = timeStamp;
                                                            p.SetTimeStamp(timeStamp);
                                                            tsb.samplesValues.Add("");
                                                        }
                                                        break;
                                                    case "modbusNode":
                                                        if (sme2 is Property)
                                                        {
                                                            string node = (sme2 as Property).Value;
                                                            string[] split = node.Split(',');
                                                            if (tsb.modbusNodes == null)
                                                                tsb.modbusNodes = new List<string>();
                                                            tsb.modbusNodes.Add(split[1] + "," + split[2] + "," + split[3] + "," + split[4]);
                                                            var p = new Property(DataTypeDefXsd.String, idShort: split[0]);
                                                            //var p = Property.CreateNew(split[0]);
                                                            tsb.samplesProperties.Add(p);
                                                            p.TimeStampCreate = timeStamp;
                                                            p.SetTimeStamp(timeStamp);
                                                            tsb.samplesValues.Add("");
                                                        }
                                                        break;
                                                }
                                                if (tsb.sourceType == "aas" && sme2 is ReferenceElement r)
                                                {
                                                    var el = env.AasEnv.FindReferableByReference(r.GetModelReference());
                                                    if (el is Property p)
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
                                                countSmec = smec.Value.Count;
                                            }
                                        }
                                        tsb.opcLastTimeStamp = DateTime.UtcNow + TimeSpan.FromMinutes(tsb.correctionMinutes) - TimeSpan.FromMinutes(2);

                                        if (tsb.data != null)
                                        {
                                            //tsb.latestData = SubmodelElementCollection.CreateNew("latestData");
                                            tsb.latestData = new SubmodelElementCollection(idShort: "latestData");
                                            tsb.latestData.SetTimeStamp(timeStamp);
                                            tsb.latestData.TimeStampCreate = timeStamp;
                                            tsb.data.Value.Add(tsb.latestData);

                                            ISubmodelElement latestDataProperty = null;
                                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("lowDataIndex");
                                            if (latestDataProperty == null)
                                            {
                                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "lowDataIndex", value: "0");
                                                //latestDataProperty = Property.CreateNew("lowDataIndex");
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Value.Add(latestDataProperty);
                                                tsb.lowDataIndex = latestDataProperty as Property;
                                            }
                                            latestDataProperty.SetTimeStamp(timeStamp);

                                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("highDataIndex");
                                            if (latestDataProperty == null)
                                            {
                                                //latestDataProperty = Property.CreateNew("highDataIndex");
                                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "highDataIndex", value: "-1");
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Value.Add(latestDataProperty);
                                                tsb.highDataIndex = latestDataProperty as Property;
                                            }
                                            latestDataProperty.SetTimeStamp(timeStamp);

                                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("totalSamples");
                                            if (latestDataProperty == null)
                                            {
                                                //latestDataProperty = Property.CreateNew("totalSamples");
                                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "totalSamples", value: "0");
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Value.Add(latestDataProperty);
                                                tsb.totalSamples = latestDataProperty as Property;
                                            }
                                            latestDataProperty.SetTimeStamp(timeStamp);

                                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("timeStamp");
                                            if (latestDataProperty == null)
                                            {
                                                //latestDataProperty = Property.CreateNew("timeStamp");
                                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "timeStamp");
                                                latestDataProperty.TimeStampCreate = timeStamp;
                                                tsb.latestData.Value.Add(latestDataProperty);
                                            }
                                            latestDataProperty.SetTimeStamp(timeStamp);
                                        }
                                        if (tsb.sampleRate != null)
                                            tsb.threadCounter = Convert.ToInt32(tsb.sampleRate.Value);
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

        static bool setChangeNumber(IReferable r, ulong changeNumber)
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
            SubmodelElementCollection smc,
            string idShort,
            string semanticIdKey,
            string smeValue = null) where T : ISubmodelElement
        {
            var newElem = CreateSubmodelElementIstance(typeof(T));
            newElem.IdShort = idShort;
            newElem.SemanticId = new Reference(AasCore.Aas3_0_RC02.ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, semanticIdKey) });
            newElem.SetTimeStamp(timestamp);
            newElem.TimeStampCreate = timestamp;
            if (smc?.Value != null)
                smc.Value.Add(newElem);
            if (smeValue != null && newElem is Property newP)
                newP.Value = smeValue;
            if (smeValue != null && newElem is Blob newB)
                newB.Value = Encoding.ASCII.GetBytes(smeValue);
            return (T)newElem;
        }

        private static ISubmodelElement CreateSubmodelElementIstance(Type type)
        {
            if (type == null || !type.IsSubclassOf(typeof(ISubmodelElement)))
                return null;
            var sme = Activator.CreateInstance(type) as ISubmodelElement;
            return sme;
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

                if (tsb.sampleStatus.Value == "stop")
                {
                    tsb.sampleStatus.Value = "stopped";
                    final = true;
                }
                else
                {
                    if (tsb.sampleStatus.Value != "start")
                        continue;
                }

                if (tsb.sampleRate == null)
                    continue;

                tsb.threadCounter -= 100;
                if (tsb.threadCounter > 0)
                    continue;

                tsb.threadCounter = Convert.ToInt32(tsb.sampleRate.Value);

                int actualSamples = Convert.ToInt32(tsb.actualSamples.Value);
                int maxSamples = Convert.ToInt32(tsb.maxSamples.Value);
                int actualSamplesInCollection = Convert.ToInt32(tsb.actualSamplesInCollection.Value);
                int maxSamplesInCollection = Convert.ToInt32(tsb.maxSamplesInCollection.Value);

                if (final || actualSamples < maxSamples)
                {
                    int updateMode = 0;
                    if (!final)
                    {
                        int valueCount = 1;
                        if (tsb.sourceType == "json" && tsb.sourceAddress != "")
                        {
                            SubmodelElementCollection c =
                                tsb.block.FindFirstIdShortAs<SubmodelElementCollection>("jsonData");
                            if (c == null)
                            {
                                c = new SubmodelElementCollection();
                                c.IdShort = "jsonData";
                                c.TimeStampCreate = timeStamp;
                                tsb.block.Value.Add(c);
                                c.SetTimeStamp(timeStamp);
                            }
                            if (parseJSON(tsb.sourceAddress, "", "", c, tsb.sourceNames, tsb.minDiffAbsolute, tsb.minDiffPercent))
                            {
                                foreach (var el in c.Value)
                                {
                                    if (el is Property p)
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
                                tsb.samplesTimeStamp += $"[{tsb.totalSamples.Value}, {t}]";
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

                            // tsb.latestData.Value.Clear();
                            // tsb.latestData.SetTimeStamp(timeStamp);
                            ISubmodelElement latestDataProperty = null;
                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("lowDataIndex");
                            if (latestDataProperty == null)
                            {
                                //latestDataProperty = Property.CreateNew("lowDataIndex");
                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "lowDataIndex", value: "0");
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Value.Add(latestDataProperty);
                                tsb.lowDataIndex = latestDataProperty as Property;
                            }
                            (latestDataProperty as Property).Value = "" + tsb.lowDataIndex.Value;
                            latestDataProperty.SetTimeStamp(timeStamp);

                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("highDataIndex");
                            if (latestDataProperty == null)
                            {
                                //latestDataProperty = Property.CreateNew("highDataIndex");
                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "highDataIndex", value: "-1");
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Value.Add(latestDataProperty);
                                tsb.highDataIndex = latestDataProperty as Property;
                            }
                            // (latestDataProperty as Property).Value = "" + tsb.highDataIndex;
                            latestDataProperty.SetTimeStamp(timeStamp);

                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("totalSamples");
                            if (latestDataProperty == null)
                            {
                                //latestDataProperty = Property.CreateNew("totalSamples");
                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "totalSamples", value: "0");
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Value.Add(latestDataProperty);
                                tsb.totalSamples = latestDataProperty as Property;
                            }
                            // (latestDataProperty as Property).Value = "" + tsb.highDataIndex;
                            latestDataProperty.SetTimeStamp(timeStamp);

                            latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>("timeStamp");
                            if (latestDataProperty == null)
                            {
                                //latestDataProperty = Property.CreateNew("timeStamp");
                                latestDataProperty = new Property(DataTypeDefXsd.String, idShort: "timeStamp");
                                latestDataProperty.TimeStampCreate = timeStamp;
                                tsb.latestData.Value.Add(latestDataProperty);
                            }
                            (latestDataProperty as Property).Value = dt.ToString("yy-MM-dd HH:mm:ss.fff");
                            latestDataProperty.SetTimeStamp(timeStamp);

                            updateMode = 1;
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                string latestDataName = tsb.samplesProperties[i].IdShort;
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
                                            tsb.samplesValues[i] += $"[{tsb.totalSamples.Value}, {latestDataValue}]";
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
                                            tsb.samplesValues[i] += $"[{tsb.totalSamples.Value}, {latestDataValue}]";
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
                                            tsb.samplesValues[i] += $"[{tsb.totalSamples.Value}, {latestDataValue}]";
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
                                    latestDataValue = p.Value;

                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                    {
                                        tsb.samplesValues[i] += $"[{tsb.totalSamples.Value}, {latestDataValue}]";
                                    }
                                    else
                                    {
                                        tsb.samplesValues[i] += latestDataValue;
                                    }
                                }

                                latestDataProperty = tsb.latestData.FindFirstIdShortAs<Property>(latestDataName);
                                if (latestDataProperty == null)
                                {
                                    //latestDataProperty = Property.CreateNew(latestDataName);
                                    latestDataProperty = new Property(DataTypeDefXsd.String, idShort: latestDataName);
                                    latestDataProperty.TimeStampCreate = timeStamp;
                                    string val =
                                        "{ grp:1, src: \"Event\"," +
                                        "title: \"" + latestDataName + "\"," +
                                        "fmt: \"F0\"," +
                                        "row: " + (tsb.plotRowOffset + i) + "," +
                                        "col: 0, rowspan: 1, colspan:1, unit: \"\", linewidth: 1.0 }";
                                    Qualifier q = new Qualifier(type: "Plotting.Args", DataTypeDefXsd.String, value: val);
                                    //q.Type = "Plotting.Args";

                                    if (latestDataProperty.Qualifiers == null)
                                        //latestDataProperty.Qualifiers = new QualifierCollection();
                                        latestDataProperty.Qualifiers = new List<Qualifier>();
                                    latestDataProperty.Qualifiers.Add(q);
                                    tsb.latestData.Value.Add(latestDataProperty);
                                }
                                (latestDataProperty as Property).Value = latestDataValue;
                                latestDataProperty.SetTimeStamp(timeStamp);
                            }
                            tsb.samplesValuesCount++;
                            actualSamples++;
                            // tsb.totalSamples++;
                            int totalSamples = Convert.ToInt32(tsb.totalSamples.Value);
                            totalSamples++;
                            tsb.totalSamples.Value = totalSamples.ToString();
                            tsb.actualSamples.Value = "" + actualSamples;
                            tsb.actualSamples.SetTimeStamp(timeStamp);
                            actualSamplesInCollection++;
                            tsb.actualSamplesInCollection.Value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.SetTimeStamp(timeStamp);
                            if (actualSamples >= maxSamples)
                            {
                                if (tsb.sampleMode.Value == "continuous")
                                {
                                    var firstName = "data" + tsb.lowDataIndex.Value;
                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        firstName = "Segment_" + tsb.lowDataIndex.Value;

                                    var first =
                                        tsb.data.FindFirstIdShortAs<SubmodelElementCollection>(
                                            firstName);
                                    if (first != null)
                                    {
                                        actualSamples -= maxSamplesInCollection;
                                        tsb.actualSamples.Value = "" + actualSamples;
                                        tsb.actualSamples.SetTimeStamp(timeStamp);
                                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                            first, "Remove", tsb.submodel, (ulong)timeStamp.Ticks);
                                        tsb.data.Value.Remove(first);
                                        tsb.data.SetTimeStamp(timeStamp);
                                        // tsb.lowDataIndex++;
                                        int index = Convert.ToInt32(tsb.lowDataIndex.Value);
                                        tsb.lowDataIndex.Value = (index + 1).ToString();
                                        updateMode = 1;
                                    }
                                }
                            }
                            if (actualSamplesInCollection >= maxSamplesInCollection)
                            {
                                if (actualSamplesInCollection > 0)
                                {
                                    // tsb.highDataIndex = tsb.samplesCollectionsCount;
                                    int index = Convert.ToInt32(tsb.highDataIndex.Value);
                                    tsb.highDataIndex.Value = (index + 1).ToString();

                                    SubmodelElementCollection nextCollection = null;
                                    // decide
                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                    {
                                        nextCollection = AddToSMC<SubmodelElementCollection>(
                                            timeStamp, null,
                                            // "Segment_" + tsb.samplesCollectionsCount++,
                                            "Segment_" + tsb.highDataIndex.Value,
                                            semanticIdKey: PrefTimeSeries10.CD_TimeSeriesSegment.Value);

                                        var smcvar = AddToSMC<SubmodelElementCollection>(
                                            timeStamp, nextCollection,
                                            "TSvariable_timeStamp", semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable.Value);

                                        AddToSMC<Property>(timeStamp, smcvar,
                                            "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId.Value,
                                            smeValue: "timeStamp");

                                        AddToSMC<Property>(timeStamp, smcvar,
                                            "UtcTime", semanticIdKey: PrefTimeSeries10.CD_UtcTime.Value);

                                        AddToSMC<Blob>(timeStamp, smcvar,
                                            "timeStamp", semanticIdKey: PrefTimeSeries10.CD_ValueArray.Value,
                                            smeValue: tsb.samplesTimeStamp);
                                    }
                                    else
                                    {
                                        // nextCollection = SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                                        //nextCollection = SubmodelElementCollection.CreateNew("data" + tsb.highDataIndex.Value);
                                        nextCollection = new SubmodelElementCollection(idShort: "data" + tsb.highDataIndex.Value);
                                        var p = new Property(DataTypeDefXsd.String, idShort: "timeStamp");
                                        //var p = Property.CreateNew("timeStamp");
                                        p.Value = tsb.samplesTimeStamp;
                                        p.SetTimeStamp(timeStamp);
                                        p.TimeStampCreate = timeStamp;

                                        nextCollection.Value.Add(p);
                                        nextCollection.SetTimeStamp(timeStamp);
                                        nextCollection.TimeStampCreate = timeStamp;
                                    }

                                    tsb.samplesTimeStamp = "";
                                    for (int i = 0; i < tsb.samplesProperties.Count; i++)
                                    {
                                        if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                        {
                                            var smcvar = AddToSMC<SubmodelElementCollection>(
                                                timeStamp, nextCollection,
                                                "TSvariable_" + tsb.samplesProperties[i].IdShort,
                                                semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable.Value);

                                            // MICHA: bad hack
                                            // if (tsb.samplesProperties[i].IdShort.ToLower().Contains("int2"))
                                            //    smcvar.AddQualifier("TimeSeries.Args", "{ type: \"Bars\" }");

                                            AddToSMC<Property>(timeStamp, smcvar,
                                                "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId.Value,
                                                smeValue: "" + tsb.samplesProperties[i].IdShort);

                                            if (tsb.samplesProperties[i].IdShort.ToLower().Contains("float"))
                                                AddToSMC<Property>(timeStamp, smcvar,
                                                    "" + tsb.samplesProperties[i].IdShort,
                                                    semanticIdKey: PrefTimeSeries10.CD_GeneratedFloat.Value);
                                            else
                                                AddToSMC<Property>(timeStamp, smcvar,
                                                    "" + tsb.samplesProperties[i].IdShort,
                                                    semanticIdKey: PrefTimeSeries10.CD_GeneratedInteger.Value);

                                            AddToSMC<Blob>(timeStamp, smcvar,
                                                "ValueArray", semanticIdKey: PrefTimeSeries10.CD_ValueArray.Value,
                                                smeValue: tsb.samplesValues[i]);
                                        }
                                        else
                                        {
                                            //var p = Property.CreateNew(tsb.samplesProperties[i].IdShort);
                                            var p = new Property(DataTypeDefXsd.String, idShort: tsb.samplesProperties[i].IdShort);
                                            nextCollection.Value.Add(p);
                                            p.Value = tsb.samplesValues[i];
                                            p.SetTimeStamp(timeStamp);
                                            p.TimeStampCreate = timeStamp;
                                        }

                                        tsb.samplesValues[i] = "";
                                    }
                                    tsb.data.Value.Add(nextCollection);
                                    tsb.data.SetTimeStamp(timeStamp);
                                    AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                        nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                                    tsb.samplesValuesCount = 0;
                                    actualSamplesInCollection = 0;
                                    tsb.actualSamplesInCollection.Value = "" + actualSamplesInCollection;
                                    tsb.actualSamplesInCollection.SetTimeStamp(timeStamp);
                                    updateMode = 1;
                                    var json = JsonConvert.SerializeObject(nextCollection, Newtonsoft.Json.Formatting.Indented,
                                                                        new JsonSerializerSettings
                                                                        {
                                                                            NullValueHandling = NullValueHandling.Ignore
                                                                        });
                                    Program.connectPublish(tsb.block.IdShort + "." + nextCollection.IdShort, json);
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
                            int index = Convert.ToInt32(tsb.highDataIndex.Value);
                            tsb.highDataIndex.Value = (index + 1).ToString();

                            SubmodelElementCollection nextCollection = null;
                            if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                            {
                                nextCollection = AddToSMC<SubmodelElementCollection>(
                                    timeStamp, null,
                                    // "Segment_" + tsb.samplesCollectionsCount++,
                                    "Segment_" + tsb.highDataIndex.Value,
                                    semanticIdKey: PrefTimeSeries10.CD_TimeSeriesSegment.Value);

                                var smcvar = AddToSMC<SubmodelElementCollection>(
                                    timeStamp, nextCollection,
                                    "TSvariable_timeStamp", semanticIdKey: PrefTimeSeries10.CD_TimeSeriesVariable.Value);

                                AddToSMC<Property>(timeStamp, smcvar,
                                    "RecordId", semanticIdKey: PrefTimeSeries10.CD_RecordId.Value,
                                    smeValue: "timeStamp");

                                AddToSMC<Property>(timeStamp, smcvar,
                                    "UtcTime", semanticIdKey: PrefTimeSeries10.CD_UtcTime.Value);

                                AddToSMC<Blob>(timeStamp, smcvar,
                                    "timeStamp", semanticIdKey: PrefTimeSeries10.CD_ValueArray.Value,
                                    smeValue: tsb.samplesTimeStamp);
                            }
                            else
                            {
                                // nextCollection = SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                                //nextCollection = SubmodelElementCollection.CreateNew("data" + tsb.highDataIndex.Value);
                                nextCollection = new SubmodelElementCollection(idShort: "data" + tsb.highDataIndex.Value);
                                var p = new Property(DataTypeDefXsd.String, idShort: "timeStamp");
                                p.Value = tsb.samplesTimeStamp;
                                p.SetTimeStamp(timeStamp);
                                p.TimeStampCreate = timeStamp;
                                tsb.samplesTimeStamp = "";
                                nextCollection.Value.Add(p);
                                nextCollection.SetTimeStamp(timeStamp);
                                nextCollection.TimeStampCreate = timeStamp;
                            }
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                var p = new Property(DataTypeDefXsd.String, idShort: tsb.samplesProperties[i].IdShort);
                                p.Value = tsb.samplesValues[i];
                                p.SetTimeStamp(timeStamp);
                                p.TimeStampCreate = timeStamp;
                                tsb.samplesValues[i] = "";
                                nextCollection.Value.Add(p);
                            }
                            tsb.data.Value.Add(nextCollection);
                            tsb.data.SetTimeStamp(timeStamp);
                            AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                            tsb.samplesValuesCount = 0;
                            actualSamplesInCollection = 0;
                            tsb.actualSamplesInCollection.Value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.SetTimeStamp(timeStamp);
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

        static bool parseJSON(string url, string username, string password, SubmodelElementCollection c,
            List<string> filter, Property minDiffAbsolute, Property minDiffPercent)
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
                minDiffAbsolute = Convert.ToInt32(tsb.minDiffAbsolute.Value);
            if (tsb.minDiffPercent != null)
                minDiffPercent = Convert.ToInt32(tsb.minDiffPercent.Value);

            Console.WriteLine("Read Modbus Data:");
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
