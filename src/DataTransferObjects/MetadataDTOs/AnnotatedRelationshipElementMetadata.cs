/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class AnnotatedRelationshipElementMetadata(
            List<ExtensionDTO>? extensions = null,
            string category = null,
            string idShort = null,
            List<LangStringNameTypeDTO>? displayName = null,
            List<LangStringTextTypeDTO>? description = null,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            List<QualifierDTO>? qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            List<ISubmodelElementMetadata?> annotations = null,
            string modelType = "AnnotatedRelationshipElement") : ISubmodelElementMetadata;
}
