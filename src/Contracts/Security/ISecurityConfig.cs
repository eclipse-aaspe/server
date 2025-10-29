/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

namespace Contracts.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

public interface ISecurityConfig
{
    public bool NoSecurity { get; }

    public ClaimsPrincipal Principal { get; set; }

    public NeededRights NeededRightsClaim { get; set; }


    //public void SetIdShortPathClaim(string requestedIdShortPath, string idShortPathFromDB);
}

