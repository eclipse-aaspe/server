using System;
using System.Collections.Generic;
using System.Text;

namespace AasxServerBlazor.Data
{
    public class BlazorSessionService : IDisposable
    {
        public static int sessionCounter = 0;
        public int sessionNumber = 0;
        public static int sessionTotal = 0;

        public BlazorSessionService()
        {
            sessionNumber = ++sessionCounter;
            sessionTotal++;
        }

        public void Dispose()
        {
            System.IO.File.Delete(@"wwwroot/images/scottplot/smc_timeseries_clientid" + sessionNumber + ".png");
            sessionTotal--;
        }
    }
}
