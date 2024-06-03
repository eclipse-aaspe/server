using AasxServer;
using Extensions;
using System.Security.Cryptography.X509Certificates;
using AdminShellNS;

namespace AasSecurity;

public static class SecurityHelper
{
    public static void SecurityInit()
    {
        GlobalSecurityVariables.WithAuthentication = !Program.noSecurity;
        ParseSecurityMetamodel();
    }

    private static void ParseSecurityMetamodel()
    {
        foreach (var env in Program.env.Where(IsValidEnvironment))
        {
            foreach (var submodel in GetAllValidSubmodels(env))
            {
                ProcessSecuritySubmodel(env, submodel);
            }
        }
    }

    private static bool IsValidEnvironment(AdminShellPackageEnv env)
    {
        return env?.AasEnv?.AssetAdministrationShells != null;
    }

    private static IEnumerable<ISubmodel> GetAllValidSubmodels(AdminShellPackageEnv env)
    {
        return (env.AasEnv.AssetAdministrationShells ?? new List<IAssetAdministrationShell>())
            .Where(aas => aas?.Submodels != null)
            .SelectMany(aas => (aas.Submodels ?? new List<IReference>()).Select(submodelReference => env.AasEnv.FindSubmodel(submodelReference)))
            .Where(submodel => !string.IsNullOrEmpty(submodel.IdShort));
    }

    private static void ProcessSecuritySubmodel(AdminShellPackageEnv env, ISubmodel submodel)
    {
        switch (submodel.IdShort?.ToLower())
        {
            case "securitysettingsforserver":
                SecuritySettingsForServerParser.ParseSecuritySettingsForServer(env, submodel);
                break;
            case "securitymetamodelforaas":
            case "securitymetamodelforserver":
                SecurityMetamodelParser.ParserSecurityMetamodel(env, submodel);
                break;
        }
    }

    public static X509Certificate2? FindServerCertificate(string serverName)
    {
        for (var i = 0; i < GlobalSecurityVariables.ServerCertFileNames.Count; i++)
        {
            if (Path.GetFileName(GlobalSecurityVariables.ServerCertFileNames[i]).Equals($"{serverName}.cer", StringComparison.OrdinalIgnoreCase))
            {
                return GlobalSecurityVariables.ServerCertificates[i];
            }
        }

        return null;
    }
}