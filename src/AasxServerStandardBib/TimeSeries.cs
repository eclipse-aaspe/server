using AasxServer;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using static AasxServer.Program;

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
            public AdminShell.Property sampleStatus = null;
            public AdminShell.Property sampleMode = null;
            public AdminShell.Property sampleRate = null;
            public AdminShell.Property lowDataIndex = null;
            public AdminShell.Property highDataIndex = null;
            public AdminShell.Property maxSamples = null;
            public AdminShell.Property actualSamples = null;
            public AdminShell.Property maxSamplesInCollection = null;
            public AdminShell.Property actualSamplesInCollection = null;
            public AdminShell.Property maxCollections = null;
            public AdminShell.Property actualCollections = null;

            public TimeSeriesDestFormat destFormat;

            public int threadCounter = 0;
            public string sourceType = "";
            public string sourceAddress = "";
            public string username = "";
            public string password = "";
            public int samplesCollectionsCount = 0;
            public List<AdminShell.Property> samplesProperties = null;
            public List<string> samplesValues = null;
            public string samplesTimeStamp = "";
            public int samplesValuesCount = 0;
            public int totalSamples = 0;

            public List<string> opcNodes = null;
            public DateTime opcLastTimeStamp;
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
                                        tsb.opcLastTimeStamp = DateTime.UtcNow - TimeSpan.FromMinutes(1) + TimeSpan.FromMinutes(120);

                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.value[iSmec].submodelElement;
                                            var idShort = sme2.idShort;
                                            if (idShort.Contains("opcNode"))
                                                idShort = "opcNode";
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
                                                case "data":
                                                    if (sme2 is AdminShell.SubmodelElementCollection)
                                                    {
                                                        tsb.data = sme2 as AdminShell.SubmodelElementCollection;
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
                                                case "lowDataIndex":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.lowDataIndex = sme2 as AdminShell.Property;
                                                        tsb.lowDataIndex.value = "0";
                                                    }
                                                    break;
                                                case "highDataIndex":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.highDataIndex = sme2 as AdminShell.Property;
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

            timeSeriesThread = new Thread(new ThreadStart(timeSeriesSamplingLoop));
            timeSeriesThread.Start();
        }

        static Thread timeSeriesThread;

        public static void timeSeriesSamplingLoop()
        {
            while (true)
            {
                Thread.Sleep(100);
                timeSeriesSampling(false);
            }
        }

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
            return newElem as T;
        }

        public static bool timeSeriesSampling(bool final)
        {
            if (Program.isLoading)
            {
                Thread.Sleep(1000);
                return true;
            }

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
                    TreeUpdateMode updateMode = TreeUpdateMode.ValuesOnly;
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
                            parseJSON(tsb.sourceAddress, "", "", c);

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

                        DateTime dt;
                        int valueIndex = 0;
                        while (valueIndex < valueCount)
                        {
                            dt = DateTime.UtcNow;
                            if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                            {
                                var t = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                                if (tsb.samplesTimeStamp != "")
                                    tsb.samplesTimeStamp += ", ";
                                tsb.samplesTimeStamp += $"[{tsb.totalSamples}, {t}]";
                            }
                            else
                            {
                                if (tsb.samplesTimeStamp == "")
                                {
                                    tsb.samplesTimeStamp += dt.ToString("yy-MM-dd HH:mm:ss.fff");
                                }
                                else
                                {
                                    tsb.samplesTimeStamp += "," + dt.ToString("HH:mm:ss.fff");
                                }
                            }

                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                if (tsb.samplesValues[i] != "")
                                {
                                    tsb.samplesValues[i] += ",";
                                }

                                var p = tsb.samplesProperties[i];

                                if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                {
                                    tsb.samplesValues[i] += $"[{tsb.totalSamples}, {p.value}]";
                                }
                                else
                                {
                                    tsb.samplesValues[i] += p.value;
                                }
                            }
                            tsb.samplesValuesCount++;
                            actualSamples++;
                            tsb.totalSamples++;
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
                                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.Add(
                                            first, "Remove", tsb.submodel, (ulong)timeStamp.Ticks);
                                        tsb.data.Remove(first);
                                        tsb.data.setTimeStamp(timeStamp);
                                        tsb.lowDataIndex.value = "" + (Convert.ToInt32(tsb.lowDataIndex.value) + 1);
                                        tsb.lowDataIndex.setTimeStamp(timeStamp);
                                        updateMode = TreeUpdateMode.Rebuild;
                                    }
                                }
                            }
                            if (actualSamplesInCollection >= maxSamplesInCollection)
                            {
                                if (actualSamplesInCollection > 0)
                                {
                                    if (tsb.highDataIndex != null)
                                    {
                                        tsb.highDataIndex.value = "" + tsb.samplesCollectionsCount;
                                        tsb.highDataIndex.setTimeStamp(timeStamp);
                                    }

                                    AdminShell.SubmodelElementCollection nextCollection = null;

                                    // decide
                                    if (tsb.destFormat == TimeSeriesDestFormat.TimeSeries10)
                                    {
                                        nextCollection = AddToSMC<AdminShell.SubmodelElementCollection>(
                                            timeStamp, null,
                                            "Segment_" + tsb.samplesCollectionsCount++,
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
                                        nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);

                                        var p = AdminShell.Property.CreateNew("timeStamp");
                                        p.value = tsb.samplesTimeStamp;
                                        p.setTimeStamp(timeStamp);
                                        p.TimeStampCreate = timeStamp;

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
                                            if (tsb.samplesProperties[i].idShort.ToLower().Contains("int2"))
                                                smcvar.AddQualifier("TimeSeries.Args", "{ type: \"Bars\" }");

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
                                    tsb.data.Add(nextCollection);
                                    tsb.data.setTimeStamp(timeStamp);
                                    AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.Add(
                                        nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                                    tsb.samplesValuesCount = 0;
                                    actualSamplesInCollection = 0;
                                    tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                                    tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                                    updateMode = TreeUpdateMode.Rebuild;
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
                            if (tsb.highDataIndex != null)
                            {
                                tsb.highDataIndex.value = "" + tsb.samplesCollectionsCount;
                                tsb.highDataIndex.setTimeStamp(timeStamp);
                            }
                            var nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                            var p = AdminShell.Property.CreateNew("timeStamp");
                            p.value = tsb.samplesTimeStamp;
                            p.setTimeStamp(timeStamp);
                            p.TimeStampCreate = timeStamp;
                            tsb.samplesTimeStamp = "";
                            nextCollection.Add(p);
                            nextCollection.setTimeStamp(timeStamp);
                            nextCollection.TimeStampCreate = timeStamp;
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                                p.value = tsb.samplesValues[i];
                                p.setTimeStamp(timeStamp);
                                p.TimeStampCreate = timeStamp;
                                tsb.samplesValues[i] = "";
                                nextCollection.Add(p);
                            }
                            tsb.data.Add(nextCollection);
                            tsb.data.setTimeStamp(timeStamp);
                            AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.Add(
                                nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                            tsb.samplesValuesCount = 0;
                            actualSamplesInCollection = 0;
                            tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                            updateMode = TreeUpdateMode.Rebuild;
                        }
                    }

                    if (updateMode != 0)
                    {
                        Program.SignalNewData(updateMode);
                    }
                }
            }

            return !final;
        }

        static void parseJSON(string url, string username, string password, AdminShell.SubmodelElementCollection c)
        {
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
                    Program.parseJson(c, parsed);
                }
            } catch (Exception ex)
            {
                Console.WriteLine("GetJSON() expection: " + ex.Message);
            }
        }
    }
}
