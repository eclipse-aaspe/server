using DataTransferObjects.MetadataDTOs;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models
{
    public class MetadataPagedResult
    {
        public List<IMetadataDTO> result { get; set; }

        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
