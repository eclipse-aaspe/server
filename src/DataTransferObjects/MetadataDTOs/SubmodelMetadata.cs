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

using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class SubmodelMetadata(
            string Id,
            List<ExtensionDTO>? Extensions = null,
            string? Category = null,
            string? IdShort = null,
            List<LangStringNameTypeDTO>? DisplayName = null,
            List<LangStringTextTypeDTO>? Description = null,
            AdministrativeInformationDTO? Administration = null,
            ModellingKind? Kind = null,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            List<QualifierDTO>? Qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            string ModelType = "Submodel") : IMetadataDTO;
}
