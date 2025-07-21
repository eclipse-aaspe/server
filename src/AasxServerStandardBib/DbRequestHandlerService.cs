namespace AasxServerStandardBib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AasxServer;
using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using Contracts;
using Contracts.DbRequests;
using Contracts.Pagination;
using Contracts.QueryResult;
using Contracts.Security;
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

                Exception exception = null;
                DbRequestResult dbRequestResult = null;

                if (operation.CrudType != DbRequestCrudType.Read)
                {
                    while (ActiveReadOperations > 0)
                    {
                        Thread.Sleep(100);
                    }

                    //ReadActiveEvents.WaitOne();
                }
                try
                {
                    if (operation.CrudType == DbRequestCrudType.Read)
                    {
                        _lock.Release();

                        IncrementCounter();
                    }

                    dbRequestResult = await _persistenceService.DoDbOperation(operation);
                }
                catch (Exception ex)
                {
                    exception = ex;
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

                        //if(ActiveReadOperations == 0)
                        //{
                        //    ReadActiveEvents.Set();
                        //}
                    }

                    if (exception != null)
                    {
                        operation.TaskCompletionSource.SetException(exception);
                    }
                    else
                    {
                        operation.TaskCompletionSource.SetResult(dbRequestResult);
                    }
                }
            }
        }
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

        var dbRequest = new DbRequest(DbRequestOp.ReadPagedAssetAdministrationShells, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

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

    public async Task<IAssetAdministrationShell> CreateAssetAdministrationShell(ISecurityConfig securityConfig, IAssetAdministrationShell body)
    {
        var parameters = new DbRequestParams()
        {
            AasBody = body,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.CreateAssetAdministrationShell, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.AssetAdministrationShells[0];
    }

    public async Task<DbRequestResult> ReplaceAssetAdministrationShellById(ISecurityConfig security, string aasIdentifier, AssetAdministrationShell body)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            AasBody = body
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = security,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceAssetAdministrationShellById, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> DeleteAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier)
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

        var dbRequest = new DbRequest(DbRequestOp.DeleteAssetAdministrationShellById, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<IReference> CreateSubmodelReferenceInAAS(ISecurityConfig securityConfig, Reference body, string aasIdentifier)
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

        var dbRequest = new DbRequest(DbRequestOp.CreateSubmodelReference, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.References[0];
    }

    public async Task<DbRequestResult> DeleteSubmodelReferenceById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
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

        var dbRequest = new DbRequest(DbRequestOp.DeleteSubmodelReferenceById, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
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

        var dbRequest = new DbRequest(DbRequestOp.CreateSubmodel, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.Submodels[0];
    }

    public async Task<DbRequestResult> ReplaceSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            SubmodelBody = body
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceSubmodelById, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> UpdateSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            SubmodelBody = body
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.UpdateSubmodelById, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> DeleteSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.DeleteSubmodelById, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            PaginationParameters = paginationParameters,
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

    public async Task<ISubmodelElement> ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath
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
    public async Task<ISubmodelElement> CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, string idShortPath, bool first)
    {
        var parameters = new DbRequestParams()
        {
            SubmodelElementBody = body,
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath,
            First = first
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.CreateSubmodelElement, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.SubmodelElements[0];
    }

    public async Task<DbRequestResult> UpdateSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath,
            SubmodelElementBody = body
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.UpdateSubmodelElementByPath, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> ReplaceSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath,
            SubmodelElementBody = body
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceSubmodelElementByPath, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.DeleteSubmodelElementByPath, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
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

    public async Task<DbRequestResult> ReplaceAssetInformation(ISecurityConfig securityConfig, string aasIdentifier, AssetInformation body)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            AssetInformation = body
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceAssetInformation, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbFileRequestResult> ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath
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

    public async Task<DbRequestResult> ReplaceFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath,
            FileRequest = new DbFileRequestResult()
            {
                Stream = stream,
                ContentType = contentType,
                File = fileName
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceFileByPath, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            SubmodelIdentifier = submodelIdentifier,
            IdShort = idShortPath
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.DeleteFileByPath, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
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

        var tcs = await taskCompletionSource.Task.ConfigureAwait(false);
        return tcs.FileRequestResult;
    }

    public async Task<DbRequestResult> ReplaceThumbnail(ISecurityConfig securityConfig, string aasIdentifier, string fileName, string contentType, MemoryStream stream)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasIdentifier,
            FileRequest = new DbFileRequestResult()
            {
                Stream = stream,
                ContentType = contentType,
                File = fileName
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceThumbnail, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> DeleteThumbnail(ISecurityConfig securityConfig, string aasIdentifier)
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

        var dbRequest = new DbRequest(DbRequestOp.DeleteThumbnail, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<Contracts.Events.EventPayload> ReadEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest)
    {
        var parameters = new DbRequestParams()
        {
            EventRequest = dbEventRequest,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadEventMessages, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task.ConfigureAwait(false);
        return tcs.EventPayload;
    }

    public async Task<DbRequestResult> UpdateEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest)
    {
        var parameters = new DbRequestParams()
        {
            EventRequest = dbEventRequest,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.UpdateEventMessages, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);
        var tcs = await taskCompletionSource.Task.ConfigureAwait(false);
        return tcs;
    }

    public async Task<List<IConceptDescription>> ReadPagedConceptDescriptions(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string idShort = null, IReference isCaseOf = null, IReference dataSpecificationRef = null)
    {
        var parameters = new DbRequestParams()
        {
            PaginationParameters = paginationParameters,
            IdShort = idShort,
            IsCaseOf = isCaseOf,
            DataSpecificationRef = dataSpecificationRef
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
        return tcs.ConceptDescriptions;
    }

    public async Task<IConceptDescription> ReadConceptDescriptionById(ISecurityConfig securityConfig, string cdIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            ConceptDescriptionIdentifier = cdIdentifier,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadConceptDescriptionById, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.ConceptDescriptions[0];
    }

    public async Task<IConceptDescription> CreateConceptDescription(ISecurityConfig securityConfig, IConceptDescription body)
    {
        var parameters = new DbRequestParams()
        {
            ConceptDescriptionBody = body,
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.CreateConceptDescription, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.ConceptDescriptions[0];
    }

    public async Task<DbRequestResult> ReplaceConceptDescriptionById(ISecurityConfig securityConfig, IConceptDescription body, string cdIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            ConceptDescriptionBody = body,
            ConceptDescriptionIdentifier = cdIdentifier
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceConceptDescriptionById, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> DeleteConceptDescriptionById(ISecurityConfig securityConfig, string cdIdentifier)
    {
        var parameters = new DbRequestParams()
        {
            ConceptDescriptionIdentifier = cdIdentifier
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.DeleteConceptDescriptionById, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbRequestResult> GenerateSerializationByIds(ISecurityConfig securityConfig, List<string> aasIds = null, List<string> submodelIds = null, bool includeCD = false, bool createAASXPackage = false)
    {
        var parameters = new DbRequestParams()
        {
            AasIds = aasIds,
            SubmodelIds = submodelIds,
            IncludeCD = includeCD,
            CreateAASXPackage = createAASXPackage
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.GenerateSerializationByIds, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<QResult> QuerySearchSMs(ISecurityConfig securityConfig, bool withTotalCount, bool withLastId, string semanticId, string identifier, string diff, IPaginationParameters paginationParameters, string expression)
    {
        var parameters = new DbRequestParams()
        {
            QueryRequest = new DbQueryRequest()
            {
                WithTotalCount = withTotalCount,
                WithLastId = withLastId,
                SemanticId = semanticId,
                Identifier = identifier,
                Diff = diff,
                PageFrom = paginationParameters.Cursor,
                PageSize = paginationParameters.Limit,
                Expression = expression
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.QuerySearchSMs, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.QueryResult;
    }

    public async Task<int> QueryCountSMs(ISecurityConfig securityConfig, string semanticId, string identifier, string diff, IPaginationParameters paginationParameters, string expression)
    {
        var parameters = new DbRequestParams()
        {
            QueryRequest = new DbQueryRequest()
            {
                PageFrom = paginationParameters.Cursor,
                PageSize = paginationParameters.Limit,
                SemanticId = semanticId,
                Identifier = identifier,
                Diff = diff,
                Expression = expression
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.QueryCountSMs, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.Count;
    }

    public async Task<QResult> QuerySearchSMEs(ISecurityConfig securityConfig, string requested, bool withTotalCount, bool withLastId, string smSemanticId, string smIdentifier, string semanticId, string diff, string contains, string equal, string lower, string upper, IPaginationParameters paginationParameters, string expression)
    {
        var parameters = new DbRequestParams()
        {
            QueryRequest = new DbQueryRequest()
            {
                Requested = requested,
                WithTotalCount = withTotalCount,
                WithLastId = withLastId,
                PageFrom = paginationParameters.Cursor,
                PageSize = paginationParameters.Limit,
                SmSemanticId = smSemanticId,
                Identifier = smIdentifier,
                SemanticId = semanticId,
                Diff = diff,
                Contains = contains,
                Equal = equal,
                Lower = lower,
                Upper = upper,
                Expression = expression
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.QuerySearchSMEs, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.QueryResult;
    }

    public async Task<int> QueryCountSMEs(ISecurityConfig securityConfig, string smSemanticId, string smIdentifier, string semanticId, string diff, string contains,
        string equal, string lower, string upper,  IPaginationParameters paginationParameters, string expression)
    {
        var parameters = new DbRequestParams()
        {
            QueryRequest = new DbQueryRequest()
            {
                PageFrom = paginationParameters.Cursor,
                PageSize = paginationParameters.Limit,
                SmSemanticId = smSemanticId,
                Identifier = smIdentifier,
                SemanticId = semanticId,
                Diff = diff,
                Contains = contains,
                Equal = equal,
                Lower = lower,
                Upper = upper,
                Expression = expression
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.QueryCountSMEs, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.Count;
    }

    public async Task<List<object>> QueryGetSMs(ISecurityConfig securityConfig, IPaginationParameters paginationParameters, string expression)
    {
        var parameters = new DbRequestParams()
        {
            QueryRequest = new DbQueryRequest()
            {
                PageFrom = paginationParameters.Cursor,
                PageSize = paginationParameters.Limit,
                Expression = expression
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.QueryGetSMs, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;

        if (tcs.Ids != null)
        {
            return tcs.Ids.ConvertAll(r => r as object);
        }

        return tcs.Submodels.ConvertAll(r => r as object);
    }

    public async Task<DbRequestResult> DeleteAASXByPackageId(ISecurityConfig securityConfig, string packageId)
    {
        var parameters = new DbRequestParams()
        {
            PackageIdentifier = packageId
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.DeleteAASXByPackageId, DbRequestCrudType.Delete, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }

    public async Task<DbFileRequestResult> ReadAASXByPackageId(ISecurityConfig securityConfig, string packageId)
    {
        var parameters = new DbRequestParams()
        {
            PackageIdentifier = packageId
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadAASXByPackageId, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs.FileRequestResult;
    }

    public async Task<List<PackageDescription>> ReadPagedAASXPackageIds(ISecurityConfig securityConfig, IPaginationParameters paginationParameters, string aasId)
    {
        var parameters = new DbRequestParams()
        {
            AssetAdministrationShellIdentifier = aasId,
            PaginationParameters = paginationParameters
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReadPagedAASXPackageIds, DbRequestCrudType.Read, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task.ConfigureAwait(false);

        var packageDescriptions = tcs.PackageDescriptions;

        if (packageDescriptions.Count == 0)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<DbRequestHandlerService>>();
                scopedLogger.LogInformation($"No packages with requested asset id found.");
            }
        }

        return packageDescriptions;
    }

    public async Task<PackageDescription> CreateAASXPackage(ISecurityConfig securityConfig, MemoryStream stream, string fileName)
    {
        var parameters = new DbRequestParams()
        {
            FileRequest = new DbFileRequestResult()
            {
                Stream = stream,
                File = fileName
            }
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.CreateAASXPackage, DbRequestCrudType.Create, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;

        var packageDescriptions = tcs.PackageDescriptions;

        return packageDescriptions[0];
    }

    public async Task<DbRequestResult> UpdateAASXPackageById(ISecurityConfig securityConfig, string packageId, MemoryStream stream, string fileName)
    {
        var parameters = new DbRequestParams()
        {
            FileRequest = new DbFileRequestResult()
            {
                Stream = stream,
                File = fileName,
            },
            PackageIdentifier = packageId
        };

        var dbRequestContext = new DbRequestContext()
        {
            SecurityConfig = securityConfig,
            Params = parameters
        };
        var taskCompletionSource = new TaskCompletionSource<DbRequestResult>();

        var dbRequest = new DbRequest(DbRequestOp.ReplaceAASXPackageById, DbRequestCrudType.Update, dbRequestContext, taskCompletionSource);

        _queryOperations.Add(dbRequest);

        var tcs = await taskCompletionSource.Task;
        return tcs;
    }



    private void IncrementCounter()
    {
        Interlocked.Increment(ref ActiveReadOperations);
    }

    private void DecrementCounter()
    {
        Interlocked.Decrement(ref ActiveReadOperations);
    }

}
