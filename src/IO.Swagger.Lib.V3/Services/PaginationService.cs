using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services;

/// <inheritdoc />
public class PaginationService : IPaginationService
{
    private readonly IAppLogger<PaginationService> _logger;

    /// <summary>
    /// Constructor for PaginationService.
    /// </summary>
    /// <param name="logger">Logger instance for logging.</param>
    public PaginationService(IAppLogger<PaginationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public PagedResult GetPaginatedList<T>(List<T> sourceList, PaginationParameters paginationParameters)
    {
        var outputList = new List<T>();
        var startIndex = paginationParameters.Cursor;
        var endIndex   = startIndex + paginationParameters.Limit - 1;

        //cap the endIndex
        if (endIndex > sourceList.Count - 1)
        {
            endIndex = sourceList.Count - 1;
        }

        //If there are fewer elements in the sourceList than "from"
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

        var paginationResult = new PagedResult {result = outputList.ConvertAll(r => r as IClass), paging_metadata = pagingMetadata};

        //return paginationResult;
        return paginationResult;
    }

    /// <inheritdoc />
    public PackageDescriptionPagedResult GetPaginatedPackageDescriptionList(List<PackageDescription> sourceList, PaginationParameters paginationParameters)
    {
        var startIndex = paginationParameters.Cursor;
        var endIndex   = startIndex + paginationParameters.Limit - 1;
        var outputList = GetPaginationList(sourceList, startIndex, endIndex);

        //Creating pagination result
        var pagingMetadata = new PagedResultPagingMetadata();
        if (endIndex < sourceList.Count - 1)
        {
            pagingMetadata.cursor = Convert.ToString(endIndex + 1);
        }

        var paginationResult = new PackageDescriptionPagedResult {result = outputList, paging_metadata = pagingMetadata};

        //return paginationResult;
        return paginationResult;
    }

    /// <summary>
    /// Retrieves a paginated subset of a generic list based on provided indices.
    /// </summary>
    /// <typeparam name="T">Type of items in the list.</typeparam>
    /// <param name="sourceList">Source list to paginate.</param>
    /// <param name="startIndex">Starting index of the subset.</param>
    /// <param name="endIndex">Ending index of the subset.</param>
    /// <returns>Paginated subset of the source list.</returns>
    private List<T> GetPaginationList<T>(List<T> sourceList, int startIndex, int endIndex)
    {
        var outputList = new List<T>();

        //cap the endIndex
        if (endIndex > sourceList.Count - 1)
        {
            endIndex = sourceList.Count - 1;
        }

        //If there are fewer elements in the sourceList than "from"
        if (startIndex > sourceList.Count - 1)
        {
            _logger.LogError($"There are less elements in the retried list than requested pagination - (from: {startIndex}, size:{endIndex})");
        }

        for (var i = startIndex; i <= endIndex; i++)
        {
            outputList.Add(sourceList[i]);
        }

        return outputList;
    }
}