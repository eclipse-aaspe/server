
namespace AasSecurity.Models
{
    internal class PolicyAdministrationPoint
    {
        internal bool? ExternalAccessControl { get; set; }

        internal AccessControl LocalAccessControl { get; set; }
    }
}