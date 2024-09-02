using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models
{
    public class ReferencePagedResult : PagedResult
    {
        public ReferencePagedResult(List<IReference>? result, PagedResultPagingMetadata? pagingMetadata)
        {
            this.result = result.ConvertAll(a=> (IClass)a);
            paging_metadata = pagingMetadata;
        }
    }
}
