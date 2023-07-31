using AasSecurity.Exceptions;
using AasSecurity.Models;
using AasxServer;
using AasxServerStandardBib.Logging;
using Jose;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace AasSecurity
{
    public class SecurityService : ISecurityService
    {
        private static ILogger _logger = ApplicationLogging.CreateLogger("SecurityService");

        //TODO:jtikekar uncomment
        //public SecurityService(IAppLogger<SecurityService> logger)
        //{
        //    _logger = logger;
        //}


        public void SecurityCheckInit(HttpContext context, string route, string httpOperation)
        {
            //TODO:jtikekar @Andreas purpose of index
            int index = -1;
            if (!GlobalSecurityVariables.WithAuthentication)
            {
                return;
            }

            //Retrieve security related query strings from the request
            NameValueCollection queries = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());

            //Retrieve headers from the request
            NameValueCollection headers = new NameValueCollection();
            foreach (var header in context.Request.Headers)
            {
                headers.Add(header.Key, header.Value.FirstOrDefault());
            }

            var accessRole = GetAccessRole(queries, headers, index);
            //TODO:jtikekar what if accessRole is null?
            var aasSecurityContext = new AasSecurityContext(accessRole, route, httpOperation);
            context.Items.Add("AasSecurityContext", aasSecurityContext);
        }

        private string? GetAccessRole(NameValueCollection queries, NameValueCollection headers, int index)
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
                if (accessRole == null)
                {
                    return accessRole;
                }
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

            //TODO:jtikekar refactor
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
                                    //TODO:jtikekar support basic auth
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
                                        return AccessRights.CREATE;
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
                                //TODO:jtikekar support
                            }
                            break;
                        }
                }
            }

            return null;
        }

        public void SecurityCheck(HttpContext httpContext, string objPath = "", string aasResourceType = null, IClass aasResource = null)
        {
            if (!GlobalSecurityVariables.WithAuthentication)
                return;

            //Get AasSecurityContext
            var aasSecurityContext = httpContext.Items["AasSecurityContext"] as AasSecurityContext ?? throw new Exception($"AasSecurityContext not set.");

            CheckAccessRights(aasSecurityContext.AccessRole, aasSecurityContext.Route, aasSecurityContext.NeededRights,
                objPath, aasResourceType, aasResource);
        }

        private static bool CheckAccessRights(string currentRole, string operation, AccessRights neededRights,
            string objPath = "", string aasResourceType = null, IClass aasResource = null, bool testOnly = false)
        {
            bool withAllow = false;
            return CheckAccessRightsWithAllow(currentRole, operation, neededRights, out withAllow,
                objPath, aasResourceType, aasResource, testOnly);
        }

        private static bool CheckAccessRightsWithAllow(string currentRole, string operation, AccessRights neededRights, out bool withAllow,
           string objPath = "", string aasResourceType = null, IClass aasResource = null, bool testOnly = false)
        {
            string error = "Access not allowed";
            withAllow = false;

            if (Program.secretStringAPI != null)
            {
                //TODO:jtikekar check with Andreas
                if (neededRights == AccessRights.READ)
                    return true;
                //TODO:jtikekar @andreas, why currentRole as accessRight?
                if ((neededRights == AccessRights.UPDATE || neededRights == AccessRights.DELETE) && currentRole == "UPDATE")
                    return true;
                if (currentRole == "CREATE")
                {
                    return true;
                }
            }
            else
            {
                if (CheckAccessLevelWithError(
                    out error, currentRole, operation, neededRights, out withAllow,
                    objPath, aasResourceType, aasResource))
                    return true;

                if (currentRole == null)
                {
                    //TODO:jtikekar @Andreas, do we need this code?
                    /*
                    if (AasxServer.Program.redirectServer != "")
                    {
                        System.Collections.Specialized.NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                        string originalRequest = context.Request.Url.ToString();
                        queryString.Add("OriginalRequest", originalRequest);
                        Console.WriteLine("\nRedirect OriginalRequset: " + originalRequest);
                        string response = AasxServer.Program.redirectServer + "?" + "authType=" + AasxServer.Program.authType + "&" + queryString;
                        Console.WriteLine("Redirect Response: " + response + "\n");
                        SendRedirectResponse(context, response);
                        return false;
                    }
                    */
                }
            }

            /*
            dynamic res = new ExpandoObject();
            res.error = "You are not authorized for this operation!";
            context.Response.StatusCode = HttpStatusCode.Unauthorized;
            SendJsonResponse(context, res);
            */

            // Exception
            if (!testOnly)
            {
                throw new NotAllowed(error);
            }

            return false;
        }

        private static bool CheckAccessLevelWithError(out string error, string currentRole, string operation, AccessRights neededRights, out bool withAllow, string objPath, string aasResourceType, IClass aasResource)
        {
            error = "";
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

            if (objPath == string.Empty)
            {
                return CheckAccessLevelEmptyObjPath(currentRole, operation, aasResourceType, aasResource, neededRights, out error);
            }

            if (objPath != string.Empty && (operation.Contains("/submodels") || (operation.Equals("/submodels") && operation.Equals("/submodel-elements"))))
            {
                return CheckAccessLevelForOperation(currentRole, operation, aasResourceType, aasResource, neededRights, objPath, out withAllow, out error);
            }

            error = "ALLOW not defined";
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

                if (!securityRole.ObjectType.Equals("semanticid", StringComparison.OrdinalIgnoreCase))
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
                    //TODO:jtikekar @Andreas, where aasResource is a string?
                    //if (aasResource is string s2)
                    //{
                    //    if (s2 != null && s2 != "")
                    //    {
                    //        if (securityRole.semanticId == s2)
                    //        {
                    //            if (securityRole.kind == "allow")
                    //            {
                    //                if (deepestAllow == "")
                    //                {
                    //                    deepestAllow = objPath;
                    //                    withAllow = true;
                    //                }
                    //            }
                    //            if (securityRole.kind == "deny")
                    //            {
                    //                if (deepestDeny == "")
                    //                    deepestDeny = objPath;
                    //            }
                    //        }
                    //    }
                    //}
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
            error = string.Empty;
            if (GlobalSecurityVariables.SecurityRoles != null)
            {
                foreach (var securityRole in GlobalSecurityVariables.SecurityRoles)
                {
                    if (aasResourceType == "aas" && securityRole.ObjectType == "aas")
                    {
                        if (aasResourceType != null && securityRole.ObjectReference == aasResource && securityRole.Permission == neededRights)
                        {
                            if ((securityRole.Condition == "" && securityRole.Name == currentRole) ||
                                    (securityRole.Condition == "not" && securityRole.Name != currentRole))
                            {
                                if (securityRole.Kind == KindOfPermissionEnum.Allow)
                                    return true;
                                if (securityRole.Kind == KindOfPermissionEnum.Deny)
                                {
                                    error = "DENY AAS " + (aasResource as AssetAdministrationShell).Id;
                                    return false;
                                }
                            }
                        }
                    }

                    if (securityRole.Name == currentRole && securityRole.ObjectType == "api" &&
                        securityRole.Permission == neededRights)
                    {
                        if (securityRole.ApiOperation == "*" || securityRole.ApiOperation == operation)
                        {
                            if (securityRole.Permission == neededRights)
                            {
                                return CheckUsage(out error, securityRole);
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool CheckUsage(out string error, SecurityRole securityRole)
        {
            //TODO:jtikekar need to support
            throw new NotImplementedException();
        }
    }
}
