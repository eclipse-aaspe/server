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
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using static QRCoder.PayloadGenerator;

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
                            case "entraid":
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

            if (!authServerEndPoint.EndsWith("/token"))
            {
                if (authServerEndPoint != null)
                {
                    Console.WriteLine("authServerEndPoint " + authServerEndPoint);

                    var entraid = "";
                    var clientCertificate = "";
                    var clientCertificatePW = "";

                    if (c.type == "entraid")
                    {
                        entraid = c.parameters[1];
                        Console.WriteLine("entraid " + entraid);
                    }
                    else
                    {
                        clientCertificate = c.parameters[1];
                        clientCertificatePW = c.parameters[2];

                        Console.WriteLine("clientCertificate " + clientCertificate);
                        Console.WriteLine("clientCertificatePW " + clientCertificatePW);
                    }

                    /*
                    if (c.bearer != string.Empty)
                    {
                        bool valid = true;
                        var jwtToken = new JwtSecurityToken(c.bearer);
                        if ((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow))
                            valid = false;
                        if (valid)
                            return;
                    }
                    */

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
                    if (disco.IsError)
                        return;
                    Console.WriteLine("OpenID Discovery JSON:");
                    Console.WriteLine(disco.Raw);

                    var now = DateTime.UtcNow;
                    var claims = new List<Claim>
                    {
                        new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "client.jwt"),
                        new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
                    };

                    JwtSecurityToken? token = null;
                    X509SigningCredentials credential = null;
                    if (entraid != "")
                    {
                        claims.Add(new("entraid", entraid));

                        // var secret = "test-with-entra-id-34zu8934h89ehhghbgeg54tgfbufrbbssdbsbibu4trui45tr";
                        var secret = entraid;
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                        token = new JwtSecurityToken(
                            issuer: "client.jwt",
                            audience: disco.TokenEndpoint,
                            claims: claims,
                            notBefore: now,
                            expires: now.AddMinutes(1),
                            signingCredentials: credentials
                        );
                    }
                    else
                    {
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

                            credential = new X509SigningCredentials(certificate);
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
                        }

                        token = new JwtSecurityToken(
                            "client.jwt",
                            disco.TokenEndpoint,
                            new List<Claim>()
                            {
                            new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                            new Claim(JwtClaimTypes.Subject, "client.jwt"),
                            new Claim(JwtClaimTypes.IssuedAt, DateTime.UtcNow.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                            // OZ
                            // new Claim(JwtClaimTypes.Email, email)
                            },
                            now,
                            now.AddMinutes(1),
                            credential);

                        token.Header.Add("x5c", x5c);
                    }

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

                    if (response.IsError)
                        return;

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
            else
            {
                string clientID = c.parameters[1];
                string clientCredential = c.parameters[2];

                if (authServerEndPoint != null && clientID != null && clientCredential != null)
                {
                    Console.WriteLine("authServerEndPoint " + authServerEndPoint);
                    Console.WriteLine("clientID " + clientID);
                    Console.WriteLine("clientCredential " + clientCredential);

                    var handler = new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = delegate { return true; },
                    };
                    if (AasxServer.AasxTask.proxy != null)
                        handler.Proxy = AasxServer.AasxTask.proxy;
                    else
                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    var client = new HttpClient(handler);

                    var request1 = new HttpRequestMessage(HttpMethod.Post, authServerEndPoint);
                    request1.Headers.Add("Accept", "application/json");

                    var content1 = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", clientID),
                        new KeyValuePair<string, string>("client_secret", clientCredential)
                    });

                    request1.Content = content1;

                    string json = "";
                    var task = Task.Run(async () =>
                    {
                        var response = await client.SendAsync(request1);
                        if (response != null)
                        {
                            json = response.Content.ContentToString();
                        }
                    });
                    task.Wait();
                    if (json != "")
                    {
                        using JsonDocument doc = JsonDocument.Parse(json);
                        string accessToken = doc.RootElement.GetProperty("access_token").GetString();

                        c.bearer = accessToken;
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
}
