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

namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.Events;


public enum DbEventRequestType
{
    Status,
    Submodels,
    SubmodelElements,
    Shells
}

public class DbEventRequest
{
    public AdminShellPackageEnv[] Env { get; set; }

    public int PackageIndex { get; set; }

    public string EventName { get; set; }

    public ISubmodel Submodel { get; set; }

    public string ExternalBlazor { get; set; }

    //For Read request
    public DbEventRequestType DbEventRequestType { get; set; }

    public bool IsWithPayload { get; set; }

    public int LimitSm { get; set; }

    public int LimitSme { get; set; }

    public int OffsetSm { get; set; }

    public int OffsetSme { get; set; }

    public string Time { get; set; } = string.Empty;

    //For Update request
    public string Body { get; set; } = string.Empty;
}

