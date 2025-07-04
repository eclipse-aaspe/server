namespace Contracts.Security;
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

    public NeededRights NeededRightsClaim { get; set; }


    //public void SetIdShortPathClaim(string requestedIdShortPath, string idShortPathFromDB);
}
