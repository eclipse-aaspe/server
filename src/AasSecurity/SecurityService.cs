using AasSecurity.Models;
using AasxServer;
using AasxServerStandardBib.Logging;
using Extensions;
using Jose;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace AasSecurity
{
    public class SecurityService : ISecurityService
    {
        private static ILogger _logger = ApplicationLogging.CreateLogger("SecurityService");

        public AuthenticationTicket AuthenticateRequest(HttpContext context, string route, string httpOperation, string authenticationSchemeName)
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
            }

            var accessRole = GetAccessRole(queries, headers);
            if (accessRole == null)
            {
                _logger.LogDebug($"Access Role found null. Hence setting the access role as isNotAuthenticated.");
                accessRole = "isNotAuthenticated";
            }

            _logger.LogInformation($"Access role in authentication: {accessRole}");
            var aasSecurityContext = new AasSecurityContext(accessRole, route, httpOperation);
            //Create claims
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Role, aasSecurityContext.AccessRole),
                    new Claim("NeededRights", aasSecurityContext.NeededRights.ToString()),
                };

            var identity = new ClaimsIdentity(claims, authenticationSchemeName);
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            return new AuthenticationTicket(principal, authenticationSchemeName);
        }

        private string? GetAccessRole(NameValueCollection queries, NameValueCollection headers)
        {
            _logger.LogDebug("Getting the access rights.");
            string? accessRole = null;
            string? user = null;
            bool error = false;
            string? bearerToken = null;

            ParseBearerToken(queries, headers, ref bearerToken, ref error, ref user, ref accessRole);
            if (accessRole != null)
            {
                return accessRole;
            }

            if (!error)
            {
                accessRole = HandleBearerToken(bearerToken, ref user, ref error);
                //if (accessRole == null)
                //{
                //    return accessRole;
                //}
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
                    foreach (var securityRight in securityRights)
                    {
                        if (!securityRight.Name.Contains('@'))
                        {
                            string domain = null;
                            if (user.Contains('@'))
                            {
                                string[] split = user.Split('@');
                                domain = split[1];
                            }
                            if (domain != null && domain.Equals(securityRight.Name))
                            {
                                accessRole = securityRight.Role;
                                return accessRole;
                            }
                        }
                    }
                }
            }

            return accessRole;
        }

        private string? HandleBearerToken(string? bearerToken, ref string? user, ref bool error)
        {
            if (bearerToken == null)
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(bearerToken);
                if (jwtSecurityToken != null)
                {
                    if (jwtSecurityToken.Claims != null)
                    {
                        var emailClaim = jwtSecurityToken.Claims.Where(c => c.Type.Equals("email"));
                        if (!emailClaim.IsNullOrEmpty())
                        {
                            var email = emailClaim.First().Value;
                            if (!string.IsNullOrEmpty(email))
                            {
                                user = email.ToLower();
                            }
                        }
                        var serverNameClaim = jwtSecurityToken.Claims.Where(c => c.Type.Equals("serverName"));
                        if (!serverNameClaim.IsNullOrEmpty())
                        {
                            var serverName = serverNameClaim.First().Value;
                            if (!string.IsNullOrEmpty(serverName))
                            {
                                X509Certificate2? cert = SecurityHelper.FindServerCertificate(serverName);
                                if (cert == null) return null;

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
                                }
                                catch
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                serverName = "keycloak";
                            }
                        }

                        var userNameClaim = jwtSecurityToken.Claims.Where(c => c.Type.Equals("userName"));
                        if (!userNameClaim.IsNullOrEmpty())
                        {
                            var userName = userNameClaim.First().Value;
                            if (!string.IsNullOrEmpty(userName))
                            {
                                user = userName.ToLower();
                            }
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

            // TODO (jtikekar, 2023-09-04): refactor
            return "";
        }

        private AccessRights? ParseBearerToken(NameValueCollection queries, NameValueCollection headers, ref string? bearerToken, ref bool error, ref string? user, ref string? accessRights)
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
                                string[] split = token.Split(new Char[] { ' ', '\t' });
                                if (split[0].ToLower().Equals("bearer"))
                                {
                                    _logger.LogDebug($"Received bearer token {split[1]}");
                                    bearerToken = split[1];
                                }
                                else if (split[0].ToLower().Equals("basic") && bearerToken == null)
                                {
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
                                                // accessrights = "READ";
                                                {
                                                    if (p == Program.secretStringAPI)
                                                        accessRights = "CREATE";
                                                }
                                                _logger.LogDebug("accessrights " + accessRights);
                                                AccessRights output = (AccessRights)Enum.Parse(typeof(AccessRights), accessRights);
                                                return output;
                                            }
                                        }

                                        string username = CheckUserPW(split[1]);
                                        if (username != null)
                                        {
                                            user = username;
                                            Console.WriteLine("Received username+password http header = " + user);
                                        }
                                    }
                                    catch { }
                                }
                            }
                            break;
                        }
                    case "email":
                        {
                            var token = headers[key];
                            if (token != null)
                            {
                                _logger.LogDebug($"Received email token from header: {token}");
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
                            string secretQuery = queries["s"]!;
                            if (!secretQuery.IsNullOrEmpty())
                            {
                                _logger.LogDebug($"Received token of type s: {secretQuery}");
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
                                _logger.LogDebug($"Received token of type bear {token}");
                                bearerToken = token;
                            }
                            break;
                        }
                    case "email":
                        {
                            var token = queries[key];
                            if (token != null)
                            {
                                _logger.LogDebug($"Received token of type email {token}");
                                user = token;
                                error = false;
                            }
                            break;
                        }
                    case "_up":
                        {
                            var token = queries[key];
                            if (token != null)
                            {
                                _logger.LogDebug($"Received token of type username-password {token}");
                                try
                                {
                                    string username = CheckUserPW(token);
                                    if (username != null)
                                    {
                                        user = username;
                                        _logger.LogDebug("Received username+password query string = " + user);
                                    }
                                }
                                catch { }
                            }
                            break;
                        }
                }
            }

            return null;
        }

        private string CheckUserPW(string userPW64)
        {
            var credentialBytes = Convert.FromBase64String(userPW64);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            string username = credentials[0];
            string password = credentials[1];

            var found = GlobalSecurityVariables.SecurityUsernamePassword.TryGetValue(username, out string storedPassword);
            if (found)
            {
                if (password.Equals(storedPassword))
                {
                    return (username);
                }
            }

            return null;
        }

        public bool AuthorizeRequest(string accessRole, string httpRoute, AccessRights neededRights, out string error, out bool withAllow, string objPath = null, string aasResourceType = null, IClass aasResource = null)
        {
            return CheckAccessRights(accessRole, httpRoute, neededRights, out error, out withAllow, objPath, aasResourceType, aasResource);
        }

        private static bool CheckAccessRights(string currentRole, string operation, AccessRights neededRights, out string error, out bool withAllow,
            string objPath = "", string aasResourceType = null, IClass aasResource = null, bool testOnly = false)
        {
            withAllow = false;
            return CheckAccessRightsWithAllow(currentRole, operation, neededRights, out error, out withAllow,
                objPath, aasResourceType, aasResource, testOnly);
        }

        private static bool CheckAccessRightsWithAllow(string currentRole, string operation, AccessRights neededRights, out string error, out bool withAllow,
           string objPath = "", string aasResourceType = null, IClass aasResource = null, bool testOnly = false)
        {
            error = "Access not allowed";
            withAllow = false;

            if (Program.secretStringAPI != null)
            {
                if (currentRole == "CREATE")
                {
                    return true;
                }
            }
            else
            {
                // TODO (jtikekar, 2023-09-04): uncomment
                if (CheckAccessLevelWithError(
                    out error, currentRole, operation, neededRights, out withAllow,
                    objPath, aasResourceType, aasResource))
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

        private static bool CheckAccessLevelWithError(out string error, string currentRole, string operation, AccessRights neededRights, out bool withAllow, string objPath, string aasResourceType, IClass aasResource)
        {
            withAllow = false;

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
                return CheckAccessLevelApi(currentRole, operation, neededRights, out error);
            }

            if (string.IsNullOrEmpty(objPath))
            {
                return CheckAccessLevelEmptyObjPath(currentRole, operation, aasResourceType, aasResource, neededRights, out error);
            }

            if (objPath != string.Empty && (operation.Contains("/submodel-elements") || operation.Contains("/submodels")))
            {
                return CheckAccessLevelForOperation(currentRole, operation, aasResourceType, aasResource, neededRights, objPath, out withAllow, out error);
            }

            error = "ALLOW not defined";
            return false;
        }

        private static bool CheckAccessLevelApi(string currentRole, string operation, AccessRights neededRights, out string error)
        {
            if (GlobalSecurityVariables.SecurityRoles != null)
            {
                foreach (var securityRole in GlobalSecurityVariables.SecurityRoles)
                {
                    if (securityRole.Name == currentRole && securityRole.ObjectType == "api" &&
                        securityRole.Permission == neededRights)
                    {
                        if (securityRole.ApiOperation == "*" || MatchApiOperation(securityRole.ApiOperation, operation))
                        {
                            if (securityRole.Permission == neededRights)
                            {
                                return CheckUsage(out error, securityRole);
                            }
                        }
                    }
                }
            }

            error = "API access NOT allowed!";
            return false;
        }

        private static bool CheckAccessLevelForOperation(string currentRole, string operation, string aasResourceType, IClass aasResource, AccessRights neededRights, string objPath, out bool withAllow, out string error)
        {
            error = "";
            withAllow = false;
            string deepestDeny = "";
            string deepestAllow = "";
            foreach (var securityRole in GlobalSecurityVariables.SecurityRoles)
            {
                if (!securityRole.Name.Equals(currentRole))
                    continue;

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
            return true;
        }

        private static bool CheckAccessLevelEmptyObjPath(string currentRole, string operation, string aasResourceType, IClass aasResource, AccessRights neededRights, out string error)
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
                        if (aasResourceType != null && (aas.EqualsAas((IAssetAdministrationShell)securityRole.ObjectReference) || securityRole.AAS == "*") && securityRole.Permission == neededRights)
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
            bool match = false;
            if (apiOpSplit.Length == opSplit.Length)
            {
                for (int i = 0; i < apiOpSplit.Length; i++)
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
                                catch { }
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

        public string GetSecurityRules()
        {
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
