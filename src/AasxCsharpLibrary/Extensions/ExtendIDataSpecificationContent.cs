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

using AdminShellNS;
using System.Collections.Generic;

namespace Extensions
{
    public static class ExtendIDataSpecificationContent
    {
        public enum ContentTypes { NoInfo, Iec61360, PhysicalUnit }

        public static Key GetKeyForIec61360()
        {
            return new Key(KeyTypes.GlobalReference,
                "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0");
        }

        public static Reference GetReferencForIec61360()
        {
            return new Reference(ReferenceTypes.ExternalReference, new List<IKey> { GetKeyForIec61360() });
        }

        public static Key GetKeyForPhysicalUnit()
        {
            return new Key(KeyTypes.GlobalReference,
                "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationPhysicalUnit/3/0");
        }

        public static Key GetKeyFor(ContentTypes ct)
        {
            if (ct == ContentTypes.Iec61360)
                return GetKeyForIec61360();
            if (ct == ContentTypes.PhysicalUnit)
                return GetKeyForPhysicalUnit();
            return null;
        }

        public static IDataSpecificationContent ContentFactoryFor(ContentTypes ct)
        {
            if (ct == ContentTypes.Iec61360)
                return new DataSpecificationIec61360(null);
            // TODO (jtikekar, 2023-09-04): DataSpecificationPhysicalUnit
            //if (ct == ContentTypes.PhysicalUnit)
            //    return new DataSpecificationPhysicalUnit("", "", null);
            return null;
        }

        public static ContentTypes GuessContentTypeFor(IReference? rf)
        {
            foreach (var v in AdminShellUtil.GetEnumValues<ContentTypes>(new[] { ContentTypes.NoInfo }))
                if (rf?.MatchesExactlyOneKey(GetKeyFor(v)) == true)
                    return v;
            return ContentTypes.NoInfo;
        }

        public static ContentTypes GuessContentTypeFor(IDataSpecificationContent? content)
        {
            if (content is DataSpecificationIec61360)
                return ContentTypes.Iec61360;
            // TODO (jtikekar, 2023-09-04): DataSpecificationPhysicalUnit
            //if (content is DataSpecificationPhysicalUnit)
            //    return ContentTypes.PhysicalUnit;
            return ContentTypes.NoInfo;
        }
    }
}
