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

using AasSecurity.Exceptions;
using AasSecurity.Models;

namespace AasSecurity
{
    public class AasSecurityContext
    {
        public AasSecurityContext(string accessRole, string route, string httpOperation)
        {
            AccessRole = accessRole;
            Route = route;
            switch (httpOperation.ToLower())
            {
                case "post":
                    NeededRights = AccessRights.CREATE;
                    break;
                case "head":
                case "get":
                    NeededRights = AccessRights.READ;
                    break;
                case "put":
                    NeededRights = AccessRights.UPDATE;
                    break;
                case "delete":
                    NeededRights = AccessRights.DELETE;
                    break;
                case "patch":
                    NeededRights = AccessRights.UPDATE;
                    break;
                default:
                    throw new AuthorizationException($"Unsupported HTTP Operation {httpOperation}");
            }
        }

        internal string AccessRole { get; }
        internal string Route { get; }
        internal AccessRights NeededRights { get; set; }
    }
}
