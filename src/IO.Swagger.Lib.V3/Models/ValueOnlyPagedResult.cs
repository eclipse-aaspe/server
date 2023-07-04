using DataTransferObjects.ValueDTOs;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace AdminShellNS.Lib.V3.Models
{
    public class ValueOnlyPagedResult
    {
        public List<IValueDTO> result { get; set; }

        public PagedResultPagingMetadata paging_metadata { get; set; }
    }
}
