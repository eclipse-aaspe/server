namespace IO.Swagger.Lib.V3.Models;

using Contracts.Pagination;
using IO.Swagger.Models;
using System.Collections.Generic;
using System;

public class QueryResult
{
    /// <summary>
     /// Gets or sets the list of items in the paged result.
     /// </summary>
    public List<object?> result { get; set; }

    /// <summary>
    /// Gets or sets the paging metadata for the paged result.
    /// </summary>
    public QueryResultPagingMetadata? paging_metadata { get; set; }
}