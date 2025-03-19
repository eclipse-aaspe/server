namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;

public class DbRequest
{
    public DbRequest(string requestedTypeName, DbRequestContext dbRequestContext, DbRequestCrudType crudType, bool isRequestingMany, TaskCompletionSource<DbRequestResult> completionSource)
    {
        RequestedTypeName = requestedTypeName;
        Context = dbRequestContext;
        TaskCompletionSource = completionSource;
        CrudType = crudType;
        IsRequestingMany = isRequestingMany;
    }

    public string RequestedTypeName { get; set; }

    public bool IsRequestingMany { get; set; }

    public DbRequestCrudType CrudType { get; set; }

    //public DbRequestOp Operation { get; set; }

    //public string OperationName { get; set; }

    public DbRequestContext Context { get; set; }

    public TaskCompletionSource<DbRequestResult> TaskCompletionSource { get; set; }
}
