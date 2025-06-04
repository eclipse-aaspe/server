namespace AasxServerDB;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AasxServerDB.Context;
using AdminShellNS.Extensions;
using AdminShellNS;
using Contracts;
using Microsoft.IdentityModel.Tokens;
using AasCore.Aas3_0;
using Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using AasxServerStandardBib.Logging;
using Contracts.Exceptions;
using Contracts.DbRequests;
using System.Threading.Tasks;

using System.Linq.Dynamic.Core;
using AasxServerDB.Entities;
using System.Net.Mime;

public class EntityFrameworkPersistenceService : IPersistenceService
{
    private readonly IContractSecurityRules _contractSecurityRules;
    private readonly IEventService _eventService;
    private readonly QueryGrammarJSON _grammar;
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkPersistenceService(IServiceProvider serviceProvider, IContractSecurityRules contractSecurityRules, IEventService eventService, QueryGrammarJSON grammar)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _contractSecurityRules = contractSecurityRules ?? throw new ArgumentNullException(nameof(contractSecurityRules));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
    }

    public void InitDB(bool reloadDB, string dataPath)
    {
        AasContext.DataPath = dataPath;

        //Provoke OnConfiguring so that IsPostgres is set
        using (var db = new AasContext())
        {
            bool isPostgres = AasContext.IsPostgres;
        
            // Get database
            Console.WriteLine($"Use {(isPostgres ? "POSTGRES" : "SQLITE")}");

            if (isPostgres)
            {
                using (var postgredDb = new PostgreAasContext())
                {
                    // Get path
                    var connectionString = postgredDb.Database.GetConnectionString();
                    Console.WriteLine($"Database connection string: {connectionString}");
                    if (connectionString.IsNullOrEmpty())
                    {
                        throw new Exception("Database connection string is empty.");
                    }

                    // Check if db exists
                    var canConnect = postgredDb.Database.CanConnect();
                    if (!canConnect)
                    {
                        Console.WriteLine($"Unable to connect to the database.");
                    }

                    // Delete database
                    if (canConnect && reloadDB)
                    {
                        Console.WriteLine("Clear database.");
                        postgredDb.Database.EnsureDeleted();
                    }

                    // Create the database if it does not exist
                    // Applies any pending migrations
                    try
                    {
                        postgredDb.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Migration failed: {ex.Message}\nTry deleting the database.");
                    }
                }
            }
            else
            {
                using (var sqliteDb = new SqliteAasContext())
                {
                    // Get path
                    var connectionString = sqliteDb.Database.GetConnectionString();
                    Console.WriteLine($"Database connection string: {connectionString}");
                    if (connectionString.IsNullOrEmpty())
                    {
                        throw new Exception("Database connection string is empty.");
                    }
                    if (!isPostgres && !Directory.Exists(Path.GetDirectoryName(connectionString.Replace("Data Source=", string.Empty))))
                    {
                        throw new Exception($"Directory to the database does not exist. Check appsettings.json. Connection string: {connectionString}");
                    }

                    // Check if db exists
                    var canConnect = sqliteDb.Database.CanConnect();
                    if (!canConnect)
                    {
                        Console.WriteLine($"Unable to connect to the database.");
                    }

                    // Delete database
                    if (canConnect && reloadDB)
                    {
                        Console.WriteLine("Clear database.");
                        sqliteDb.Database.EnsureDeleted();
                    }

                    // Create the database if it does not exist
                    // Applies any pending migrations
                    try
                    {
                        sqliteDb.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Migration failed: {ex.Message}\nTry deleting the database.");
                    }
                }
            }
        }
    }

    public void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles)
    {
        VisitorAASX.ImportAASXIntoDB(filePath, createFilesOnly, withDbFiles);
    }

    public List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list)
    {
        return CrudOperator.GetFilteredPackages(filterPath, list);
    }

    public async Task<DbRequestResult> DoDbOperation(DbRequest dbRequest)
    {
        var result = new DbRequestResult();

        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(dbRequest.Context.SecurityConfig, out securityCondition);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: API route");
        }

        var aasIdentifier = dbRequest.Context.Params.AssetAdministrationShellIdentifier;
        var submodelIdentifier = dbRequest.Context.Params.SubmodelIdentifier;
        var conceptDescriptionIdentifier = dbRequest.Context.Params.ConceptDescriptionIdentifier;

        var idShort = dbRequest.Context.Params.IdShort;

        using (var scope = _serviceProvider.CreateScope())
        {
            var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();

            scopedLogger.LogDebug($"Starting operation in database service: {dbRequest.Operation}");

            using (var db = new AasContext())
            {
                switch (dbRequest.Operation)
                {
                    case DbRequestOp.ReadPackageEnv:
                        var envFile = "";

                        if (IsPackageEnvPresent(db, aasIdentifier, submodelIdentifier, true, out envFile, out AdminShellPackageEnv packageEnv))
                        {
                            result.PackageEnv = new DbRequestPackageEnvResult()
                            {
                                PackageEnv = packageEnv,
                                EnvFileName = envFile
                            };
                        }
                        break;

                    case DbRequestOp.ReadPagedAssetAdministrationShells:
                        // SpecificAssetIds is serialized as JSON and can not be handled by DB currently
                        var assetAdministrationShells =
                            CrudOperator.ReadPagedAssetAdministrationShells(
                                db,
                                dbRequest.Context.Params.PaginationParameters,
                                dbRequest.Context.Params.AssetIds,
                                dbRequest.Context.Params.IdShort);
                        result.AssetAdministrationShells = assetAdministrationShells;
                        break;
                    case DbRequestOp.ReadAssetAdministrationShellById:
                        var found = IsAssetAdministrationShellPresent(db, aasIdentifier, true, out _, out IAssetAdministrationShell aas);
                        if (found)
                        {
                            scopedLogger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
                        }
                        else
                        {
                            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
                        }

                        result.AssetAdministrationShells = new List<IAssetAdministrationShell>
                        {
                            aas
                        };
                        break;
                    case DbRequestOp.CreateAssetAdministrationShell:
                        var body = dbRequest.Context.Params.AasBody;
                        found = IsAssetAdministrationShellPresent(db, body.Id, false, out _, out _);
                        if (found)
                        {
                            scopedLogger.LogDebug($"Cannot create requested AAS !!");
                            throw new DuplicateException($"AssetAdministrationShell with id {body.Id} already exists.");
                        }

                        var addedInDb = CrudOperator.CreateAas(db, body);

                        result.AssetAdministrationShells = new List<IAssetAdministrationShell>
                        {
                            addedInDb
                        };
                        break;
                    case DbRequestOp.ReplaceAssetAdministrationShellById:
                        found = IsAssetAdministrationShellPresent(db, aasIdentifier, false, out AASSet aasDb, out IAssetAdministrationShell _);

                        if (found)
                        {
                            CrudOperator.ReplaceAssetAdministrationShellById(db, aasDb, dbRequest.Context.Params.AasBody);
                        }
                        else
                        {
                            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
                        }
                        break;
                    case DbRequestOp.DeleteAssetAdministrationShellById:
                        if (IsAssetAdministrationShellPresent(db, aasIdentifier, false, out aasDb, out _))
                        {
                            scopedLogger.LogDebug($"Found aas with id {aasIdentifier}");

                            CrudOperator.DeleteAAS(db, aasIdentifier);
                        }
                        else
                        {
                            throw new NotFoundException($"AAS with id {aasIdentifier} NOT found");
                        }
                        break;


                    case DbRequestOp.CreateSubmodelReference:
                        IReference createdSubmodelReference = null;
                        found = IsAssetAdministrationShellPresent(db, aasIdentifier, false, out _, out _);
                        if (found)
                        {
                            createdSubmodelReference = CrudOperator.CreateSubmodelReferenceInAAS(db, dbRequest.Context.Params.Reference,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                        }
                        else
                        {
                            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
                        }

                        result.References = new List<IReference>
                    {
                        createdSubmodelReference
                    };
                        break;
                    case DbRequestOp.DeleteSubmodelReferenceById:
                        found = IsAssetAdministrationShellPresent(db, aasIdentifier, false, out _, out _);
                        if (found)
                        {
                            createdSubmodelReference = CrudOperator.CreateSubmodelReferenceInAAS(db, dbRequest.Context.Params.Reference,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                        }
                        else
                        {
                            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
                        }
                        break;

                    case DbRequestOp.ReadPagedSubmodels:
                        var reqSemanticId = dbRequest.Context.Params.Reference;

                        var output = CrudOperator.ReadPagedSubmodels(db, dbRequest.Context.Params.PaginationParameters, securityCondition, reqSemanticId, dbRequest.Context.Params.IdShort);

                        if (dbRequest.Context.Params.Reference != null)
                        {
                            scopedLogger.LogDebug($"Filtering Submodels with requested Semnatic Id.");
                            output = output.Where(s => s.SemanticId != null && s.SemanticId.Matches(reqSemanticId)).ToList();
                            if (output.IsNullOrEmpty())
                            {
                                scopedLogger.LogInformation($"No Submodels with requested semanticId found.");
                            }
                        }
                        result.Submodels = output;
                        break;
                    case DbRequestOp.ReadSubmodelById:
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, true, out ISubmodel submodel);

                        if (found)
                        {
                            scopedLogger.LogDebug($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} found.");
                        }
                        else
                        {
                            throw new NotFoundException($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} not found.");
                        }

                        result.Submodels = new List<ISubmodel>
                        {
                            submodel
                        };
                        break;
                    case DbRequestOp.CreateSubmodel:
                        var newSubmodel = dbRequest.Context.Params.SubmodelBody;
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, newSubmodel.Id, false, out _);

                        if (found)
                        {
                            throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
                        }

                        var createdSubmodel = CrudOperator.CreateSubmodel(db, newSubmodel, aasIdentifier);

                        result.Submodels = new List<ISubmodel>
                        {
                            createdSubmodel
                        };
                        break;
                    case DbRequestOp.UpdateSubmodelById:
                        throw new NotImplementedException();
                    case DbRequestOp.ReplaceSubmodelById:
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _);

                        if (found)
                        {
                            CrudOperator.ReplaceSubmodelById(db, aasIdentifier, submodelIdentifier, dbRequest.Context.Params.SubmodelBody);
                        }
                        else
                        {
                            var message = $"Submodel with id {submodelIdentifier} NOT found";
                            if (!String.IsNullOrEmpty(aasIdentifier))
                            {
                                message += $" in AAS with id {aasIdentifier}";
                            }
                            throw new NotFoundException(message);
                        }
                        break;
                    case DbRequestOp.DeleteSubmodelById:
                        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _))
                        {
                            scopedLogger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                            CrudOperator.DeleteSubmodel(db, submodelIdentifier);
                        }
                        else
                        {
                            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }

                        break;
                    case DbRequestOp.ReadPagedSubmodelElements:
                        var submodelElements = CrudOperator.ReadPagedSubmodelElements(db, dbRequest.Context.Params.PaginationParameters,
                            securityCondition, aasIdentifier, submodelIdentifier);
                        if (submodelElements == null)
                        {
                            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }

                        result.SubmodelElements = submodelElements;
                        break;
                    case DbRequestOp.ReadSubmodelElementByPath:
                        var submodelElement = CrudOperator.ReadSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShort, out SMESet smE);
                        if (submodelElement == null)
                        {
                            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }

                        result.SubmodelElements = new List<ISubmodelElement>
                        {
                            submodelElement
                        };
                        break;
                    case DbRequestOp.CreateSubmodelElement:
                        ISubmodelElement createdSubmodelElement = null;

                        var newSubmodelElement = dbRequest.Context.Params.SubmodelElementBody;

                        var smFound = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _);
                        if (smFound)
                        {
                            scopedLogger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");

                            //ToDo: Do not use in-memory submodel for check
                            /*
                            var smeFound = IsSubmodelElementPresent(output, newSubmodelElement.IdShort);
                            if (smeFound)
                            {
                                scopedLogger.LogDebug($"Cannot create requested submodel element !!");
                                throw new DuplicateException($"SubmodelElement with idShort {newSubmodelElement.IdShort} already exists in the submodel.");
                            }
                            else
                            {
                                return Converter.CreateSubmodelElement(aasIdentifier, submodelIdentifier, newSubmodelElement, idShortPath, first);
                            }
                            */
                            createdSubmodelElement = CrudOperator.CreateSubmodelElement(db, aasIdentifier, submodelIdentifier, newSubmodelElement, idShort, dbRequest.Context.Params.First);
                        }
                        else
                        {
                            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }

                        result.SubmodelElements = new List<ISubmodelElement>
                        {
                            createdSubmodelElement
                        };
                        break;
                    case DbRequestOp.ReplaceSubmodelElementByPath:
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _);
                        if (found)
                        {
                            CrudOperator.ReplaceSubmodelElementByPath(
                                db,
                                securityCondition,
                                aasIdentifier,
                                submodelIdentifier,
                                idShort,
                                dbRequest.Context.Params.SubmodelElementBody);
                        }
                        else
                        {
                            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }
                        break;
                    case DbRequestOp.UpdateSubmodelElementByPath:
                        throw new NotImplementedException();

                    case DbRequestOp.DeleteSubmodelElementByPath:
                        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _))
                        {
                            CrudOperator.DeleteSubmodelElement(
                                db,
                                securityCondition,
                                aasIdentifier,
                                submodelIdentifier,
                                idShort);
                        }
                        else
                        {
                            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }
                        break;
                    case DbRequestOp.ReadFileByPath:
                        byte[] content;
                        long fileSize;
                        var file = ReadFileByPath(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                            dbRequest.Context.Params.SubmodelIdentifier,
                            dbRequest.Context.Params.IdShort, out content, out fileSize);
                        result.FileRequestResult = new DbFileRequestResult()
                        {
                            Content = content,
                            File = file,
                            FileSize = fileSize
                        };
                        break;
                    case DbRequestOp.ReplaceFileByPath:
                        ReplaceFileByPath(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                            dbRequest.Context.Params.SubmodelIdentifier,
                            dbRequest.Context.Params.IdShort,
                            dbRequest.Context.Params.FileRequest.File,
                            dbRequest.Context.Params.FileRequest.ContentType,
                            dbRequest.Context.Params.FileRequest.Stream);
                        break;
                    case DbRequestOp.DeleteFileByPath:
                        DeleteFileByPath(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                            dbRequest.Context.Params.SubmodelIdentifier,
                            dbRequest.Context.Params.IdShort);
                        break;

                    case DbRequestOp.ReadAssetInformation:
                        IAssetInformation assetInformation = null;

                        found = IsAssetAdministrationShellPresent(db, aasIdentifier, true, out _, out aas);
                        if (found)
                        {
                            assetInformation = aas.AssetInformation;
                            scopedLogger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
                        }
                        else
                        {
                            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
                        }

                        result.AssetInformation = assetInformation;
                        break;
                    case DbRequestOp.ReplaceAssetInformation:
                        found = IsAssetAdministrationShellPresent(db, aasIdentifier, false, out AASSet aasdb, out _);

                        if (found)
                        {
                            CrudOperator.ReplaceAssetInformation(db, aasdb, dbRequest.Context.Params.AssetInformation);

                            scopedLogger.LogDebug($"AssetInformation from AAS with id {aasIdentifier} updated successfully.");
                        }
                        else
                        {
                            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
                        }
                        break;

                    case DbRequestOp.ReadThumbnail:
                        var thumbnail = ReadThumbnail(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                            out content, out fileSize);

                        result.FileRequestResult = new DbFileRequestResult()
                        {
                            Content = content,
                            File = thumbnail,
                            FileSize = fileSize
                        };
                        break;
                    case DbRequestOp.ReplaceThumbnail:
                        ReplaceThumbnail(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                            dbRequest.Context.Params.FileRequest.File,
                            dbRequest.Context.Params.FileRequest.ContentType,
                            dbRequest.Context.Params.FileRequest.Stream);
                        break;
                    case DbRequestOp.DeleteThumbnail:
                        DeleteThumbnail(
                            db,
                            dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                        break;
                    case DbRequestOp.ReadEventMessages:
                        var eventPayload = ReadEventMessages(securityCondition,
                            dbRequest.Context.Params.EventRequest);
                        result.EventPayload = eventPayload;
                        break;
                    case DbRequestOp.UpdateEventMessages:
                        UpdateEventMessages(db, securityCondition,
                            dbRequest.Context.Params.EventRequest);
                        break;

                    case DbRequestOp.QuerySearchSMs:
                        var queryRequest = dbRequest.Context.Params.QueryRequest;
                        var query = new Query(_grammar);
                        var qresult = query.SearchSMs(db, queryRequest.WithTotalCount, queryRequest.WithLastId, queryRequest.SemanticId,
                            queryRequest.Identifier, queryRequest.Diff, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.QueryResult = qresult;
                        break;
                    case DbRequestOp.QueryCountSMs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        var count = query.CountSMs(db, queryRequest.SemanticId, queryRequest.Identifier, queryRequest.Diff,
                            queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.Count = count;
                        break;
                    case DbRequestOp.QuerySearchSMEs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        qresult = query.SearchSMEs(db, queryRequest.Requested, queryRequest.WithTotalCount, queryRequest.WithLastId, queryRequest.SmSemanticId, queryRequest.Identifier, queryRequest.SemanticId, queryRequest.Diff,
                            queryRequest.Contains, queryRequest.Equal, queryRequest.Lower, queryRequest.Upper, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.QueryResult = qresult;
                        break;
                    case DbRequestOp.QueryCountSMEs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        count = query.CountSMEs(db, queryRequest.SmSemanticId, queryRequest.Identifier, queryRequest.SemanticId, queryRequest.Diff,
                            queryRequest.Contains, queryRequest.Equal, queryRequest.Lower, queryRequest.Upper, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.Count = count;
                        break;
                    case DbRequestOp.QueryGetSMs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        var submodels = query.GetSubmodelList(db, securityCondition, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.Submodels = submodels;
                        break;
                    case DbRequestOp.ReadPagedConceptDescriptions:
                        //ToDo: Filter on IsCaseOf and DataSpecificationRef
                        var conceptDescriptions = CrudOperator.GetPagedConceptDescriptions(
                            db,
                            dbRequest.Context.Params.PaginationParameters,
                            idShort,
                            dbRequest.Context.Params.IsCaseOf,
                            dbRequest.Context.Params.DataSpecificationRef);

                        //if (output.Any())
                        //{
                        //    //Filter based on IsCaseOf
                        //    if (isCaseOf != null)
                        //    {
                        //        var cdList = new List<IConceptDescription>();
                        //        foreach (var conceptDescription in output)
                        //        {
                        //            if (!conceptDescription.IsCaseOf.IsNullOrEmpty())
                        //            {
                        //                foreach (var reference in conceptDescription.IsCaseOf)
                        //                {
                        //                    if (reference != null && reference.Matches(isCaseOf))
                        //                    {
                        //                        cdList.Add(conceptDescription);
                        //                        break;
                        //                    }
                        //                }
                        //            }
                        //        }
                        //        if (cdList.IsNullOrEmpty())
                        //        {
                        //            //_logger.LogDebug($"No Concept Description with requested IsCaseOf found.");
                        //        }
                        //        else
                        //        {
                        //            output = cdList;
                        //        }

                        //    }

                        //    //Filter based on DataSpecificationRef
                        //    if (dataSpecificationRef != null)
                        //    {
                        //        var cdList = new List<IConceptDescription>();
                        //        foreach (var conceptDescription in output)
                        //        {
                        //            if (!conceptDescription.EmbeddedDataSpecifications.IsNullOrEmpty())
                        //            {
                        //                foreach (var reference in conceptDescription.EmbeddedDataSpecifications)
                        //                {
                        //                    if (reference != null && reference.DataSpecification.Matches(dataSpecificationRef))
                        //                    {
                        //                        cdList.Add(conceptDescription);
                        //                        break;
                        //                    }
                        //                }
                        //            }
                        //        }
                        //        if (cdList.IsNullOrEmpty())
                        //        {
                        //            //_logger.LogDebug($"No Concept Description with requested DataSpecificationReference found.");
                        //        }
                        //        else
                        //        {
                        //            output = cdList;
                        //        }
                        //    }
                        //}
                        result.ConceptDescriptions = conceptDescriptions;
                        break;
                    case DbRequestOp.ReadConceptDescriptionById:
                        found = IsConceptDescriptionPresent(
                            db,
                            conceptDescriptionIdentifier,
                            true,
                            out IConceptDescription conceptDescription);
                        if (found)
                        {
                            scopedLogger.LogDebug($"Concept Description with id {conceptDescriptionIdentifier} found.");
                        }
                        else
                        {
                            throw new NotFoundException($"Concept Description with id {conceptDescriptionIdentifier} not found.");
                        }
                        result.ConceptDescriptions = new List<IConceptDescription>
                        {
                            conceptDescription
                        };
                        break;
                    case DbRequestOp.CreateConceptDescription:
                        var conceptDescriptionBody = dbRequest.Context.Params.ConceptDescriptionBody;
                        found = IsConceptDescriptionPresent(db, conceptDescriptionBody.Id, false, out _);
                        if (found)
                        {
                            scopedLogger.LogDebug($"Cannot create requested CD !!");
                            throw new DuplicateException($"Concept description with id {conceptDescriptionBody.Id} already exists.");
                        }

                        conceptDescription = CrudOperator.CreateConceptDescription(db, conceptDescriptionBody);

                        result.ConceptDescriptions = new List<IConceptDescription>
                        {
                            conceptDescription
                        };
                        break;
                    case DbRequestOp.ReplaceConceptDescriptionById:
                        found = IsConceptDescriptionPresent(db, conceptDescriptionIdentifier, false, out _);
                        conceptDescriptionBody = dbRequest.Context.Params.ConceptDescriptionBody;

                        if (found)
                        {
                            CrudOperator.ReplaceConceptdescription(
                                db,
                                conceptDescriptionIdentifier,
                                conceptDescriptionBody);

                            scopedLogger.LogDebug($"Concept description with id {conceptDescriptionIdentifier} updated successfully.");
                        }
                        else
                        {
                            throw new NotFoundException($"Concept description with id {conceptDescriptionIdentifier} not found.");
                        }
                        break;
                    case DbRequestOp.DeleteConceptDescriptionById:
                        if (IsConceptDescriptionPresent(db, conceptDescriptionIdentifier, false, out _))
                        {
                            scopedLogger.LogDebug($"Found concept description with id {conceptDescriptionIdentifier}");

                            CrudOperator.DeleteConceptDescription(db, conceptDescriptionIdentifier);
                        }
                        else
                        {
                            throw new NotFoundException($"Concept description with id {conceptDescriptionIdentifier} NOT found");
                        }
                        break;
                    case DbRequestOp.GenerateSerializationByIds:
                        var environment = CrudOperator.GenerateSerializationByIds(
                            db,
                            dbRequest.Context.Params.AasIds,
                            dbRequest.Context.Params.SubmodelIds,
                            dbRequest.Context.Params.IncludeCD);
                        result.Environment = environment;
                        break;
                    default:
                        dbRequest.TaskCompletionSource.SetException(new Exception("Unknown Operation"));
                        break;
                }
            }
        }
        return result;
    }

    private string ReadFileByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize)
    {
        var fileElement = CrudOperator.ReadSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, out SMESet smE);
        content = null;
        fileSize = 0;

        var found = fileElement != null;
        if (found)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                scopedLogger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");

                string fileName = null;

                if (fileElement is AasCore.Aas3_0.File file)
                {
                    var envFileName = string.Empty;

                    var packageEnvFound = IsPackageEnvPresent(db, aasIdentifier, submodelIdentifier, false, out envFileName, out _);

                    if (packageEnvFound)
                    {
                        FileService.ReadFileInZip(envFileName, out content, out fileSize, scopedLogger, out fileName, file);
                    }
                    else
                    {
                        throw new NotFoundException($"Package for aas id {aasIdentifier} and submodel id {submodelIdentifier} not found");
                    }
                }
                else
                {
                    throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                }

                return fileName;
            }
        }
        else
        {
            throw new NotFoundException($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    public void ReplaceFileByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream)
    {
        var fileElement = CrudOperator.ReadSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, out SMESet smE);

        var found = fileElement != null;
        if (found)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();

                if (fileElement is AasCore.Aas3_0.File file)
                {
                    var envFileName = string.Empty;

                    var packageEnvFound = IsPackageEnvPresent(db, aasIdentifier, submodelIdentifier, false, out envFileName, out _);

                    if (packageEnvFound)
                    {
                        FileService.ReplaceFileInZip(envFileName, scopedLogger, file, fileName, contentType, stream);

                        //ToDo: Not sure what to do here?
                        //CrudOperator.DeleteSubmodelElement(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath);
                    }
                    else
                    {
                        throw new NotFoundException($"Package for aas id {aasIdentifier} and submodel id {submodelIdentifier} not found");
                    }
                }
                else
                {
                    //throw new OperationNotSupported($"Cannot delete the file. SubmodelElement {idShortPath} does not have a file attached.");
                }
            }
        }
        else
        {
            //throw new OperationNotSupported($"SubmodelElement found at {idShortPath} is not of type File");
        }
    }

    public void DeleteFileByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _))
        {
            SMESet sME = null;
            var fileElement = CrudOperator.ReadSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, out sME);

            var found = fileElement != null;
            if (found)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");

                    if (fileElement is AasCore.Aas3_0.File file)
                    {
                        var envFileName = string.Empty;

                        var packageEnvFound = IsPackageEnvPresent(db, aasIdentifier, submodelIdentifier, false, out envFileName, out _);

                        if (packageEnvFound)
                        {
                            FileService.DeleteFileInZip(envFileName, scopedLogger, file);
                        }
                    }
                    else
                    {
                        //throw new OperationNotSupported($"Cannot delete the file. SubmodelElement {idShortPath} does not have a file attached.");
                    }
                }
            }
            else
            {
                //throw new OperationNotSupported($"SubmodelElement found at {idShortPath} is not of type File");
            }
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    private string ReadThumbnail(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, out byte[] byteArray, out long fileSize)
    {
        string thumbnail = null;
        byteArray = null;
        fileSize = 0;

        if (IsAssetAdministrationShellPresent(db, aasIdentifier, true, out _, out IAssetAdministrationShell aas))
        {
            var envFileName = string.Empty;
            var found = IsPackageEnvPresent(db, aasIdentifier, null, false, out envFileName, out AdminShellPackageEnv packageEnv);

            if (found)
            {
                if (aas.AssetInformation != null)
                {
                    if (FileService.ReadFromThumbnail(aas.AssetInformation, envFileName, out byteArray, out fileSize))
                    {
                        thumbnail = aas.AssetInformation.DefaultThumbnail.Path;
                    }
                }
                else
                {
                    throw new NotFoundException($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogDebug($"Updated the thumbnail in AAS with Id {aasIdentifier}");
                }
            }
            else
            {
                throw new NotFoundException($"Package for aas id {aasIdentifier} not found");
            }
        }

        return thumbnail;
    }


    public void ReplaceThumbnail(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string fileName, string contentType, MemoryStream stream)
    {
        if (IsAssetAdministrationShellPresent(db, aasIdentifier, true, out AASSet aasDB, out IAssetAdministrationShell aas))
        {
            var assetInformation = aas.AssetInformation;
            var envFileName = string.Empty;
            var found = IsPackageEnvPresent(db, aasIdentifier, null, false, out envFileName, out AdminShellPackageEnv packageEnv);

            if (found)
            {
                if (assetInformation != null)
                {
                    if (FileService.ReplaceThumbnail(ref assetInformation, envFileName, fileName, contentType, stream))
                    {
                        CrudOperator.ReplaceAssetInformation(db, aasDB, assetInformation);
                    }
                    else
                    {
                        throw new Exception($"Could not save thumbnail with {fileName}");
                    }
                }
                else
                {
                    throw new NotFoundException($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogDebug($"Updated the thumbnail in AAS with Id {aasIdentifier}");
                }
            }
            else
            {
                throw new NotFoundException($"Package for aas id {aasIdentifier} not found");
            }
        }
    }

    public void DeleteThumbnail(AasContext db, string aasIdentifier)
    {
        if (IsAssetAdministrationShellPresent(db, aasIdentifier, true, out AASSet aasDB, out IAssetAdministrationShell aas))
        {
            var envFileName = string.Empty;
            var found = IsPackageEnvPresent(db, aasIdentifier, null, false, out envFileName, out AdminShellPackageEnv packageEnv);

            if (found)
            {
                var assetInformation = aas.AssetInformation;

                if (assetInformation != null)
                {
                    if (FileService.DeleteThumbnail(ref assetInformation, envFileName))
                    {
                        CrudOperator.ReplaceAssetInformation(db, aasDB,
                            assetInformation);
                    }
                    else
                    {
                        throw new Exception($"Could not delete thumbnail");
                    }
                }
                else
                {
                    throw new NotFoundException($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogDebug($"Deleted the thumbnail in AAS with Id {aasIdentifier}");
                }
            }
            else
            {
                throw new NotFoundException($"Package for aas id {aasIdentifier} not found");
            }
        }
    }

    private Contracts.Events.EventPayload ReadEventMessages(Dictionary<string, string>? securityCondition, DbEventRequest dbEventRequest)
    {
        var op = _eventService.FindEvent(dbEventRequest.Submodel, dbEventRequest.EventName);
        var eventData = _eventService.ParseData(op, dbEventRequest.Env[dbEventRequest.PackageIndex]);

        var diff = dbEventRequest.Diff;
        var wp = dbEventRequest.IsWithPayload;
        var limSm = dbEventRequest.LimitSm;
        var offSm = dbEventRequest.OffsetSm;
        var limSme = dbEventRequest.LimitSme;
        var offSme = dbEventRequest.OffsetSme;

        var eventPayload = new Contracts.Events.EventPayload();
        List<String> diffEntry = new List<String>();
        string changes = "CREATE UPDATE DELETE";

        if (eventData.Persistence == null || eventData.Persistence.Value == "" || eventData.Persistence.Value == "memory")
        {
            IReferable data = null;
            if (eventData.DataSubmodel != null)
            {
                data = eventData.DataSubmodel;
            }
            if (eventData.DataCollection != null)
            {
                data = eventData.DataCollection;
                // OUT: data
                // IN: data.sme[0], copy
                /*
                if (eventData.direction != null && eventData.direction.Value == "IN")
                {
                    data = null;
                    if (eventData.dataCollection is SubmodelElementCollection sme && sme.Value != null && sme.Value.Count == 1 && sme.Value[0] is SubmodelElementCollection smc)
                    {
                        data = smc;
                    }
                }
                */
                /*
                if (eventData.direction != null && eventData.direction.Value == "IN" && eventData.mode != null && (eventData.mode.Value == "PUSH" || eventData.mode.Value == "PUT"))
                {
                    if (eventData.dataCollection.Value != null && eventData.dataCollection.Value.Count == 1 && eventData.dataCollection.Value[0] is SubmodelElementCollection)
                    {
                        data = eventData.dataCollection.Value[0];
                    }
                }
                */
            }
            // if (data == null)
            if (data == null || (data is SubmodelElementCollection smc && (smc.Value == null || smc.Value.Count == 0)))
            {
                return null;
            }
            int depth = 0;
            if (eventData.Direction != null && eventData.Direction.Value == "IN" && eventData.Mode != null && (eventData.Mode.Value == "PUSH" || eventData.Mode.Value == "PUT"))
            {
                depth = 1;
            }

            eventPayload = _eventService.CollectPayload(securityCondition, changes, depth,
            eventData.StatusData, eventData.DataReference, data, eventData.ConditionSM, eventData.ConditionSME,
            diff, diffEntry, wp, limSm, offSm, limSme, offSme);
        }
        else // database
        {
            eventPayload = _eventService.CollectPayload(securityCondition, changes, 0,
            eventData.StatusData, eventData.DataReference, null, eventData.ConditionSM, eventData.ConditionSME,
            diff, diffEntry, wp, limSm, limSme, offSm, offSme);
        }

        if (diff == "status")
        {
            /*
            if (eventData.LastUpdate != null && eventData.LastUpdate.Value != null && eventData.LastUpdate.Value != "")
            {
                eventPayload.status.lastUpdate = eventData.LastUpdate.Value;
            }
            */
        }
        else
        {
            var timeStamp = DateTime.UtcNow;
            if (eventData.Transmitted != null)
            {
                eventData.Transmitted.Value = eventPayload.status.transmitted;
                eventData.Transmitted.SetTimeStamp(DateTime.UtcNow);
            }
            var dt = DateTime.Parse(eventPayload.status.lastUpdate);
            if (eventData.LastUpdate != null)
            {
                eventData.LastUpdate.Value = eventPayload.status.lastUpdate;
                eventData.LastUpdate.SetTimeStamp(dt);
            }
            if (eventData.Diff != null)
            {
                if (diffEntry.Count > 0)
                {
                    eventData.Diff.Value = new List<ISubmodelElement>();
                    int i = 0;
                    foreach (var d in diffEntry)
                    {
                        var p = new Property(DataTypeDefXsd.String);
                        p.IdShort = "diff" + i;
                        p.Value = d;
                        p.SetTimeStamp(dt);
                        eventData.Diff.Value.Add(p);
                        p.SetAllParentsAndTimestamps(eventData.Diff, dt, dt, DateTime.MinValue);
                        i++;
                    }
                    eventData.Diff.SetTimeStamp(dt);
                }
            }
        }
        return eventPayload;

    }

    private void UpdateEventMessages(AasContext db, Dictionary<string, string>? securityCondition, DbEventRequest eventRequest)
    {
        var op = _eventService.FindEvent(eventRequest.Submodel, eventRequest.EventName);
        var eventData = _eventService.ParseData(op, eventRequest.Env[eventRequest.PackageIndex]);

        string transmitted = "";
        string lastDiffValue = "";
        string statusValue = "";
        List<String> diffEntry = new List<string>();
        int count = 0;

        if (eventData.Persistence == null || eventData.Persistence.Value == "" || eventData.Persistence.Value == "memory")
        {
            IReferable data = null;
            if (eventData.DataSubmodel != null)
            {
                data = eventData.DataSubmodel;
            }
            if (eventData.DataCollection != null)
            {
                data = eventData.DataCollection;
            }
            if (data == null)
            {
                return;
            }

            count = _eventService.ChangeData(eventRequest.Body, eventData, eventRequest.Env, data, out transmitted, out lastDiffValue, out statusValue, diffEntry, eventRequest.PackageIndex);
        }
        else // DB
        {
            count = _eventService.ChangeData(eventRequest.Body, eventData, eventRequest.Env, null, out transmitted, out lastDiffValue, out statusValue, diffEntry, eventRequest.PackageIndex);
            CrudOperator.ReplaceSubmodelById(db, null, eventRequest.Submodel.Id, eventRequest.Submodel);
        }

        var dt = DateTime.Parse(lastDiffValue);
        var dtTransmit = DateTime.Parse(transmitted);
        if (eventData.Transmitted != null)
        {
            eventData.Transmitted.Value = transmitted;
            eventData.Transmitted.SetTimeStamp(dtTransmit);
        }
        if (eventData.LastUpdate != null)
        {
            eventData.LastUpdate.Value = lastDiffValue;
            eventData.LastUpdate.SetTimeStamp(dt);
        }
        if (eventData.Message != null && statusValue != null)
        {
            eventData.Message.Value = statusValue;
            eventData.Message.SetTimeStamp(dtTransmit);
        }
        if (eventData.Diff != null)
        {
            if (diffEntry.Count > 0)
            {
                eventData.Diff.Value = new List<ISubmodelElement>();
                int i = 0;
                foreach (var d in diffEntry)
                {
                    var p = new Property(DataTypeDefXsd.String);
                    p.IdShort = "diff" + i;
                    p.Value = d;
                    p.SetTimeStamp(dt);
                    eventData.Diff.Value.Add(p);
                    p.SetAllParentsAndTimestamps(eventData.Diff, dt, dt, DateTime.MinValue);
                    i++;
                }
                eventData.Diff.SetTimeStamp(dt);
            }
        }
    }

    public static bool IsPackageEnvPresent(AasContext db, string aasID, string smId, bool loadInMemory, out string envFileName, out AdminShellPackageEnv? packageEnv)
    {
        envFileName = ";";
        packageEnv = null;

        int envId = -1;

        if (!smId.IsNullOrEmpty())
        {
            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == smId);
            if (!aasID.IsNullOrEmpty())
            {
                var aasDB = db.AASSets
                        .Where(aas => aas.Identifier == aasID).ToList();
                if (aasDB == null || aasDB.Count != 1)
                {
                    return false;
                }
                var aasDBId = aasDB[0].Id;
                smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDBId);
            }
            var smDB = smDBQuery.ToList();
            if (smDB == null || smDB.Count != 1)
            {
                return false;
            }
            if (smDB[0].EnvId.HasValue)
            {
                envId = smDB[0].EnvId.Value;
            }
        }
        else
        {
            var aasDBList = db.AASSets.Where(aas => aas.Identifier == aasID).ToList();

            if (aasDBList.Count != 1)
            {
                return false;
            }
            envId = aasDBList[0].EnvId.Value;
        }

        if (smId.IsNullOrEmpty() && envId == -1)
        {
            return false;
        }

        envFileName = CrudOperator.GetAASXPath(envId);

        if (loadInMemory)
        {
            packageEnv = CrudOperator.GetPackageEnv(db, envId, smId);
        }

        return true;
    }

    private bool IsAssetAdministrationShellPresent(AasContext db, string aasIdentifier, bool loadIntoMemory, out AASSet aasDB, out IAssetAdministrationShell output)
    {
        output = null;
        aasDB = null;

        if (!aasIdentifier.IsNullOrEmpty())
        {
            aasDB = db.AASSets
                .Include(aas => aas.SMRefSets)
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);
            if (aasDB == null)
            {
                return false;
            }
            else
            {
                if (loadIntoMemory)
                {
                    output = CrudOperator.ReadAssetAdministrationShell(db, ref aasDB);

                    if (output != null)
                    {
                        var smDBList = aasDB.SMRefSets.ToList();

                        foreach (var sm in smDBList)
                        {
                            if (sm.Identifier != null)
                            {
                                output?.Submodels?.Add(new Reference(type: ReferenceTypes.ModelReference,
                                    keys: new List<IKey>() { new Key(KeyTypes.Submodel, sm.Identifier) }
                                ));
                            }
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }

    private bool IsSubmodelPresent(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, bool loadIntoMemory, out ISubmodel output)
    {
        output = null;

        var result = false;

        var smDBQuery = db.SMSets.AsQueryable();

        if (!string.IsNullOrEmpty(submodelIdentifier))
        {
            smDBQuery = smDBQuery.Where(sm => sm.Identifier == submodelIdentifier);
        }

        if (!string.IsNullOrEmpty(aasIdentifier))
        {
            var aasDB = db.AASSets
                .Include(aas => aas.SMRefSets)
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);

            if (aasDB != null)
            {
                var smRefDB = aasDB.SMRefSets.FirstOrDefault(s => s.Identifier == submodelIdentifier);
                if (smRefDB == null)
                {
                    return false;
                }
            }
        }

        if (securityCondition != null)
        {
            smDBQuery = smDBQuery.Where(securityCondition["sm."]);

            if (!string.IsNullOrEmpty(aasIdentifier))
            {
                //ToDo: Add aas securityCondition
            }
        }

        var smDB = smDBQuery.ToList();

        if (smDB.Count != 1)
        {
            return false;
        }

        if (loadIntoMemory)
        {
            output = CrudOperator.ReadSubmodel(db, smDB[0], securityCondition: securityCondition);
        }

        result = true;

        /*
        using (var db = new AasContext())
        {
            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasDB = db.AASSets
                    .Where(aas => aas.Identifier == aasIdentifier);
                if (aasDB == null || aasDB.Count() != 1)
                {
                    return false;
                }
                var aasDBId = aasDB.First().Id;
                smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDBId);
            }

            if (!securityConditionSM.IsNullOrEmpty())
            {
                smDBQuery = smDBQuery.Where(securityConditionSM);
            }
            var smDB = smDBQuery.ToList();

            if (smDB == null || smDB.Count != 1)
            {
                return result;
            }
            else
            {
                if (loadIntoMemory)
                {
                    output = Converter.GetSubmodel(smDB[0]);
                }
                result = true;
            }
        }
        */

        //    if (!aasIdentifier.IsNullOrEmpty())
        //{
        //    var aas = ReadAssetAdministrationShellById(aasIdentifier);
        //    if (aas != null)
        //    {
        //        foreach (var submodelReference in aas.Submodels)
        //        {
        //            if (submodelReference.GetAsExactlyOneKey().Value.Equals(submodelIdentifier))
        //            {
        //                output = Converter.GetSubmodel(null, submodelIdentifier);
        //                return true;
        //            }
        //        }
        //    }
        //}
        //else
        //{
        //    output = Converter.GetSubmodel(null, submodelIdentifier);
        //    bool isSubmodelPresent = output != null;
        //    return isSubmodelPresent;
        //}

        return result;
    }

    private bool IsConceptDescriptionPresent(AasContext db, string cdIdentifier, bool loadIntoMemory, out IConceptDescription output)
    {
        output = null;
        if (!cdIdentifier.IsNullOrEmpty())
        {
            var cdDB = db.CDSets
                .FirstOrDefault(cd => cd.Identifier == cdIdentifier);
            if (cdDB == null)
            {
                return false;
            }
            else
            {
                if (loadIntoMemory)
                {
                    output = CrudOperator.ReadConceptDescription(db,cdDB);
                }
                return true;
            }
        }
        return false;
    }

    //ToDo: Move into security?
    private bool InitSecurity(ISecurityConfig? securityConfig, out Dictionary<string, string>? securityCondition)
    {
        securityCondition = null;
        if (securityConfig != null && !securityConfig.NoSecurity)
        {
            // Get claims
            var authResult = false;
            var accessRole = securityConfig.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();
            var httpRoute = securityConfig.Principal.FindFirst("Route")?.Value;
            var neededRightsClaim = securityConfig.Principal.FindFirst("NeededRights")?.Value;
            securityCondition = _contractSecurityRules.GetCondition(accessRole, neededRightsClaim);
            if (accessRole != null && httpRoute != null && Enum.TryParse(neededRightsClaim, out AasSecurity.Models.AccessRights neededRights))
            {
                authResult = _contractSecurityRules.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _);
            }

            return authResult;
        }

        return true;
    }
}