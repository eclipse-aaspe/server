namespace AasxServerStandardBib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AasxServerStandardBib.Logging;
using Contracts;
using Contracts.DbRequests;
using Contracts.Pagination;
using Microsoft.Extensions.DependencyInjection;

public class DbRequestHandlerService : IDbRequestHandlerService
{
    private readonly BlockingCollection<DbRequest> _queryOperations = new BlockingCollection<DbRequest>();

    private IPersistenceService _persistenceService;
    private readonly IServiceProvider _serviceProvider;

    private readonly SemaphoreSlim _lock = new(0);

    private static int ActiveReadOperations = 0;


    public DbRequestHandlerService(IServiceProvider serviceProvider, IPersistenceService persistenceService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _lock.Release();

        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
    }

    private async Task ProcessQueryOperations()
    {
        foreach (var operation in _queryOperations.GetConsumingEnumerable())
        {
            if (operation != null)
            {
                _lock.Wait();
                if (operation.CrudType != DbRequestCrudType.Read)
                {
                    while (ActiveReadOperations > 0)
                    {
                        Thread.Sleep(100);
                    }
                }

                try
                {
                    if (operation.CrudType == DbRequestCrudType.Read)
                    {
                        _lock.Release();
                        IncrementCounter();
                    }

                    var result = await _persistenceService.DoDbOperation(operation);
                }
                catch (Exception ex)
                {
                    operation.TaskCompletionSource.SetException(ex);
                }
                finally
                {
                    if (operation.CrudType != DbRequestCrudType.Read)
                    {
                        _lock.Release();
                    }
                    else
                    {
                        DecrementCounter();
                    }
                }
            }
        }
    }
    public void IncrementCounter()
    {
        Interlocked.Increment(ref ActiveReadOperations);
    }

    public void DecrementCounter()
    {
        Interlocked.Decrement(ref ActiveReadOperations);
    }

    public async Task<List<IAssetAdministrationShell>> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort)
    {
        var parameters = new DbRequestParams()
        {
            AssetIds = assetIds,
            IdShort = idShort,
            PaginationParameters = paginationParameters
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadAllAssetAdministrationShells, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;

        var aasList = tcs.AssetAdministrationShells;

        if (aasList.Count == 0)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<DbRequestHandlerService>>();
                scopedLogger.LogInformation($"No AAS with requested assetId found.");
            }
        }

        return aasList;
    }

    public async Task<ISubmodel> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadSubmodelById, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.Submodels[0];
    }

    public async Task<List<ISubmodel>> ReadPagedSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference reqSemanticId, string idShort)
    {
        var parameters = new DbRequestParams()
        {
            PaginationParameters = paginationParameters,
            Reference = reqSemanticId,
            IdShort = idShort
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadPagedSubmodels, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.Submodels;
    }

    public async Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadPagedSubmodelElements, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.SubmodelElements;
    }

    public async Task<IAssetAdministrationShell> ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadAssetAdministrationShellById, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.AssetAdministrationShells[0];
    }

    public async Task<ISubmodelElement> ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShortElements = idShortPathElements
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadSubmodelElementByPath, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.SubmodelElements[0];
    }

    public async Task<DbFileRequestResult> ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShortElements = idShortPathElements
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadFileByPath, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.FileRequestResult;
    }

    public async Task<IAssetInformation> ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadAssetInformation, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.AssetInformation;
    }

    public async Task<DbFileRequestResult> ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadThumbnail, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.FileRequestResult;
    }

    public async Task<DbRequestPackageEnvResult> ReadPackageEnv(string aasId, string smId)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasId,
            SubmodelIdentifier = smId
        };

        var dbRequestContext = new DbRequestContext()
        {
            //SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadPackageEnv, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task.ConfigureAwait(false);
        return tcs.PackageEnv;
    }

    public async Task<Contracts.Events.EventPayload> ReadEventMessages(DbEventRequest dbEventRequest)
    {
        var parameters = new DbRequestParams()
        {
            EventRequest = dbEventRequest,
        };

        var dbRequestContext = new DbRequestContext()
        {
            //SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadEventMessages, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task.ConfigureAwait(false);
        return tcs.EventPayload;
    }

    public Task UpdateEventMessages(DbEventRequest dbEventRequest)
    {
        var parameters = new DbRequestParams()
        {
            EventRequest = dbEventRequest,
        };

        var dbRequestContext = new DbRequestContext()
        {
            //SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.UpdateEventMessages, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        return taskCompletionSource.Task;
    }

    public async Task<ISubmodel> CreateSubmodel(ISecurityConfig securityConfig, ISubmodel newSubmodel, string aasIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelBody = newSubmodel,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadSubmodelById, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.Submodels[0];
    }
}
