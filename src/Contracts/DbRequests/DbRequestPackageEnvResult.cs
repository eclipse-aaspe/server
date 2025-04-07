namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

public class DbRequestPackageEnvResult
{
    public AdminShellPackageEnv PackageEnv { get; set; }

    public string EnvFileName { get; set; }
}
