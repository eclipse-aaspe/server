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

using AdminShellNS;

namespace Extensions
{
    public static class ExtendModelKind
    {
        public static void Validate(this ModellingKind modelingKind, AasValidationRecordList results, IReferable container)
        {
            // access
            if (results == null || container == null)
                return;

            // check
            if (modelingKind != ModellingKind.Template && modelingKind != ModellingKind.Instance)
            {
                // violation case
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SchemaViolation, container,
                    $"ModelingKind: enumeration value neither Template nor Instance",
                    () =>
                    {
                        modelingKind = ModellingKind.Instance;
                    }));
            }
        }
    }
}
