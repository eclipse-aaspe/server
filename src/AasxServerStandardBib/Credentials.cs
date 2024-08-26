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
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using IdentityModel.Client;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AasxServer
{
    public class AasxCredentialsEntry
    {

        // input
        public string urlPrefix = string.Empty;
        public string type = string.Empty;
        public List<string> parameters = new List<string>();
        // store state
        public string bearer = string.Empty;
        public DateTime bearerValidFrom = DateTime.MinValue;
        public DateTime bearerValidTo = DateTime.MinValue;
    }

    public class cs
    {
        // static for the moment
        // service already prepared CredentialService.cs
        public static List<AasxServer.AasxCredentialsEntry> credentials = new List<AasxServer.AasxCredentialsEntry>();
    }

    public class AasxCredentials
    {
        public static void initEmpty(List<AasxCredentialsEntry> cList)
        {
            cList.Clear();
        }
        public static void initByFile(List<AasxCredentialsEntry> cList, string fileName)
        {
            cList.Clear();
            if (System.IO.File.Exists(fileName))
            {
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(fileName))
                    {
                        var line = sr.ReadLine();
                        while (line != null)
                        {
                            if (line != "" && line.Substring(0, 1) != "#")
                            {
                                var cols = line.Split(',');
                                if (cols.Length > 2)
                                {
                                    var c = new AasxCredentialsEntry();
                                    c.urlPrefix = cols[0];
                                    c.type = cols[1];
                                    for (int i = 2; i < cols.Length; i++)
                                        c.parameters.Add(cols[i]);
                                    cList.Add(c);
                                }
                            }
                            line = sr.ReadLine();
                        }
                    }
                    Console.WriteLine("CREDENTIALS " + fileName + ":" + cList.Count + " entries read");
                }
                catch (IOException e)
                {
                    Console.WriteLine(fileName + " could not be read!");
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }
        public static void initAnonymous(List<AasxCredentialsEntry> cList)
        {
            initByFile(cList, "CREDENTIALS-ANONYMOUS.DAT");
        }

        public static void initByEmail(List<AasxCredentialsEntry> cList, string email)
        {
            initAnonymous(cList);
            var c = new AasxCredentialsEntry();
            c.urlPrefix = "*";
            c.type = "email";
            c.parameters.Add(email);
            cList.Add(c);
        }

        public static void initByUserPW(List<AasxCredentialsEntry> cList, string user, string pw)
        {
            initAnonymous(cList);
            var c = new AasxCredentialsEntry();
            c.urlPrefix = "*";
            c.type = "userpw";
            c.parameters.Add(user);
            c.parameters.Add(pw);
            cList.Add(c);
        }

        public static void initByEdc(List<AasxCredentialsEntry> cList, string user, string pw, string urlEdcWrapper)
        {
            initAnonymous(cList);
            var c = new AasxCredentialsEntry();
            c.urlPrefix = "*";
            c.type = "edc";
            c.parameters.Add(user);
            c.parameters.Add(pw);
            c.parameters.Add(urlEdcWrapper);
            cList.Add(c);
        }

        public static bool get(List<AasxCredentialsEntry> cList, string urlPath, out string queryPara, out string userPW,
            out string urlEdcWrapper, out string replace, bool blazor = false)
        {
            queryPara = "";
            userPW = "";
            urlEdcWrapper = "";
            replace = "";
            List<string> qp = new List<string>();
            bool result = false;

            for (int i = 0; i < cList.Count; i++)
            {
                int lenPrefix = cList[i].urlPrefix.Length;
                int lenUrl = urlPath.Length;
                if (lenPrefix <= lenUrl)
                {
                    string u = urlPath.Substring(0, lenPrefix);
                    if (cList[i].urlPrefix == "*" || u == cList[i].urlPrefix)
                    {
                        switch (cList[i].type)
                        {
                            case "email":
                                qp.Add("Email=" + cList[i].parameters[0]);
                                result = true;
                                break;
                            case "basicauth": // for http header
                            case "userpw": // as query parameter _up
                                if (cList[i].parameters.Count == 2)
                                {
                                    var upw = cList[i].parameters[0] + ":" + cList[i].parameters[1];
                                    var bytes = Encoding.ASCII.GetBytes(upw);
                                    var basicAuth64 = Convert.ToBase64String(bytes);
                                    switch (cList[i].type)
                                    {
                                        case "basicauth": // for http header
                                            userPW = basicAuth64;
                                            break;
                                        case "userpw": // as query parameter _up
                                            qp.Add("_up=" + basicAuth64);
                                            break;
                                    }
                                    result = true;
                                }
                                break;
                            case "bearer":
                                bearerCheckAndInit(cList[i]);
                                qp.Add("bearer=" + cList[i].bearer);
                                result = true;
                                break;
                            case "querypara":
                                if (cList[i].parameters.Count == 2)
                                {
                                    qp.Add(cList[i].parameters[0] + "=" + cList[i].parameters[1]);
                                    result = true;
                                }
                                break;
                            case "edc":
                                if (cList[i].parameters.Count == 3)
                                {
                                    var upw = cList[i].parameters[0] + ":" + cList[i].parameters[1];
                                    var bytes = Encoding.ASCII.GetBytes(upw);
                                    var basicAuth64 = Convert.ToBase64String(bytes);
                                    userPW = basicAuth64;
                                    // urlEdcWrapper = cList[i].parameters[2];
                                    urlEdcWrapper = urlPath.Replace(u, cList[i].parameters[2]);
                                }
                                result = true;
                                break;
                            case "replace":
                                if (cList[i].parameters.Count == 1 ||
                                    (cList[i].parameters.Count == 2 && cList[i].parameters[1] == "blazor" && blazor))
                                {
                                    replace = urlPath.Replace(u, cList[i].parameters[0]);
                                    result = true;
                                }
                                break;
                        }
                    }
                }
            }
            for (int i = 0; i < qp.Count; i++)
            {
                if (i == 0)
                    queryPara = qp[0];
                else
                    queryPara += "&" + qp[i];
            }

            return result;
        }

        public static void bearerCheckAndInit(AasxCredentialsEntry c)
        {
            // check if existing bearer is still valid
            if (c.bearer != string.Empty)
            {
                bool valid = true;
                if (c.bearerValidFrom > DateTime.UtcNow || c.bearerValidTo < DateTime.UtcNow)
                    valid = false;
                if (valid)
                    return;
            }

            string authServerEndPoint = c.parameters[0];
            string clientCertificate = c.parameters[1];
            string clientCertificatePW = c.parameters[2];

            if (authServerEndPoint != null && clientCertificate != null && clientCertificatePW != null)
            {
                Console.WriteLine("authServerEndPoint " + authServerEndPoint);
                Console.WriteLine("clientCertificate " + clientCertificate);
                Console.WriteLine("clientCertificatePW " + clientCertificatePW);

                if (c.bearer != string.Empty)
                {
                    bool valid = true;
                    var jwtToken = new JwtSecurityToken(c.bearer);
                    if ((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow))
                        valid = false;
                    if (valid) return;
                }

                var handler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = delegate { return true; },
                };
                if (AasxServer.AasxTask.proxy != null)
                    handler.Proxy = AasxServer.AasxTask.proxy;
                else
                    handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                var client = new HttpClient(handler);
                DiscoveryDocumentResponse disco = null;

                client.Timeout = TimeSpan.FromSeconds(20);
                var task = Task.Run(async () => { disco = await client.GetDiscoveryDocumentAsync(authServerEndPoint); });
                task.Wait();
                if (disco.IsError) return;
                Console.WriteLine("OpenID Discovery JSON:");
                Console.WriteLine(disco.Raw);

                if (!System.IO.File.Exists(clientCertificate))
                {
                    Console.WriteLine(clientCertificate + " does not exist!");
                    return;
                }

                string[] x5c = null;
                X509Certificate2 certificate = new X509Certificate2(clientCertificate, clientCertificatePW);
                if (certificate != null)
                {
                    X509Certificate2Collection xc = new X509Certificate2Collection();
                    xc.Import(clientCertificate, clientCertificatePW, X509KeyStorageFlags.PersistKeySet);

                    string[] X509Base64 = new string[xc.Count];

                    int j = xc.Count;
                    var xce = xc.GetEnumerator();
                    for (int i = 0; i < xc.Count; i++)
                    {
                        xce.MoveNext();
                        X509Base64[--j] = Convert.ToBase64String(xce.Current.GetRawCertData());
                    }
                    x5c = X509Base64;

                    var credential = new X509SigningCredentials(certificate);
                    string clientId = "client.jwt";
                    string email = "";
                    string subject = certificate.Subject;
                    var split = subject.Split(new Char[] { ',' });
                    if (split[0] != "")
                    {
                        var split2 = split[0].Split(new Char[] { '=' });
                        if (split2[0] == "E")
                        {
                            email = split2[1];
                        }
                    }
                    Console.WriteLine("email: " + email);

                    var now = DateTime.UtcNow;
                    var token = new JwtSecurityToken(
                        clientId,
                        disco.TokenEndpoint,
                        new List<Claim>()
                        {
                            new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                            new Claim(JwtClaimTypes.Subject, clientId),
                            new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                            // OZ
                            new Claim(JwtClaimTypes.Email, email)
                        },
                        now,
                        now.AddMinutes(1),
                        credential);

                    token.Header.Add("x5c", x5c);
                    var tokenHandler = new JwtSecurityTokenHandler();
                    string clientToken = tokenHandler.WriteToken(token);

                    TokenResponse response = null;
                    task = Task.Run(async () =>
                    {
                        response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                        {
                            Address = disco.TokenEndpoint,
                            Scope = "resource1.scope1",

                            ClientAssertion =
                                        {
                                            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                                            Value = clientToken
                                        }
                        });
                    });
                    task.Wait();

                    if (response.IsError) return;

                    c.bearer = response.AccessToken;
                    Console.WriteLine("bearer = " + c.bearer);
                    var jwtToken = new JwtSecurityToken(c.bearer);
                    if (jwtToken == null)
                    {
                        c.bearer = string.Empty;
                        return;
                    }
                    c.bearerValidFrom = jwtToken.ValidFrom;
                    c.bearerValidTo = jwtToken.ValidTo;
                    Console.WriteLine("Valid from: " + jwtToken.ValidFrom);
                    Console.WriteLine("Valid to: " + jwtToken.ValidTo);
                }
            }
        }
    }
}
