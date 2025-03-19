namespace IO.Swagger.Lib.V3.Models;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
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
