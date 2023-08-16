using AasSecurity.Exceptions;
using AasSecurity.Models;

namespace AasSecurity
{
    public class AasSecurityContext
    {
        public AasSecurityContext(string accessRole, string route, string httpOperation)
        {
            AccessRole = accessRole;
            Route = route;
            switch (httpOperation.ToLower())
            {
                case "post":
                    NeededRights = AccessRights.CREATE;
                    break;
                case "get":
                    NeededRights = AccessRights.READ;
                    break;
                case "put":
                    NeededRights = AccessRights.UPDATE;
                    break;
                case "delete":
                    NeededRights = AccessRights.DELETE;
                    break;
                case "patch":
                    NeededRights = AccessRights.UPDATE;
                    break;
                default:
                    throw new AuthorizationException($"Unsupported HTTP Operation {httpOperation}");
            }
        }

        internal string AccessRole { get; }
        internal string Route { get; }
        internal AccessRights NeededRights { get; set; }
    }
}
