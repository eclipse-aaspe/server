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

using System;
using System.Collections.Generic;
using AasRegistryDiscovery.WebApi.Interfaces;
using AasRegistryDiscovery.WebApi.Models;
using Microsoft.Extensions.Logging;

namespace AasRegistryDiscovery.WebApi.Services.InMemory
{
    public class DescriptorPaginationService : IDescriptorPaginationService
    {
        private readonly ILogger<DescriptorPaginationService> _logger;

        public DescriptorPaginationService(ILogger<DescriptorPaginationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AasDescriptorPagedResult GetPaginatedList(List<AssetAdministrationShellDescriptor> sourceList, PaginationParameters paginationParameters)
        {
            if (sourceList == null)
            {
                return null;
            }

            var startIndex = paginationParameters.Cursor;
            var endIndex = startIndex + paginationParameters.Limit - 1;
            var outputList = GetPaginationList(sourceList, startIndex, endIndex);

            //Creating pagination result
            var pagingMetadata = new PagedResultPagingMetadata();
            if (endIndex < sourceList.Count - 1)
            {
                pagingMetadata.Cursor = Convert.ToString(endIndex + 1);
            }

            var paginationResult = new AasDescriptorPagedResult()
            {
                result = outputList,
                paging_metadata = pagingMetadata
            };

            return paginationResult;
        }

        public SubmodelDescriptorPagedResult GetPaginatedList(List<SubmodelDescriptor> sourceList, PaginationParameters paginationParameters)
        {
            if (sourceList == null)
            {
                return null;
            }

            var startIndex = paginationParameters.Cursor;
            var endIndex = startIndex + paginationParameters.Limit - 1;
            var outputList = GetPaginationList(sourceList, startIndex, endIndex);

            //Creating pagination result
            var pagingMetadata = new PagedResultPagingMetadata();
            if (endIndex < sourceList.Count - 1)
            {
                pagingMetadata.Cursor = Convert.ToString(endIndex + 1);
            }

            var paginationResult = new SubmodelDescriptorPagedResult()
            {
                result = outputList,
                paging_metadata = pagingMetadata
            };

            return paginationResult;
        }

        private List<T> GetPaginationList<T>(List<T> sourceList, int startIndex, int endIndex)
        {
            if(sourceList == null)
            {
                return null;
            }

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
                outputList.Add(sourceList[ i ]);
            }

            return outputList;
        }
    }
}