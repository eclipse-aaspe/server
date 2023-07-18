using IO.Swagger.Lib.V3.Exceptions;

namespace IO.Swagger.Lib.V3.Security
{
    public class AasSecurityContext
    {
        public AasSecurityContext(string accessRights, string route, string httpOperation)
        {
            AccessRights = accessRights;
            Route = route;
            switch (httpOperation.ToLower())
            {
                case "post":
                    NeededRights = "CREATE";
                    break;
                case "get":
                    NeededRights = "READ";
                    break;
                //TODO:jtikekar change to "REPLACE"
                case "put":
                    NeededRights = "UPDATE";
                    break;
                case "delete":
                    NeededRights = "DELETE";
                    break;
                case "patch":
                    NeededRights = "UPDATE";
                    break;
                default:
                    throw new AuthorizationException($"Unsupported HTTP Operation {httpOperation}");
            }
        }

        public string AccessRights { get; }
        public string Route { get; }
        public string NeededRights { get; set; }
    }
}
