namespace AasxServerDB.Tests;

using System.Collections.Generic;
using System.Security.Claims;
using AasSecurity.Models;
using Contracts;

/// <summary>
/// Stub for IContractSecurityRules that disables all security checks.
/// Used in tests to run queries without authentication.
/// </summary>
internal sealed class NoSecurityRules : IContractSecurityRules
{
    public Dictionary<string, string> GetCondition(
        string accessRole, string neededRightsClaim,
        string? httpRoute = null, List<Claim>? tokenClaims = null)
        => new();

    public List<AccessPermissionRule> GetAccessRules(
        string accessRole, string neededRightsClaim,
        string? httpRoute = null, List<Claim>? tokenClaims = null)
        => new();

    public bool AuthorizeRequest(
        string accessRole, string httpRoute, AccessRights neededRights,
        out string error, out bool withAllow, out string? getPolicy,
        string objPath = null!, string? aasResourceType = null,
        AasCore.Aas3_0.IClass? aasResource = null, string? policy = null,
        List<Claim>? tokenClaims = null)
    {
        error = string.Empty;
        withAllow = true;
        getPolicy = null;
        return true;
    }

    public void ClearSecurityRules() { }

    public void AddSecurityRule(
        string name, string access, string right,
        string objectType, string semanticId, string route) { }
}
