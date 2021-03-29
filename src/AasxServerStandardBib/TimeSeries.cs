using AasxServer;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AasxTimeSeries
{
    public static class TimeSeries
    {
        public class TimeSeriesBlock
        {
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
            public int samplesCollectionsCount = 0;

            public List<AdminShell.Property> samplesProperties = null;
            public List<string> samplesValues = null;
            public int samplesValuesCount = 0;

        }
        static public List<TimeSeriesBlock> timeSeriesBlockList = null;
        public static void timeSeriesInit()
        {
            timeSeriesBlockList = new List<TimeSeriesBlock>();

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
                            if (sm != null && sm.idShort != null)
                            {
                                int countSme = sm.submodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.submodelElements[iSme].submodelElement;
                                    if (sme is AdminShell.SubmodelElementCollection && sme.idShort.Contains("TimeSeries"))
                                    {
                                        var smec = sme as AdminShell.SubmodelElementCollection;
                                        int countSmec = smec.value.Count;

                                        var tsb = new TimeSeriesBlock();
                                        tsb.block = smec;
                                        tsb.samplesProperties = new List<AdminShell.Property>();
                                        tsb.samplesValues = new List<string>();

                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.value[iSmec].submodelElement;
                                            switch (sme2.idShort)
                                            {
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
                                            }
                                            if (sme2 is AdminShell.ReferenceElement r)
                                            {
                                                var el = env.AasEnv.FindReferableByReference(r.value);
                                                if (el is AdminShell.Property p)
                                                {
                                                    tsb.samplesProperties.Add(p);
                                                    tsb.samplesValues.Add("");
                                                }
                                            }
                                        }
                                        if (tsb.samplesProperties.Count != 0 && tsb.data != null)
                                        {
                                            timeSeriesBlockList.Add(tsb);
                                        }
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

        public static bool timeSeriesSampling(bool final)
        {
            int wait = 0;

            foreach (var tsb in timeSeriesBlockList)
            {
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

                if (tsb.sampleRate != null)
                    wait = Convert.ToInt32(tsb.sampleRate.value);

                int actualSamples = Convert.ToInt32(tsb.actualSamples.value);
                int maxSamples = Convert.ToInt32(tsb.maxSamples.value);
                int actualSamplesInCollection = Convert.ToInt32(tsb.actualSamplesInCollection.value);
                int maxSamplesInCollection = Convert.ToInt32(tsb.maxSamplesInCollection.value);

                if (final || actualSamples < maxSamples)
                {
                    if (!final)
                    {
                        for (int i = 0; i < tsb.samplesProperties.Count; i++)
                        {
                            var p = tsb.samplesProperties[i];
                            if (tsb.samplesValues[i] != "")
                            {
                                tsb.samplesValues[i] += ",";
                            }
                            // tsb.samplesValues[i] += p.value;
                            tsb.samplesValues[i] += dummy++;
                        }
                        tsb.samplesValuesCount++;
                        actualSamples++;
                        tsb.actualSamples.value = "" + actualSamples;
                        actualSamplesInCollection++;
                        tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                        Program.signalNewData(0);
                        if (actualSamples >= maxSamples)
                        {
                            if (tsb.sampleMode.value == "continuous")
                            {
                                var first = 
                                    tsb.data.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>(
                                        "data" + tsb.lowDataIndex.value);
                                if (first != null)
                                {
                                    actualSamples -= maxSamplesInCollection;
                                    tsb.actualSamples.value = "" + actualSamples;
                                    tsb.data.Remove(first);
                                    tsb.lowDataIndex.value = "" + (Convert.ToInt32(tsb.lowDataIndex.value) + 1);
                                    Program.signalNewData(1);
                                }
                            }
                        }
                    }
                    if (final || actualSamplesInCollection >= maxSamplesInCollection)
                    {
                        if (actualSamplesInCollection > 0)
                        {
                            if (tsb.highDataIndex != null)
                                tsb.highDataIndex.value = "" + tsb.samplesCollectionsCount;
                            var nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                var p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                                p.value = tsb.samplesValues[i];
                                tsb.samplesValues[i] = "";
                                nextCollection.Add(p);
                            }
                            tsb.data.Add(nextCollection);
                            tsb.samplesValuesCount = 0;
                            actualSamplesInCollection = 0;
                            tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                            Program.signalNewData(1);
                        }
                    }
                }
            }
            if (wait != 0)
            {
                if (!test)
                    Thread.Sleep(wait);
            }
            return !final;
        }
    }
}