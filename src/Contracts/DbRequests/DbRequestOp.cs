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

public enum DbRequestOp
{
    ReadPackageEnv,
    ReadPagedAssetAdministrationShells,
    ReadAssetAdministrationShellById,
    CreateAssetAdministrationShell,
    ReplaceAssetAdministrationShellById,
    DeleteAssetAdministrationShellById,

    CreateSubmodelReference,
    DeleteSubmodelReferenceById,

    ReadPagedSubmodels,
    ReadSubmodelById,
    CreateSubmodel,
    UpdateSubmodelById,
    ReplaceSubmodelById,
    DeleteSubmodelById,

    ReadPagedSubmodelElements,
    ReadSubmodelElementByPath,
    CreateSubmodelElement,
    UpdateSubmodelElementByPath,
    ReplaceSubmodelElementByPath,
    DeleteSubmodelElementByPath,

    ReadAssetInformation,
    ReplaceAssetInformation,

    ReadFileByPath,
    ReplaceFileByPath,
    DeleteFileByPath,

    ReadThumbnail,
    ReplaceThumbnail,
    DeleteThumbnail,

    ReadEventMessages,
    UpdateEventMessages,

    ReadPagedConceptDescriptions,
    ReadConceptDescriptionById,
    CreateConceptDescription,
    ReplaceConceptDescriptionById,
    DeleteConceptDescriptionById,

    GenerateSerializationByIds,

    QuerySearchSMs,
    QueryCountSMs,
    QuerySearchSMEs,
    QueryCountSMEs,

    QueryGetSMs,

    DeleteAASXByPackageId,
    ReadAASXByPackageId,
    ReadPagedAASXPackageIds,
    CreateAASXPackage,
    ReplaceAASXPackageById
}

