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

using System.Collections.Specialized;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;
using AasSecurity.Exceptions;
using AasSecurity.Models;
using AasxServer;
using AasxServerStandardBib.Logging;
using AasxServerStandardBib.Services;
using Contracts;
using Extensions;
using Irony.Parsing;
using Jose;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Namotion.Reflection;
using Newtonsoft.Json.Linq;
using static QRCoder.PayloadGenerator;
using File = AasCore.Aas3_0.File;

namespace AasSecurity
{
    public class SecurityService : ISecurityService, IContractSecurityRules
    {
        public static List<Dictionary<string, string>>? _condition = new List<Dictionary<string, string>>();

        public static AllAccessPermissionRules? _accessRules = null;

        public SecurityService()
        {
            parseAccessRuleFile();
        }
        public void parseAccessRuleFile()
        {
            var grammar = new QueryGrammarJSON(this);
            var parser = new Parser(grammar);
            parser.Context.TracingEnabled = true;
            var filePath = "accessrules.txt";
            if (System.IO.File.Exists(filePath))
            {
                // if (expression == "")
                if (true)
                {
                    var expression = System.IO.File.ReadAllText(filePath);
                    var parseTree = parser.Parse(expression);

                    if (parseTree.HasErrors())
                    {
                        var pos = parser.Context.CurrentToken.Location.Position;
                        var text2 = expression.Substring(0, pos) + "$$$" + expression.Substring(pos);
                        text2 = string.Join("\n", parseTree.ParserMessages) + "\nSee $$$: " + text2;
                        Console.WriteLine(text2);
                        expression = "";
                    }
                    else
                    {
                        ClearSecurityRules();
                        grammar.ParseAccessRules(expression);
                        _accessRules = QueryGrammarJSON._accessRules;
                        _condition = QueryGrammarJSON.allAccessRuleExpressions;
                    }
                }
            }
        }

        public List<AccessPermissionRule>? GetAccessRules(string accessRole, string neededRightsClaim, string? httpRoute = null, List<Claim>? tokenClaims = null)
        {
            return GetAccessRulesStatic(accessRole, neededRightsClaim, httpRoute, tokenClaims);
        }

        public static List<AccessPermissionRule>? GetAccessRulesStatic(string accessRole, string neededRightsClaim, string? httpRoute = null, List<Claim>? tokenClaims = null)
        {
            if (_accessRules != null)
            {
                var rules = _accessRules.Rules.Where(r =>
                    r.Acl != null &&
                    r.Acl.Access == "ALLOW" &&
                    r.Acl.Rights.Contains(neededRightsClaim) && r.Acl.Attributes[0].ItemType == "CLAIM" &&
                    (
                        (accessRole != null && r.Acl.Attributes[0].Value == accessRole) ||
                        (accessRole == null && tokenClaims != null && tokenClaims.Any(t => t.ValueType == "token:" + r.Acl.Attributes[0].Value))
                    ) &&
                    (
                        (httpRoute == null) ||
                        (httpRoute != null && r.Objects.Any(o => o.ItemType == "ROUTE" && MatchApiOperation(o.Value, httpRoute)))
                    )
                ).ToList();

                if (rules.Count == 0)
                {
                    return null;
                }

                return rules;
            }

            return null;
        }
        public Dictionary<string, string>? GetCondition(string accessRole, string neededRightsClaim, string? httpRoute = null, List<Claim>? tokenClaims = null)
        {
            var rules = GetAccessRules(accessRole, neededRightsClaim);

            if (rules == null)
            {
                return null;
            }

            Dictionary<string, string> condition = [];
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                foreach (var c in rule._formula_conditions)
                {
                    if (i == 0)
                    {
                        condition[c.Key] = c.Value;
                    }
                    else
                    {
                        var hasValue = condition.TryGetValue(c.Key, out var value);
                        if (hasValue && value != "")
                        {
                            if (value != "(True)")
                            {
                                if (c.Value != "")
                                {
                                    condition[c.Key] = value + " || " + c.Value;
                                }
                                else
                                {
                                    condition[c.Key] = "(True)";
                                }
                            }
                        }
                        else
                        {
                            condition[c.Key] = c.Value;
                        }
                    }
                }
                foreach (var c in rule._filter_conditions)
                {
                    if (i == 0)
                    {
                        condition["filter-" + c.Key] = c.Value;
                    }
                    else
                    {
                        var hasValue = condition.TryGetValue("filter-" + c.Key, out var value);
                        if (hasValue && value != "")
                        {
                            if (value != "(True)")
                            {
                                if (c.Value != "")
                                {
                                    condition["filter-" + c.Key] = value + " || " + c.Value;
                                }
                                else
                                {
                                    condition["filter-" + c.Key] = "(True)";
                                }
                            }
                        }
                        else
                        {
                            condition["filter-" + c.Key] = c.Value;
                        }
                    }
                }
            }

