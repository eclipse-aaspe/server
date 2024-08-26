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

public partial class Description
{
    /// <summary>
    /// Gets or Sets Profiles
    /// </summary>
    public enum ServerDescriptionProfiles
    {
        /// <summary>
        /// Enum AssetAdministrationShellServiceSpecificationV30 for AssetAdministrationShellServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "AssetAdministrationShellServiceSpecification/V3.0")]
        AssetAdministrationShellServiceSpecificationV30 = 0,

        /// <summary>
        /// Enum AssetAdministrationShellServiceSpecificationV30MinimalProfile for AssetAdministrationShellServiceSpecification/V3.0-MinimalProfile
        /// </summary>
        [EnumMember(Value = "AssetAdministrationShellServiceSpecification/V3.0-MinimalProfile")]
        AssetAdministrationShellServiceSpecificationV30MinimalProfile = 1,

        /// <summary>
        /// Enum SubmodelServiceSpecificationV30 for SubmodelServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "SubmodelServiceSpecification/V3.0")]
        SubmodelServiceSpecificationV30 = 2,

        /// <summary>
        /// Enum SubmodelServiceSpecificationV30ValueProfile for SubmodelServiceSpecification/V3.0-ValueProfile
        /// </summary>
        [EnumMember(Value = "SubmodelServiceSpecification/V3.0-ValueProfile")]
        SubmodelServiceSpecificationV30ValueProfile = 3,

        /// <summary>
        /// Enum SubmodelServiceSpecificationV30MinimalProfile for SubmodelServiceSpecification/V3.0-MinimalProfile
        /// </summary>
        [EnumMember(Value = "SubmodelServiceSpecification/V3.0-MinimalProfile")]
        SubmodelServiceSpecificationV30MinimalProfile = 4,

        /// <summary>
        /// Enum AasxFileServerServiceSpecificationV30 for AasxFileServerServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "AasxFileServerServiceSpecification/V3.0")]
        AasxFileServerServiceSpecificationV30 = 5,

        /// <summary>
        /// Enum RegistryServiceSpecificationV30 for RegistryServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "RegistryServiceSpecification/V3.0")]
        RegistryServiceSpecificationV30 = 6,

        /// <summary>
        /// Enum RegistryServiceSpecificationV30AssetAdministrationShellRegistry for RegistryServiceSpecification/V3.0- AssetAdministrationShellRegistry
        /// </summary>
        [EnumMember(Value = "RegistryServiceSpecification/V3.0- AssetAdministrationShellRegistry")]
        RegistryServiceSpecificationV30AssetAdministrationShellRegistry = 7,

        /// <summary>
        /// Enum RegistryServiceSpecificationV30SubmodelRegistry for RegistryServiceSpecification/V3.0-SubmodelRegistry
        /// </summary>
        [EnumMember(Value = "RegistryServiceSpecification/V3.0-SubmodelRegistry")]
        RegistryServiceSpecificationV30SubmodelRegistry = 8,

        /// <summary>
        /// Enum RepositoryServiceSpecificationV30 for RepositoryServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "RepositoryServiceSpecification/V3.0")]
        RepositoryServiceSpecificationV30 = 9,

        /// <summary>
        /// Enum RepositoryServiceSpecificationV30MinimalProfile for RepositoryServiceSpecification/V3.0-MinimalProfile
        /// </summary>
        [EnumMember(Value = "RepositoryServiceSpecification/V3.0-MinimalProfile")]
        RepositoryServiceSpecificationV30MinimalProfile = 10,

        /// <summary>
        /// Enum AssetAdministrationShellRepositoryServiceSpecificationV30 for AssetAdministrationShellRepositoryServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "AssetAdministrationShellRepositoryServiceSpecification/V3.0")]
        AssetAdministrationShellRepositoryServiceSpecificationV30 = 11,

        /// <summary>
        /// Enum AssetAdministrationShellRepositoryServiceSpecificationV30MinimalProfile for AssetAdministrationShellRepositoryServiceSpecification/V3.0-MinimalProfile
        /// </summary>
        [EnumMember(Value = "AssetAdministrationShellRepositoryServiceSpecification/V3.0-MinimalProfile")]
        AssetAdministrationShellRepositoryServiceSpecificationV30MinimalProfile = 12,

        /// <summary>
        /// Enum SubmodelRepositoryServiceSpecificationV30 for SubmodelRepositoryServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "SubmodelRepositoryServiceSpecification/V3.0")]
        SubmodelRepositoryServiceSpecificationV30 = 13,

        /// <summary>
        /// Enum SubmodelRepositoryServiceSpecificationV30MinimalProfile for SubmodelRepositoryServiceSpecification/V3.0-MinimalProfile
        /// </summary>
        [EnumMember(Value = "SubmodelRepositoryServiceSpecification/V3.0-MinimalProfile")]
        SubmodelRepositoryServiceSpecificationV30MinimalProfile = 14,

        /// <summary>
        /// Enum RegistryAndDiscoveryServiceSpecificationV30 for RegistryAndDiscoveryServiceSpecification/V3.0
        /// </summary>
        [EnumMember(Value = "RegistryAndDiscoveryServiceSpecification/V3.0")]
        RegistryAndDiscoveryServiceSpecificationV30 = 15
    }
}