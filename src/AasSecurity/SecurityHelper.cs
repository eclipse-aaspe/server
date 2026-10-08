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
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace AasSecurity
{
    /**
     * This class initiates the security from Program.cs
     */
    public static class SecurityHelper
    {
        //private static ILogger _logger = ApplicationLogging.CreateLogger("SecurityHelper");

        public static void SecurityInit()
        {
            GlobalSecurityVariables.WithAuthentication = !Program.noSecurity;
            //_logger.LogInformation($"IsSecurityEnabled: {GlobalSecurityVariables.WithAuthentication}");
            ParseSecurityMetamodel();
        }

        private static void ParseSecurityMetamodel()
        {
            if (Program.env == null)
            {
                return;
            }

            foreach (var env in Program.env)
            {
                if (env != null && env.AasEnv != null && env.AasEnv.AssetAdministrationShells != null)
                {
                    foreach (var aas in env.AasEnv.AssetAdministrationShells)
                    {
                        if (aas != null && aas.Submodels != null)
                        {
                            foreach (var submodelReference in aas.Submodels)
                            {
                                var submodel = env.AasEnv.FindSubmodel(submodelReference);
                                if (submodel != null && !string.IsNullOrEmpty(submodel.IdShort))
                                {
                                    switch (submodel.IdShort.ToLower())
                                    {
                                        case "securitysettingsforserver":
                                            {
                                                //_logger.LogDebug($"Parsing the submodel {submodel.IdShort}");
                                                SecuritySettingsForServerParser.ParseSecuritySettingsForServer(env, submodel);
                                            }
                                            break;
                                        case "securitymetamodelforaas":
                                        case "securitymetamodelforserver":
                                            {
                                                //_logger.LogDebug($"Parsing the submodel {submodel.IdShort}");
                                                SecurityMetamodelParser.ParserSecurityMetamodel(env, submodel);
                                            }
                                            break;
                                        default:
                                            {

                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static X509Certificate2? FindServerCertificate(string serverName, out string domain)
        {
            domain = "";
            foreach (var entry in GlobalSecurityVariables.TrustedServers)
            {
                if (entry.Certificate != null && Path.GetFileName(entry.CertFileName) == serverName + ".cer")
                {
                    domain = entry.Domain;
                    return entry.Certificate;
                }
            }

            return null;
        }

        internal static TrustedServer? FindTrustedJwksEntry(string? kid, string? iss)
            => FindTrustedJwksEntry(GlobalSecurityVariables.TrustedServers, kid, iss);

        /// <summary>
        /// Finds the trust list entry for a token: first by issuer (iss claim), then by kid header.
        /// Empty values never match.
        /// </summary>
        internal static TrustedServer? FindTrustedJwksEntry(IReadOnlyList<TrustedServer> entries, string? kid, string? iss)
        {
            var normalizedIss = NormalizeIssuer(iss);
            if (normalizedIss != "")
            {
                var byIssuer = entries.FirstOrDefault(e => e.Issuer != "" && NormalizeIssuer(e.Issuer) == normalizedIss);
                if (byIssuer != null)
                {
                    return byIssuer;
                }
            }

            if (!string.IsNullOrEmpty(kid))
            {
                return entries.FirstOrDefault(e => e.Kid != "" && e.Kid == kid);
            }

            return null;
        }

        internal static string NormalizeIssuer(string? issuer) => (issuer ?? "").Trim().TrimEnd('/');
    }
}
