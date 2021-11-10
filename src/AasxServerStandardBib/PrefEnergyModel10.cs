using System;
using System.Collections.Generic;
using System.Text;
using AdminShellNS;

namespace AasxDemonstration
{
    public static class PrefEnergyModel10
    {
        public static AdminShell.Key SM_EnergyModel =
            new AdminShell.Key(AdminShell.Key.Submodel, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/pi40/CarbonMonitoring/1/0");

        public static string QualiADXDataPoint = "ADX";

        public static string QualiADXHubSeries = "ADXSeries";
    }
}
