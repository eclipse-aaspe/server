namespace AasxServerDB.Tests;

using AasSecurity;
using Contracts;

/// <summary>
/// Parses an inline <c>AllAccessPermissionRules</c> JSON document into the process-wide access-rule
/// caches and hands back a handle that restores the previous values.
/// <para>
/// Two pieces of global state have to be managed: <c>QueryGrammarJSON.ParseAccessRules</c> reads
/// <c>jsonschema-access.txt</c> from the current working directory (so the cwd has to point at
/// src/AasxServerBlazor for the duration of the parse), and the parsed result lands in public static
/// fields that outlive the test. Tests must dispose the handle so they cannot leak rules into each
/// other — see <see cref="AccessRulesCollection"/>.
/// </para>
/// </summary>
internal sealed class AccessRulesTestHost : IDisposable
{
    private readonly AllAccessPermissionRules? _previousGrammarRules;
    private readonly SqlConditions? _previousAccumulator;
    private readonly AllAccessPermissionRules? _previousServiceRules;

    private AccessRulesTestHost()
    {
        _previousGrammarRules = QueryGrammarJSON._accessRules;
        _previousAccumulator = QueryGrammarJSON.allAccessRuleSqlConditions;
        _previousServiceRules = SecurityService._accessRules;
    }

    /// <summary>
    /// Parses <paramref name="expression"/> and publishes it to both
    /// <c>QueryGrammarJSON._accessRules</c> and <c>SecurityService._accessRules</c>, the way
    /// <c>SecurityService.parseAccessRuleFile</c> does for the real accessrules.txt.
    /// </summary>
    internal static AccessRulesTestHost Parse(string expression)
    {
        var host = new AccessRulesTestHost();

        var blazorDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AasxServerBlazor"));
        var grammar = new QueryGrammarJSON(new NoSecurityRules());
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(blazorDir);
            grammar.ParseAccessRules(expression);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }

        SecurityService._accessRules = QueryGrammarJSON._accessRules;
        return host;
    }

    public void Dispose()
    {
        QueryGrammarJSON._accessRules = _previousGrammarRules!;
        QueryGrammarJSON.allAccessRuleSqlConditions = _previousAccumulator;
        SecurityService._accessRules = _previousServiceRules;
    }
}
