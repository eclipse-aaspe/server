using AasSecurity.Models;
using System.Security.Cryptography.X509Certificates;

namespace AasSecurity
{
    public static class GlobalSecurityVariables
    {
        public static bool WithAuthentication { get; set; }
        internal static List<SecurityRole> SecurityRoles = new List<SecurityRole>();
        internal static List<X509Certificate2> ServerCertificates = new List<X509Certificate2>();
        internal static List<string> ServerCertFileNames = new List<string>();
        internal static List<SecurityRight> SecurityRights = new List<SecurityRight>();
        internal static Dictionary<string, string> SecurityUsernamePassword = new Dictionary<string, string>();
    }
}