            foreach (var c in condition)
            {
                if (condition[c.Key] is not "" and not "(True)")
                {
                    condition[c.Key] = "(" + c.Value + ")";
                }

                while (condition[c.Key].Contains("CLAIM("))
                {
                    var split = c.Value.Split("CLAIM(");
                    split = split[1].Split(")");
                    var claim = split[0];
                    if (claim.StartsWith("token:"))
                    {
                        var value = tokenClaims.Where(tc => tc.Type == claim).FirstOrDefault().Value;
                        condition[c.Key] = c.Value.Replace($"CLAIM({claim})", $"\"{value}\"");
                    }
                }
            }

            return condition;
        }

        public void ClearSecurityRules()
        {
            _condition.Clear();
            _accessRules = null;
            GlobalSecurityVariables.SecurityRoles.Clear();
        }

        public void AddSecurityRule(string name, string access, string right, string objectType, string semanticId, string route)
        {
            SecurityRole role = new SecurityRole();

            role.Name = name;
            if (access != "ALLOW")
            {
                return;
            }
            role.Kind = KindOfPermissionEnum.Allow;
            switch (right)
            {
                case "READ":
                    role.Permission = AccessRights.READ;
                    break;
                case "CREATE":
                    role.Permission = AccessRights.CREATE;
                    break;
                case "UPDATE":
                    role.Permission = AccessRights.UPDATE;
                    break;
                case "DELETE":
                    role.Permission = AccessRights.DELETE;
                    break;
                default:
                    return;
            }
            role.ObjectType = objectType;
            role.ApiOperation = route;
            role.SemanticId = semanticId;
            role.RulePath = "";

            GlobalSecurityVariables.SecurityRoles.Add(role);
        }

        private static readonly ILogger _logger = ApplicationLogging.CreateLogger("SecurityService");

        public AuthenticationTicket? AuthenticateRequest(HttpContext context, string route, string httpOperation, string? authenticationSchemeName)
        {
            if (!GlobalSecurityVariables.WithAuthentication)
            {
                return null;
            }

            //Retrieve security related query strings from the request
            NameValueCollection queries = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());

            //Retrieve headers from the request
            NameValueCollection headers = new NameValueCollection();
            foreach (var header in context.Request.Headers)
            {
                headers.Add(header.Key, header.Value.FirstOrDefault());
                if (header.Key != "FORCE-POLICY")
                {
                    continue;
                }

                Program.withPolicy = header.Value.FirstOrDefault() != "OFF";
                _logger.LogDebug("FORCE-POLICY {Sanitize}", LogSanitizer.Sanitize(header.Value.FirstOrDefault()));
            }

            var accessRole = GetAccessRole(queries, headers, out var policy, out var policyRequestedResource, out var tokenClaims);
            if (string.IsNullOrEmpty(accessRole))
            {
                _logger.LogDebug($"Access Role found null. Hence setting the access role as isNotAuthenticated.");
                accessRole = "isNotAuthenticated";
            }

            _logger.LogInformation($"Access role in authentication: {accessRole}, policy: {policy}, policyRequestedResource: {policyRequestedResource}");
            var aasSecurityContext = new AasSecurityContext(accessRole, route, httpOperation);
            //Create claims
            var claims = new List<Claim>
                         {
                             new Claim(ClaimTypes.Role, aasSecurityContext.AccessRole),
                             new Claim("NeededRights", aasSecurityContext.NeededRights.ToString()),
                             new Claim("Policy", policy),
                             new Claim("Route", aasSecurityContext.Route)
                         };

            foreach (var tc in tokenClaims)
            {
                claims.Add(new Claim("token:" + tc.Type, tc.Value));
            }

