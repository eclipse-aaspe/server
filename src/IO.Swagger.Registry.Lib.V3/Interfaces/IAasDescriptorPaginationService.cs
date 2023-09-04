using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    public interface IAasDescriptorPaginationService
    {
        AasDescriptorPagedResult GetPaginatedList(List<AssetAdministrationShellDescriptor> sourceList, PaginationParameters paginationParameters);
    }
}
