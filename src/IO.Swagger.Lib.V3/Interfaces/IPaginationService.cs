using AdminShellNS.Models;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IPaginationService
    {
        PagedResult GetPaginatedList<T>(List<T> sourceList, PaginationParameters paginationParameters);
        PackageDescriptionPagedResult GetPaginatedPackageDescriptionList(List<PackageDescription> sourceList, PaginationParameters paginationParameters);
    }
}
