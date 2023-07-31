namespace AasSecurity.Models
{
    internal class Permission
    {
        internal KindOfPermissionEnum? KindOfPermission { get; set; }
        //TODO:jtikekar change to string
        internal List<string> Permissions { get; set; }
    }
}