using Xunit;

// Several tests in this assembly mutate process-global state that cannot be scoped to a collection:
//   * Directory.SetCurrentDirectory(...) — needed so QueryGrammarJSON.ParseAccessRules finds
//     jsonschema-access.txt next to accessrules.txt in src/AasxServerBlazor.
//   * the public static caches QueryGrammarJSON._accessRules,
//     QueryGrammarJSON.allAccessRuleSqlConditions and SecurityService._accessRules.
// Collections only serialize tests *within* a collection, so parallel collections would still race
// on those. Disabling parallelization assembly-wide is the only airtight fix, and it costs close to
// nothing here because the suite is dominated by the serialized DatabaseFixture anyway.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
