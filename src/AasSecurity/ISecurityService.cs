/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System.Security.Claims;
using AasCore.Aas3_1;
using AasSecurity.Models;
using Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AasSecurity
{
    public interface ISecurityService
    {
        AuthenticationTicket? AuthenticateRequest(HttpContext context, string route, string httpOperation, string? authenticationSchemeName = null);

        /// <summary>
        /// Effective merged SQL conditions for the role, right, and optional route (same merge as DB path).
        /// </summary>
        SqlConditions? GetSqlConditions(string accessRole, string neededRightsClaim, string? httpRoute = null, List<Claim>? tokenClaims = null);

        /// <summary>
        /// Tree preview: READ for <paramref name="sm"/> is true if <b>any</b> matching access rule passes in-memory (per-rule <c>sm.</c>/<c>sme.</c> correlation).
        /// Unlike <see cref="GetSqlConditions"/>, this does not use OR-merged scope columns across rules (which would mis-correlate sm/sme for the UI).
        /// </summary>
        bool EvaluateTreeSubmodelRead(string? accessRole, Submodel sm, string httpRoute, List<Claim>? tokenClaims = null);

        /// <summary>
        /// Tree preview: READ for <paramref name="objPath"/> if any matching rule passes (per-rule formulas; see <see cref="EvaluateTreeSubmodelRead"/>).
        /// </summary>
        bool EvaluateTreeSubmodelElementRead(string? accessRole, Submodel parentSubmodel, string objPath, string httpRoute, List<Claim>? tokenClaims = null);

        bool AuthorizeRequest(string accessRole,
                              string httpRoute,
                              AccessRights neededRights,
                              out string error,
                              out bool withAllow,
                              out string? getPolicy,
                              string objPath = null,
                              string? aasResourceType = null,
                              IClass? aasResource = null,
                              string? policy = null,
                              List<Claim> tokenClaims = null);

        string GetSecurityRules(out List<Dictionary<string, string>> condition);
    }
}