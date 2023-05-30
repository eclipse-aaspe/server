using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
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
        public List<T> GetPaginatedList<T>(List<T> sourceList, PaginationParameters paginationParameters)
        {
            var result = new List<T>();
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
                _logger.LogError($"There are less elements in the retrived list than requested pagination - (from: {startIndex}, size:{endIndex})");
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                result.Add(sourceList[i]);
            }

            return result;
        }
    }
}
