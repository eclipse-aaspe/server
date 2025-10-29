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

using Microsoft.EntityFrameworkCore;

namespace SMDataGenerator.Models;

[Index(nameof(Id))]
[Index(nameof(SMId))]
[Index(nameof(ParentSMEId))]
[Index(nameof(IdShortPath))]
public class SME
{
    public int Id { get; set; }
    public int SMId { get; set; }
    public SM SM { get; set; } = null!;

    public int? ParentSMEId { get; set; }
    public SME? ParentSME { get; set; }

    public string A { get; set; } = string.Empty;
    public string B { get; set; } = string.Empty;
    public string C { get; set; } = string.Empty;
    public string D { get; set; } = string.Empty;
    public string E { get; set; } = string.Empty;
    public string F { get; set; } = string.Empty;

    public string IdShort { get; set; } = string.Empty;
    public string IdShortPath { get; set; } = string.Empty;
    public ICollection<SME> Children { get; set; } = new List<SME>();
    public ICollection<Value> Values { get; set; } = new List<Value>();
}
