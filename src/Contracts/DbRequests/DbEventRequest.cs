namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.Events;

public class DbEventRequest
{
    public AdminShellPackageEnv[] Env { get; set; }

    public int PackageIndex { get; set; }

    public string EventName { get; set; }

    public ISubmodel Submodel { get; set; }

    //For Read request
    public bool IsWithPayload { get; set; }

    public int LimitSm { get; set; }

    public int LimitSme { get; set; }

    public int OffsetSm { get; set; }

    public int OffsetSme { get; set; }

    public string Diff { get; set; } = string.Empty;

    //For Update request
    public string Body { get; set; } = string.Empty;
}
