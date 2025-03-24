namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;

public class DbRequest
{
    public DbRequest(DbRequestOp dbRequestOp, DbRequestCrudType crudType, DbRequestContext dbRequestContext, TaskCompletionSource<DbRequestResult> completionSource)
    {
        Operation = dbRequestOp;
        Context = dbRequestContext;
        TaskCompletionSource = completionSource;
        CrudType = crudType;
    }

    public DbRequestCrudType CrudType { get; set; }

    public DbRequestOp Operation { get; set; }

    public DbRequestContext Context { get; set; }

    public TaskCompletionSource<DbRequestResult> TaskCompletionSource { get; set; }
}
