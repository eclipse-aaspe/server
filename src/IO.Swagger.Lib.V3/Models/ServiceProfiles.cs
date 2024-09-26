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

namespace IO.Swagger.Models;

using System.Runtime.Serialization;

public partial class ServiceDescription
{
    /// <summary>
    /// Gets or Sets Profiles
    /// </summary>
    public enum ServiceProfiles
    {
        /// <summary>
        /// Enum AssetAdministrationShellServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-001")]
        AssetAdministrationShellServiceSpecificationSSP001 = 0,

        /// <summary>
        /// Enum AssetAdministrationShellServiceSpecificationSSP002 for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-002
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-002")]
        AssetAdministrationShellServiceSpecificationSSP002 = 1,

        /// <summary>
        /// Enum SubmodelServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001")]
        SubmodelServiceSpecificationSSP001 = 2,

        /// <summary>
        /// Enum SubmodelServiceSpecificationSSP002 for https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-002
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-002")]
        SubmodelServiceSpecificationSSP002 = 3,

        /// <summary>
        /// Enum SubmodelServiceSpecificationSSP003 for https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-003
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-003")]
        SubmodelServiceSpecificationSSP003 = 4,

        /// <summary>
        /// Enum AasxFileServerServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/AasxFileServerServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AasxFileServerServiceSpecification/SSP-001")]
        AasxFileServerServiceSpecificationSSP001 = 5,

        /// <summary>
        /// Enum AssetAdministrationShellRegistryServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-001")]
        AssetAdministrationShellRegistryServiceSpecificationSSP001 = 6,

        /// <summary>
        /// Enum AssetAdministrationShellRegistryServiceSpecificationSSP002 for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-002
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-002")]
        AssetAdministrationShellRegistryServiceSpecificationSSP002 = 7,

        /// <summary>
        /// Enum SubmodelRegistryServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-001")]
        SubmodelRegistryServiceSpecificationSSP001 = 8,

        /// <summary>
        /// Enum SubmodelRegistryServiceSpecificationSSP002 for https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-002
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-002")]
        SubmodelRegistryServiceSpecificationSSP002 = 9,

        /// <summary>
        /// Enum DiscoveryServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/DiscoveryServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/DiscoveryServiceSpecification/SSP-001")]
        DiscoveryServiceSpecificationSSP001 = 10,

        /// <summary>
        /// Enum AssetAdministrationShellRepositoryServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-001")]
        AssetAdministrationShellRepositoryServiceSpecificationSSP001 = 11,

        /// <summary>
        /// Enum AssetAdministrationShellRepositoryServiceSpecificationSSP002 for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-002
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-002")]
        AssetAdministrationShellRepositoryServiceSpecificationSSP002 = 12,

        /// <summary>
        /// Enum SubmodelRepositoryServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-001")]
        SubmodelRepositoryServiceSpecificationSSP001 = 13,

        /// <summary>
        /// Enum SubmodelRepositoryServiceSpecificationSSP002 for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-002
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-002")]
        SubmodelRepositoryServiceSpecificationSSP002 = 14,

        /// <summary>
        /// Enum SubmodelRepositoryServiceSpecificationSSP003 for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-003
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-003")]
        SubmodelRepositoryServiceSpecificationSSP003 = 15,

        /// <summary>
        /// Enum SubmodelRepositoryServiceSpecificationSSP004 for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-004
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-004")]
        SubmodelRepositoryServiceSpecificationSSP004 = 16,

        /// <summary>
        /// Enum ConceptDescriptionServiceSpecificationSSP001 for https://admin-shell.io/aas/API/3/0/ConceptDescriptionServiceSpecification/SSP-001
        /// </summary>
        [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/ConceptDescriptionServiceSpecification/SSP-001")]
        ConceptDescriptionServiceSpecificationSSP001 = 17
    }
}