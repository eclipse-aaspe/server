using AasSecurity.Models;

namespace AasSecurity
{
    public interface ISecurityService
    {
        bool AuthorizeRequest(string accessRole, string httpRoute, AccessRights neededRights, out string error, out bool withAllow, out string getPolicy, string objPath = null, string aasResourceType = null,
                IClass aasResource = null, string policy = null);

        string GetSecurityRules();
    }
}
