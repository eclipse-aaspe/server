using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models
{
    public class PathPagedResult
    {
        public List<List<string>> result { get; set; }

        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
