namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Pagination;
using Contracts.Security;

public class DbRequestContext
{
    public ISecurityConfig SecurityConfig { get; set; }

    public DbRequestParams Params { get; set; }
}
