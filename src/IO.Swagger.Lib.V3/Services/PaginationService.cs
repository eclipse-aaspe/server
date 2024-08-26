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
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services
{
    public class PaginationService : IPaginationService
    {
        private readonly IAppLogger<PaginationService> _logger;

        public PaginationService(IAppLogger<PaginationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public PagedResult GetPaginatedList<T>(List<T> sourceList, PaginationParameters paginationParameters)
        {
            var outputList = new List<T>();
            var startIndex = paginationParameters.Cursor;
            var endIndex = startIndex + paginationParameters.Limit - 1;

            //cap the endIndex
            if (endIndex > sourceList.Count - 1)
            {
                endIndex = sourceList.Count - 1;
            }

            //If there are less elements in the sourceList than "from"
            if (startIndex > sourceList.Count - 1)
            {
                _logger.LogError($"There are less elements in the retrieved list than requested pagination - (from: {startIndex}, size:{endIndex})");
            }

            for (var i = startIndex; i <= endIndex; i++)
            {
                outputList.Add(sourceList[i]);
            }

            //Creating pagination result
            var pagingMetadata = new PagedResultPagingMetadata();
            if (endIndex < sourceList.Count - 1)
            {
                pagingMetadata.cursor = Convert.ToString(endIndex + 1);
            }

            var paginationResult = new PagedResult()
            {
                result          = outputList.ConvertAll(r => r as IClass),
                paging_metadata = pagingMetadata
            };

            //return paginationResult;
            return paginationResult;
        }

        public PackageDescriptionPagedResult GetPaginatedPackageDescriptionList(List<PackageDescription> sourceList, PaginationParameters paginationParameters)
        {
            var startIndex = paginationParameters.Cursor;
            var endIndex = startIndex + paginationParameters.Limit - 1;
            var outputList = GetPaginationList(sourceList, startIndex, endIndex);

            //Creating pagination result
            var pagingMetadata = new PagedResultPagingMetadata();
            if (endIndex < sourceList.Count - 1)
            {
                pagingMetadata.cursor = Convert.ToString(endIndex + 1);
            }

            var paginationResult = new PackageDescriptionPagedResult()
            {
                result = outputList,
                paging_metadata = pagingMetadata
            };

            //return paginationResult;
            return paginationResult;
        }

        private List<T> GetPaginationList<T>(List<T> sourceList, int startIndex, int endIndex)
        {
            var outputList = new List<T>();

            //cap the endIndex
            if (endIndex > sourceList.Count - 1)
            {
                endIndex = sourceList.Count - 1;
            }

            //If there are less elements in the sourceList than "from"
            if (startIndex > sourceList.Count - 1)
            {
                _logger.LogError($"There are less elements in the retrieved list than requested pagination - (from: {startIndex}, size:{endIndex})");
            }

            for (var i = startIndex; i <= endIndex; i++)
            {
                outputList.Add(sourceList[i]);
            }

            return outputList;
        }
    }
}