            var identity = new ClaimsIdentity(claims, authenticationSchemeName);
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            return new AuthenticationTicket(principal, authenticationSchemeName);
        }

        private string GetAccessRole(NameValueCollection queries, NameValueCollection headers,
            out string policy, out string policyRequestedResource, out List<Claim> tokenClaims)
        {
            _logger.LogDebug("Getting the access rights.");
            string accessRole = null;
            string user = null;
            bool error = false;
            string? bearerToken = null;
            policy = "";
            policyRequestedResource = "";
            tokenClaims = [];
            var domain = "";

            ParseBearerToken(queries, headers, ref bearerToken, ref error, ref user, ref accessRole);
            if (accessRole != null)
            {
                return accessRole;
            }

            if (!error)
            {
                accessRole = HandleBearerToken(bearerToken, ref user, ref error, out policy, out policyRequestedResource, out tokenClaims, out domain);
            }

            if (!string.IsNullOrEmpty(user))
            {
                var securityRights = GlobalSecurityVariables.SecurityRights;
                if (securityRights != null)
                {
                    foreach (var securityRight in securityRights)
                    {
                        //Email
                        if (securityRight.Name.Contains('@'))
                        {
                            if (user.Contains('@') && user.Equals(securityRight.Name))
                            {
                                accessRole = securityRight.Role;
                                return accessRole;
                            }
                        }
                    }

                    //Domain
                    if (domain != "")
                    {
                        foreach (var securityRight in securityRights)
                        {
                            if (!securityRight.Name.Contains('@'))
                            {
                                if (domain != null && domain.Equals(securityRight.Name))
                                {
                                    accessRole = securityRight.Role;
                                    return accessRole;
                                }

                                if (user == securityRight.Name)
                                {
                                    accessRole = securityRight.Role;
                                    return accessRole;
                                }
                            }
                        }
                    }
                }
            }

            return accessRole;
        }

        private string HandleBearerToken(string? bearerToken, ref string user, ref bool error,
            out string policy, out string policyRequestedResource, out List<Claim> tokenClaims, out string domain)
        {
            domain = "";
            policy = "";
            policyRequestedResource = "";
            tokenClaims = [];

            if (bearerToken == null)
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(bearerToken);
                if (jwtSecurityToken != null && jwtSecurityToken.Claims != null)
                {
                    var iss = "";
                    var issClaim = jwtSecurityToken.Claims.Where(c => c.Type == "iss");
                    if (issClaim.Any())
                    {
                        iss = issClaim.First().Value;
                    }

                    var valid = false;
                    if (!string.IsNullOrEmpty(iss) && iss.StartsWith("https://login.microsoftonline.com"))
                    {
                        var clientId = "865f6ac0-cdbc-44c6-98cc-3e35c39ecb6e";
                        var tenantId = jwtSecurityToken.Claims
                            .First(c => c.Type == "tid").Value;

                        var jwksUrl = $"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys";
                        var clientHandler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
                        using var httpClient = new HttpClient(clientHandler);
                        var jwksJson = httpClient.GetStringAsync(jwksUrl).Result;
                        var jwks = new JsonWebKeySet(jwksJson);
                        var signingKeys = jwks.GetSigningKeys();

                        var tokenHandler = new JwtSecurityTokenHandler();
                        var validationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
                            ValidateAudience = true,
                            ValidAudience = clientId,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKeys = signingKeys
                        };

                        try
                        {
                            var principal = tokenHandler.ValidateToken(bearerToken, validationParameters, out var validatedToken);
                            Console.WriteLine("Token is valid.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Token validation failed: {ex.Message}");
                            user = "";
                            return "";
                        }
                        valid = true;
                    }
                    if (!valid)
                    {
                        if (jwtSecurityToken.Header.TryGetValue("kid", out _))
                        {
                            user = "";
                            var jwksUrl = "";
                            var kid = jwtSecurityToken.Header["kid"].ToString();
                            if (kid != null)
                            {
                                jwksUrl = SecurityHelper.FindServerJwksUrl(kid, out domain);
                            }
                            if (!jwksUrl.IsNullOrEmpty())
                            {
                                var clientHandler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
                                using var httpClient = new HttpClient(clientHandler);
                                var jwksJson = httpClient.GetStringAsync(jwksUrl + "/jwks").Result;
                                var jwks = new JsonWebKeySet(jwksJson);
                                var signingKeys = jwks.GetSigningKeys();

                                var tokenHandler = new JwtSecurityTokenHandler();
                                var validationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = false,
                                    ValidateAudience = false,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKeys = signingKeys
                                };

                                try
                                {
                                    var principal = tokenHandler.ValidateToken(bearerToken, validationParameters, out var validatedToken);

                                    valid = true;
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }
                    if (!valid)
                    {
                        var serverNameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == "serverName");
                        if (serverNameClaim != null)
                        {
                            var serverName = serverNameClaim.Value;
                            X509Certificate2? cert = SecurityHelper.FindServerCertificate(serverName, out domain);
                            if (cert == null)
                            {
                                user = "";
                                return "";
                            }

                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine("-----BEGIN CERTIFICATE-----");
                            builder.AppendLine(
                                               Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
                            builder.AppendLine("-----END CERTIFICATE-----");
                            _logger.LogDebug("Token Server Certificate: " + serverName);
                            _logger.LogDebug(builder.ToString());
                            //check if server cert is correctly signed
                            try
                            {
                                Jose.JWT.Decode(bearerToken, cert.GetRSAPublicKey(), JwsAlgorithm.RS256); // correctly signed by auth server cert?

                                valid = true;
                            }
                            catch
                            {
                                user = "";
                                return "";
                            }
                        }
                    }

                    if (valid)
                    {
                        tokenClaims.AddRange(jwtSecurityToken.Claims);

                        var emailClaim = jwtSecurityToken.Claims.Where(c => c.Type == "userName");
                        if (emailClaim.Any())
                        {
                            var userName = emailClaim.First().Value;
                            if (!string.IsNullOrEmpty(userName))
                            {
                                user = userName.ToLower();
                            }
                        }
                        else
                        {
                            emailClaim = jwtSecurityToken.Claims.Where(c => c.Type == "email");
                            if (emailClaim.Any())
                            {
                                var email = emailClaim.First().Value;
                                if (!string.IsNullOrEmpty(email))
                                {
                                    user = email.ToLower();
                                }
                            }
                            else
                            {
                                emailClaim = jwtSecurityToken.Claims.Where(c => c.Type == "sub");
                                if (emailClaim.Any())
                                {
                                    var email = emailClaim.First().Value;
                                    if (!string.IsNullOrEmpty(email) && email.Contains('@'))
                                    {
                                        user = email.ToLower();
                                    }
                                }
                            }
                        }

                        var policyClaim = jwtSecurityToken.Claims.Where(c => c.Type == "policy");
                        if (policyClaim.Any())
                        {
                            policy = policyClaim.First().Value;
                        }

                        var policyRequestedResourceClaim = jwtSecurityToken.Claims.Where(c => c.Type == "policyRequestedResource");
                        if (policyRequestedResourceClaim.Any())
                        {
                            policyRequestedResource = policyRequestedResourceClaim.First().Value;
                        }

                        if (user != "")
                        {
                            if (domain == "" && user != "")
                            {
                                var split = user.Split('@');
                                if (split.Length == 2)
                                {
                                    domain = split[1];
                                }
                            }
                            if (domain != "")
                            {
                                tokenClaims.Add(new Claim("domain", domain));
                            }

                            return "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = true;
                _logger.LogError($"Exception occurred while parsing the bearer token.{ex.Message}");
                _logger.LogDebug(ex.StackTrace);
            }

            user = "";
            return "";
        }

        private AccessRights? ParseBearerToken(NameValueCollection queries, NameValueCollection headers, ref string? bearerToken, ref bool error, ref string user,
                                               ref string accessRights)
        {
            //Check the token in header
            foreach (string key in headers.Keys)
            {
                switch (key.ToLower())
                {
                    case "authorization":
                    {
                        var token = headers[key];
                        if (token != null)
                        {
                            var split = token.Split(new[] { ' ', '\t' });
                            switch (split[0].ToLower())
                            {
                                case "bearer":
                                    _logger.LogDebug("Received bearer token {Sanitize}", LogSanitizer.Sanitize(split[1]));
                                    bearerToken = split[1];
                                    break;
                                case "basic" when bearerToken == null:
                                    try
                                    {
                                        if (Program.secretStringAPI != null)
                                        {
                                            var credentialBytes = Convert.FromBase64String(split[1]);
                                            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                                            string u = credentials[0];
                                            string p = credentials[1];
                                            Console.WriteLine("Received username+password http header = " + u + " : " + p);

                                            if (u == "secret")
                                            {
                                                accessRights = "READ";
                                                {
                                                    if (p == Program.secretStringAPI)
                                                        accessRights = "CREATE";
                                                }
                                                _logger.LogDebug("accessrights " + accessRights);
                                                AccessRights output = (AccessRights)Enum.Parse(typeof(AccessRights), accessRights);
                                                return output;
                                            }
                                        }

                                        var username = CheckUserPW(split[1], out var userName, out var passWord);
                                        if (username != null)
                                        {
                                            user = username;
                                            Console.WriteLine("Received username+password http header = " + user);
                                        }
                                        else
                                        {
                                            if (userName != "" && passWord != "")
                                            {
                                                accessRights = $"__{userName}__{passWord}";
                                            }
                                        }
                                    }
                                    catch (ArgumentException argumentException)
                                    {
                                        Console.WriteLine($"Exception while extracting {nameof(Program.secretStringAPI)}: {argumentException.Message}");
                                    }

                                    break;
                            }
                        }

                        break;
                    }
                    case "email":
                    {
                        var token = headers[key];
                        if (token != null)
                        {
                            _logger.LogDebug("Received email token from header: {Sanitize}", LogSanitizer.Sanitize(token));
                            user = token;
                            error = false;
                        }

                        break;
                    }
                }
            }

            //Check the token in queries
            foreach (string key in queries.Keys)
            {
                switch (key.ToLower())
                {
                    case "s":
                    {
                        var secretQuery = queries["s"]!;
                        if (!string.IsNullOrEmpty(secretQuery))
                        {
                            _logger.LogDebug("Received token of type s: {Sanitize}", LogSanitizer.Sanitize(secretQuery));
                            if (Program.secretStringAPI != null)
                            {
                                if (secretQuery.Equals(Program.secretStringAPI))
                                {
                                    return AccessRights.CREATE; // Set AccessRole to create
                                }
                            }
                        }

                        break;
                    }
                    case "bearer":
                    {
                        var token = queries[key];
                        if (token != null)
                        {
                            _logger.LogDebug("Received token of type bear {Sanitize}", LogSanitizer.Sanitize(token));
                            bearerToken = token;
                        }

                        break;
                    }
                    case "email":
                    {
                        var token = queries[key];
                        if (token != null)
                        {
                            _logger.LogDebug("Received token of type email {Sanitize}", LogSanitizer.Sanitize(token));
                            user = token;
                            accessRights = user;
                            error = false;
                        }

                        break;
                    }
                    case "_up":
                    {
                        var token = queries[key];
                        if (token != null)
                        {
                            _logger.LogDebug("Received token of type username-password {Sanitize}", LogSanitizer.Sanitize(token));
                            var username = CheckUserPW(token, out var userName, out var passWord);
                            if (username != null)
                            {
                                user = username;
                                _logger.LogDebug("Received username+password query string = {Sanitize}", LogSanitizer.Sanitize(user));
                            }
                            else
                            {
                                if (userName != "" && passWord != "")
                                {
                                    accessRights = $"__{userName}__{passWord}";
                                }
                            }
                        }

                        break;
                    }
                }
            }

            return null;
        }

        private string CheckUserPW(string userPW64, out string userName, out string passWord)
        {
            userName = "";
            passWord = "";
            var credentialBytes = Convert.FromBase64String(userPW64);
            string[] credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            var username = credentials[0];
            var password = credentials[1];

            userName = username;
            passWord = password;
            var found = GlobalSecurityVariables.SecurityUsernamePassword.TryGetValue(username, out string? storedPassword);
            return found ? password != null && password.Equals(storedPassword) ? username : null : null;
        }

        public bool AuthorizeRequest(string accessRole, string httpRoute, AccessRights neededRights,
                                     out string error, out bool withAllow, out string? getPolicy, string objPath = null, string? aasResourceType = null,
                                     IClass? aasResource = null, string? policy = null)
        {
            return CheckAccessRights(accessRole, httpRoute, neededRights, out error, out withAllow, out getPolicy, objPath, aasResourceType, aasResource, policy: policy);
        }

        private static bool CheckAccessRights(string currentRole, string operation, AccessRights neededRights, out string error, out bool withAllow, out string? getPolicy,
                                              string objPath = "", string? aasResourceType = null, IClass? aasResource = null, bool testOnly = false, string? policy = null)
        {
            withAllow = false;
            return CheckAccessRightsWithAllow(currentRole, operation, neededRights, out error, out withAllow, out getPolicy,
                                              objPath, aasResourceType, aasResource, testOnly, policy);
        }

        private static bool CheckAccessRightsWithAllow(string currentRole, string operation, AccessRights neededRights, out string error, out bool withAllow, out string? getPolicy,
                                                       string objPath = "", string? aasResourceType = null, IClass? aasResource = null, bool testOnly = false,
                                                       string? policy = null)
        {
            error = "Access not allowed";
            withAllow = false;
            getPolicy = "";

            if (Program.secretStringAPI != null && currentRole == "CREATE")
            {
                return true;
            }
            else
            {
                // TODO (jtikekar, 2023-09-04): uncomment
                if (CheckAccessLevelWithError(
                                              out error, currentRole, operation, neededRights, out withAllow, out getPolicy,
                                              objPath, aasResourceType, aasResource, policy))
                    return true;
            }

            // Exception
            if (!testOnly)
            {
                //throw new NotAllowed(error);
                return false;
            }

            return false;
        }

        private static bool CheckAccessLevelWithError(out string error, string currentRole, string operation, AccessRights neededRights, out bool withAllow, out string? getPolicy,
                                                      string objPath, string? aasResourceType, IClass? aasResource, string? policy = null)
        {
            withAllow = false;
            getPolicy = "";

            if (currentRole == null)
            {
                currentRole = "isNotAuthenticated";
            }

            _logger.LogDebug("checkAccessLevel: " +
                             " currentRole = " + currentRole +
                             " operation = " + operation +
                             " neededRights = " + neededRights +
                             " objPath = " + objPath
                            );

            if (aasResource == null)
            {
                //API security check
                return CheckAccessLevelApi(currentRole, operation, neededRights, out error, out getPolicy);
            }

            if (string.IsNullOrEmpty(objPath))
            {
                return CheckAccessLevelEmptyObjPath(currentRole, operation, aasResourceType, aasResource, neededRights, out error);
            }

            if (objPath != string.Empty && (operation.Contains("/submodel-elements") || operation.Contains("/submodels")))
            {
                return CheckAccessLevelForOperation(currentRole, operation, aasResourceType, aasResource, neededRights, objPath, out withAllow, out getPolicy, out error, policy);
            }

            error = "ALLOW not defined";
            return false;
        }

        private static bool CheckAccessLevelApi(string currentRole, string operation, AccessRights neededRights, out string error, out string? getPolicy)
        {
            error = string.Empty;
            getPolicy = string.Empty;

            var rules = GetAccessRulesStatic(currentRole, neededRights.ToString(), operation);

            if (rules != null && rules.Count != 0)
            {
                return true;
            }

            error = "API access NOT allowed!";
            return false;

            getPolicy = string.Empty;
            foreach (var securityRole in GlobalSecurityVariables.SecurityRoles.Where(securityRole => securityRole.Name == currentRole && securityRole.ObjectType == "api" &&
                                                                                                     securityRole.Permission == neededRights &&
                                                                                                     (securityRole.ApiOperation == "*" ||
                                                                                                      MatchApiOperation(securityRole.ApiOperation, operation)))
                                                                .Where(securityRole => securityRole.Permission == neededRights))
            {
                //return CheckUsage(out error, securityRole);
                return CheckPolicy(out error, securityRole, out getPolicy);
            }

            error = "API access NOT allowed!";
            return false;
        }

        private static bool CheckPolicy(out string error, SecurityRole securityRole, out string getPolicy, string? policy = null)
        {
            error = "";
            getPolicy = "";
            Property? pPolicy = null;
            File? fPolicy = null;

            if (securityRole.Usage == null)
                return true;

            foreach (var sme in securityRole.Usage.Value!)
            {
                switch (sme.IdShort)
                {
                    case "accessPerDuration":
                        if (sme is SubmodelElementCollection smc)
                        {
                            Property maxCount = null;
                            Property actualCount = null;
                            Property duration = null;
                            Property actualTime = null;
                            foreach (var sme2 in smc.Value)
                            {
                                switch (sme2.IdShort)
                                {
                                    case "maxCount":
                                        maxCount = sme2 as Property;
                                        break;
                                    case "duration":
                                        duration = sme2 as Property;
                                        break;
                                    case "actualCount":
                                        actualCount = sme2 as Property;
                                        break;
                                    case "actualTime":
                                        actualTime = sme2 as Property;
                                        break;
                                }
                            }

                            if (maxCount == null || duration == null || actualCount == null || actualTime == null)
                                return false;
                            int d = 0;
                            if (!int.TryParse(duration.Value, out d))
                            {
                                return false;
                            }

                            DateTime dt = new DateTime();
                            if (actualTime.Value != null && actualTime.Value != "")
                            {
                                try
                                {
                                    dt = DateTime.Parse(actualTime.Value);
                                    if (dt.AddSeconds(d) < DateTime.UtcNow)
                                    {
                                        Program.signalNewData(0);
                                        actualTime.Value = null;
                                    }
                                }
                                catch (FormatException formatException)
                                {
                                    Console.WriteLine($"Exception while parsing {actualTime}: {formatException.Message}");
                                }
                            }

                            if (string.IsNullOrEmpty(actualTime.Value))
                            {
                                actualTime.Value = DateTime.UtcNow.ToString();
                                actualCount.Value = null;
                            }

                            if (actualCount.Value == null || actualCount.Value == "")
                            {
                                actualCount.Value = "0";
                            }

                            if (!int.TryParse(actualCount.Value, out var ac))
                            {
                                Program.signalNewData(0);
                                return false;
                            }

                            if (!int.TryParse(maxCount.Value, out var mc))
                            {
                                Program.signalNewData(0);
                                return false;
                            }

                            ac++;
                            actualCount.Value = ac.ToString();
                            if (ac <= mc)
                            {
                                Program.signalNewData(0);
                                return true;
                            }
                        }

                        break;
                    case "policy":
                        pPolicy = sme as Property;
                        break;
                    case "license":
                        fPolicy = sme as AasCore.Aas3_0.File;
                        break;
                    case "policyRequestedResource":
                        break;
                }
            }

            if (!Program.withPolicy || pPolicy == null)
                return true;

            getPolicy = pPolicy.Value;
            if (getPolicy == "" && fPolicy != null)
            {
                try
                {
                    using (Stream s = securityRole.UsageEnv.GetLocalStreamFromPackage(fPolicy.Value))
                    using (SHA256 mySHA256 = SHA256.Create())
                    {
                        if (s != null)
                        {
                            s.Position = 0;
                            byte[] hashValue = mySHA256.ComputeHash(s);
                            getPolicy = Convert.ToHexString(hashValue);
                            Console.WriteLine("hash: " + getPolicy);
                            pPolicy.Value = getPolicy;
                        }
                    }
                }
                catch (ObjectDisposedException objectDisposedException)
                {
                    Console.WriteLine($"Exception in {nameof(CheckPolicy)}: {objectDisposedException.Message}");
                }
            }

            if (policy == null || policy.Contains(getPolicy))
            {
                return true;
            }


            // Program.signalNewData(0);
            return false;
        }

        private static bool CheckAccessLevelForOperation(string currentRole, string operation, string? aasResourceType, IClass? aasResource, AccessRights neededRights,
                                                         string objPath, out bool withAllow, out string? getPolicy, out string error, string? policy = null)
        {
            error = "";
            withAllow = false;
            var deepestDeny = "";
            var deepestAllow = "";
            SecurityRole deepestAllowRole = null;
            getPolicy = "";

            var conditionSM = "";
            var conditionSME = "";
            // TODO find all correct entries: copied from GetCondition()
            foreach (var c in _condition)
            {
                var a = c["claim"];
                var n = c["right"];
                if (a == currentRole && n.Contains(neededRights.ToString()))
                {
                    conditionSM = c["sm."];
                    conditionSME = c["sme."];
                }
            }

            if (conditionSME != "" && objPath.Contains('.') && aasResource is Submodel s2 && s2.SubmodelElements != null)
            {
                var submodelElements = s2.SubmodelElements;
                var path = objPath.Split('.');
                int i = 1;
                while (i < path.Length)
                {
                    var idShort = path[i];
                    var found = submodelElements.FindIndex(x => x.IdShort == idShort);
                    if (found == -1)
                    {
                        break;
                    }
                    if (i == path.Length - 1)
                    {
                        List<ISubmodelElement> list = new List<ISubmodelElement>();
                        list.Add(submodelElements[found]);
                        var c = conditionSME;
                        var x = list.AsQueryable().Where(c);
                        if (x.Any())
                        {
                            return true;
                        }
                    }
                    switch (submodelElements[found])
                    {
                        case SubmodelElementCollection smc:
                            submodelElements = smc.Value;
                            break;
                        case SubmodelElementList sml:
                            submodelElements = sml.Value;
                            break;
                    }
                    i++;
                }
            }
            if (conditionSM != "" && (conditionSME == "" || !objPath.Contains('.')) && aasResource is Submodel s)
            {
                List<Submodel> submodels = new List<Submodel>();
                submodels.Add(s);
                var c = conditionSM;
                var x = submodels.AsQueryable().Where(c);
                if (x.Any())
                {
                    return true;
                }
            }

            foreach (var securityRole in GlobalSecurityVariables.SecurityRoles.Where(securityRole => securityRole.Name.Equals(currentRole)))
            {
                if (securityRole.ObjectType.Equals("semanticid", StringComparison.OrdinalIgnoreCase))
                {
                    if (aasResource is Submodel submodel)
                    {
                        if (securityRole.SemanticId != null)
                        {
                            if (securityRole.SemanticId == "*" || (submodel.SemanticId != null && submodel.SemanticId.Keys != null && submodel.SemanticId.Keys.Count != 0))
                            {
                                if (securityRole.SemanticId == "*" || (securityRole.SemanticId.ToLower() == submodel.SemanticId?.Keys?[0].Value.ToLower()))
                                {
                                    if (securityRole.Kind == KindOfPermissionEnum.Allow)
                                    {
                                        if (deepestAllow == "")
                                        {
                                            deepestAllow = submodel.IdShort!;
                                            withAllow = true;
                                            deepestAllowRole = securityRole;
                                        }
                                    }

                                    if (securityRole.Kind == KindOfPermissionEnum.Deny)
                                    {
                                        if (deepestDeny == "")
                                            deepestDeny = submodel.IdShort!;
                                    }
                                }
                            }
                        }
                    }
                }

                if ((securityRole.ObjectType == "sm" || securityRole.ObjectType == "submodelElement") &&
                    securityRole.Submodel == aasResource && securityRole.Permission == neededRights)
                {
                    if (securityRole.Kind == KindOfPermissionEnum.Deny)
                    {
                        if (objPath.Length >= securityRole.ObjectPath.Length) // deny in tree above
                        {
                            if (securityRole.ObjectPath == objPath.Substring(0, securityRole.ObjectPath.Length))
                                deepestDeny = securityRole.ObjectPath;
                        }

                        if (securityRole.ObjectPath.Length >= objPath.Length) // deny in tree below
                        {
                            if (objPath == securityRole.ObjectPath.Substring(0, objPath.Length))
                            {
                                error = "DENY " + securityRole.ObjectPath;
                                return false;
                            }
                        }
                    }

                    if (securityRole.Kind == KindOfPermissionEnum.Allow)
                    {
                        if (objPath.Length >= securityRole.ObjectPath.Length) // allow in tree above
                        {
                            if (securityRole.ObjectPath == objPath.Substring(0, securityRole.ObjectPath.Length))
                            {
                                deepestAllow = securityRole.ObjectPath;
                                withAllow = true;
                                deepestAllowRole = securityRole;
                            }
                        }
                    }
                }
            }

            if (deepestAllow == "")
            {
                error = "ALLOW not defined";
                return false;
            }

            if (deepestDeny.Length > deepestAllow.Length)
            {
                error = "DENY " + deepestDeny;
                return false;
            }

            return CheckPolicy(out error, deepestAllowRole, out getPolicy, policy);
            //return true;
        }

        private static bool CheckAccessLevelEmptyObjPath(string currentRole, string operation, string? aasResourceType, IClass? aasResource, AccessRights neededRights,
                                                         out string error)
        {
            //error = string.Empty;
            if (GlobalSecurityVariables.SecurityRoles != null)
            {
                foreach (var securityRole in GlobalSecurityVariables.SecurityRoles)
                {
                    if (aasResourceType == "aas" && securityRole.ObjectType == "aas")
                    {
                        var aas = aasResource as IAssetAdministrationShell;
                        //if (aasResourceType != null && securityRole.ObjectReference == aasResource && securityRole.Permission == neededRights)
                        if (aasResourceType != null && (aas.EqualsAas((IAssetAdministrationShell)securityRole.ObjectReference) || securityRole.AAS == "*") &&
                            securityRole.Permission == neededRights)
                        {
                            if ((securityRole.Condition == "" && securityRole.Name == currentRole) ||
                                (securityRole.Condition == "not" && securityRole.Name != currentRole))
                            {
                                if (securityRole.Kind == KindOfPermissionEnum.Allow)
                                {
                                    error = "";
                                    return true;
                                }

                                if (securityRole.Kind == KindOfPermissionEnum.Deny)
                                {
                                    error = "DENY AAS " + (aasResource as AssetAdministrationShell).Id;
                                    return false;
                                }
                            }
                        }
                    }

                    // TODO (jtikekar, 2023-09-04): remove
                    //if (securityRole.Name == currentRole && securityRole.ObjectType == "api" &&
                    //    securityRole.Permission == neededRights)
                    //{
                    //    if (securityRole.ApiOperation == "*" || MatchApiOperation(securityRole.ApiOperation, operation))
                    //    {
                    //        if (securityRole.Permission == neededRights)
                    //        {
                    //            return CheckUsage(out error, securityRole);
                    //        }
                    //    }
                    //}
                }
            }

            error = "ALLOW not defined";
            return false;
        }

        private static bool MatchApiOperation(string apiOperation, string operation)
        {
            if (apiOperation == operation)
                return true;

            var apiOpSplit = apiOperation.Split('/');
            var opSplit = operation.Split('/');

            var i = 0;
            while (i < apiOpSplit.Length && i < opSplit.Length)
            {
                if (apiOpSplit[i] == "*")
                {
                    return true;
                }
                if (apiOpSplit[i] != opSplit[i])
                {
                    return false;
                }
                i++;
            }
            if (apiOpSplit.Length > i)
            {
                return false;
            }
            return true;

            /*
            bool match = false;
            if (apiOpSplit.Length == opSplit.Length)
            {
                for (var i = 0; i < apiOpSplit.Length; i++)
                {
                    if (apiOpSplit[i].Equals(opSplit[i]))
                    {
                        match = true;
                    }
                    else if (apiOpSplit[i].StartsWith("{"))
                    {
                        continue;
                    }
                    else
                    {
                        match = false;
                    }
                }

                return match;
            }
            */

            return false;
        }

        private static bool CheckUsage(out string error, SecurityRole securityRole)
        {
            error = "";
            if (securityRole.Usage == null)
            {
                return true;
            }

            foreach (var sme in securityRole.Usage.Value)
            {
                switch (sme.IdShort)
                {
                    case "accessPerDuration":
                        if (sme is SubmodelElementCollection smc)
                        {
                            Property maxCount = null;
                            Property actualCount = null;
                            Property duration = null;
                            Property actualTime = null;
                            foreach (var sme2 in smc.Value)
                            {
                                switch (sme2.IdShort)
                                {
                                    case "maxCount":
                                        maxCount = sme2 as Property;
                                        break;
                                    case "duration":
                                        duration = sme2 as Property;
                                        break;
                                    case "actualCount":
                                        actualCount = sme2 as Property;
                                        break;
                                    case "actualTime":
                                        actualTime = sme2 as Property;
                                        break;
                                }
                            }

                            if (maxCount == null || duration == null || actualCount == null || actualTime == null)
                                return false;
                            int d = 0;
                            if (!int.TryParse(duration.Value, out d))
                            {
                                return false;
                            }

                            DateTime dt = new DateTime();
                            if (actualTime.Value != null && actualTime.Value != "")
                            {
                                try
                                {
                                    dt = DateTime.Parse(actualTime.Value);
                                    if (dt.AddSeconds(d) < DateTime.UtcNow)
                                    {
                                        Program.signalNewData(0);
                                        actualTime.Value = null;
                                    }
                                }
                                catch
                                {
                                }
                            }

                            if (actualTime.Value == null || actualTime.Value == "")
                            {
                                actualTime.Value = DateTime.UtcNow.ToString();
                                actualCount.Value = null;
                            }

                            if (actualCount.Value == null || actualCount.Value == "")
                            {
                                actualCount.Value = "0";
                            }

                            int ac = 0;
                            if (!int.TryParse(actualCount.Value, out ac))
                            {
                                Program.signalNewData(0);
                                return false;
                            }

                            int mc = 0;
                            if (!int.TryParse(maxCount.Value, out mc))
                            {
                                Program.signalNewData(0);
                                return false;
                            }

                            ac++;
                            actualCount.Value = ac.ToString();
                            if (ac <= mc)
                            {
                                Program.signalNewData(0);
                                return true;
                            }
                        }

                        break;
                }
            }

            Program.signalNewData(0);
            return false;
        }

        public string GetSecurityRules(out List<Dictionary<string, string>> condition)
        {
            condition = new List<Dictionary<string, string>>();
            if (_condition != null)
            {
                condition = _condition;
            }

            string rules = "";

            foreach (var r in GlobalSecurityVariables.SecurityRoles)
            {
                if (r.Condition != null)
                    rules += r.Condition;
                if (r.Name != null)
                    rules += r.Name;
                rules += "\t";
                if (r.Kind != null)
                    rules += r.Kind.ToString();
                rules += "\t";
                if (r.Permission != null)
                    rules += r.Permission.ToString();
                rules += "\t";
                if (r.ObjectType != null)
                    rules += r.ObjectType;
                rules += "\t";
                if (r.ApiOperation != null)
                    rules += r.ApiOperation;
                rules += "\t";
                if (r.ObjectType != null)
                    rules += r.ObjectType;
                rules += "\t";
                if (r.SemanticId != null)
                    rules += r.SemanticId;
                rules += "\t";
                if (r.RulePath != null)
                    rules += r.RulePath;
                rules += "\n";
            }

            return rules;
        }
    }
}

