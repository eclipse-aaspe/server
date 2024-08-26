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

namespace IO.Swagger.Models;

using IO.Swagger.Lib.V3.Exceptions;

public class PaginationParameters
{
    private const int MaxResultSize = 500;

    private int _cursor;
    private int _limit;

    public PaginationParameters(string? cursor, int? limit)
    {
        // Initialize cursor with default value if null or empty or not a valid integer
        _cursor = string.IsNullOrEmpty(cursor) || !int.TryParse(cursor, out var parsedCursor) ? 0 : parsedCursor;
        
        // Set limit to provided value or default to MaxResultSize
        if(limit < 0)
        {
            throw new InvalidPaginationParameterException("Limit", limit);
        }
        _limit = limit ?? MaxResultSize;
    }

    /// <summary>
    /// The maximum size of the result list.
    /// </summary>
    public int Limit
    {
        get => _limit;
        set => _limit = value;
    }

    /// <summary>
    /// The position from which to resume a result listing.
    /// </summary>
    public int Cursor
    {
        get => _cursor;
        set => _cursor = value;
    }
}