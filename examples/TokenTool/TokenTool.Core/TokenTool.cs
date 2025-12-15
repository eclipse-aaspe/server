
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

// *** Alias: lenkt Console-Aufrufe auf IOConsole (Blazor) oder echte Konsole ***
using Console = MyApp.IOConsole;

namespace MyApp;

public static class TokenTool
{
    public static async Task RunAsync()
    {
        var tenant = "common"; // Damit auch externe Konten wie @live.de funktionieren
        var clientId = "865f6ac0-cdbc-44c6-98cc-3e35c39ecb6e"; // aus der App-Registrierung
        var scopes = new[] { "openid", "profile", "email" }; // für ID Token im JWT-Format

        List<string> configUrlList = [
            "https://www.admin-shell-io.com/50001/.well-known/openid-configuration",
            "https://auth.aas-voyager.com/.well-known/openid-configuration",
            // "https://idp.admin-shell-io.com/api/.well-known/openid-configuration/",
            "https://credential.aas-voyager.com/token",
            "https://credential3.aas-voyager.com/token"
        ];
        for (var i = 0; i < configUrlList.Count; i++)
        {
            Console.WriteLine(i + ": " + configUrlList[i]);
        }
        var input = "0";
        if (configUrlList.Count > 1)
        {
            Console.WriteLine("Enter index: ");
            input = Console.ReadLine();
        }
        var configUrl = configUrlList[Convert.ToInt32(input ?? "0")];

        var text = "Enter character: (F)ile or (E)ntraID or (S)ecret + optional Token(X)Change + optional Double(2)XChange";
        if (OperatingSystem.IsWindows())
        {
            text = "Enter character: (C)ertificateStore or (F)ile or (E)ntraID or (I)nteractive Entra or (S)ecret + optional Token(X)Change + optional Double(2)XChange";
        }
        Console.WriteLine(text);


        input = (Console.ReadLine() ?? "").ToLower();

        var exchange = "0";
        if (input.EndsWith("x") || input.EndsWith("1") || input.EndsWith("2"))
        {
            exchange = input.Substring(1, 1);
            input = input.Substring(0, 1);
        }

        X509Certificate2? certificate = null;
        string[]? x5c = null;

        var handler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
        var client = new HttpClient(handler);

        JsonDocument doc;
        string accessToken;

        if (input == "s")
        {
            Console.WriteLine("Client ID (YourEmail or empty): ");
            var id = Console.ReadLine();
            Console.WriteLine("Client Secret (YourEmail-secret or empty): ");
            var secret = Console.ReadLine();
            if (id == "" && secret == "")
            {
                id = "aorzelski@phoenixcontact.com";
                secret = "aorzelski@phoenixcontact.com-secret";
                Console.WriteLine($"Client ID: {id}");
                Console.WriteLine($"Client Secret: {secret}");
            }

            var request1 = new HttpRequestMessage(HttpMethod.Post, configUrl);
            request1.Headers.Add("Accept", "application/json");

            var content1 = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", id ?? ""),
                new KeyValuePair<string, string>("client_secret", secret ?? "")
            });

            request1.Content = content1;

            var response1 = await client.SendAsync(request1);
            string json = await response1.Content.ReadAsStringAsync();

            doc = JsonDocument.Parse(json);
            accessToken = doc.RootElement.GetProperty("access_token").GetString();

