using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using IdentityModel.Client;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using ScottPlot.Drawing.Colormaps;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AasxServer
{
    class AasxCredentialsEntry
    {
        public string urlPrefix = string.Empty;
        public string type = string.Empty;
        public List<string> parameters = new List<string>();
    }

    public class AasxCredentials
    {
        static List<AasxCredentialsEntry> credentials = new List<AasxCredentialsEntry>();

        public static void init()
        {
            init("CREDENTIALS-DEFAULT.DAT");
        }
        public static void init(string fileName)
        {
            credentials.Clear();
            if (File.Exists(fileName))
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
                                    credentials.Add(c);
                                }
                            }
                            line = sr.ReadLine();
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine(fileName + " could not be read!");
                    Console.WriteLine(e.Message);
                    return;
                }
            }
            // bearerInit("https://admin-shell-io.com/50001", "Andreas_Orzelski_Chain.pfx", "i40");
        }

        public static bool get(string urlPath, out string queryPara, out string userPW)
        {
            queryPara="";
            userPW="";

            if (AasxServer.Program.Email != "")
            {
                queryPara = "Email=" + AasxServer.Program.Email;
                return true;
            }

            for (int i = 0; i < credentials.Count; i++)
            {
                int len = credentials[i].urlPrefix.Length;
                string u = urlPath.Substring(0, len);
                if (u == credentials[i].urlPrefix)
                {
                    switch (credentials[i].type)
                    {
                        case "email":
                            queryPara = "Email=" + credentials[i].parameters[0];
                            return true;
                        case "basicauth":
                            if (credentials[i].parameters.Count == 2)
                            {
                                userPW = credentials[i].parameters[0] + ":" + credentials[i].parameters[1];
                                return true;
                            }
                            break;
                        case "userpw":
                            if (credentials[i].parameters.Count == 2)
                            {
                                var upw = credentials[i].parameters[0] + ":" + credentials[i].parameters[1];
                                var bytes = Encoding.ASCII.GetBytes(upw);
                                var basicAuth64 = Convert.ToBase64String(bytes);
                                queryPara = "_up=" + basicAuth64;
                                return true;
                            }
                            break;
                        case "bearer":
                            if (!bearerInitialized)
                            {
                                string authServerEndPoint = credentials[i].parameters[0];
                                string clientCertificate = credentials[i].parameters[1];
                                string clientCertificatePW = credentials[i].parameters[2];

                                bearerInit(authServerEndPoint, clientCertificate, clientCertificatePW);
                            }
                            if (bearer != null)
                            {
                                queryPara = "bearer=" + bearer;
                                return true;
                            }
                            break;
                    }
                }
            }

            return false; // no entry found
        }

        public static bool bearerInitialized = false;
        public static string bearer = null;
        public static void bearerInit(string authServerEndPoint, string clientCertificate, string clientCertificatePW)
        {
            if (bearerInitialized)
                return;
            bearerInitialized = true;


            if (authServerEndPoint != null && clientCertificate != null && clientCertificatePW != null)
            {
                Console.WriteLine("authServerEndPoint " + authServerEndPoint);
                Console.WriteLine("clientCertificate " + clientCertificate);
                Console.WriteLine("clientCertificatePW " + clientCertificatePW);
                if (bearer != null)
                {
                    bool valid = true;
                    var jwtToken = new JwtSecurityToken(bearer);
                    if ((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow))
                        valid = false;
                    if (valid) return;
                }

                var handler = new HttpClientHandler();
                if (AasxServer.AasxTask.proxy != null)
                    handler.Proxy = AasxServer.AasxTask.proxy;
                else
                    handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                var client = new HttpClient(handler);
                DiscoveryDocumentResponse disco = null;

                var task = Task.Run(async () => { disco = await client.GetDiscoveryDocumentAsync(authServerEndPoint); });
                task.Wait();
                if (disco.IsError) return;
                Console.WriteLine("OpenID Discovery JSON:");
                Console.WriteLine(disco.Raw);

                if (!File.Exists(clientCertificate))
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
                            credential)
                    ;

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

                    bearer = response.AccessToken;
                    Console.WriteLine("bearer = " + bearer);
                }
            }
        }
    }
}
