/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

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
using Contracts.Security;
using System.IO.Packaging;
using System.Reflection.Metadata;
using AdminShellNS.Models;
using Microsoft.Extensions.Hosting;
using System.IO.Compression;

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

    public void ImportAASXIntoDB(string filePath, bool createFilesOnly)
    {
        VisitorAASX.ImportAASXIntoDB(filePath, createFilesOnly);
    }

    public List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list)
    {
        return CrudOperator.GetFilteredPackages(filterPath, list);
    }

    public async Task<DbRequestResult> DoDbOperation(DbRequest dbRequest)
    {
        var result = new DbRequestResult();
        var securityConfig = dbRequest.Context.SecurityConfig;

        Dictionary<string, string>? securityCondition = null;
        List<AccessPermissionRule> accessRules = null;
        bool isAllowed = false;

        switch (dbRequest.Operation)
        {
            //ToDo: Ignore security when from Page
            case DbRequestOp.ReadPackageEnv:
            case DbRequestOp.ReadThumbnail:
            case DbRequestOp.ReadPagedAASXPackageIds:
                isAllowed = InitSecurity(securityConfig, out securityCondition, out accessRules, ignoreNullConfig: true);

                if (!isAllowed)
                {
                    throw new NotAllowed($"NOT ALLOWED: API route");
                }

                break;
            case DbRequestOp.QuerySearchSMs:
            case DbRequestOp.QuerySearchSMEs:
            case DbRequestOp.QueryCountSMs:
            case DbRequestOp.QueryCountSMEs:
                isAllowed = InitSecurity(securityConfig, out securityCondition, out accessRules, "/query");

                if (!isAllowed)
                {
                    throw new NotAllowed($"NOT ALLOWED: API route");
                }
                break;
            default:
                isAllowed = InitSecurity(securityConfig, out securityCondition, out accessRules);

                if (!isAllowed)
                {
                    throw new NotAllowed($"NOT ALLOWED: API route");
                }
                break;
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
                        if (IsPackageEnvPresent(db, securityCondition, null, aasIdentifier, submodelIdentifier, true, out EnvSet envSet, out AdminShellPackageEnv packageEnv))
                        {
                            result.PackageEnv = new DbRequestPackageEnvResult()
                            {
                                PackageEnv = packageEnv,
                                EnvFileName = envSet.Path
                            };
                        }
                        else if (IsAssetAdministrationShellPresent(db,aasIdentifier, false, out AASSet aasDB, out _))
                        {
                            var aasEnv = CrudOperator.GetEnvironment(db, securityCondition, aasDb: aasDB);
                            result.PackageEnv = new DbRequestPackageEnvResult()
                            {
                                PackageEnv = new AdminShellPackageEnv(aasEnv),
                            };
                        }
                        else if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out SMSet smDB, out _))
                        {
                            //ToDo: Need to be tested
                            var smEnv = CrudOperator.GetEnvironment(db, securityCondition, smDb: smDB);
                            result.PackageEnv = new DbRequestPackageEnvResult()
                            {
                                PackageEnv = new AdminShellPackageEnv(smEnv),
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
                        FileService.CreateThumbnailZipFile(body);

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
                        var querySM = new Query(_grammar);

                        var output = CrudOperator.ReadPagedSubmodels(db, querySM, dbRequest.Context.Params.PaginationParameters, securityCondition, reqSemanticId, dbRequest.Context.Params.IdShort);

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
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, true, out _, out ISubmodel submodel);

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
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, newSubmodel.Id, false, out _, out _);

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
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _);

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
                        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _))
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

                        var smFound = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _);
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
                        found = IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _);
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
                        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _))
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
                        var qresult = query.SearchSMs(securityCondition, db, queryRequest.WithTotalCount, queryRequest.WithLastId, queryRequest.SemanticId,
                            queryRequest.Identifier, queryRequest.Diff, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.QueryResult = qresult;
                        break;
                    case DbRequestOp.QueryCountSMs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        var count = query.CountSMs(securityConfig, securityCondition, db, queryRequest.SemanticId, queryRequest.Identifier, queryRequest.Diff,
                            queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.Count = count;
                        break;
                    case DbRequestOp.QuerySearchSMEs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        qresult = query.SearchSMEs(securityConfig, securityCondition,
                            db, queryRequest.Requested, queryRequest.WithTotalCount, queryRequest.WithLastId, queryRequest.SmSemanticId, queryRequest.Identifier, queryRequest.SemanticId, queryRequest.Diff,
                            queryRequest.Contains, queryRequest.Equal, queryRequest.Lower, queryRequest.Upper, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.QueryResult = qresult;
                        break;
                    case DbRequestOp.QueryCountSMEs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        count = query.CountSMEs(securityConfig, securityCondition, db, queryRequest.SmSemanticId, queryRequest.Identifier, queryRequest.SemanticId, queryRequest.Diff,
                            queryRequest.Contains, queryRequest.Equal, queryRequest.Lower, queryRequest.Upper, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);
                        result.Count = count;
                        break;
                    case DbRequestOp.QueryGetSMs:
                        queryRequest = dbRequest.Context.Params.QueryRequest;
                        query = new Query(_grammar);
                        var queryResult = query.GetSubmodelList(securityConfig.NoSecurity, db, securityCondition, queryRequest.PageFrom, queryRequest.PageSize, queryRequest.Expression);

                        if (queryResult.Submodels != null)
                        {
                            result.Submodels = queryResult.Submodels;
                        }
                        else
                        {
                            result.Ids = queryResult.Ids;
                        }
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
                        var aasIds = dbRequest.Context.Params.AasIds;
                        var submodelIds = dbRequest.Context.Params.SubmodelIds;
                        var includeCD = dbRequest.Context.Params.IncludeCD;

                        var environment = CrudOperator.GetEnvironment(db, securityCondition, -1, aasIds, submodelIds, includeCD: includeCD);

                        if (!dbRequest.Context.Params.CreateAASXPackage)
                        {
                            result.Environment = environment;
                        }
                        else
                        {
                            var requestedPackage = new AdminShellPackageEnv(environment);

                            string tempFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");

                            using (new FileStream(tempFileName, FileMode.CreateNew))
                            { }

                            requestedPackage.SetTempFn(tempFileName);

                            //Create Temp file
                            string copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
                            AddPackageFilesToAdd(securityCondition, db, environment, requestedPackage);

                            requestedPackage.SaveAs(copyFileName, true, true);

                            content = System.IO.File.ReadAllBytes(copyFileName);
                            fileSize = content.Length;

                            //Delete Temp file
                            System.IO.File.Delete(copyFileName);
                            System.IO.File.Delete(tempFileName);

                            result.FileRequestResult = new DbFileRequestResult()
                            {
                                Content = content,
                                File = "new_serialization.aasx",
                                FileSize = fileSize
                            };
                        }
                        break;
                    case DbRequestOp.ReadAASXByPackageId:
                        var packageFileName = ReadAASXByPackageId(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.PackageIdentifier,
                            out content, out fileSize);

                        result.FileRequestResult = new DbFileRequestResult()
                        {
                            Content = content,
                            File = packageFileName,
                            FileSize = fileSize
                        };
                        break;
                    case DbRequestOp.ReadPagedAASXPackageIds:
                        var packageDescriptions = CrudOperator.ReadPagedPackageDescriptions(db, dbRequest.Context.Params.PaginationParameters,
                            securityCondition, aasIdentifier);
                        if (packageDescriptions == null)
                        {
                            throw new NotFoundException($"Package descriptions with  id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
                        }

                        result.PackageDescriptions = packageDescriptions;
                        break;
                    case DbRequestOp.CreateAASXPackage:
                        var packageDescription = CreateAASXPackage(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.FileRequest.File,
                            dbRequest.Context.Params.FileRequest.Stream);
                        result.PackageDescriptions = new List<PackageDescription>
                        {
                            packageDescription
                        };
                        break;
                    case DbRequestOp.ReplaceAASXPackageById:
                        ReplaceAASXPackageById(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.PackageIdentifier,
                            dbRequest.Context.Params.FileRequest.File,
                            dbRequest.Context.Params.FileRequest.Stream);
                        break;
                    case DbRequestOp.DeleteAASXByPackageId:
                        DeleteAASXByPackageId(
                            db,
                            securityCondition,
                            dbRequest.Context.Params.PackageIdentifier);
                        break;
                    default:
                        dbRequest.TaskCompletionSource.SetException(new Exception("Unknown Operation"));
                        break;
                }
            }
        }
        return result;
    }

    private static void AddPackageFilesToAdd(Dictionary<string, string>? securityCondition, AasContext db, AasCore.Aas3_0.Environment environment, AdminShellPackageEnv requestedPackage)
    {
        foreach (var aasInEnv in environment.AssetAdministrationShells)
        {
            if (aasInEnv.AssetInformation != null
                && aasInEnv.AssetInformation.DefaultThumbnail != null
                    && aasInEnv.AssetInformation.DefaultThumbnail.Path != null)
            {
                byte[] bytesFromThumbnailsFile()
                {
                    using (var fileStream = new FileStream(FileService.GetThumbnailZipPath(aasInEnv.Id), FileMode.Open))
                    {
                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                        {
                            var archiveFile = archive.GetEntry(aasInEnv.AssetInformation.DefaultThumbnail.Path);
                            using var tempStream = archiveFile.Open();
                            var ms = new MemoryStream();
                            tempStream.CopyTo(ms);
                            ms.Position = 0;
                            return ms.ToByteArray();
                        }
                    }
                }
                requestedPackage.AddSupplementaryFileToStore(null,
                                            aasInEnv.AssetInformation.DefaultThumbnail.Path,
                                            true,
                                            bytesFromThumbnailsFile);

            }
        }

        foreach (var submodelInEnv in environment.Submodels)
        {
            var zipPath = FileService.GetFilesZipPath();

            if (IsPackageEnvPresent(db, securityCondition, null, null, submodelInEnv.Id, false, out EnvSet env, out _))
            {
                zipPath = FileService.GetFilesZipPath(env.Path);
            }

            submodelInEnv.RecurseOnSubmodelElements(null, (state, parents, sme) =>
            {
                if (sme is AasCore.Aas3_0.File file
                    && file.Value != null)
                {
                    if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
                    {
                        byte[] bytesFromZipFile()
                        {
                            using (var fileStream = new FileStream(zipPath, FileMode.Open))
                            {
                                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                                {
                                    var archiveFile = archive.GetEntry(file.Value);
                                    using var tempStream = archiveFile.Open();
                                    var ms = new MemoryStream();
                                    tempStream.CopyTo(ms);
                                    ms.Position = 0;
                                    return ms.ToByteArray();
                                }
                            }
                        }
                        requestedPackage.AddSupplementaryFileToStore(null,
                                                    file.Value,
                                                    true,
                                                    bytesFromZipFile);
                    }
                }

                return true;
            });
        }
    }

    private void DeleteAASXByPackageId(AasContext db, Dictionary<string, string>? securityCondition, string packageIdentifier)
    {
        if (IsPackageEnvPresent(db, securityCondition, packageIdentifier, null, null, true, out EnvSet envDB, out AdminShellPackageEnv packageEnv))
        {
            //ToDo: Do we really want to delete the aasx file, too?
            if (System.IO.File.Exists(envDB.Path))
            {
                System.IO.File.Delete(envDB.Path);
            }

            var zipFile = Path.Combine(AasContext.DataPath, "files", Path.GetFileName(envDB.Path) + ".zip");

            if (System.IO.File.Exists(zipFile))
            {
                System.IO.File.Delete(zipFile);
            }

            var deleteEnvList = db.EnvSets.Where(e => e.Path == packageEnv.Filename);
            var deleteEnv = deleteEnvList.FirstOrDefault();
            var deleteAasList = db.AASSets.Where(a => a.EnvId == envDB.Id);
            var deleteSmList = db.SMSets.Where(s => s.EnvId == envDB.Id);
            var deleteCDList = db.EnvCDSets.Where(s => s.EnvId == envDB.Id);

            foreach (var s in deleteSmList)
            {
                if (s.Identifier != null)
                {
                    CrudOperator.DeleteSubmodel(db, s.Identifier);
                }
            }
            foreach (var a in deleteAasList)
            {
                if (a.Identifier != null)
                {
                    CrudOperator.DeleteAAS(db, a.Identifier);
                }
            }
            deleteCDList.ExecuteDeleteAsync().Wait();
            deleteEnvList.ExecuteDeleteAsync().Wait();
        }
        else
        {
            throw new NotFoundException($"Package with id {packageIdentifier} NOT found");
        }
    }

    private void ReplaceAASXPackageById(AasContext db, Dictionary<string, string>? securityCondition, string packageIdentifier, string file,
        MemoryStream fileContent)
    {
        if (IsPackageEnvPresent(db, securityCondition, packageIdentifier, null, null, true, out EnvSet envDB, out AdminShellPackageEnv packageEnv))
        {
            var newFileName = Path.Combine(AasContext.DataPath, file);

            var isFileNameEqual = false;

            if (newFileName == envDB.Path)
            {
                System.IO.File.Delete(envDB.Path);
                isFileNameEqual = true;
            }

            //Check if file already exists
            if (System.IO.File.Exists(newFileName))
            {
                throw new DuplicateException($"File already exists");
            }

            using (var fileStream = System.IO.File.Create(newFileName))
            {
                fileContent.Seek(0, SeekOrigin.Begin);
                fileContent.CopyTo(fileStream);
            }

            if (System.IO.File.Exists(envDB.Path)
                && !isFileNameEqual)
            {
                System.IO.File.Delete(envDB.Path);
            }

            var zipFile = Path.Combine(AasContext.DataPath, "files", Path.GetFileName(envDB.Path) + ".zip");

            if (System.IO.File.Exists(zipFile))
            {
                System.IO.File.Delete(zipFile);
            }

            var deleteEnvList = db.EnvSets.Where(e => e.Path == packageEnv.Filename);
            var deleteEnv = deleteEnvList.FirstOrDefault();
            var deleteAasList = db.AASSets.Where(a => a.EnvId == envDB.Id);
            var deleteSmList = db.SMSets.Where(s => s.EnvId == envDB.Id);
            var deleteCDList = db.EnvCDSets.Where(s => s.EnvId == envDB.Id);

            foreach (var s in deleteSmList)
            {
                if (s.Identifier != null)
                {
                    CrudOperator.DeleteSubmodel(db, s.Identifier);
                }
            }
            foreach (var a in deleteAasList)
            {
                if (a.Identifier != null)
                {
                    CrudOperator.DeleteAAS(db, a.Identifier);
                }
            }
            deleteCDList.ExecuteDeleteAsync().Wait();
            deleteEnvList.ExecuteDeleteAsync().Wait();

            using var newAasx = new AdminShellPackageEnv(newFileName, true);
            if (newAasx != null)
            {
                envDB.Path = newFileName;
                VisitorAASX.ImportAASIntoDB(newAasx, envDB);
                db.Add(envDB);
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    db.Dispose();
                }
            }
            else
            {
                throw new Exception($"Cannot load new package {file}.");
            }
        }
        else
        {
            CreateAASXPackage(db, securityCondition, file, fileContent);
        }
    }

    private PackageDescription CreateAASXPackage(AasContext db, Dictionary<string, string>? securityCondition, string fileName, Stream fileContent)
    {
        var newFileName = Path.Combine(AasContext.DataPath, fileName);
        //Check if file already exists
        if (System.IO.File.Exists(newFileName))
        {
            throw new DuplicateException($"File already exists");
        }

        using (var fileStream = System.IO.File.Create(newFileName))
        {
            fileContent.Seek(0, SeekOrigin.Begin);
            fileContent.CopyTo(fileStream);
        }

        // open again
        using var newAasx = new AdminShellPackageEnv(newFileName, true);
        if (newAasx != null)
        {
            VisitorAASX.ImportAASXIntoDB(newFileName, false);
        }
        else
        {
            throw new Exception($"Cannot load new package {fileName}.");
        }

        var aasIds = newAasx.AasEnv.AssetAdministrationShells.Select(a => a.Id).ToList();

        if (!aasIds.Any())
        {
            throw new Exception($"No aas in package found.");
        }

        var aas = db.AASSets.FirstOrDefault(a => a.Identifier == aasIds[0]);
        var envId = aas.EnvId;

        return new PackageDescription()
        {
            AasIds = aasIds,
            PackageId = envId.ToString()
        };
    }

    private string ReadAASXByPackageId(AasContext db, Dictionary<string, string>? securityCondition, string packageIdentifier, out byte[] content, out long fileSize)
    {
        content = null;
        fileSize = 0;

        AdminShellPackageEnv requestedPackage;
        string requestedFileName;

        bool isFromUnpackedAAS = false;

        if (IsPackageEnvPresent(db, securityCondition, packageIdentifier, null, null, true, out EnvSet env, out AdminShellPackageEnv packageEnv))
        {
            requestedPackage = packageEnv;
            requestedFileName = env.Path;
        }
        else if (IsAssetAdministrationShellPresent(db, packageIdentifier, true, out AASSet aasDB, out IAssetAdministrationShell aas))
        {
            requestedPackage = CrudOperator.GetPackageEnv(db, securityCondition, - 1, aas: aasDB);

            requestedFileName = Path.Combine(AasContext.DataPath, aas.IdShort + ".aasx");

            if (System.IO.File.Exists(requestedFileName))
            {
                System.IO.File.Delete(requestedFileName);
            }

            using (new FileStream(requestedFileName, FileMode.CreateNew))
            { }

            isFromUnpackedAAS = true;
        }
        else
        {
            throw new NotFoundException($"Package wit id {packageIdentifier} not found.");

        }

        requestedPackage.SetTempFn(requestedFileName);

        //Create Temp file
        string copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
        System.IO.File.Copy(requestedFileName, copyFileName, true);

        AddPackageFilesToAdd(securityCondition, db, requestedPackage.AasEnv, requestedPackage);

        requestedPackage.SaveAs(copyFileName, true, true);

        content = System.IO.File.ReadAllBytes(copyFileName);
        string fileName = Path.GetFileName(requestedFileName);
        fileSize = content.Length;

        //Delete Temp file
        System.IO.File.Delete(copyFileName);

        if (isFromUnpackedAAS)
        {
            System.IO.File.Delete(requestedFileName);
        }

        return fileName;
    }

    private string ReadFileByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize)
    {
        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _))
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

                    string fileName = null;

                    if (fileElement is AasCore.Aas3_0.File file)
                    {
                        var envFileName = string.Empty;

                        var packageEnvFound = IsPackageEnvPresent(db, securityCondition, null, aasIdentifier, submodelIdentifier, false, out EnvSet env, out _);

                        if (packageEnvFound)
                        {
                            envFileName = env.Path;
                        }

                        if (FileService.ReadFileInZip(scopedLogger, envFileName, file, out content, out fileSize, out fileName))
                        {
                            return fileName;
                        }
                        else
                        {
                            throw new Exception($"Could not read Submodel element {fileElement.IdShort}");
                        }
                    }
                    else
                    {
                        throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                    }

                }
            }
            else
            {
                throw new NotFoundException($"Submodel element with sm id {submodelIdentifier} and id short path not found {idShortPath}");
            }
        }
        else
        {
            throw new NotFoundException($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    public void ReplaceFileByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream)
    {
        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _))
        {
            var fileElement = CrudOperator.ReadSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, out _);

            var found = fileElement != null;
            if (found)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();

                    if (fileElement is AasCore.Aas3_0.File file)
                    {
                        var envFileName = string.Empty;

                        var packageEnvFound = IsPackageEnvPresent(db, securityCondition, null, aasIdentifier, submodelIdentifier, false, out EnvSet env, out _);
                        if (packageEnvFound)
                        {
                            envFileName = env.Path;
                        }

                        if (FileService.ReplaceFileInZip(scopedLogger, envFileName, ref file, fileName, contentType, stream))
                        {
                            CrudOperator.ReplaceSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, file);
                        }
                        else
                        {
                            throw new Exception($"File operation failed: Could not replace Submodel element {fileElement.IdShort}");
                        }
                    }
                    else
                    {
                        throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                    }
                }
            }
            else
            {
                throw new NotFoundException($"Submodel element with sm id {submodelIdentifier} and id short path not found {idShortPath}");
            }
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteFileByPath(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        if (IsSubmodelPresent(db, securityCondition, aasIdentifier, submodelIdentifier, false, out _, out _))
        {
            var fileElement = CrudOperator.ReadSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, out _);

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

                        var packageEnvFound = IsPackageEnvPresent(db, securityCondition, null, aasIdentifier, submodelIdentifier, false, out EnvSet envSet, out _);
                        if (packageEnvFound)
                        {
                            envFileName = envSet.Path;
                        }

                        if (FileService.DeleteFileInZip(scopedLogger, envFileName, ref file))
                        {
                            CrudOperator.ReplaceSubmodelElementByPath(db, securityCondition, aasIdentifier, submodelIdentifier, idShortPath, file);
                        }
                        else
                        {
                            throw new Exception($"File operation failed: Could not delete Submodel element {fileElement.IdShort}");
                        }
                    }
                    else
                    {
                        throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                    }
                }
            }
            else
            {
                throw new NotFoundException($"Submodel element with sm id {submodelIdentifier} and id short path not found {idShortPath}");
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
            if (aas.AssetInformation != null)
            {
                if (FileService.ReadFromThumbnail(aas.AssetInformation, aasIdentifier, out byteArray, out fileSize))
                {
                    thumbnail = aas.AssetInformation.DefaultThumbnail.Path;
                }
                else
                {
                    throw new Exception($"Could not read thumbnail from {aasIdentifier}");
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
            throw new NotFoundException($"AAS with id {aasIdentifier} not found");
        }

        return thumbnail;
    }


    public void ReplaceThumbnail(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string fileName, string contentType, MemoryStream stream)
    {
        if (IsAssetAdministrationShellPresent(db, aasIdentifier, true, out AASSet aasDB, out IAssetAdministrationShell aas))
        {
            var assetInformation = aas.AssetInformation;

            if (assetInformation != null)
            {
                if (FileService.ReplaceThumbnail(ref assetInformation, aasIdentifier, fileName, contentType, stream))
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
            throw new NotFoundException($"AAS with id {aasIdentifier} not found");
        }
    }

    public void DeleteThumbnail(AasContext db, string aasIdentifier)
    {
        if (IsAssetAdministrationShellPresent(db, aasIdentifier, true, out AASSet aasDB, out IAssetAdministrationShell aas))
        {
            var assetInformation = aas.AssetInformation;

            if (assetInformation != null)
            {
                if (FileService.DeleteThumbnail(ref assetInformation, aasIdentifier))
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

    public static bool IsPackageEnvPresent(AasContext db, Dictionary<string, string>? securityCondition, string packageId, string aasID, string smId,
        bool loadInMemory, out EnvSet? envSet, out AdminShellPackageEnv? packageEnv)
    {
        packageEnv = null;
        envSet = null;

        int envId = -1;

        if (!packageId.IsNullOrEmpty())
        {
            //ToDo: Remove, when package identifier is added
            var isParsed = int.TryParse(packageId, out envId);

            if (!isParsed)
            {
                return false;
            }

            envSet = db.EnvSets.FirstOrDefault(e => e.Id == envId);

            if (envSet == null)
            {
                return false;
            }
        }
        else
        {
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
                    envSet = db.EnvSets.FirstOrDefault(e => e.Id == envId);
                }
            }
            else if (!aasID.IsNullOrEmpty())
            {
                var aasDBList = db.AASSets.Where(aas => aas.Identifier == aasID).ToList();

                if (aasDBList.Count != 1)
                {
                    return false;
                }
                if (aasDBList[0].EnvId.HasValue)
                {
                    envId = aasDBList[0].EnvId.Value;
                    envSet = db.EnvSets.FirstOrDefault(e => e.Id == envId);
                }
            }
        }

        if (envId == -1)
        {
            return false;
        }

        if (loadInMemory)
        {
            packageEnv = CrudOperator.GetPackageEnv(db, securityCondition, envId);
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
                }
                return true;
            }
        }
        return false;
    }

    private bool IsSubmodelPresent(AasContext db, Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier,
        bool loadIntoMemory, out SMSet smDb, out ISubmodel output)
    {
        output = null;
        smDb = null;

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
            if (output != null)
            {
                smDb = smDB[0];
                return true;
            }
            else
            {
                return false;
            }
        }

        smDb = smDB[0];
        return true;

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

    //ToDo: Move into security? Currently this is also in SubmodelRepositoryAPIApiController (for events)
    private bool InitSecurity(ISecurityConfig? securityConfig, out Dictionary<string, string>? securityCondition,
        out List<AccessPermissionRule> accessRules, string httpRoute = "", bool ignoreNullConfig = false)
    {
        accessRules = null;
        securityCondition = null;


        if (securityConfig == null)
        {
            return ignoreNullConfig;
        }


        if (securityConfig.NoSecurity)
        {
            return true;
        }

        var authResult = false;
        var accessRole = "isNotAuthenticated";
        List<Claim> tokenClaims = [];
        AasSecurity.Models.AccessRights neededRights = AasSecurity.Models.AccessRights.READ;
        if (securityConfig?.Principal != null)
        {
            // Get claims
            accessRole = securityConfig.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();
            httpRoute = securityConfig.Principal.FindFirst("Route")?.Value;
            tokenClaims = securityConfig.Principal.Claims.Where(c => c.Type.StartsWith("token:")).ToList();

            switch (securityConfig.NeededRightsClaim)
            {
                case NeededRights.TakeFromPrincipal:
                    var neededRightsClaim = securityConfig.Principal.FindFirst("NeededRights")?.Value;
                    Enum.TryParse(neededRightsClaim, out neededRights);

                    break;
                case NeededRights.Create:
                    neededRights = AasSecurity.Models.AccessRights.CREATE;
                    break;
                case NeededRights.Read:
                    neededRights = AasSecurity.Models.AccessRights.READ;
                    break;
                case NeededRights.Update:
                    neededRights = AasSecurity.Models.AccessRights.UPDATE;
                    break;
                case NeededRights.Delete:
                    neededRights = AasSecurity.Models.AccessRights.DELETE;
                    break;
                case NeededRights.Execute:
                    neededRights = AasSecurity.Models.AccessRights.EXECUTE;
                    break;
                default:
                    break;
            }
        }

        securityCondition = _contractSecurityRules.GetCondition(accessRole, neededRights.ToString(), tokenClaims: tokenClaims);
        accessRules = _contractSecurityRules.GetAccessRules(accessRole, neededRights.ToString(), tokenClaims: tokenClaims);

        if (accessRole != null && httpRoute != null)
        {
            authResult = _contractSecurityRules.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _);
        }

        return authResult;
    }

    public void InitDBFiles(bool reloadDBFiles, string dataPath)
    {
        if (AasContext.DataPath.IsNullOrEmpty())
        {
            AasContext.DataPath = dataPath;
        }

        FileService.InitFileSystem(reloadDBFiles);
    }

}
