namespace AasxServerDB.Tests;

/// <summary>
/// Groups every test that parses access rules, so they cannot interleave over the shared statics
/// (<c>QueryGrammarJSON._accessRules</c>, <c>QueryGrammarJSON.allAccessRuleSqlConditions</c>,
/// <c>SecurityService._accessRules</c>) or over the current working directory that
/// <c>ParseAccessRules</c> has to switch. Assembly-wide parallelization is already off
/// (see AssemblyInfo.cs); this collection documents the intent and keeps it enforced if that
/// assembly-level switch is ever relaxed.
/// </summary>
[CollectionDefinition(AccessRulesCollection.Name, DisableParallelization = true)]
public sealed class AccessRulesCollection
{
    public const string Name = "AccessRules";
}
