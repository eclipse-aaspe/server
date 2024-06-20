using System;

namespace AasxServerBlazor.Data
{
    public class BlazorSessionService : IDisposable
    {
        public static int sessionCounter;
        public int sessionNumber;

        public BlazorSessionService()
        {
            sessionNumber = ++sessionCounter;
        }

        public void Dispose()
        {
            System.IO.File.Delete($@"wwwroot/images/scottplot/smc_timeseries_clientid{sessionNumber}.png");
        }
    }
}
