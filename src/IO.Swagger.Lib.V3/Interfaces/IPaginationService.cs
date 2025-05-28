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

using AdminShellNS.Models;
using Contracts.Pagination;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces;

/// <summary>
/// Service for handling pagination of lists.
/// </summary>
public interface IPaginationService
{
    /// <summary>
    /// Add metadata to paginated list.
    /// </summary>
    /// <typeparam name="T">Type of items in the list.</typeparam>
    /// <param name="paginatedList">Paginated list.</param>
    /// <param name="paginationParameters">Pagination parameters including cursor and limit.</param>
    /// <returns>Paginated result containing a subset of the source list.</returns>
    PagedResult GetPaginatedResult<T>(List<T> paginatedList, PaginationParameters paginationParameters);

    /// <summary>
    /// Add metadata a paginated list of PackageDescription objects based on provided parameters.
    /// </summary>
    /// <param name="paginatedList">Source list of paginated PackageDescription objects</param>
    /// <param name="paginationParameters">Pagination parameters including cursor and limit.</param>
    /// <returns>Paginated result containing a subset of the source list.</returns>
    PackageDescriptionPagedResult GetPaginatedPackageDescriptionList(List<PackageDescription> paginatedList, PaginationParameters paginationParameters);
}