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

using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using Contracts;
using Contracts.Pagination;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.Services
{
    public class PaginationService : IPaginationService
    {
        private readonly IAppLogger<PaginationService> _logger;

        public PaginationService(IAppLogger<PaginationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public PagedResult GetPaginatedResult<T>(List<T> paginatedList, PaginationParameters paginationParameters)
        {
            //Creating pagination result
            var pagingMetadata = new PagedResultPagingMetadata();

            int size = paginatedList.Count;
            if (paginatedList.Count < paginationParameters.Limit)
            {
                _logger.LogInformation($"There are less elements in the retrieved list than requested for pagination - (cursor: {paginationParameters.Cursor}, size:{paginationParameters.Limit})");
                pagingMetadata.cursor = String.Empty;
            }
            else
            {
                pagingMetadata.cursor = Convert.ToString(paginationParameters.Cursor + paginationParameters.Limit);
                size = paginationParameters.Limit;
            }
            paginatedList = paginatedList.Take(size).ToList();

            var paginationResult = new PagedResult()
            {
                result = paginatedList.ConvertAll(r => r as IClass),
                paging_metadata = pagingMetadata
            };

            return paginationResult;
        }

        public QueryResult GetPaginatedQueryResult<T>(List<T> paginatedList, PaginationParameters paginationParameters)
        {
            //Creating pagination result
            var pagingMetadata = new QueryResultPagingMetadata();

            pagingMetadata.resultType = ResultType.Identifier.ToString();

            if (paginatedList.Count != 0)
            {
                if (paginatedList.First() is ISubmodel)
                {
                    pagingMetadata.resultType = ResultType.Submodel.ToString();
                }
                if (paginatedList.First() is SubmodelValue)
                {
                    pagingMetadata.resultType = ResultType.SubmodelValue.ToString();
                }
                if (paginatedList.First() is ISubmodelElement)
                {
                    pagingMetadata.resultType = ResultType.SubmodelElement.ToString();
                }
            }

            if (paginatedList.Count < paginationParameters.Limit)
            {
                _logger.LogInformation($"There are less elements in the retrieved list than requested for pagination - (cursor: {paginationParameters.Cursor}, size:{paginationParameters.Limit})");
                pagingMetadata.cursor = String.Empty;
            }
            else
            {
                pagingMetadata.cursor = Convert.ToString(paginationParameters.Cursor + paginatedList.Count);
            }

            var paginationResult = new QueryResult() { paging_metadata = pagingMetadata, result = paginatedList.ConvertAll(r => r as object) };

            return paginationResult;
        }


        public PackageDescriptionPagedResult GetPaginatedPackageDescriptionList(List<PackageDescription> sourceList, PaginationParameters paginationParameters)
        {
            //Creating pagination result
            var pagingMetadata = new PagedResultPagingMetadata();

            if (sourceList.Count < paginationParameters.Limit)
            {
                _logger.LogInformation($"There are less elements in the retrieved list than requested for pagination - (cursor: {paginationParameters.Cursor}, size:{paginationParameters.Limit})");
                pagingMetadata.cursor = String.Empty;
            }
            else
            {
                pagingMetadata.cursor = Convert.ToString(paginationParameters.Cursor + sourceList.Count);
            }


            var paginationResult = new PackageDescriptionPagedResult()
            {
                result = sourceList,
                paging_metadata = pagingMetadata
            };

            return paginationResult;
        }
    }
}
