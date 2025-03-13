namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;

public interface IDbRequest
{
    public string MethodName { get; set; }

    public DbRequestContext Context { get; set; }

    public TaskCompletionSource<IDbRequestResult> TaskCompletionSource { get; set; }

}
