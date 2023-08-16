namespace AasSecurity.Models
{
    internal class Permission
    {
        internal KindOfPermissionEnum? KindOfPermission { get; set; }
        internal List<string> Permissions { get; set; }
    }
}