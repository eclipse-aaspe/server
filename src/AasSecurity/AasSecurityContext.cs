using AasSecurity.Exceptions;
using AasSecurity.Models;

namespace AasSecurity;

public class AasSecurityContext
{
    public AasSecurityContext(string accessRole, string route, string httpOperation)
    {
        AccessRole = accessRole;
        Route = route;
        NeededRights = httpOperation.ToLower() switch
        {
            "post" => AccessRights.CREATE,
            "head" or "get" => AccessRights.READ,
            "put" => AccessRights.UPDATE,
            "delete" => AccessRights.DELETE,
            "patch" => AccessRights.UPDATE,
            _ => throw new AuthorizationException($"Unsupported HTTP Operation {httpOperation}")
        };
    }

    internal string AccessRole { get; }
    internal string Route { get; }
    internal AccessRights NeededRights { get; set; }
}