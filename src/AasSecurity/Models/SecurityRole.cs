namespace AasSecurity.Models
{
    internal class SecurityRole
    {
        internal string RulePath { get; set; }
        internal string Condition { get; set; }
        internal string Name { get; set; }
        internal string ObjectType { get; set; }
        internal string ApiOperation { get; set; }
        internal IClass ObjectReference { get; set; }
        internal string ObjectPath { get; set; }
        internal AccessRights Permission { get; set; }
        internal KindOfPermissionEnum? Kind { get; set; }
        internal ISubmodel Submodel { get; set; }
        internal string SemanticId { get; set; }
        internal ISubmodelElementCollection Usage { get; set; }
        internal string AAS { get; set; }
    }
}
