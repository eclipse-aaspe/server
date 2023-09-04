using IO.Swagger.Models;
using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models
{
    public class AasDescriptorPagedResult
    {
        public List<AssetAdministrationShellDescriptor> result { get; set; }
        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
