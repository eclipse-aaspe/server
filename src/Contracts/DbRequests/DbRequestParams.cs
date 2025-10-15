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
using Contracts.Pagination;

public class DbRequestParams
{
    //Db Ids
    public string AssetAdministrationShellIdentifier { get; set; }
    public string SubmodelIdentifier { get; set; }
    public string ConceptDescriptionIdentifier { get; set; }
    public string PackageIdentifier { get; set; }

    public string IdShort { get; set; }
    public List<ISpecificAssetId> AssetIds { get; set; }

    public List<string> AasIds { get; set; }
    public List<string> SubmodelIds { get; set; }

    //Bodies for Create and Replace
    public IAssetAdministrationShell AasBody { get; set; }
    public ISubmodel SubmodelBody { get; set; }
    public ISubmodelElement SubmodelElementBody { get; set; }
    public IConceptDescription ConceptDescriptionBody { get; set; }
    public IReference Reference { get; set; }
    public IAssetInformation AssetInformation { get; set; }

    //Metadata
    public IPaginationParameters PaginationParameters { get; set; }
    public bool First { get; set; }
    public bool IncludeCD { get; set; }
    public bool CreateAASXPackage { get; set; }
    public bool IsSigned { get; set; } = false;
    public bool IsSkipPayload { get; set; }


    public IReference IsCaseOf { get; set; }
    public IReference DataSpecificationRef { get; set; }

    //Whole requests
    public DbFileRequestResult FileRequest { get; set; }

    public DbEventRequest EventRequest { get; set; }

    public DbQueryRequest QueryRequest { get; set; }
}

