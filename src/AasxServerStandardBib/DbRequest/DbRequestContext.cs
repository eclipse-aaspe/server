namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Contracts.Pagination;

public class DbRequestContext
{
    public ISecurityConfig SecurityConfig { get; set; }

    public IPaginationParameters PaginationParameters { get; set; }

    public List<ISpecificAssetId> AssetIds { get; set; }

    public string IdShort { get; set; }

    public List<object> IdShortElements { get; set; }

    public string AssetAdministrationShellIdentifier { get; set; }

    public string SubmodelIdentifier { get; set; }
}
