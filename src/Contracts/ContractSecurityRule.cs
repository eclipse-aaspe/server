namespace Contracts
{
    using System.Diagnostics.Contracts;
    using AasCore.Aas3_0;
    using AasSecurity.Models;

    public interface IContractSecurityRules
    {
        public string GetConditionSM();
        public string GetConditionSME();
        public QueryGrammarJSON GetGrammarJSON();

        bool AuthorizeRequest(string accessRole,
                      string httpRoute,
                      AccessRights neededRights,
                      out string error,
                      out bool withAllow,
                      out string? getPolicy,
                      string objPath = null,
                      string? aasResourceType = null,
                      IClass? aasResource = null, string? policy = null);

        public void ClearSecurityRules();
        public void AddSecurityRule(string name, string acccess, string right, string objectType, string semanticId, string route);
    }
}
