/********************************************************************************
* Copyright (c) 2025 Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://mit-license.org/
*
* SPDX-License-Identifier: MIT
********************************************************************************/

using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;
using IdentityModel.Client;
using System.Net.Sockets;
using System.Text;
using Microsoft.Identity.Client;
using static System.Net.WebRequestMethods;

var tenant = "common"; // Damit auch externe Konten wie @live.de funktionieren
var clientId = "865f6ac0-cdbc-44c6-98cc-3e35c39ecb6e"; // aus der App-Registrierung
var scopes = new[] { "openid", "profile", "email" }; // für ID Token im JWT-Format

List<string> configUrlList = [
    "https://www.admin-shell-io.com/50001/.well-known/openid-configuration",
    "https://auth.aas-voyager.com/.well-known/openid-configuration",
    "https://idp.admin-shell-io.com/api/.well-known/openid-configuration/",
    "https://credential.aas-voyager.com/token"
    ];
for (var i = 0; i < configUrlList.Count; i++)
{
    Console.WriteLine(i + ": " + configUrlList[i]);
}
Console.WriteLine("Enter index: ");
var input = Console.ReadLine();
var configUrl = configUrlList[Convert.ToInt32(input)];

Console.WriteLine("Enter character: (C)ertificateStore or (F)ile or (E)ntraID or (I)nteractive Entra or (S)ecret");
input = Console.ReadLine().ToLower();

X509Certificate2? certificate = null;
string[]? x5c = null;

var handler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
var client = new HttpClient(handler);

if (input == "s")
{
    Console.WriteLine("Client ID: ");
    var id = Console.ReadLine();
    Console.WriteLine("Client Secret: ");
    var secret = Console.ReadLine();

    var request1 = new HttpRequestMessage(HttpMethod.Post, configUrl);
    request1.Headers.Add("Accept", "application/json");

    var content1 = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", id),
        new KeyValuePair<string, string>("client_secret", secret)
    });

    request1.Content = content1;

    var response1 = await client.SendAsync(request1);
    string json = await response1.Content.ReadAsStringAsync();

    using JsonDocument doc = JsonDocument.Parse(json);
    string accessToken = doc.RootElement.GetProperty("access_token").GetString();

    Console.WriteLine("Access Token : " + accessToken);
    return;
}

var configJson = await client.GetStringAsync(configUrl);
var config = JsonDocument.Parse(configJson);
var tokenEndpoint = config.RootElement.GetProperty("token_endpoint").GetString();

List<string> rootCertSubjects = [];
if (config.RootElement.TryGetProperty("rootCertSubjects", out JsonElement rootCerts))
{
    foreach (var subject in rootCerts.EnumerateArray())
    {
        var s = subject.GetString();
        if (s != null)
        {
            rootCertSubjects.Add(s);
        }
    }
}

string? email = "";
var entraid = "";

switch (input)
{
    case "c":
        X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

        X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
        X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(
            X509FindType.FindByTimeValid, DateTime.Now, false);

        Boolean rootCertFound = false;
        X509Certificate2Collection fcollection2 = new X509Certificate2Collection();
        foreach (X509Certificate2 fc in fcollection)
        {
            X509Chain fch = new X509Chain();
            fch.Build(fc);
            foreach (X509ChainElement element in fch.ChainElements)
            {
                if (rootCertSubjects.Contains(element.Certificate.Subject))
                {
                    rootCertFound = true;
                    fcollection2.Add(fc);
                }
            }
        }
        if (rootCertFound)
            fcollection = fcollection2;

        X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection,
            "Test Certificate Select",
            "Select a certificate from the following list to get information on that certificate",
            X509SelectionFlag.SingleSelection);
        if (scollection.Count != 0)
        {
            certificate = scollection[0];
            X509Chain ch = new X509Chain();
            ch.Build(certificate);

            string[] X509Base64 = new string[ch.ChainElements.Count];

            int j = 0;
            foreach (X509ChainElement element in ch.ChainElements)
            {
                X509Base64[j++] = Convert.ToBase64String(element.Certificate.GetRawCertData());
            }

            x5c = X509Base64;
        }
        break;
    case "f":
        certificate = new X509Certificate2("../../../Andreas_Orzelski_Chain.pfx", "i40");

        // Zertifikatskette vorbereiten
        var chain = new X509Certificate2Collection();
        chain.Import("../../../Andreas_Orzelski_Chain.pfx", "i40");
        x5c = chain.Cast<X509Certificate2>().Reverse().Select(c => Convert.ToBase64String(c.RawData)).ToArray();
        break;
    case "e":
        Console.WriteLine("Entra ID?");
        entraid = Console.ReadLine();
        break;
    case "i":
        var app = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenant)
            .WithDefaultRedirectUri()             // entspricht http://localhost
            .Build();

        var result = await app
            .AcquireTokenInteractive(scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync();

        entraid = result.IdToken;
        break;
}

if (entraid == "")
{
    if (certificate == null || x5c == null)
        return;

    // E-Mail extrahieren
    email = certificate.GetNameInfo(X509NameType.EmailName, false);
    if (string.IsNullOrEmpty(email))
    {
        var subject = certificate.Subject;
        var match = subject.Split(',').FirstOrDefault(s => s.Trim().StartsWith("E="));
        email = match?.Split('=')[1];
    }
}


// JWT erstellen
var now = DateTime.UtcNow;
var claims = new List<Claim>
{
    new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "client.jwt"),
    new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
};

JwtSecurityToken? token = null;

if (entraid == "")
{
    claims.Add(new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, email!));

    var credentials = new X509SigningCredentials(certificate);
    token = new JwtSecurityToken(
        issuer: "client.jwt",
        audience: tokenEndpoint,
        claims: claims,
        notBefore: now,
        expires: now.AddMinutes(1),
        signingCredentials: credentials
    );
    token.Header["x5c"] = x5c;
}
else
{
    claims.Add(new("entraid", entraid));

    // var secret = "test-with-entra-id-34zu8934h89ehhghbgeg54tgfbufrbbssdbsbibu4trui45tr";
    var secret = entraid;
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    token = new JwtSecurityToken(
        issuer: "client.jwt",
        audience: tokenEndpoint,
        claims: claims,
        notBefore: now,
        expires: now.AddMinutes(1),
        signingCredentials: credentials
    );
}

var jwt = new JwtSecurityTokenHandler().WriteToken(token);

// Token anfordern
var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
{
    Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "grant_type", "client_credentials" },
        // { "scope", "resource1.scope1" },
        { "scope", "factoryx" },
        { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
        { "client_assertion", jwt }
    })
    /*
    Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "grant_type", "client_credentials" },
        { "scope", "read write" },
        { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
        { "client_assertion", jwt }
    })
    */
};
request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

var response = await client.SendAsync(request);
var content = await response.Content.ReadAsStringAsync();
Console.WriteLine("Access Token Response: " + content);
