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

namespace IO.Swagger.Registry.Lib.V3.Extensions;

using System.Linq;

public static class ReferenceElementExtensions
{
    /// <summary>
    /// Reverses the keys in the Value property of the ReferenceElement.
    /// </summary>
    /// <param name="referenceElement">The reference element whose keys are to be reversed.</param>
    public static void ReverseReferenceKeys(this ReferenceElement referenceElement)
    {
        if (referenceElement?.Value?.Keys == null) return;

        var keys = referenceElement.Value.Keys.ToList();
        keys.Reverse();

        referenceElement.Value.Keys = keys;
    }
}
