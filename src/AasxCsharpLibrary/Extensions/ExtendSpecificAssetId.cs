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

using Extensions;
using System.Collections.Generic;
using System.Linq;

namespace AdminShellNS.Extensions
{
    public static class ExtendSpecificAssetId
    {
        public static bool Matches(this ISpecificAssetId specificAssetId, ISpecificAssetId other)
        {
            if (specificAssetId == null) return false;
            if (other == null) return false;

            //check mandatory parameters first
            if (specificAssetId.Name != other.Name) return false;
            if (specificAssetId.Value != other.Value) return false;
            if (!specificAssetId.ExternalSubjectId.Matches(other.ExternalSubjectId)) return false;

            // TODO (jtikekar, 2023-09-04): Check optional parameter i.e., Semantic Id and supplementatry semantic id

            return true;
        }

        #region ListOfSpecificAssetIds

        public static bool ContainsSpecificAssetId(this List<ISpecificAssetId> specificAssetIds, ISpecificAssetId other)
        {
            if (specificAssetIds == null) return false;
            if (other == null) return false;

            var foundIds = specificAssetIds.Where(assetId => assetId.Matches(other));
            if (foundIds.Any()) return true;

            return false;
        }

        #endregion

    }
}
