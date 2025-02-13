namespace Contracts.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IPaginationParameters
{
    /// <summary>
    /// The maximum size of the result list.
    /// </summary>
    public int Limit{ get; set; }

    /// <summary>
    /// The position from which to resume a result listing.
    /// </summary>
    public int Cursor { get; set;}
}
