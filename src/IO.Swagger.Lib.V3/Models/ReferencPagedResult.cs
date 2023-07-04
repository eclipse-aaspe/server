using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models
{
    public class ReferencPagedResult : PagedResult
    {
        public List<IReference> result { get; set; }

        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
