using AdminShellNS.Models;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models
{
    public class PackageDescriptionPagedResult
    {
        public List<PackageDescription> result { get; set; }
        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
