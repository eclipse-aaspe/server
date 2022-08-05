using System;
using System.Collections.Generic;
using System.Text;
using AasCore.Aas3_0_RC02;
using AdminShellNS;

namespace AasxDemonstration
{
    public static class PrefEnergyModel10
    {
        public static Key SM_EnergyModel =
            new Key(KeyTypes.Submodel,
            "https://admin-shell.io/sandbox/pi40/CarbonMonitoring/1/0");

        public static string QualiIoTHubDataPoint = "IoTHub";

        public static string QualiIoTHubSeries = "IoTHubSeries";
    }
}
