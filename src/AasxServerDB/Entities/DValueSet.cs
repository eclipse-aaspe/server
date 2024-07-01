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

using System.ComponentModel.DataAnnotations.Schema;

namespace AasxServerDB.Entities
{
    public class DValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get;              set; }
        public virtual SMESet? SMESet { get; set; }

        public double? Value { get; set; }
        public string? Annotation { get; set; }

        public SValueSet asStringValue()
        {
            return new SValueSet
            {
                Id = Id,
                SMEId = SMEId,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }
}