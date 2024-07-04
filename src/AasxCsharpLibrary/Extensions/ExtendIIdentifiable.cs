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

using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendIIdentifiable
    {
        #region List of Identifiers

        public static string ToStringExtended(this List<IIdentifiable> identifiables, string delimiter = ",")
        {
            return string.Join(delimiter, identifiables.Select((x) => x.Id));
        }

        #endregion
        public static Reference GetReference(this IIdentifiable identifiable)
        {
            var key = new Key(ExtensionsUtil.GetKeyType(identifiable), identifiable.Id);
            // TODO (jtikekar, 2023-09-04): if model or Global reference?
            var outputReference = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { key });

            return outputReference;
        }
    }
}
