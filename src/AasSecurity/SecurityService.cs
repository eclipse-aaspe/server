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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace AasSecurity;

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
        var queries = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());

        //Retrieve headers from the request
        var headers = new NameValueCollection();
        foreach (var header in context.Request.Headers)
        {
            headers.Add(header.Key, header.Value.FirstOrDefault());
            if (!header.Key.Equals("FORCE-POLICY"))
            {
                continue;
            }

            Program.withPolicy = !header.Value.FirstOrDefault().Equals("OFF");
            _logger.LogDebug("FORCE-POLICY " + header.Value.FirstOrDefault());
        }

        var accessRole = GetAccessRole(queries, headers, out var policy, out var policyRequestedResource);
        if (accessRole == null)
        {
            _logger.LogDebug("Access Role found null. Hence setting the access role as isNotAuthenticated.");
            accessRole = "isNotAuthenticated";
        }

        _logger.LogInformation($"Access role in authentication: {accessRole}, policy: {policy}, policyRequestedResource: {policyRequestedResource}");
        var aasSecurityContext = new AasSecurityContext(accessRole, route, httpOperation);
        //Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, aasSecurityContext.AccessRole),
            new("NeededRights", aasSecurityContext.NeededRights.ToString()),
            new("Policy", policy)
        };

        var identity = new ClaimsIdentity(claims, authenticationSchemeName);
        var principal = new System.Security.Principal.GenericPrincipal(identity, null);
        return new AuthenticationTicket(principal, authenticationSchemeName);
    }

    private string? GetAccessRole(NameValueCollection queries, NameValueCollection headers, out string policy, out string policyRequestedResource)
    {
        _logger.LogDebug("Getting the access rights.");
        string? accessRole = null;
        string? user = null;
        bool error = false;
        string? bearerToken = null;
        policy = "";
        policyRequestedResource = "";

        ParseBearerToken(queries, headers, ref bearerToken, ref error, ref user, ref accessRole);
        if (accessRole != null)
        {
            return accessRole;
        }

        if (!error)
        {
            accessRole = HandleBearerToken(bearerToken, ref user, ref error, out policy, out policyRequestedResource);
        }

        if (string.IsNullOrEmpty(user)) return accessRole;
        var securityRights = GlobalSecurityVariables.SecurityRights;
        if (securityRights == null) return accessRole;
        foreach (var securityRight in securityRights.Where(securityRight => securityRight.Name.Contains('@'))
                     .Where(securityRight => user.Contains('@') && user.Equals(securityRight.Name)))
        {
            accessRole = securityRight.Role;
            return accessRole;
        }

        //Domain
        foreach (var securityRight in securityRights)
        {
            if (securityRight.Name.Contains('@')) continue;
            string domain = null;
            if (user.Contains('@'))
            {
                var split = user.Split('@');
                domain = split[1];
            }

            if (domain != null && domain.Equals(securityRight.Name))
            {
                accessRole = securityRight.Role;
                return accessRole;
            }

            if (user != securityRight.Name) continue;
            accessRole = securityRight.Role;
            return accessRole;
        }

        return accessRole;
    }

    private string? HandleBearerToken(string? bearerToken, ref string? user, ref bool error, out string policy, out string policyRequestedResource)
    {
        policy = string.Empty;
        policyRequestedResource = string.Empty;
        if (bearerToken == null)
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(bearerToken);
            if (jwtSecurityToken?.Claims != null)
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
                        var cert = SecurityHelper.FindServerCertificate(serverName);
                        if (cert == null) return null;

                        var builder = new StringBuilder();
                        builder.AppendLine("-----BEGIN CERTIFICATE-----");
                        builder.AppendLine(
                            Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
                        builder.AppendLine("-----END CERTIFICATE-----");
                        _logger.LogDebug("Token Server Certificate: " + serverName);
                        _logger.LogDebug(builder.ToString());
                        //check if server cert is correctly signed
                        try
                        {
                            JWT.Decode(bearerToken, cert.GetRSAPublicKey(), JwsAlgorithm.RS256); // correctly signed by auth server cert?
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }

                var policyClaim = jwtSecurityToken.Claims.Where(c => c.Type.Equals("policy"));
                if (!policyClaim.IsNullOrEmpty())
                {
                    policy = policyClaim.First().Value;
                }

                var policyRequestedResourceClaim = jwtSecurityToken.Claims.Where(c => c.Type.Equals("policyRequestedResource"));
                if (!policyRequestedResourceClaim.IsNullOrEmpty())
                {
                    policyRequestedResource = policyRequestedResourceClaim.First().Value;
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
        catch (Exception ex)
        {
            error = true;
            _logger.LogError($"Exception occurred while parsing the bearer token.{ex.Message}");
            _logger.LogDebug(ex.StackTrace);
        }

        return string.Empty;
    }

    private void ParseBearerToken(NameValueCollection queries, NameValueCollection headers, ref string? bearerToken, ref bool error, ref string? user, ref string? accessRights)
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
                        var split = token.Split(' ', '\t');
                        switch (split[0].ToLower())
                        {
                            case "bearer":
                                _logger.LogDebug($"Received bearer token {split[1]}");
                                bearerToken = split[1];
                                break;
                            case "basic" when bearerToken == null:
                                try
                                {
                                    if (Program.secretStringAPI != null)
                                    {
                                        var credentialBytes = Convert.FromBase64String(split[1]);
                                        var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] {':'}, 2);
                                        var u = credentials[0];
                                        var p = credentials[1];
                                        Console.WriteLine("Received username+password http header = " + u + " : " + p);

                                        if (u == "secret")
                                        {
                                            accessRights = "READ";
                                            {
                                                if (p == Program.secretStringAPI)
                                                    accessRights = "CREATE";
                                            }
                                            _logger.LogDebug("accessrights " + accessRights);
                                            return;
                                        }
                                    }

                                    var username = CheckUserPW(split[1]);
                                    if (username != null)
                                    {
                                        user = username;
                                        Console.WriteLine("Received username+password http header = " + user);
                                    }
                                }
                                catch
                                {
                                    // ignored
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
                    var secretQuery = queries["s"]!;
                    if (!secretQuery.IsNullOrEmpty())
                    {
                        _logger.LogDebug($"Received token of type s: {secretQuery}");
                        if (Program.secretStringAPI != null && secretQuery.Equals(Program.secretStringAPI))
                        {
                            return;
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
                            var username = CheckUserPW(token);
                            user = username;
                            _logger.LogDebug("Received username+password query string = " + user);
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    break;
                }
            }
        }
    }

    private string CheckUserPW(string userPW64)
    {
        var credentialBytes = Convert.FromBase64String(userPW64);
        var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] {':'}, 2);
        var username = credentials[0];
        var password = credentials[1];

        var found = GlobalSecurityVariables.SecurityUsernamePassword.TryGetValue(username, out string storedPassword);
        if (found)
        {
            return password.Equals(storedPassword) ? username : null;
        }

        return null;
    }

    public bool AuthorizeRequest(string accessRole, string httpRoute, AccessRights neededRights,
        out string? error, out bool withAllow, out string getPolicy, string? objPath = null, string aasResourceType = null,
        IClass aasResource = null, string? policy = null)
    {
        return CheckAccessRights(accessRole, httpRoute, neededRights, out error, out withAllow, out getPolicy, objPath, aasResourceType, aasResource, policy: policy);
    }

    private static bool CheckAccessRights(string currentRole, string operation, AccessRights neededRights, out string? error, out bool withAllow, out string getPolicy,
        string? objPath = "", string aasResourceType = null, IClass aasResource = null, bool testOnly = false, string? policy = null)
    {
        withAllow = false;
        return CheckAccessRightsWithAllow(currentRole, operation, neededRights, out error, out withAllow, out getPolicy,
            objPath, aasResourceType, aasResource, testOnly, policy);
    }

    private static bool CheckAccessRightsWithAllow(string currentRole, string operation, AccessRights neededRights, out string? error, out bool withAllow, out string getPolicy,
        string? objPath = "", string aasResourceType = null, IClass aasResource = null, bool testOnly = false, string? policy = null)
    {
        error = "Access not allowed";
        withAllow = false;
        getPolicy = "";

        if (Program.secretStringAPI != null && currentRole == "CREATE")
        {
            return true;
        }

        if (CheckAccessLevelWithError(
                out error, currentRole, operation, neededRights, out withAllow, out getPolicy,
                objPath, aasResourceType, aasResource, policy))
            return true;
        return !testOnly && false;
    }

    private static bool CheckAccessLevelWithError(out string? error, string currentRole, string operation, AccessRights neededRights, out bool withAllow, out string getPolicy,
        string? objPath, string aasResourceType, IClass aasResource, string? policy = null)
    {
        withAllow = false;
        getPolicy = string.Empty;

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
            return CheckAccessLevelForOperation(currentRole, aasResource, neededRights, objPath, out withAllow, out getPolicy, out error, policy);
        }

        error = "ALLOW not defined";
        return false;
    }

    private static bool CheckAccessLevelApi(string currentRole, string operation, AccessRights neededRights, out string? error, out string getPolicy)
    {
        getPolicy = string.Empty;
        if (GlobalSecurityVariables.SecurityRoles != null)
        {
            foreach (var securityRole in GlobalSecurityVariables.SecurityRoles.Where(securityRole => securityRole.Name == currentRole && securityRole.ObjectType == "api" &&
                                                                                                     securityRole.Permission == neededRights &&
                                                                                                     (securityRole.ApiOperation == "*" ||
                                                                                                      MatchApiOperation(securityRole.ApiOperation, operation)) &&
                                                                                                     securityRole.Permission == neededRights))
            {
                return CheckPolicy(out error, securityRole, out getPolicy);
            }
        }

        error = "API access NOT allowed!";
        return false;
    }

    private static bool CheckPolicy(out string? error, SecurityRole securityRole, out string getPolicy, string? policy = null)
    {
        error = "";
        getPolicy = "";
        Property pPolicy = null;
        AasCore.Aas3_0.File fPolicy = null;

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
                        if (!int.TryParse(duration.Value, out var d))
                        {
                            return false;
                        }

                        var dt = new DateTime();
                        if (!string.IsNullOrEmpty(actualTime.Value))
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
                                // ignored
                            }
                        }

                        if (string.IsNullOrEmpty(actualTime.Value))
                        {
                            actualTime.Value = DateTime.UtcNow.ToString();
                            actualCount.Value = null;
                        }

                        if (string.IsNullOrEmpty(actualCount.Value))
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
        if (string.IsNullOrEmpty(getPolicy) && fPolicy != null)
        {
            try
            {
                using var s = securityRole.UsageEnv.GetLocalStreamFromPackage(fPolicy.Value);
                using var mySHA256 = SHA256.Create();
                if (s != null)
                {
                    s.Position = 0;
                    var hashValue = mySHA256.ComputeHash(s);
                    getPolicy = Convert.ToHexString(hashValue);
                    Console.WriteLine("hash: " + getPolicy);
                    pPolicy.Value = getPolicy;
                }
            }
            catch
            {
                // ignored
            }
        }

        return policy == null || policy.Contains(getPolicy);
    }

    private static bool CheckAccessLevelForOperation(string currentRole, IClass aasResource, AccessRights neededRights,
        string? objPath, out bool withAllow, out string getPolicy, out string? error, string? policy = null)
    {
        error = string.Empty;
        withAllow = false;
        var deepestDeny = string.Empty;
        var deepestAllow = string.Empty;
        SecurityRole deepestAllowRole = null;
        getPolicy = string.Empty;
        foreach (var securityRole in GlobalSecurityVariables.SecurityRoles.Where(securityRole => securityRole.Name.Equals(currentRole)))
        {
            if (securityRole.ObjectType.Equals("semanticid", StringComparison.OrdinalIgnoreCase) && aasResource is Submodel submodel && securityRole.SemanticId != null && (securityRole.SemanticId == "*" || (submodel.SemanticId != null && submodel.SemanticId.Keys != null && submodel.SemanticId.Keys.Count != 0)))
            {
                if (securityRole.SemanticId == "*" || (string.Equals(securityRole.SemanticId, submodel.SemanticId?.Keys?[0].Value, StringComparison.CurrentCultureIgnoreCase)))
                {
                    switch (securityRole.Kind)
                    {
                        case KindOfPermissionEnum.Allow when string.IsNullOrEmpty(deepestAllow):
                            deepestAllow = submodel.IdShort!;
                            withAllow = true;
                            deepestAllowRole = securityRole;
                            break;
                        case KindOfPermissionEnum.Deny:
                        {
                            if (string.IsNullOrEmpty(deepestDeny))
                                deepestDeny = submodel.IdShort!;
                            break;
                        }
                        case KindOfPermissionEnum.NotApplicable:
                            break;
                        case KindOfPermissionEnum.Undefined:
                            break;
                        case null:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if (securityRole.ObjectType is "sm" or "submodelElement" &&
                securityRole.Submodel == aasResource && securityRole.Permission == neededRights)
            {
                switch (securityRole.Kind)
                {
                    case KindOfPermissionEnum.Deny:
                    {
                        if (objPath.Length >= securityRole.ObjectPath.Length && securityRole.ObjectPath == objPath.Substring(0, securityRole.ObjectPath.Length))
                        {
                            // deny in tree above
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

                        break;
                    }
                    case KindOfPermissionEnum.Allow:
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

                        break;
                    }
                    case KindOfPermissionEnum.NotApplicable:
                        break;
                    case KindOfPermissionEnum.Undefined:
                        break;
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        if (string.IsNullOrEmpty(deepestAllow))
        {
            error = "ALLOW not defined";
            return false;
        }

        if (deepestDeny.Length <= deepestAllow.Length) return CheckPolicy(out error, deepestAllowRole, out getPolicy, policy);
        error = "DENY " + deepestDeny;
        return false;

    }

    private static bool CheckAccessLevelEmptyObjPath(string currentRole, string operation, string aasResourceType, IClass aasResource, AccessRights neededRights,
        out string? error)
    {
        if (GlobalSecurityVariables.SecurityRoles != null)
        {
            foreach (var securityRole in GlobalSecurityVariables.SecurityRoles)
            {
                if (aasResourceType != "aas" || securityRole.ObjectType != "aas") continue;
                var aas = aasResource as IAssetAdministrationShell;
                        
                if (aasResourceType == null || (!aas.EqualsAas((IAssetAdministrationShell) securityRole.ObjectReference) && securityRole.AAS != "*") ||
                    securityRole.Permission != neededRights) continue;
                if ((securityRole.Condition != "" || securityRole.Name != currentRole) &&
                    (securityRole.Condition != "not" || securityRole.Name == currentRole)) continue;
                switch (securityRole.Kind)
                {
                    case KindOfPermissionEnum.Allow:
                        error = string.Empty;
                        return true;
                    case KindOfPermissionEnum.Deny:
                        error = $"DENY AAS {(aasResource as AssetAdministrationShell).Id}";
                        return false;
                    case KindOfPermissionEnum.NotApplicable:
                        break;
                    case KindOfPermissionEnum.Undefined:
                        break;
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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
        var match = false;
        if (apiOpSplit.Length != opSplit.Length) return false;
        for (var i = 0; i < apiOpSplit.Length; i++)
        {
            if (apiOpSplit[i].Equals(opSplit[i]))
            {
                match = true;
            }
            else if (apiOpSplit[i].StartsWith("{"))
            {
            }
            else
            {
                match = false;
            }
        }

        return match;

    }

    private static bool CheckUsage(out string error, SecurityRole securityRole)
    {
        error = string.Empty;
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
                        if (!int.TryParse(duration.Value, out var d))
                        {
                            return false;
                        }

                        var dt = new DateTime();
                        if (!string.IsNullOrEmpty(actualTime.Value))
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
                                // ignored
                            }
                        }

                        if (string.IsNullOrEmpty(actualTime.Value))
                        {
                            actualTime.Value = DateTime.UtcNow.ToString();
                            actualCount.Value = null;
                        }

                        if (string.IsNullOrEmpty(actualCount.Value))
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
            }
        }

        Program.signalNewData(0);
        return false;
    }

    public string GetSecurityRules()
    {
        var rules = string.Empty;

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