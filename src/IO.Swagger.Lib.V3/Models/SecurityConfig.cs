namespace IO.Swagger.Lib.V3.Models;
//Move into Security

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Contracts;
using Contracts.DbRequests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class SecurityConfig : ISecurityConfig
{
    public SecurityConfig(bool noSecurity, ControllerBase controllerBase)
    {
        this.NoSecurity = noSecurity;
        this.Principal = controllerBase.User;
    }

    //private bool InitSecurity(ISecurityConfig? securityConfig, out string securityConditionSM, out string securityConditionSME)
    //{
    //    securityConditionSM = "";
    //    securityConditionSME = "";
    //    if (securityConfig != null && !securityConfig.NoSecurity)
    //    {
    //        securityConditionSM = _contractSecurityRules.GetConditionSM();
    //        securityConditionSME = _contractSecurityRules.GetConditionSME();
    //        // Get claims
    //        var authResult = false;
    //        var accessRole = securityConfig.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();
    //        var httpRoute = securityConfig.Principal.FindFirst("Route")?.Value;
    //        var neededRightsClaim = securityConfig.Principal.FindFirst("NeededRights")?.Value;
    //        if (accessRole != null && httpRoute != null && Enum.TryParse(neededRightsClaim, out AasSecurity.Models.AccessRights neededRights))
    //        {
    //            authResult = _contractSecurityRules.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _);
    //        }

    //        return authResult;
    //    }

    //    return true;
    //}

    public bool NoSecurity { get; }

    public ClaimsPrincipal Principal { get; set ; }

    //public void SetIdShortPathClaim(string requestedIdShortPath, string submodelIdShortPathFromDB)
    //{
    //    this.Principal.Claims.ToList().Add(new Claim("idShortPath", $"{submodelIdShortPathFromDB}.{requestedIdShortPath}"));
    //    var claimsList = new List<Claim>(Principal.Claims) { new Claim("IdShortPath", $"{submodelIdShortPathFromDB} . {requestedIdShortPath}") };
    //    var identity = new ClaimsIdentity(claimsList, "AasSecurityAuth");
    //    var principal = new System.Security.Principal.GenericPrincipal(identity, null);
    //    this.Principal = principal;
    //}
}
