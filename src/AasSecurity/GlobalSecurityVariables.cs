using AasSecurity.Models;
using System.Security.Cryptography.X509Certificates;

namespace AasSecurity
{
    public static class GlobalSecurityVariables
    {
        public static bool WithAuthentication { get; set; }
        internal static List<SecurityRole> SecurityRoles = new();
        internal static List<X509Certificate2> ServerCertificates = new();
        internal static List<string> ServerCertFileNames = new();
        internal static List<SecurityRight> SecurityRights = new();
        internal static Dictionary<string, string> SecurityUsernamePassword = new();
    }
}
