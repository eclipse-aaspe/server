using IO.Swagger.Lib.V3.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IPaginationService
    {
        List<T> GetPaginatedList<T>(List<T> sourceList, PaginationParameters paginationParameters);
    }
}
