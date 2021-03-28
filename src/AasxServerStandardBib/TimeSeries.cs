using AdminShellNS;
using System;
using System.Collections.Generic;

namespace AasxTimeSeries
{
    public static class TimeSeries
    {
        public class TimeSeriesBlock
        {
            public AdminShell.SubmodelElementCollection block = null;
            public AdminShell.SubmodelElementCollection data = null;
            public int maxSamples = 100;
            public int maxSamplesPerCollection = 10;
            public int samplesCount;
            public List<AdminShell.Property> samplesProperties = null;
            public List<string> samplesValues = null;
            public int samplesValuesCount = 0;
            public int samplesCollectionsCount = 0;
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
                                                case "maxSamples":
                                                    if (sme2 is AdminShell.Property p2)
                                                    {
                                                        tsb.maxSamples = Convert.ToInt32(p2.value);
                                                    }
                                                    break;
                                                case "maxSamplesPerCollection":
                                                    if (sme2 is AdminShell.Property p3)
                                                    {
                                                        tsb.maxSamplesPerCollection = Convert.ToInt32(p3.value);
                                                    }
                                                    break;
                                                case "data":
                                                    if (sme2 is AdminShell.SubmodelElementCollection c)
                                                    {
                                                        tsb.data = c;
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
            dummy = 0;

            for (int i = 0; i < 1000; i++)
            {
                timeSeriesSampling();
            }
            timeSeriesSampling(true);
        }

        static int dummy = 0;

        public static void timeSeriesSampling(bool final = false)
        {
            foreach (var tsb in timeSeriesBlockList)
            {
                if (final || tsb.samplesCount+tsb.samplesValuesCount < tsb.maxSamples)
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
                    }
                    if (final || tsb.samplesValuesCount >= tsb.maxSamplesPerCollection)
                    {
                        var nextCollection = AdminShell.SubmodelElementCollection.CreateNew("Data" + tsb.samplesCollectionsCount++);
                        for (int i = 0; i < tsb.samplesProperties.Count; i++)
                        {
                            var p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                            p.value = tsb.samplesValues[i];
                            tsb.samplesValues[i] = "";
                            nextCollection.Add(p);
                        }
                        tsb.data.Add(nextCollection);
                        tsb.samplesCount += tsb.samplesValuesCount;
                        tsb.samplesValuesCount = 0;
                    }
                }
            }
        }
    }
}