using AasxServer;
using System;

namespace AasxTimeSeries
{
    public enum TimeSeriesDestFormat { Plain, TimeSeries10 }

    public static class TimeSeries
    {
        public static void timeSeriesInit()
        {
            DateTime timeStamp = DateTime.UtcNow;

            for (int i = 0; i < Program.env.Length; i++)
            {
                var env = Program.env[i];
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
                            }
                        }
                    }
                }
            }
        }
    }
}
