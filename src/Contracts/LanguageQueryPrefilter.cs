/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System.Linq;
using AasCore.Aas3_0;

namespace Contracts;

/// <summary>
/// BCP-47 validation for <c>sme.language</c> in JSON query expressions (aligned with AAS verification and DB join semantics).
/// </summary>
public static class LanguageQueryPrefilter
{
    /// <summary>Returns true if <paramref name="literal"/> is a non-empty BCP 47 tag per AAS verification.</summary>
    public static bool TryValidateLanguageLiteral(string literal, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(literal))
            return false;
        var t = literal.Trim();
        if (Verification.VerifyBcp47LanguageTag(t).Any())
            return false;
        normalized = t;
        return true;
    }
}