            Console.WriteLine("Access Token : " + accessToken);
        }
        else
        {
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
                    certificate = SelectCertificateWithUI(rootCertSubjects);
                    x5c = BuildChainX5C(certificate);
                    break;

                case "f":
                    certificate = LoadCertificateFromPfx(IOConsole.UploadedPfxBytes, IOConsole.PfxPassword);
                    x5c = BuildChainX5C(certificate);
                    break;

                case "e":
                    Console.WriteLine("Entra ID (from https://entraid.aas-voyager.com/)?");
                    entraid = Console.ReadLine() ?? "";
                    break;

                case "i":
                    entraid = await AcquireEntraIdInteractiveAsync(clientId, tenant, scopes);
                    break;
            }

            if (entraid == "")
            {
                if (certificate == null || x5c == null)
                    return;

                // E-Mail extrahieren
                email = ExtractEmailFromCert(certificate);
            }

            // JWT erstellen und Access-Token anfordern
            var (jwt, accessTok) = await BuildAndRequestTokenAsync(client, tokenEndpoint, entraid, x5c, certificate, email);
            accessToken = accessTok;
        }

        var target = "";
        if (exchange != "0")
        {
            Console.WriteLine("Token Exchange");
            configUrlList = [
                "https://iam-security-training.com/sts"
            ];
            for (var i = 0; i < configUrlList.Count; i++)
            {
                Console.WriteLine(i + ": " + configUrlList[i]);
            }
            input = "0";
            if (configUrlList.Count > 1)
            {
                Console.WriteLine("Enter index: ");
                input = Console.ReadLine();
            }
            configUrl = configUrlList[Convert.ToInt32(input ?? "0")];

            configUrlList = [
                "",
                "basyx",
                "assetfox",
                "factory-x"
            ];
            for (var i = 0; i < configUrlList.Count; i++)
            {
                Console.WriteLine(i + ": " + configUrlList[i]);
            }
            input = "0";
            if (configUrlList.Count > 1)
            {
                Console.WriteLine("Enter index: ");
                input = Console.ReadLine();
            }
            target = configUrlList[Convert.ToInt32(input ?? "0")];

            var d = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:token-exchange" },
                { "subject_token_type", "urn:ietf:params:oauth:token-type:jwt" },
                { "requested_token_type", "urn:ietf:params:oauth:token-type:access_token" },
                { "subject_token", accessToken },
            };
            if (target != "")
            {
                d.Add("audience", target);
            }
            var request = new HttpRequestMessage(HttpMethod.Post, $"{configUrl}/token")
            {
                Content = new FormUrlEncodedContent(d)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            accessToken = "";
            doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                accessToken = tokenElement.GetString();
                Console.WriteLine("Access Token: " + accessToken);

                using var httpClient = new HttpClient(handler);
                var jwksJson = await httpClient.GetStringAsync($"{configUrl}/jwks");
                var jwks = JObject.Parse(jwksJson)["keys"];

                /*
                var handler2 = new JwtSecurityTokenHandler();
                var jwt2 = handler2.ReadJwtToken(accessToken);
                var kid = jwt2.Header["kid"].ToString();

                // 3. Find matching key
                var key = jwks.First(k => k["kid"].ToString() == kid);

                // 4. Build RSA key
                var e = Base64UrlEncoder.DecodeBytes(key["e"].ToString());
                var n = Base64UrlEncoder.DecodeBytes(key["n"].ToString());
                var rsa = new RSAParameters { Exponent = e, Modulus = n };
                var rsaKey = new RsaSecurityKey(rsa);

                // 5. Validate token
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaKey,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                try
                {
                    handler2.ValidateToken(accessToken, validationParams, out _);
                    Console.WriteLine("Token is valid");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Validation failed: {ex.Message}");
                }
                */

                // 1) Handler
                var handler2 = new JsonWebTokenHandler();

                // 2) JWK aus JWKS direkt verwenden (hier exemplarisch LINQ auf Dein jwks-Array)
                var jwtHeaderKid = new JsonWebToken(accessToken).Kid; // liest 'kid' robust
                var jwkJson = jwks.First(k => k["kid"].ToString() == jwtHeaderKid).ToString(); // k ist i. d. R. ein JObject
                var jwk = new JsonWebKey(jwkJson);

                // 3) Validierungsparameter
                var validationParams = new TokenValidationParameters
                {
                    // Signaturprüfung
                    IssuerSigningKey = jwk,            // kein manuelles RSAParameters nötig
                    ValidateIssuerSigningKey = true,

                    // Lebenszeit
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),   // bei UTC+0 gut, ggf. 2–5 Minuten

                    // Issuer/Audience je nach Bedarf (bei Tests oft aus)
                    ValidateIssuer = false,                // später auf true + ValidIssuer setzen
                    ValidateAudience = false,              // später auf true + ValidAudience setzen

                    // Keine Legacy-Claim-Mappings
                    // MapInboundClaims = false,

                    // Optional: Name-/Rollen-Claims aus dem JWT
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                try
                {
                    var result = handler2.ValidateToken(accessToken, validationParams);
                    if (!result.IsValid)
                        Console.WriteLine($"Validation failed: {result.Exception?.Message}");
                    else
                        Console.WriteLine("Token is valid");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Validation failed: {ex.Message}");
                }
            }
        }

        if (exchange == "2")
        {
            handler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
            client = new HttpClient(handler);

            Console.WriteLine("Token Exchange");
            configUrlList = [
                "https://demo2.digital-twin.host/identity-management/realms/D4E/protocol/openid-connect",
                "https://integration.assetfox.apps.siemens.cloud/auth/realms/assetfox/protocol/openid-connect"
            ];
            for (var i = 0; i < configUrlList.Count; i++)
            {
                Console.WriteLine(i + ": " + configUrlList[i]);
            }
            input = "0";
            if (configUrlList.Count > 1)
            {
                Console.WriteLine("Enter index: ");
                input = Console.ReadLine();
            }
            configUrl = configUrlList[Convert.ToInt32(input ?? "0")];

            var service = "service-user-basyx";
            if (target == "assetfox")
            {
                service = "sts-client";
            }
            var request = new HttpRequestMessage(HttpMethod.Post, $"{configUrl}/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", service },
                    { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                    { "client_assertion", accessToken }
                })
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            accessToken = "";
            doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                accessToken = tokenElement.GetString();
                Console.WriteLine("Access Token: " + accessToken);
            }
        }
    }

    private static X509Certificate2? SelectCertificateWithUI(List<string> rootCertSubjects)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("Certificate UI is only available on Windows. Please use mode 'f' (PFX).");
            return null;
        }

        var store = new X509Store("MY", StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

        var collection = (X509Certificate2Collection)store.Certificates;
        var fcollection = (X509Certificate2Collection)collection.Find(
            X509FindType.FindByTimeValid, DateTime.Now, false);

        bool rootCertFound = false;
        var fcollection2 = new X509Certificate2Collection();
        foreach (X509Certificate2 fc in fcollection)
        {
            var fch = new X509Chain();
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

        var scollection = X509Certificate2UI.SelectFromCollection(fcollection,
            "Test Certificate Select",
            "Select a certificate from the following list to get information on that certificate",
            X509SelectionFlag.SingleSelection);

        if (scollection.Count != 0)
        {
            var cert = scollection[0];
            return cert;
        }
        return null;
    }

    private static X509Certificate2? LoadCertificateFromPfx(byte[]? uploadedPfx, string? password)
    {
        if (uploadedPfx is { Length: > 0 } && password != "")
            return new X509Certificate2(uploadedPfx, password ?? "");

        var name = "Andreas_Orzelski_Chain.pfx";
        var pw = "i40";
        int level = 0;
        X509Certificate2 xc = null;
        while (xc == null && level < 4)
        {
            if (System.IO.File.Exists(name))
            {
                xc = new X509Certificate2($"{name}", pw);
            }
            else
            {
                level++;
                name = "../" + name;
            }
        }
        return xc;
    }

    private static string[]? BuildChainX5C(X509Certificate2? certificate)
    {
        if (certificate == null) return null;

        var ch = new X509Chain
        {
            ChainPolicy = {
                RevocationMode   = X509RevocationMode.NoCheck,
                VerificationFlags= X509VerificationFlags.NoFlag
            }
        };

        ch.Build(certificate);

        var x5c = new List<string>();
        foreach (X509ChainElement element in ch.ChainElements)
            x5c.Add(Convert.ToBase64String(element.Certificate.GetRawCertData()));

        return x5c.ToArray();
    }

    private static async Task<string> AcquireEntraIdInteractiveAsync(string clientId, string tenant, string[] scopes)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("Interaktive EntraID is only available on Windows; please use mode 'e' and copy token.");
            return "";
        }

        var app = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenant)
            .WithDefaultRedirectUri()
            .Build();

        var result = await app
            .AcquireTokenInteractive(scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync();

        return result.IdToken;
    }

    private static string? ExtractEmailFromCert(X509Certificate2 certificate)
    {
        var email = certificate.GetNameInfo(X509NameType.EmailName, false);
        if (!string.IsNullOrEmpty(email)) return email;

        var subject = certificate.Subject;
        var match = subject.Split(',').FirstOrDefault(s => s.Trim().StartsWith("E="));
        return match?.Split('=')[1];
    }

    private static async Task<(string jwt, string accessToken)> BuildAndRequestTokenAsync(
        HttpClient client,
        string? tokenEndpoint,
        string entraid,
        string[]? x5c,
        X509Certificate2? certificate,
        string? email)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "client.jwt"),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
        };

        JwtSecurityToken token;

        if (string.IsNullOrEmpty(entraid))
        {
            claims.Add(new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, email ?? ""));

            var credentials = new X509SigningCredentials(certificate!);
            token = new JwtSecurityToken(
                issuer: "client.jwt",
                audience: tokenEndpoint,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(1),
                signingCredentials: credentials
            );
            token.Header["x5c"] = x5c!;
        }
        else
        {
            claims.Add(new("entraid", entraid));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(entraid));
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

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", "factoryx" },
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", jwt }
            })
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(content);
        var accessToken = "";
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            accessToken = tokenElement.GetString();
            Console.WriteLine("Access Token: " + accessToken);
            Console.WriteLine("");
        }

        return (jwt, accessToken ?? "");
    }
}
