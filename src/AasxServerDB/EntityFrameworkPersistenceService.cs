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
using Contracts.Pagination;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using AasxServerStandardBib.Logging;
using Contracts.Exceptions;
using Contracts.DbRequests;
using System.Threading.Tasks;

using System.Linq.Dynamic.Core;
using System.IO.Compression;
using AasxServerStandardBib.Exceptions;
using AasxServerDB.Entities;
using System.Collections;

public class EntityFrameworkPersistenceService : IPersistenceService
{
    private readonly IContractSecurityRules _contractSecurityRules;
    private readonly IEventService _eventService;
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkPersistenceService(IServiceProvider serviceProvider, IContractSecurityRules contractSecurityRules, IEventService eventService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _contractSecurityRules = contractSecurityRules ?? throw new ArgumentNullException(nameof(contractSecurityRules));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    public void InitDB(bool reloadDB, string dataPath)
    {
        AasContext.DataPath = dataPath;

        //Provoke OnConfiguring so that IsPostgres is set
        var db = new AasContext();

        bool isPostgres = AasContext.IsPostgres;

        // Get database
        Console.WriteLine($"Use {(isPostgres ? "POSTGRES" : "SQLITE")}");

        db = isPostgres ? new PostgreAasContext() : new SqliteAasContext();

        // Get path
        var connectionString = db.Database.GetConnectionString();
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
        var canConnect = db.Database.CanConnect();
        if (!canConnect)
        {
            Console.WriteLine($"Unable to connect to the database.");
        }

        // Delete database
        if (canConnect && reloadDB)
        {
            Console.WriteLine("Clear database.");
            db.Database.EnsureDeleted();
        }

        // Create the database if it does not exist
        // Applies any pending migrations
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            throw new Exception($"Migration failed: {ex.Message}\nTry deleting the database.");
        }
    }

