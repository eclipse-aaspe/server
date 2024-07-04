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

using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services;

using System.Linq;

/// <inheritdoc />
public class PaginationService : IPaginationService
{
    private readonly IAppLogger<PaginationService> _logger;

    /// <summary>
    /// Constructor for PaginationService.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    public PaginationService(IAppLogger<PaginationService> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public PagedResult GetPaginatedList<T>(List<T> sourceList, PaginationParameters paginationParameters)
    {
        var startIndex = paginationParameters.Cursor;
        var endIndex   = startIndex + paginationParameters.Limit - 1;

        CapEndIndex(sourceList, ref endIndex);

        LogErrorIfStartIndexOutOfBounds(sourceList, startIndex, endIndex);

        var outputList     = GetPaginationList(sourceList, startIndex, endIndex);
        var pagingMetadata = CreatePagingMetadata(sourceList, endIndex);

        return new PagedResult {result = outputList.ConvertAll(r => r as IClass), paging_metadata = pagingMetadata};
    }

    /// <inheritdoc />
    public PackageDescriptionPagedResult GetPaginatedPackageDescriptionList(List<PackageDescription> sourceList, PaginationParameters paginationParameters)
    {
        var startIndex = paginationParameters.Cursor;
        var endIndex   = startIndex + paginationParameters.Limit - 1;

        CapEndIndex(sourceList, ref endIndex);

        LogErrorIfStartIndexOutOfBounds(sourceList, startIndex, endIndex);

        var outputList     = GetPaginationList(sourceList, startIndex, endIndex);
        var pagingMetadata = CreatePagingMetadata(sourceList, endIndex);

        return new PackageDescriptionPagedResult {result = outputList, paging_metadata = pagingMetadata};
    }

    private static void CapEndIndex<T>(List<T> sourceList, ref int endIndex)
    {
        if (endIndex > sourceList.Count - 1)
        {
            endIndex = sourceList.Count - 1;
        }
    }

    private void LogErrorIfStartIndexOutOfBounds<T>(List<T> sourceList, int startIndex, int endIndex)
    {
        if (startIndex > sourceList.Count - 1)
        {
            _logger.LogError($"There are fewer elements in the retrieved list than requested for pagination - (from: {startIndex}, size: {endIndex})");
        }
    }

    private static List<T> GetPaginationList<T>(List<T> sourceList, int startIndex, int endIndex) => sourceList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

    private static PagedResultPagingMetadata CreatePagingMetadata<T>(List<T> sourceList, int endIndex)
    {
        var pagingMetadata = new PagedResultPagingMetadata();
        if (endIndex < sourceList.Count - 1)
        {
            pagingMetadata.cursor = Convert.ToString(endIndex + 1);
        }

        return pagingMetadata;
    }
}