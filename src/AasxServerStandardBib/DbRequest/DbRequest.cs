namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;

public class DbRequest : IDbRequest
{
    public DbRequest(string methodName, DbRequestContext dbRequestContext, TaskCompletionSource<IDbRequestResult> completionSource)
    {
        MethodName = methodName;
        Context = dbRequestContext;
        TaskCompletionSource = completionSource;
    }

    public string MethodName { get; set; }

    public DbRequestContext Context { get; set; }
    public TaskCompletionSource<IDbRequestResult> TaskCompletionSource { get; set; }
}
