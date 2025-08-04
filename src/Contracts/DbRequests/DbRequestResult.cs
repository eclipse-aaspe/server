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
using AdminShellNS.Models;
using Contracts.QueryResult;

public class DbRequestResult
{
    public DbRequestPackageEnvResult PackageEnv { get; set; }

    public List<IAssetAdministrationShell> AssetAdministrationShells { get; set; }
    public List<IReference> References { get; set; }
    public List<ISubmodel> Submodels { get; set; }
    public List<PackageDescription> PackageDescriptions { get; set; }
    public List<string> Ids { get; set; }

    public List<ISubmodelElement> SubmodelElements { get; set; }
    public List<IConceptDescription> ConceptDescriptions { get; set; }
    public AasCore.Aas3_0.Environment Environment { get; set; }

    public DbFileRequestResult FileRequestResult { get; set; }

    public IAssetInformation AssetInformation { get; set; }

    public Events.EventPayload EventPayload { get; set; }

    // Queries
    public QResult QueryResult { get; set; }
    public int Count { get; set; }
}

