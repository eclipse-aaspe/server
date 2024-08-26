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

using AasSecurity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AasSecurity
{
    public interface ISecurityService
    {
        AuthenticationTicket? AuthenticateRequest(HttpContext context, string route, string httpOperation, string? authenticationSchemeName = null);

        bool AuthorizeRequest(string accessRole,
                              string httpRoute,
                              AccessRights neededRights,
                              out string error,
                              out bool withAllow,
                              out string? getPolicy,
                              string objPath = null,
                              string? aasResourceType = null,
                              IClass? aasResource = null, string? policy = null);

        string GetSecurityRules();
    }
}