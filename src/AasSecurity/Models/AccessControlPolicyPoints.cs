namespace AasSecurity.Models
{
    internal class AccessControlPolicyPoints
    {
        public PolicyAdministrationPoint PolicyAdministrationPoint { get; set; }

        public PolicyDecisionPoint PolicyDecisionPoint { get; set; }

        public PolicyEnforcementPoint PolicyEnforcementPoint { get; set; }
    }
}
