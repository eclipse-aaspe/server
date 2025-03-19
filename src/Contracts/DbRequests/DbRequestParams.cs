namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using Contracts.Pagination;

public class DbRequestParams
{
    public IPaginationParameters PaginationParameters { get; set; }

    public List<ISpecificAssetId> AssetIds { get; set; }

    public string IdShort { get; set; }

    public List<object> IdShortElements { get; set; }

    public string AssetAdministrationShellIdentifier { get; set; }

    public string SubmodelIdentifier { get; set; }

}
