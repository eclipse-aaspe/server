using Microsoft.EntityFrameworkCore;

/********************************************************************************
* Copyright (c) 2025 Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://mit-license.org/
*
* SPDX-License-Identifier: MIT
********************************************************************************/

namespace SMDataGenerator.Models;

[Index(nameof(Id))]
[Index(nameof(SMId))]
[Index(nameof(SMEId))]
[Index(nameof(value))]
public class Value
{
    public int Id { get; set; }

    public int SMEId { get; set; }
    public SME SME { get; set; } = null!;

    public int SMId { get; set; }
    public SM SM { get; set; } = null!;

    public string value { get; set; } = string.Empty;
}
