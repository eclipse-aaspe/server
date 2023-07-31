namespace AasSecurity.Models
{
    internal class AccessPermissionRule
    {
        internal List<PermissionsPerObject> PermissionsPerObject { get; set; }

        internal List<string> TargetSubjectAttributes { get; set; }
    }
}