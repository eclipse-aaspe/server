using AasSecurity.Models;
using AasxServer;
using AdminShellNS;
using System.Security.Cryptography.X509Certificates;

namespace AasSecurity;

internal static class SecuritySettingsForServerParser
{
    internal static void ParseSecuritySettingsForServer(AdminShellPackageEnv env, ISubmodel submodel)
    {
        if (submodel is not {SubmodelElements: not null})
        {
            return;
        }

        foreach (var submodelElement in submodel.SubmodelElements)
        {
            switch (submodelElement.IdShort!.ToLower())
            {
                case "authenticationserver":
                {
                    if (submodelElement is SubmodelElementCollection authServer)
                    {
                        ParseAuthenticationServer(env, authServer);
                    }
                }
                    break;
                case "rolemapping":
                {
                    if (submodelElement is SubmodelElementCollection roleMappings)
                    {
                        ParseRoleMappings(roleMappings);
                    }
                }
                    break;
                case "basicauth":
                {
                    if (submodelElement is SubmodelElementCollection basicAuth)
                    {
                        ParseBasicAuth(basicAuth);
                    }

                    break;
                }
            }
        }
    }

    private static void ParseBasicAuth(ISubmodelElementCollection basicAuth)
    {
        if (basicAuth is not {Value: not null})
        {
            return;
        }

        foreach (var submodelElement in basicAuth.Value)
        {
            if (submodelElement is Property property)
            {
                GlobalSecurityVariables.SecurityUsernamePassword.TryAdd(property.IdShort!, property.Value!);
            }
        }
    }

    private static void ParseAuthenticationServer(AdminShellPackageEnv env, SubmodelElementCollection authServer)
    {
        if (authServer is not {Value: not null})
        {
            return;
        }

        foreach (var submodelElement in authServer.Value)
        {
            if (submodelElement.IdShort != null)
                switch (submodelElement.IdShort.ToLower())
                {
                    case "endpoint":
                    {
                        if (submodelElement is Property endpoint)
                        {
                            Program.redirectServer = endpoint.Value;
                        }

                        break;
                    }
                    case "type":
                    {
                        if (submodelElement is Property authType)
                        {
                            Program.authType = authType.Value;
                        }

                        break;
                    }
                    case "publiccertificate":
                    {
                        if (submodelElement is AasCore.Aas3_0.File publicCert)
                        {
                            var certStream = env.GetLocalStreamFromPackage(publicCert.Value, init: true);
                            if (certStream != null)
                            {
                                using var memoryStream = new MemoryStream();
                                certStream.CopyTo(memoryStream);
                                var buffer = memoryStream.GetBuffer();
                                GlobalSecurityVariables.ServerCertificates.Add(new X509Certificate2(buffer));
                                var split = publicCert.Value?.Split('/');
                                GlobalSecurityVariables.ServerCertFileNames.Add(split?[3]);
                            }
                        }

                        break;
                    }
                }
        }
    }

    private static void ParseRoleMappings(ISubmodelElementCollection roleMappings)
    {
        if (roleMappings is not {Value: not null})
        {
            return;
        }

        GlobalSecurityVariables.SecurityRights = new List<SecurityRight>();
        foreach (var submodelElement in roleMappings.Value)
        {
            if (submodelElement is not SubmodelElementCollection {Value: not null} roleMapping)
            {
                continue;
            }

            var subjects = new List<string>();
            foreach (var roleMappingElement in roleMapping.Value)
            {
                switch (roleMappingElement.IdShort?.ToLower())
                {
                    case "subjects":
                    {
                        if (roleMappingElement is SubmodelElementCollection {Value: not null} subjectElements)
                            foreach (var subjectElement in subjectElements.Value)
                            {
                                if (subjectElement is Property subject)
                                {
                                    switch (subject.IdShort?.ToLower())
                                    {
                                        case "emaildomain":
                                        case "email":
                                        {
                                            subjects.Add(subject.Value);
                                            break;
                                        }
                                        default:
                                        {
                                            subjects.Add(subject.IdShort);
                                            break;
                                        }
                                    }
                                }
                            }

                        break;
                    }
                    case "roles":
                    {
                        if (roleMappingElement is SubmodelElementCollection {Value: not null} roleElements)
                            foreach (var roleElement in roleElements.Value)
                            {
                                if (roleElement is not Property role)
                                {
                                    continue;
                                }

                                foreach (var securityRight in subjects.Select(subject => new SecurityRight
                                         {
                                             Name = subject,
                                             Role = role.IdShort
                                         }))
                                {
                                    GlobalSecurityVariables.SecurityRights.Add(securityRight);
                                }
                            }

                        break;
                    }
                }
            }
        }
    }
}