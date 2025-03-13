namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using Contracts;
using Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;


public class DbRequestHandlerService : IDbRequestHandlerService
{
    private readonly BlockingCollection<IDbRequest> _queryOperations = new BlockingCollection<IDbRequest>();
    private readonly BlockingCollection<IDbRequest> _commandOperations = new BlockingCollection<IDbRequest>();

    //private ManualResetEvent _empty = new ManualResetEvent(false);

    private IPersistenceService _persistenceService;
    private readonly IServiceProvider _serviceProvider;

    private bool _isRequestingCommand = false;
    private readonly SemaphoreSlim _signal = new(0);

    public DbRequestHandlerService(IServiceProvider serviceProvider, IPersistenceService persistenceService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));

        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessCommandOperations);

    }

    public Task<IDbRequestResult> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort)
    {
        var dbRequestContext = new DbRequestContext()
        {
           AssetIds = assetIds,
           IdShort = idShort,
           SecurityConfig   = securityConfig,
           PaginationParameters = paginationParameters
        };
        var taskCompletionSource = new TaskCompletionSource<IDbRequestResult>();

        var dbRequest = new DbRequest(nameof(ReadPagedAssetAdministrationShells), dbRequestContext, taskCompletionSource);
        _queryOperations.Add(dbRequest);

        //if (aasList.Count == 0)
        //{
        //    using (var scope = _serviceProvider.CreateScope())
        //    {
        //        var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<DbRequestHandlerService>>();
        //        scopedLogger.LogInformation($"No AAS with requested assetId found.");
        //    }
        //}
        return taskCompletionSource.Task;
    }

    public Task<IDbRequestResult> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        var dbRequestContext = new DbRequestContext()
        {
            SubmodelIdentifier = submodelIdentifier,
            AssetAdministrationShellIdentifier = aasIdentifier,
            SecurityConfig = securityConfig,
        };
        var taskCompletionSource = new TaskCompletionSource<IDbRequestResult>();

        var dbRequest = new DbRequest(nameof(ReadSubmodelById), dbRequestContext, taskCompletionSource);
        _commandOperations.Add(dbRequest);

        return taskCompletionSource.Task;
    }

    private async Task ProcessQueryOperations()
    {
        foreach (var operation in _queryOperations.GetConsumingEnumerable())
        {
            if (_isRequestingCommand)
            {
                _signal.Wait();
            }

            if (operation != null)
            {
                if (operation.MethodName == nameof(ReadPagedAssetAdministrationShells))
                {
                    try
                    {
                        var aasList =
                            _persistenceService.ReadPagedAssetAdministrationShells(operation.Context.PaginationParameters, operation.Context.SecurityConfig, operation.Context.AssetIds, operation.Context.IdShort);
                        var result = new DbRequestResult()
                        {
                            AssetAdministrationShells = aasList
                        };
                        operation.TaskCompletionSource.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        var result = new DbRequestResult()
                        {
                            Exception = ex
                        };
                        operation.TaskCompletionSource.SetResult(result);
                    }
                    finally
                    {
                        _signal.Release();
                    }
                }
            }
        }
    }

    private async Task ProcessCommandOperations()
    {
        foreach (var operation in _commandOperations.GetConsumingEnumerable())
        {
            if (operation != null)
            {
                _isRequestingCommand = true;

                if (operation.MethodName == nameof(ReadSubmodelById))
                {
                    try
                    {
                        var submodel =
                            _persistenceService.ReadSubmodelById(operation.Context.SecurityConfig, operation.Context.AssetAdministrationShellIdentifier, operation.Context.SubmodelIdentifier);
                        var result = new DbRequestResult()
                        {
                            Submodel = submodel
                        };
                        operation.TaskCompletionSource.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        var result = new DbRequestResult()
                        {
                            Exception = ex
                        };
                        operation.TaskCompletionSource.SetResult(result);
                    }
                    finally
                    {
                        _isRequestingCommand = false;
                        _signal.Release();
                    }
                }
            }
        }
    }

    public Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier) => throw new NotImplementedException();
}
