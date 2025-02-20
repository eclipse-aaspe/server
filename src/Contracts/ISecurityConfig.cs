namespace Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

public interface ISecurityConfig
{
    public bool NoSecurity { get; }

    public ClaimsPrincipal Principal { get; set; }

    public string HttpRoute { get; set; }


    public void SetIdShortPathClaim(string requestedIdShortPath, string idShortPathFromDB);
}
