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

namespace AasxServerDB.Entities
{
    public class AASXSet
    {
        public int     Id   { get; set; }
        public string? AASX { get; set; }

        public virtual ICollection<AASSet> AASSets { get; } = new List<AASSet>();
        public virtual ICollection<SMSet?> SMSets  { get; } = new List<SMSet?>();
    }
}