    public void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles)
    {
        VisitorAASX.ImportAASXIntoDB(filePath, createFilesOnly, withDbFiles);
    }

    public List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list)
    {
        return Converter.GetFilteredPackages(filterPath, list);
    }

    public async Task<DbRequestResult> DoDbOperation(DbRequest dbRequest)
    {
        var result = new DbRequestResult();

        //ToDo (Debug) Log all Requests

        switch (dbRequest.Operation)
        {
            case DbRequestOp.ReadAllAssetAdministrationShells:
                var assetAdministrationShells = ReadPagedAssetAdministrationShells(dbRequest.Context.Params.PaginationParameters,
                        dbRequest.Context.SecurityConfig,
                        dbRequest.Context.Params.AssetIds,
                        dbRequest.Context.Params.IdShort);
                result.AssetAdministrationShells = assetAdministrationShells;
                break;
            case DbRequestOp.ReadSubmodelById:
                var submodel = ReadSubmodelById(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier);

                result.Submodels = new List<ISubmodel>
                {
                    submodel
                };
                break;
            case DbRequestOp.ReadPagedSubmodelElements:
                var submodelElements = ReadPagedSubmodelElements(
                    dbRequest.Context.Params.PaginationParameters,
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier);

                result.SubmodelElements = submodelElements;
                break;
            case DbRequestOp.ReadSubmodelElementByPath:
                var submodelElement = ReadSubmodelElementByPath(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShortElements);

                result.SubmodelElements = new List<ISubmodelElement>
                {
                    submodelElement
                };
                break;
            case DbRequestOp.ReadPagedSubmodels:
                var submodels = ReadPagedSubmodels(
                    dbRequest.Context.Params.PaginationParameters,
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.Reference,
                    dbRequest.Context.Params.IdShort);
                result.Submodels = submodels;
                break;
            case DbRequestOp.ReadAssetAdministrationShellById:
                var aas = ReadAssetAdministrationShellById(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                result.AssetAdministrationShells = new List<IAssetAdministrationShell>
                {
                    aas
                };
                break;
            case DbRequestOp.ReadFileByPath:
                byte[] content;
                long fileSize;
                var file = ReadFileByPath(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShortElements, out content, out fileSize);
                result.FileRequestResult = new DbFileRequestResult()
                {
                    Content = content,
                    File = file,
                    FileSize = fileSize
                };
                break;
            case DbRequestOp.ReadAssetInformation:
                var assetInformation = ReadAssetInformation(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                result.AssetInformation = assetInformation;
                break;
            case DbRequestOp.ReadThumbnail:
                var thumbnail = ReadThumbnail(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    out content, out fileSize);

                result.FileRequestResult = new DbFileRequestResult()
                {
                    Content = content,
                    File = thumbnail,
                    FileSize = fileSize
                };
                break;
            case DbRequestOp.ReadPackageEnv:
                var envFile = "";
                var packageEnv = ReadPackageEnv(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    out envFile);
                result.PackageEnv = new DbRequestPackageEnvResult()
                {
                    PackageEnv = packageEnv,
                    EnvFileName = envFile
                };
                break;
            case DbRequestOp.ReadEventMessages:
                var eventPayload = ReadEventMessages(dbRequest.Context.Params.EventRequest);
                result.EventPayload = eventPayload;

                break;
            case DbRequestOp.CreateSubmodel:
                var createdSubmodel = CreateSubmodel(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.SubmodelBody,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);

                result.Submodels = new List<ISubmodel>
                {
                    createdSubmodel
                };
                break;
            case DbRequestOp.CreateAssetAdministrationShell:
                var createdAas = CreateAssetAdministrationShell(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AasBody);
                result.AssetAdministrationShells = new List<IAssetAdministrationShell>
                {
                    createdAas
                };
                break;
            case DbRequestOp.CreateSubmodelElement:
                var createdsubmodelElement = CreateSubmodelElement(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.SubmodelElementBody,
                    dbRequest.Context.Params.IdShort,
                    dbRequest.Context.Params.First);

                result.SubmodelElements = new List<ISubmodelElement>
                {
                    createdsubmodelElement
                };
                break;
            case DbRequestOp.CreateSubmodelReference:
                var createdSubmodelReference = CreateSubmodelReferenceInAAS(dbRequest.Context.Params.Reference,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);

                result.References = new List<IReference>
                {
                    createdSubmodelReference
                };
                break;
            case DbRequestOp.UpdateSubmodelById:
                UpdateSubmodelById(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.SubmodelBody);
                break;
            case DbRequestOp.UpdateSubmodelElementByPath:
                UpdateSubmodelElementByPath(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShort,
                    dbRequest.Context.Params.SubmodelElementBody);
                break;
            case DbRequestOp.UpdateEventMessages:
                UpdateEventMessages(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.EventRequest);
                break;
            case DbRequestOp.ReplaceAssetInformation:
                ReplaceAssetInformation(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.AssetInformation);
                break;
            case DbRequestOp.ReplaceFileByPath:
                ReplaceFileByPath(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShortElements,
                    dbRequest.Context.Params.FileRequest.File,
                    dbRequest.Context.Params.FileRequest.ContentType,
                    dbRequest.Context.Params.FileRequest.Stream);
                break;
            case DbRequestOp.ReplaceThumbnail:
                ReplaceThumbnail(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.FileRequest.File,
                    dbRequest.Context.Params.FileRequest.ContentType,
                    dbRequest.Context.Params.FileRequest.Stream);
                break;
            case DbRequestOp.ReplaceAssetAdministrationShellById:
                ReplaceAssetAdministrationShellById(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.AasBody);
                break;
            case DbRequestOp.ReplaceSubmodelById:
                ReplaceSubmodelById(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.SubmodelBody);
                break;
            case DbRequestOp.ReplaceSubmodelElementByPath:
                ReplaceSubmodelElementByPath(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShort,
                    dbRequest.Context.Params.SubmodelElementBody);
                break;
            case DbRequestOp.DeleteAssetAdministrationShellById:
                DeleteAssetAdministrationShellById(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                break;
            case DbRequestOp.DeleteFileByPath:
                DeleteFileByPath(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShortElements);
                break;
            case DbRequestOp.DeleteSubmodelById:
                DeleteSubmodelById(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier);
                break;
            case DbRequestOp.DeleteSubmodelElementByPath:
                DeleteSubmodelElementByPath(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShort);
                break;
            case DbRequestOp.DeleteSubmodelReferenceById:
                DeleteSubmodelReferenceById(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier);
                break;
            case DbRequestOp.DeleteThumbnail:
                DeleteThumbnail(dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);
                break;
            case DbRequestOp.QuerySearchSMs:
                var queryRequest = dbRequest.Context.Params.QueryRequest;
                var grammar = this._contractSecurityRules.GetGrammarJSON();
                var query = new Query(grammar);
                var qresult = query.SearchSMs(queryRequest.WithTotalCount, queryRequest.WithLastId, queryRequest.SemanticId, queryRequest.Identifier, queryRequest.Diff, queryRequest.Expression);
                result.QueryResult = qresult;
                break;
            case DbRequestOp.QueryCountSMs:
                queryRequest = dbRequest.Context.Params.QueryRequest;
                grammar = this._contractSecurityRules.GetGrammarJSON();
                query = new Query(grammar);
                var count = query.CountSMs(queryRequest.SemanticId, queryRequest.Identifier, queryRequest.Diff, queryRequest.Expression);
                result.Count = count;
                break;
            case DbRequestOp.QuerySearchSMEs:
                queryRequest = dbRequest.Context.Params.QueryRequest;
                grammar = this._contractSecurityRules.GetGrammarJSON();
                query = new Query(grammar);
                qresult = query.SearchSMEs(queryRequest.Requested, queryRequest.WithTotalCount, queryRequest.WithLastId, queryRequest.SmSemanticId, queryRequest.Identifier, queryRequest.SemanticId, queryRequest.Diff,
                    queryRequest.Contains, queryRequest.Equal, queryRequest.Lower, queryRequest.Upper, queryRequest.Expression);
                result.QueryResult = qresult;
                break;
            case DbRequestOp.QueryCountSMEs:
                queryRequest = dbRequest.Context.Params.QueryRequest;
                grammar = this._contractSecurityRules.GetGrammarJSON();
                query = new Query(grammar);
                count = query.CountSMEs(queryRequest.SmSemanticId, queryRequest.Identifier, queryRequest.SemanticId, queryRequest.Diff,
                    queryRequest.Contains, queryRequest.Equal, queryRequest.Lower, queryRequest.Upper, queryRequest.Expression);
                result.Count = count;
                break;
            default:
                dbRequest.TaskCompletionSource.SetException(new Exception("Unknown Operation"));
                break;
        }

        return result;
    }

    private List<IAssetAdministrationShell> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort)
    {
        Dictionary<string, string>? securityCondition = null;
        InitSecurity(securityConfig, out securityCondition);

        var output = Converter.GetPagedAssetAdministrationShells(paginationParameters, assetIds, idShort);

        return output;

        //Apply filters
        // GlobalAssetId is done during DB access
        // SpecificAssetIds is serialized as JSON and can not be handled by DB currently
        if (output.Count != 0)
        {
            if (!assetIds.IsNullOrEmpty())
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogDebug($"Filtering AASs with requested assetIds.");
                }

                var aasList = new List<IAssetAdministrationShell>();
                foreach (var assetId in assetIds)
                {
                    var result = new List<IAssetAdministrationShell>();
                    if (!string.IsNullOrEmpty(assetId.Name))
                    {
                        if (!assetId.Name.Equals("globalAssetId", StringComparison.OrdinalIgnoreCase))
                        {
                            result = output.Where(a => a.AssetInformation.SpecificAssetIds!.ContainsSpecificAssetId(assetId)).ToList();
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("The attribute Name cannot be null in the requested assetId.");
                    }

                    if (result.Count != 0)
                    {
                        aasList.AddRange(result);
                    }
                }

                output = aasList;
            }
        }
        return output;
    }

    private ISubmodel ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        bool found = IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, true, out ISubmodel output);

        if (found)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                scopedLogger.LogDebug($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} found.");
            }
            return output;
        }
        else
        {
            throw new NotFoundException($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    private List<ISubmodelElement> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);
        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var output = Converter.GetPagedSubmodelElements(paginationParameters, securityCondition, aasIdentifier, submodelIdentifier);
        if (output == null)
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
        return output;
    }

    private ISubmodelElement ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);
        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var output = Converter.GetSubmodelElementByPath(securityCondition, aasIdentifier, submodelIdentifier, idShortPathElements, out SMESet smE);
        if (output == null)
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
        return output;
    }

    private List<ISubmodel> ReadPagedSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, IReference reqSemanticId, string idShort)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        var output = Converter.GetSubmodels(paginationParameters, securityCondition, reqSemanticId, idShort);

        if (reqSemanticId != null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                scopedLogger.LogDebug($"Filtering Submodels with requested Semnatic Id.");
                output = output.Where(s => s.SemanticId != null && s.SemanticId.Matches(reqSemanticId)).ToList();
                if (output.IsNullOrEmpty())
                {
                    scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogInformation($"No Submodels with requested semanticId found.");
                }
            }
        }

        return output;
    }

    private IAssetAdministrationShell ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: AAS with id {aasIdentifier}");
        }

        bool found = IsAssetAdministrationShellPresent(aasIdentifier, true, out IAssetAdministrationShell output);
        if (found)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                scopedLogger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
            }

            return output;
        }
        else
        {
            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    private string ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements, out byte[] content, out long fileSize)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        content = null;
        fileSize = 0;

        var fileElement = Converter.GetSubmodelElementByPath(securityCondition, aasIdentifier, submodelIdentifier, idShortPathElements, out SMESet smE);

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
                    fileName = file.Value;

                    if (string.IsNullOrEmpty(fileName))
                    {
                        scopedLogger.LogError($"File name is empty. Cannot fetch the file.");
                        throw new UnprocessableEntityException($"File value Null!!");
                    }

                    //check if it is external location
                    if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
                    {
                        scopedLogger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
                        throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
                    }
                    //Check if a directory
                    else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
                    {
                        scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");
                        var envFileName = string.Empty;

                        var packageEnvFound = Converter.IsPackageEnvPresent(aasIdentifier, submodelIdentifier, false, out envFileName, out AdminShellPackageEnv packageEnv);

                        if (packageEnvFound != null)
                        {
                            using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
                            {
                                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                                {
                                    var archiveFile = archive.GetEntry(file.Value);
                                    var tempStream = archiveFile.Open();
                                    var ms = new MemoryStream();
                                    tempStream.CopyTo(ms);
                                    ms.Position = 0;
                                    content = ms.ToByteArray();
                                }
                            }

                            // var stream = packageEnv.GetLocalStreamFromPackage(fileName);
                            fileSize = content.Length;
                        }
                        else
                        {
                            throw new NotFoundException($"Package for aas id {aasIdentifier} and submodel id {submodelIdentifier} not found");
                        }
                    }
                    // incorrect value
                    else
                    {
                        scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
                        throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
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

    private IAssetInformation ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier)
    {
        var aas = ReadAssetAdministrationShellById(securityConfig, aasIdentifier);
        return aas.AssetInformation;
    }

    private string ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier, out byte[] byteArray, out long fileSize)
    {
        string fileName = null;
        byteArray = null;
        fileSize = 0;
        var aas = ReadAssetAdministrationShellById(securityConfig, aasIdentifier);
        if (aas != null)
        {
            if (aas.AssetInformation != null)
            {
                if (aas.AssetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(aas.AssetInformation.DefaultThumbnail.Path))
                {
                    fileName = aas.AssetInformation.DefaultThumbnail.Path;

                    var envFileName = string.Empty;
                    var found = Converter.IsPackageEnvPresent(aasIdentifier, null, false, out envFileName, out AdminShellPackageEnv packageEnv);

                    if (found)
                    {
                        string fcopy = Path.GetFileName(envFileName) + "__thumbnail";
                        fcopy = fcopy.Replace("/", "_");
                        fcopy = fcopy.Replace(".", "_");
                        var result = System.IO.File.Open(AasContext.DataPath + "/files/" + fcopy + ".dat", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        // Post-condition
                        if (!(result == null || result.CanRead))
                        {
                            // throw new InvalidOperationException("Unexpected unreadable result stream");
                            return null;
                        }

                        byteArray = result.ToByteArray();
                        fileSize = byteArray.Length;
                        result.Close();
                    }
                    else
                    {
                        throw new NotFoundException($"Package for aas id {aasIdentifier} not found");
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                        scopedLogger.LogDebug($"Updated the thumbnail in AAS with Id {aasIdentifier}");
                    }
                }
                else
                {
                    throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                }
            }
            else
            {
                throw new NotFoundException($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
            }
        }

        return fileName;
    }

    private AdminShellPackageEnv ReadPackageEnv(string aasID, string smID, out string envFileName)
    {
        Converter.IsPackageEnvPresent(aasID, smID, true, out envFileName, out AdminShellPackageEnv packageEnv);
        return packageEnv;
    }

    private ISubmodel CreateSubmodel(ISecurityConfig securityConfig, ISubmodel newSubmodel, string aasIdentifier)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: AAS with id {aasIdentifier}");
        }
        bool found = IsSubmodelPresent(securityCondition, aasIdentifier, newSubmodel.Id, false, out _);

        if (found)
        {
            throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
        }

        var submodel = Converter.CreateSubmodel(newSubmodel, aasIdentifier);

        return submodel;
    }

    public IAssetAdministrationShell CreateAssetAdministrationShell(ISecurityConfig securityConfig, IAssetAdministrationShell body)
    {
        //string securityConditionSM, securityConditionSME;
        //bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        var found = IsAssetAdministrationShellPresent(body.Id, false, out _);
        if (found)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                scopedLogger.LogDebug($"Cannot create requested AAS !!");
            }
            throw new DuplicateException($"AssetAdministrationShell with id {body.Id} already exists.");
        }

        var output = Converter.CreateAas(body);

        return output;
    }

    public ISubmodelElement CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement newSubmodelElement, string idShortPath, bool first = true)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var smFound = IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, false, out ISubmodel output);
        if (smFound)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
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
                return Converter.CreateSubmodelElement(aasIdentifier, submodelIdentifier, newSubmodelElement, idShortPath, first);
            }
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier)
    {
        using (AasContext db = new AasContext())
        {
            var aasDB = db.AASSets
                .Include(aas => aas.SMRefSets)
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);
            var identifier = body.GetAsIdentifier();
            if (aasDB != null && identifier != null)
            {
                aasDB.SMRefSets.Add(new SMRefSet { Identifier = identifier });
            }
        }

        // TODO: read reference from DB
        return body;
    }

    public void DeleteAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier)
    {
        if (IsAssetAdministrationShellPresent(aasIdentifier, false, out _))
        {
            Edit.DeleteAAS(aasIdentifier);
        }
    }

    public void DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, false, out _))
        {
            SMESet sME = null;
            var fileElement = Converter.GetSubmodelElementByPath(securityCondition, aasIdentifier, submodelIdentifier, idShortPathElements, out sME);

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
                        if (!string.IsNullOrEmpty(file.Value))
                        {
                            //check if it is external location
                            if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
                            {
                                scopedLogger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
                                throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
                            }
                            //Check if a directory
                            else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
                            {
                                scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

                                var envFileName = string.Empty;

                                var packageEnvFound = Converter.IsPackageEnvPresent(aasIdentifier, submodelIdentifier, false, out envFileName, out AdminShellPackageEnv packageEnv);

                                if (packageEnvFound)
                                {
                                    using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
                                    {
                                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                                        {
                                            var entry = archive.GetEntry(file.Value);

                                            entry?.Delete();
                                        }
                                    }

                                    Edit.DeleteSubmodelElement(sME);
                                }

                                file.Value = string.Empty;

                                //ToDo: Do notification
                                //Program.signalNewData(1);
                                //scopedLogger.LogDebug($"Deleted the file at {idShortPath} from submodel with Id {submodelIdentifier}");
                            }
                            // incorrect value
                            else
                            {
                                scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
                                throw new OperationNotSupported($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
                            }
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

    public void DeleteSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, false, out _))
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                scopedLogger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            }

            Edit.DeleteSubmodel(submodelIdentifier);
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        if (IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, false, out _))
        {
            using (var db = new AasContext())
            {
                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

                if (!aasIdentifier.IsNullOrEmpty())
                {
                    var aasDB = db.AASSets
                        .Where(aas => aas.Identifier == aasIdentifier).ToList();
                    if (aasDB == null || aasDB.Count != 1)
                    {
                        return;
                    }
                    var aasDBId = aasDB[0].Id;
                    smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDBId);
                }

                if (securityCondition != null)
                {
                    smDBQuery = smDBQuery.Where(securityCondition["sm."]);
                }
                var smDB = smDBQuery.ToList();
                if (smDB == null || smDB.Count != 1)
                {
                    return;
                }
                var smDBId = smDB[0].Id;

                var idShortPathElements = idShortPath.Split(".");
                if (idShortPathElements.Length == 0)
                {
                    return;
                }
                var idShort = idShortPathElements[0];
                var smeParent = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == null && sme.IdShort == idShort).ToList();
                if (smeParent == null || smeParent.Count != 1)
                {
                    return;
                }
                var parentId = smeParent[0].Id;
                var smeFound = smeParent;

                for (int i = 1; i < idShortPathElements.Length; i++)
                {
                    idShort = idShortPathElements[i];
                    //ToDo SubmodelElementList with index (type: int) must be implemented
                    var smeFoundDB = db.SMESets.Where(sme => sme.SMId == smDBId && sme.ParentSMEId == parentId && sme.IdShort == idShort);
                    smeFound = smeFoundDB.ToList();
                    if (smeFound == null || smeFound.Count != 1)
                    {
                        return;
                    }
                    parentId = smeFound[0].Id;
                }

                var smeFoundTreeIds = Converter.GetTree(db, smDB[0], smeFound)?.Select(s => s.Id);
                if (smeFoundTreeIds?.Count() > 0)
                {
                    db.SMESets.Where(sme => smeFoundTreeIds.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                    db.SaveChanges();
                }
            }

            /*
            using (var db = new AasContext())
            {
                //ToDo: Also aasIdentifier
                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);
                var smDB = smDBQuery.FirstOrDefault();
                var smDBId = smDB.Id;

                var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
                Converter.CreateIdShortPath(db, smeSmList);

                var smeDBDelete = db.SMESets.Where(s => s.SMId == smDBId && (s.IdShortPath + ".").StartsWith(idShortPath + "."));

                if (smeDBDelete.Count() > 0)
                {
                    smeDBDelete.ExecuteDeleteAsync().Wait();
                    db.SaveChanges();
                }
                //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                //ToDo submodel service solution, do we really need a different solution for replace and update?
                //_submodelService.UpdateSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
            }
            */

            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //_submodelService.DeleteSubmodelElementByPath(submodelIdentifier, idShortPath);
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelReferenceById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        using (AasContext db = new AasContext())
        {
            var aasDB = db.AASSets
                .Include(aas => aas.SMRefSets)
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);
            if (aasDB != null)
            {
                var smRefDB = aasDB.SMRefSets.FirstOrDefault(s => s.Identifier == submodelIdentifier);
                if (smRefDB != null)
                {
                    aasDB.SMRefSets.Remove(smRefDB);
                    db.SaveChanges();
                }
            }
        }
    }

    public void DeleteThumbnail(ISecurityConfig securityConfig, string aasIdentifier)
    {
        var aas = this.ReadAssetAdministrationShellById(null, aasIdentifier);
        if (aas != null)
        {
            if (aas.AssetInformation != null)
            {
                if (aas.AssetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(aas.AssetInformation.DefaultThumbnail.Path))
                {
                    var fileName = aas.AssetInformation.DefaultThumbnail.Path;

                    var envFileName = string.Empty;
                    var found = Converter.IsPackageEnvPresent(aasIdentifier, null, false, out envFileName, out AdminShellPackageEnv packageEnv);

                    if (found)
                    {
                        string fcopy = Path.GetFileName(envFileName) + "__thumbnail";
                        fcopy = fcopy.Replace("/", "_");
                        fcopy = fcopy.Replace(".", "_");
                        System.IO.File.Delete(AasContext.DataPath + "/files/" + fcopy + ".dat");
                    }
                    else
                    {
                        throw new NotFoundException($"Package for aas id {aasIdentifier} not found");
                    }
                }
                else
                {
                    throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                }
            }
        }
    }

    public void ReplaceSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel newSubmodel)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);

        var found = IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, false, out _);
        if (found)
        {
            using (var db = new AasContext())
            {
                var visitor = new VisitorAASX(db);
                visitor.update = true;
                visitor.currentDataTime = DateTime.UtcNow;
                visitor.VisitSubmodel(newSubmodel);
                // Delete no more exisiting SMEs in SM
                var smDB = visitor._smDB;
                db.SMESets.Where(sme => sme.SMId == smDB.Id && !visitor.keepSme.Contains(sme.Id)).ExecuteDeleteAsync();
                db.SaveChanges();
            }
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
    }

    public void ReplaceSubmodelElementByPath(ISecurityConfig securityConfig,string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body)
    {
        Dictionary<string, string>? securityCondition = null;
        bool isAllowed = InitSecurity(securityConfig, out securityCondition);
        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var found = IsSubmodelPresent(securityCondition, aasIdentifier, submodelIdentifier, false, out _);
        if (found)
        {
            using (var db = new AasContext())
            {
                var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);

                if (!aasIdentifier.IsNullOrEmpty())
                {
                    var aasDB = db.AASSets
                        .Where(aas => aas.Identifier == aasIdentifier).ToList();
                    if (aasDB == null || aasDB.Count != 1)
                    {
                        return;
                    }
                    var aasDBId = aasDB[0].Id;
                    smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDBId);
                }

                if (securityCondition != null)
                {
                    smDBQuery = smDBQuery.Where(securityCondition["sm."]);
                }

                var smDB = smDBQuery.FirstOrDefault();
                var visitor = new VisitorAASX(db);
                visitor._smDB = smDB;
                visitor.currentDataTime = DateTime.UtcNow;
                var smDBId = smDB.Id;
                var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
                Converter.CreateIdShortPath(db, smeSmList);
                var smeSmMerged = Converter.GetSmeMerged(db, smeSmList);
                visitor.smSmeMerged = smeSmMerged;
                visitor.idShortPath = idShortPath;
                visitor.update = true;
                var receiveSmeDB = visitor.VisitSMESet(body);
                receiveSmeDB.SMId = smDBId;
                Converter.setTimeStampTree(db, smDB, receiveSmeDB, receiveSmeDB.TimeStamp);
                try
                {
                    // db.SMESets.Add(receiveSmeDB);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                }
                var smeDB = smeSmMerged.Where(sme =>
                        !visitor.keepSme.Contains(sme.smeSet.Id) &&
                        visitor.deleteSme.Contains(sme.smeSet.Id)
                    ).ToList();
                var smeDelete = smeDB.Select(sme => sme.smeSet.Id).Distinct().ToList();

                if (smeDelete.Count > 0)
                {
                    db.SMESets.Where(sme => smeDelete.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                    db.SaveChanges();
                }
                //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                //ToDo submodel service solution, do we really need a different solution for replace and update?
                //_submodelService.UpdateSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
            }
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void ReplaceAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier, IAssetAdministrationShell newAas)
    {
        var found = IsAssetAdministrationShellPresent(aasIdentifier, false, out _);

        if (found)
        {
            using (var db = new AasContext())
            {
                var aasDB = db.AASSets
                    .Include(aas => aas.SMRefSets)
                    .FirstOrDefault(aas => aas.Identifier == aasIdentifier);

                if (aasDB != null)
                {
                    db.SMRefSets.RemoveRange(aasDB.SMRefSets);

                    Converter.SetAas(aasDB, newAas);
                    db.SaveChanges();

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                        scopedLogger.LogDebug($"AssetInformation from AAS with id {aasIdentifier} updated successfully.");
                    }
                }
            }
            //    Program.signalNewData(0);
        }
        else
        {
            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }


    public void ReplaceAssetInformation(ISecurityConfig securityConfig, string aasIdentifier, IAssetInformation newAssetInformation)
    {
        var found = IsAssetAdministrationShellPresent(aasIdentifier, false, out _);

        if (found)
        {
            var cuurentDataTime = DateTime.UtcNow;

            using (var db = new AasContext())
            {
                var aasDB = db.AASSets
                .FirstOrDefault(aas => aas.Identifier == aasIdentifier);

                aasDB.AssetKind = Serializer.SerializeElement(newAssetInformation.AssetKind);
                aasDB.SpecificAssetIds = Serializer.SerializeList(newAssetInformation.SpecificAssetIds);
                aasDB.GlobalAssetId = newAssetInformation.GlobalAssetId;
                aasDB.AssetType = newAssetInformation.AssetType;
                aasDB.DefaultThumbnailPath = newAssetInformation.DefaultThumbnail?.Path;
                aasDB.DefaultThumbnailContentType = newAssetInformation.DefaultThumbnail?.ContentType;
                aasDB.TimeStamp = cuurentDataTime;
                aasDB.TimeStampTree = cuurentDataTime;

                db.SaveChanges();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
                    scopedLogger.LogDebug($"AssetInformation from AAS with id {aasIdentifier} updated successfully.");
                }
            }

            //    Program.signalNewData(0);
        }
        else
        {
            throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
        }

    }

    public void ReplaceFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPath, string fileName, string contentType, MemoryStream stream)
    {
        //string securityConditionSM, securityConditionSME;
        //InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        //if (IsSubmodelPresent(securityConditionSM, aasIdentifier, submodelIdentifier, false, out _))
        //{
        //    SMESet sME = null;
        //    var fileElement = Converter.GetSubmodelElementByPath(securityConditionSM, securityConditionSME, aasIdentifier, submodelIdentifier, idShortPath, out sME);

        //    var found = fileElement != null;
        //    if (found)
        //    {
        //        using (var scope = _serviceProvider.CreateScope())
        //        {
        //            var scopedLogger = scope.ServiceProvider.GetRequiredService<IAppLogger<EntityFrameworkPersistenceService>>();
        //            scopedLogger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");


        //            if (fileElement is AasCore.Aas3_0.File file)
        //            {
        //                if (!string.IsNullOrEmpty(file.Value))
        //                {
        //                    //check if it is external location
        //                    if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
        //                    {
        //                        scopedLogger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
        //                        throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
        //                    }
        //                    //Check if a directory
        //                    else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
        //                    {
        //                        scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

        //                        var envFileName = string.Empty;

        //                        var packageEnvFound = Converter.IsPackageEnvPresent(aasIdentifier, submodelIdentifier, false, out envFileName, out AdminShellPackageEnv packageEnv);

        //                        if (packageEnvFound)
        //                        {
        //                            using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
        //                            {
        //                                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
        //                                {
        //                                    var entry = archive.GetEntry(file.Value);
        //                                    entry?.Delete();
        //                                    archive.CreateEntryFromFile(, file.Value);
        //                                }
        //                            }

        //                            Edit.DeleteSubmodelElement(sME);
        //                        }

        //                        file.Value = string.Empty;

        //                        //ToDo: Do notification
        //                        //Program.signalNewData(1);
        //                        //scopedLogger.LogDebug($"Deleted the file at {idShortPath} from submodel with Id {submodelIdentifier}");
        //                    }
        //                    // incorrect value
        //                    else
        //                    {
        //                        scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
        //                        throw new OperationNotSupported($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                //throw new OperationNotSupported($"Cannot delete the file. SubmodelElement {idShortPath} does not have a file attached.");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        //throw new OperationNotSupported($"SubmodelElement found at {idShortPath} is not of type File");
        //    }
        //}
        //else
        //{
        //    throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        //}

    }
    public void ReplaceThumbnail(ISecurityConfig securityConfig, string aasIdentifier, string fileName, string contentType, MemoryStream stream) => throw new NotImplementedException();

    public void UpdateSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel newSubmodel) => throw new NotImplementedException();

    public void UpdateSubmodelElementByPath(ISecurityConfig security, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body) => throw new NotImplementedException();

    private Contracts.Events.EventPayload ReadEventMessages(DbEventRequest dbEventRequest)
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

            eventPayload = _eventService.CollectPayload(changes, depth,
            eventData.StatusData, eventData.DataReference, data, eventData.ConditionSM, eventData.ConditionSME,
            diff, diffEntry, wp, limSm, offSm, limSme, offSme);
        }
        else // database
        {
            eventPayload = _eventService.CollectPayload(changes, 0,
            eventData.StatusData, eventData.DataReference, null, eventData.ConditionSM, eventData.ConditionSME,
            diff, diffEntry, wp, limSm, limSme, offSm, offSme);
        }

        if (diff == "status")
        {
            if (eventData.LastUpdate != null && eventData.LastUpdate.Value != null && eventData.LastUpdate.Value != "")
            {
                eventPayload.status.lastUpdate = eventData.LastUpdate.Value;
            }
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

    private void UpdateEventMessages(ISecurityConfig securityConfig, DbEventRequest eventRequest)
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
            ReplaceSubmodelById(securityConfig, null, eventRequest.Submodel.Id, eventRequest.Submodel);
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

    private bool IsAssetAdministrationShellPresent(string aasIdentifier, bool loadIntoMemory, out IAssetAdministrationShell output)
    {
        output = null;

        using (var db = new AasContext())
        {
            if (!aasIdentifier.IsNullOrEmpty())
            {
                var aasDB = db.AASSets
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
                        output = Converter.GetAssetAdministrationShell(aasDB);

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
        }
        return false;
    }

    private bool IsSubmodelPresent(Dictionary<string, string>? securityCondition, string aasIdentifier, string submodelIdentifier, bool loadIntoMemory, out ISubmodel output)
    {
        output = null;

        var result = false;

        // I asked Copilot to simplify
        using (var db = new AasContext())
        {
            var smDBQuery = db.SMSets.AsQueryable();

            if (!string.IsNullOrEmpty(submodelIdentifier))
            {
                smDBQuery = smDBQuery.Where(sm => sm.Identifier == submodelIdentifier);
            }

            /*
            if (!string.IsNullOrEmpty(aasIdentifier))
            {
                var aasDB = db.AASSets.FirstOrDefault(aas => aas.Identifier == aasIdentifier);
                if (aasDB == null)
                {
                    return false;
                }
                smDBQuery = smDBQuery.Where(sm => sm.AASId == aasDB.Id);
            }
            */

            if (securityCondition != null)
            {
                smDBQuery = smDBQuery.Where(securityCondition["sm."]);
            }

            var smDB = smDBQuery.ToList();

            if (smDB.Count != 1)
            {
                return false;
            }

            if (loadIntoMemory)
            {
                output = Converter.GetSubmodel(smDB[0], securityCondition: securityCondition);
            }

            result = true;
        }

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


    //ToDo: Move into security
    private bool InitSecurity(ISecurityConfig? securityConfig, out Dictionary<string, string>? securityCondition)
    {
        securityCondition = null;
        if (securityConfig != null && !securityConfig.NoSecurity)
        {
            securityCondition = _contractSecurityRules.GetCondition();
            // Get claims
            var authResult = false;
            var accessRole = securityConfig.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();
            var httpRoute = securityConfig.Principal.FindFirst("Route")?.Value;
            var neededRightsClaim = securityConfig.Principal.FindFirst("NeededRights")?.Value;
            if (accessRole != null && httpRoute != null && Enum.TryParse(neededRightsClaim, out AasSecurity.Models.AccessRights neededRights))
            {
                authResult = _contractSecurityRules.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _);
            }

            return authResult;
        }

        return true;
    }
}