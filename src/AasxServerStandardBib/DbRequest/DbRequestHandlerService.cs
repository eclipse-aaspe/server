namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using Contracts;
using Contracts.DbRequests;
using Contracts.Pagination;
using Opc.Ua.Client;

public class DbRequestHandlerService : IDbRequestHandlerService
{
    private readonly BlockingCollection<DbRequest> _queryOperations = new BlockingCollection<DbRequest>();

    private IPersistenceService _persistenceService;
    private readonly IServiceProvider _serviceProvider;

    private readonly SemaphoreSlim _lock = new(0);
    private bool _blockOperations = false;


    public DbRequestHandlerService(IServiceProvider serviceProvider, IPersistenceService persistenceService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));

        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
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

        var dbRequest = new DbRequest(typeof(IAssetAdministrationShell).Name, dbRequestContext, DbRequestCrudType.Read, true, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        //if (aasList.Count == 0)
        //{
        //    using (var scope = _serviceProvider.CreateScope())
        //    {
        //        var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<DbRequestHandlerService>>();
        //        scopedLogger.LogInformation($"No AAS with requested assetId found.");
        //    }
        //}

        var tcs = await taskCompletionSource.Task;
        return tcs.AssetAdministrationShells;
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

        var dbRequest = new DbRequest(typeof(ISubmodel).Name, dbRequestContext, DbRequestCrudType.Create, false, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        //if (aasList.Count == 0)
        //{
        //    using (var scope = _serviceProvider.CreateScope())
        //    {
        //        var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<DbRequestHandlerService>>();
        //        scopedLogger.LogInformation($"No AAS with requested assetId found.");
        //    }
        //}

        var tcs = await taskCompletionSource.Task;
        return tcs.Submodels[0];
    }

    private async Task ProcessQueryOperations()
    {
        foreach (var operation in _queryOperations.GetConsumingEnumerable())
        {
            if (operation != null)
            {
                if (_blockOperations)
                {
                    _lock.Wait();
                }
                try
                {
                    if (operation.CrudType != DbRequestCrudType.Read)
                    {
                        _blockOperations = true;
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
                        _blockOperations = false;
                    }
                }
            }
        }
    }

    public Task<List<IClass>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier) => throw new NotImplementedException();
}
