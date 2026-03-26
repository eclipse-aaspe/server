/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace Contracts;

/// <summary>Which SME semantic applies when <c>value</c> mode maps both to <c>Annotation</c>.</summary>
public enum SmeSemanticKind
{
    None = 0,
    ValueType = 1,
    Language = 2
}
