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

namespace DataTransferObjects.CommonDTOs
{
    public record class QualifierDTO(
            string Type,
            DataTypeDefXsd ValueType,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            QualifierKind? Kind = null,
            string? Value = null,
            ReferenceDTO? ValueId = null) : IDTO;
}
