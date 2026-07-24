/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

namespace Contracts.DbRequests;

using System;
using System.Collections.Generic;

/// <summary>
/// Batch projection of explicit idShortPaths over a set of result submodels.
/// The values are read directly from SMSets/SMRefSets/SMESets/ValueSets without
/// materializing complete submodel objects (fast path for tabular exports).
/// </summary>
public class DbProjectionRequest
{
    /// <summary>Identifiers of the result submodels (one output row per entry, order preserved).</summary>
    public List<string> SubmodelIdentifiers { get; set; } = new();

    /// <summary>Projected columns; every entry must be an explicit full idShortPath.</summary>
    public List<DbProjectionPath> Paths { get; set; } = new();
}

/// <summary>One projected column.</summary>
public class DbProjectionPath
{
    /// <summary>Select entry exactly as given by the caller; used as cell key in the result rows.</summary>
    public string RawPath { get; set; } = string.Empty;

    /// <summary>
    /// IdShort of a sibling submodel of the same AAS (cross-submodel path "/SubmodelIdShort/idShortPath");
    /// null means the path refers to the result submodel itself.
    /// </summary>
    public string? TargetSubmodelIdShort { get; set; }

    /// <summary>Full dotted idShortPath inside the target submodel (matches SMESets.IdShortPath).</summary>
    public string ElementIdShortPath { get; set; } = string.Empty;
}

/// <summary>One ValueSets row of a projected element (MLP elements have one row per language).</summary>
public class DbProjectionValue
{
    public string? SValue { get; set; }
    public double? NValue { get; set; }

    /// <summary>For MultiLanguageProperty rows this holds the language code.</summary>
    public string? Annotation { get; set; }
}

/// <summary>Projected cell: the SMESets row matched by the exact idShortPath plus its values.</summary>
public class DbProjectionCell
{
    /// <summary>True when an element with the exact idShortPath exists in the target submodel.</summary>
    public bool Found { get; set; }

    /// <summary>SMESets.SMEType (may carry an operation prefix like "In-").</summary>
    public string? SmeType { get; set; }

    /// <summary>SMESets.TValue: "S" (string) or "D" (double); null when the element stores no value.</summary>
    public string? TValue { get; set; }

    public List<DbProjectionValue> Values { get; set; } = new();

    /// <summary>
    /// Identifier of the submodel the element was read from
    /// (differs from the row submodel for cross-submodel paths).
    /// </summary>
    public string? SourceSubmodelIdentifier { get; set; }
}

/// <summary>One output row: the result submodel plus one cell per projected path (keyed by RawPath).</summary>
public class DbProjectionRow
{
    public string SubmodelIdentifier { get; set; } = string.Empty;

    public Dictionary<string, DbProjectionCell> Cells { get; set; } = new(StringComparer.Ordinal);
}
