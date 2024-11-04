namespace Contracts
{
    public interface IContractSecurityRules
    {
        public void ClearSecurityRules();
        public void AddSecurityRule(string name, string acccess, string right, string objectType, string semanticId);
    }
}
