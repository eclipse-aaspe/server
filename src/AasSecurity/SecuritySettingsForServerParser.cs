/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using AasSecurity.Models;
using AasxServer;
using AdminShellNS;
using System.Security.Cryptography.X509Certificates;

namespace AasSecurity
{
    internal static class SecuritySettingsForServerParser
    {
        //private static ILogger _logger = ApplicationLogging.CreateLogger("SecuritySettingsForServerParser");

        internal static void ParseSecuritySettingsForServer(AdminShellPackageEnv env, ISubmodel submodel)
        {
            if (submodel != null && submodel.SubmodelElements != null)
            {
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
                        default:
                            {
                                //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing SecuritySettingsForServer.");
                                break;
                            }
                    }
                }
            }
        }

        private static void ParseBasicAuth(SubmodelElementCollection basicAuth)
        {
            if (basicAuth != null && basicAuth.Value != null)
            {
                foreach (var submodelElement in basicAuth.Value)
                {
                    if (submodelElement is Property property)
                    {
                        GlobalSecurityVariables.SecurityUsernamePassword.TryAdd(property.IdShort!, property.Value!);
                    }
                }
            }
        }

        private static void ParseAuthenticationServer(AdminShellPackageEnv env, SubmodelElementCollection? authServer)
        {
            if (authServer == null || authServer.Value == null)
            {
                return;
            }

            foreach (var submodelElement in authServer.Value)
            {
                switch (submodelElement.IdShort?.ToLower())
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
                                using (var memoryStream = new MemoryStream())
                                {
                                    certStream.CopyTo(memoryStream);
                                    var buffer = memoryStream.GetBuffer();
                                    GlobalSecurityVariables.ServerCertificates.Add(new X509Certificate2(buffer));
                                    string[] split = publicCert.Value.Split('/');
                                    GlobalSecurityVariables.ServerCertFileNames.Add(split[3]);
                                    //_logger.LogDebug($"Loaded auth server certificate: {split[3]}");
                                }
                            }
                        }
                        break;
                    }
                    default:
                    {
                        //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing AuthenticationServer.");
                        break;
                    }
                }
            }
        }

        private static void ParseRoleMappings(SubmodelElementCollection roleMappings)
        {
            if (roleMappings == null || roleMappings.Value == null)
            {
                return;
            }

            GlobalSecurityVariables.SecurityRights = new List<SecurityRight>();
            foreach (var submodelElement in roleMappings.Value)
            {
                if (submodelElement is SubmodelElementCollection roleMapping)
                {
                    if (roleMapping != null && roleMapping.Value != null)
                    {
                        var subjects = new List<string>();
                        foreach (var roleMappingElement in roleMapping.Value)
                        {
                            switch (roleMappingElement.IdShort?.ToLower())
                            {
                                case "subjects":
                                {
                                    if (roleMappingElement is SubmodelElementCollection subjectElements)
                                    {
                                        foreach (var subjectElement in subjectElements.Value)
                                        {
                                            if (subjectElement != null && subjectElement is Property subject)
                                            {
                                                switch (subject.IdShort.ToLower())
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
                                    }
                                    break;
                                }
                                case "roles":
                                {
                                    if (roleMappingElement is SubmodelElementCollection roleElements)
                                    {
                                        foreach (var roleElement in roleElements.Value)
                                        {
                                            if (roleElement != null && roleElement is Property role)
                                            {
                                                foreach (var subject in subjects)
                                                {
                                                    var securityRight = new SecurityRight
                                                                        {
                                                                            Name = subject,
                                                                            Role = role.IdShort
                                                                        };
                                                    GlobalSecurityVariables.SecurityRights.Add(securityRight);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }

                                default:
                                {
                                    //_logger.LogError($"Unhandled submodel element {submodelElement.IdShort} while parsing RoleMappings.");
                                    break;
                                }
                            }
                        }
                    }

                }
            }
        }


    }
}