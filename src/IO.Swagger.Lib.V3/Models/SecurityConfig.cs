namespace IO.Swagger.Lib.V3.Models;
//Move into Security

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Contracts;
using Contracts.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class SecurityConfig : ISecurityConfig
{
    public SecurityConfig(bool noSecurity, ControllerBase controllerBase, NeededRights neededRights = NeededRights.TakeFromPrincipal)
    {
        this.NoSecurity = noSecurity;

        if (!noSecurity)
        {
            if (controllerBase != null)
            {
                this.Principal = controllerBase.User;
            }
            else
            {
                this.Principal = null;
            }

            NeededRightsClaim = neededRights;
        }
    }


    public bool NoSecurity { get; }

    public ClaimsPrincipal Principal { get; set ; }

    public NeededRights NeededRightsClaim { get; set; } = NeededRights.TakeFromPrincipal;

    //public void SetIdShortPathClaim(string requestedIdShortPath, string submodelIdShortPathFromDB)
    //{
    //    this.Principal.Claims.ToList().Add(new Claim("idShortPath", $"{submodelIdShortPathFromDB}.{requestedIdShortPath}"));
    //    var claimsList = new List<Claim>(Principal.Claims) { new Claim("IdShortPath", $"{submodelIdShortPathFromDB} . {requestedIdShortPath}") };
    //    var identity = new ClaimsIdentity(claimsList, "AasSecurityAuth");
    //    var principal = new System.Security.Principal.GenericPrincipal(identity, null);
    //    this.Principal = principal;

}
