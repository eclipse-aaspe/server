using AdminShellNS;

namespace AasSecurity.Models
{
    public class SecurityRole
    {
        public string?                     RulePath        { get; set; }
        public string?                     Condition       { get; set; }
        public string?                     Name            { get; set; }
        public string?                     ObjectType      { get; set; }
        public string?                     ApiOperation    { get; set; }
        public IClass?                     ObjectReference { get; set; }
        public string                      ObjectPath      { get; set; }
        public AccessRights?               Permission      { get; set; }
        public KindOfPermissionEnum?       Kind            { get; set; }
        public ISubmodel?                  Submodel        { get; set; }
        public string?                     SemanticId      { get; set; }
        public ISubmodelElementCollection? Usage           { get; set; }
        public string?                     AAS             { get; set; }
        public AdminShellPackageEnv?       UsageEnv        { get; set; }
        public bool QueryLanguage { get; set; }
    }
}