namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DbFileRequestResult
{
    public string File { get; set; }

    public byte[] Content { get; set; }

    public long FileSize { get; set; }
}
