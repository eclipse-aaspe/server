namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections;
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
    private readonly PriorityQueue<IDbRequest,DbRequestPriority> _queueOperations = new PriorityQueue<IDbRequest, DbRequestPriority>();

    //private ManualResetEvent _empty = new ManualResetEvent(false);

    private IPersistenceService _persistenceService;
    private readonly IServiceProvider _serviceProvider;

    private readonly SemaphoreSlim _signal = new(0);
    private readonly SemaphoreSlim _lock = new(0);

    public DbRequestHandlerService(IServiceProvider serviceProvider, IPersistenceService persistenceService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));

        _lock.Release();
        Task.Run(ProcessQueryOperations);
        Task.Run(ProcessQueryOperations);
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
        _queueOperations.Enqueue(dbRequest,DbRequestPriority.Query);
        _signal.Release();

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
        _queueOperations.Enqueue(dbRequest, DbRequestPriority.Command);
        _signal.Release();

        return taskCompletionSource.Task;
    }

    private async Task ProcessQueryOperations()
    {
        while (true)
        {
            // Warte, bis eine Operation vorliegt
            await _signal.WaitAsync();


            if (_queueOperations.TryDequeue(out var operation, out var _))
            {
                if (operation != null)
                {
                    _lock.Wait();

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
                            operation.TaskCompletionSource.SetException(ex);
                        }
                        finally
                        {
                            _lock.Release();
                            _signal.Release();
                        }
                    }
                    else if (operation.MethodName == nameof(ReadSubmodelById))
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
                            operation.TaskCompletionSource.SetException(ex);
                        }
                        finally
                        {
                            _lock.Release();
                            _signal.Release();
                        }
                    }
                }
            }
        }
    }


    public Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier) => throw new NotImplementedException();
}
