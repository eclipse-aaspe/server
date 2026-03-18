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

using System;
using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using AasSecurity.Models;
using AasxServer;
using AdminShellNS;
using IdentityModel;
using Namotion.Reflection;

namespace AasSecurity
{
    internal static class SecuritySettingsForServerParser
    {
        //private static ILogger _logger = ApplicationLogging.CreateLogger("SecuritySettingsForServerParser");

        /// <summary>
        /// Import the RSA key from a pem file
        /// </summary>
        /// <param name="pemFile">public key provided as pem file</param>
        /// <returns>RSA key object instance</returns>
        private static RSA ReadPem(string privateKeyPemContent)
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPemContent.AsSpan());

            return rsa;
        }

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

        private async static void ParseAuthenticationServer(AdminShellPackageEnv env, SubmodelElementCollection? authServer)
        {
            var trustListOnServer = System.Environment.GetEnvironmentVariable("TRUST_LIST");
            var trustListPubKeyOnServer = System.Environment.GetEnvironmentVariable("TRUST_LIIST_PUBLIC_KEY");

            if (System.IO.File.Exists("trustlist.txt"))
            {
                Console.WriteLine("Read trustlist.txt");
                var lines = System.IO.File.ReadAllLines("trustlist.txt");
                {
                    var serverName = "";
                    var domain = "";
                    var base64 = "";
                    var insideBas64 = false;
                    var jwks = "";
                    var kid = "";
                    foreach (var line in lines)
                    {
                        if (line == "" || line.StartsWith("# "))
                        {
                            continue;
                        }

                        if (line.Contains("serverName: "))
                        {
                            var split = line.Split(": ");
                            serverName = split[1];
                            Console.WriteLine(" serverName: " + serverName);
                        }
                        else if (line.Contains("domain: "))
                        {
                            var split = line.Split(": ");
                            domain = split[1];
                            Console.WriteLine("  domain: " + domain);
                        }
                        else if (line.Contains("jwks: "))
                        {
                            var split = line.Split(": ");
                            jwks = split[1];
                            Console.WriteLine("  jwks: " + jwks);
                        }
                        else if (line.Contains("kid: "))
                        {
                            var split = line.Split(": ");
                            kid = split[1];
                            Console.WriteLine("  kid: " + kid);
                            GlobalSecurityVariables.ServerCertificates.Add(null);
                            GlobalSecurityVariables.ServerCertFileNames.Add("");
                            GlobalSecurityVariables.ServerDomain.Add(domain);
                            GlobalSecurityVariables.ServerJwksUrl.Add(jwks);
                            GlobalSecurityVariables.ServerKid.Add(kid);
                        }
                        else if (line.Contains("BEGIN CERTIFICATE"))
                        {
                            insideBas64 = true;
                            base64 = "";
                        }
                        else if (line.Contains("END CERTIFICATE"))
                        {
                            insideBas64 = false;
                            base64 = base64.Replace("\r", "").Replace("\n", "").Trim();
                            var certBytes = Convert.FromBase64String(base64);
                            var x509 = new X509Certificate2(certBytes);
                            GlobalSecurityVariables.ServerCertificates.Add(x509);
                            GlobalSecurityVariables.ServerCertFileNames.Add(serverName + ".cer");
                            GlobalSecurityVariables.ServerDomain.Add(domain);
                            GlobalSecurityVariables.ServerJwksUrl.Add("");
                            GlobalSecurityVariables.ServerKid.Add("");
                        }
                        else if (insideBas64)
                        {
                            base64 += line;
                        }
                    }
                }
            }

            XDocument doc = null;

            // Create a new XML document.
            XmlDocument xmlDoc = new()
            {
                // Load an XML file into the XmlDocument object.
                PreserveWhitespace = true
            };

            var handlerExchange = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
            using var client = new HttpClient(handlerExchange);

            if (!string.IsNullOrEmpty(trustListOnServer))
            {
                try
                {
                    var response = await client.GetAsync(trustListOnServer);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStreamAsync();

                    xmlDoc.Load(content);

                    content.Position = 0;

                    doc = XDocument.Load(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            var localTrustListPath = "trustlist.xml";

            if (doc == null && System.IO.File.Exists(localTrustListPath))
            {
                // Load the XML
                doc = XDocument.Load(localTrustListPath);
                xmlDoc.Load(localTrustListPath);
            }

            if (doc != null)
            {
                string pemContent = null;

                if (!string.IsNullOrEmpty(trustListPubKeyOnServer))
                {
                    var response = await client.GetAsync(trustListPubKeyOnServer);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    pemContent = content;
                }

                if (pemContent == null)
                {
                    var localPemFilePath = "publickey.pem";

                    if (System.IO.File.Exists(localPemFilePath))
                    {
                        pemContent = System.IO.File.ReadAllText(localPemFilePath);
                    }
                }

                if (pemContent != null)
                {
                    RSA rsa = ReadPem(pemContent);

                    bool sigValidation = TrustListVerifier.VerifyXmlSignature(xmlDoc, rsa);

                    if (sigValidation)
                    {
                        Console.WriteLine("Signature could get validated");
                    }
                    else
                    {
                        Console.WriteLine("Signature validation failed");
                        return;
                    }
                }

                // Default ETSI namespace present in the document
                XNamespace ns = "http://uri.etsi.org/02231/v2#"; // <- critical
                                                                 // (There's also an XMLDSIG signature later in a different ns; we can ignore it.)

                // Navigate to <TrustServiceProviderList>
                var tspList = doc
                    .Root                             // <TrustServiceStatusList>
                    ?.Element(ns + "TrustServiceProviderList"); // <TrustServiceProviderList>


                // Enumerate <TrustServiceProvider>
                var providers = tspList.Elements(ns + "TrustServiceProvider")
                    .Select(tsp => new
                    {
                        TspName = tsp
                            .Element(ns + "TSPInformation")
                            ?.Element(ns + "TSPName")
                            ?.Elements(ns + "Name")
                            .Select(n => (string)n)
                            .FirstOrDefault(),

                        Domain = tsp
                            .Element(ns + "TSPInformation")
                            ?.Element(ns + "TSPInformationExtensions")
                            ?.Elements(ns + "Extension")
                            .Elements(ns + "TSPDomainName")
                            .Select(x => (string)x)
                            .FirstOrDefault(),

                        // Grab one service’s basics (if available)
                        Service = tsp
                            .Element(ns + "TSPServices")
                            ?.Elements(ns + "TSPService")
                            .Select(svc => new
                            {
                                Type = (string)svc
                                    .Element(ns + "ServiceInformation")
                                    ?.Element(ns + "ServiceTypeIdentifier"),

                                Name = svc
                                    .Element(ns + "ServiceInformation")
                                    ?.Element(ns + "ServiceName")
                                    ?.Elements(ns + "Name")
                                    .Select(n => (string)n)
                                    .FirstOrDefault(),

                                SupplyPoint = svc
                                    .Element(ns + "ServiceInformation")
                                    ?.Element(ns + "ServiceSupplyPoints")
                                    ?.Elements(ns + "ServiceSupplyPoint")
                                    .Select(sp => (string)sp)
                                    .FirstOrDefault()
                            })
                            .ToList()
                    })
                    .ToList();

                foreach (var provider in providers)
                {
                    foreach (var service in provider?.Service)
                    {
                        var serverName = service?.Name;
                        Console.WriteLine(" serverName: " + serverName);

                        var domain = provider?.Domain;
                        Console.WriteLine("  domain: " + domain);

                        var issuer = service?.SupplyPoint;
                        Console.WriteLine("  issuer url: " + issuer);

                        var kid = "";

                        GlobalSecurityVariables.ServerCertificates.Add(null);
                        GlobalSecurityVariables.ServerCertFileNames.Add("");
                        GlobalSecurityVariables.ServerCertFileNames.Add(serverName + ".cer");
                        GlobalSecurityVariables.ServerDomain.Add(domain);
                        GlobalSecurityVariables.ServerIssuerUrl.Add(issuer);
                        GlobalSecurityVariables.ServerKid.Add(kid);
                    }
                }
            }

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