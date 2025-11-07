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
[Index(nameof(Identifier))]
public class SM
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string A { get; set; } = string.Empty;
    public string B { get; set; } = string.Empty;
    public string C { get; set; } = string.Empty;
    public string D { get; set; } = string.Empty;
    public string E { get; set; } = string.Empty;
    public string F { get; set; } = string.Empty;

    public ICollection<SME> SMEs { get; set; } = new List<SME>();
}
