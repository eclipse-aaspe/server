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
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections;
using System.Reflection.Metadata;

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

    public AdminShellPackageEnv ReadPackageEnv(int envId)
    {
        return Converter.GetPackageEnv(envId);
    }

    public string ReadAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "")
    {
        return Converter.GetAASXPath(envId, cdId, aasId, smId);
    }

    public List<IAssetAdministrationShell> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort)
    {
        string securityConditionSM, securityConditionSME;
        InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        var output = Converter.GetPagedAssetAdministrationShells(paginationParameters, assetIds, idShort);

        //Apply filters
        //ToDo: Should this be done during DB access?
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
                        if (assetId.Name.Equals("globalAssetId", StringComparison.OrdinalIgnoreCase))
                        {
                            result = output.Where(a => a.AssetInformation.GlobalAssetId!.Equals(assetId.Value)).ToList();
                        }
                        else
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

    public ISubmodel ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        string securityConditionSM, securityConditionSME;
        bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var output = Converter.GetSubmodel(null, submodelIdentifier);
        var found = output != null;
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
            throw new NotFoundException($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    public List<ISubmodelElement> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        string securityConditionSM, securityConditionSME;
        bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);
        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var output = Converter.GetPagedSubmodelElements(paginationParameters, securityConditionSM, securityConditionSME, aasIdentifier, submodelIdentifier);
        if (output == null)
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
        return output;
    }

    public ISubmodelElement ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
    {
        string securityConditionSM, securityConditionSME;
        bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);
        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        var output = Converter.GetSubmodelElementByPath(securityConditionSM, securityConditionSME, aasIdentifier, submodelIdentifier, idShortPathElements);
        if (output == null)
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
        return output;
    }

    public List<ISubmodel> ReadAllSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference? reqSemanticId, string? idShort)
    {
        string securityConditionSM, securityConditionSME;
        bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        var output = Converter.GetSubmodels(paginationParameters, securityConditionSM, securityConditionSME, reqSemanticId, idShort);

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

    public IAssetAdministrationShell ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier)
    {
        string securityConditionSM, securityConditionSME;
        bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: AAS with id {aasIdentifier}");
        }

        bool found = IsAssetAdministrationShellPresent(aasIdentifier, out IAssetAdministrationShell output);
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

    public string ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements, out byte[] content, out long fileSize)
    {
        string securityConditionSM, securityConditionSME;
        bool isAllowed = InitSecurity(securityConfig, out securityConditionSM, out securityConditionSME);

        if (!isAllowed)
        {
            throw new NotAllowed($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
        }

        content = null;
        fileSize = 0;

        var fileElement = Converter.GetSubmodelElementByPath(securityConditionSM, securityConditionSME, aasIdentifier, submodelIdentifier, idShortPathElements);

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
                        var packageEnv = Converter.GetPackageEnv(aasIdentifier, submodelIdentifier, out envFileName);

                        if (packageEnv != null)
                        {
                            var stream = packageEnv.GetLocalStreamFromPackage(fileName);
                            content = stream.ToByteArray();
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

    public IAssetInformation ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier)
    {
        var aas = ReadAssetAdministrationShellById(securityConfig, aasIdentifier);
        return aas.AssetInformation;
    }

    public string ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier, out byte[] byteArray, out long fileSize)
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
                    var packageEnv = Converter.GetPackageEnv(aasIdentifier, null, out envFileName);

                    if (packageEnv != null)
                    {
                        var stream = packageEnv.GetLocalStreamFromPackage(fileName);
                        byteArray = stream.ToByteArray();
                        fileSize = byteArray.Length;
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

    public AdminShellPackageEnv ReadPackageEnv(string aasID, out string envFileName)
    {
        return Converter.GetPackageEnv(aasID, null, out envFileName);
    }

    public void DeleteAssetAdministrationShellById(string aasIdentifier)
    {
        var aas = ReadAssetAdministrationShellById(null, aasIdentifier);

        if (aas != null)
        {
            Edit.DeleteAAS(aasIdentifier);
        }
    }

    public void DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        if (IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output))
        {
            //ToDo: Decide how to deal with files
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //_submodelService.DeleteFileByPath(submodelIdentifier, idShortPath);
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier)
    {
        if (IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output))
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");

            //ToDo Delete in DB
            //delete the submodel first, this should eventually delete the submodel reference from all the AASs
            //_submodelService.DeleteSubmodelById(submodelIdentifier);
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        if (IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output))
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //_submodelService.DeleteSubmodelElementByPath(submodelIdentifier, idShortPath);
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier)
    {
        var aas = this.ReadAssetAdministrationShellById(null, aasIdentifier);

        if (aas != null)
        {
            var submodelReference = aas.Submodels.Where(s => s.Matches(submodelIdentifier));
            if (submodelReference.Any())
            {
                //_logger.LogDebug($"Found requested submodel reference in the aas.");

                //ToDo: How to delete reference properly? This is probably not enough
                bool deleted = aas.Submodels.Remove(submodelReference.First());
                if (deleted)
                {
                    //_logger.LogDebug($"Deleted submodel reference with id {submodelIdentifier} from the AAS with id {aasIdentifier}.");
                    //_packages[packageIndex].setWrite(true);
                    //Program.signalNewData(1);
                }
                else
                {
                    //_logger.LogError($"Could not delete submodel reference with id {submodelIdentifier} from the AAS with id {aasIdentifier}.");
                }
            }
            else
            {
                throw new NotFoundException($"SubmodelReference with id {submodelIdentifier} not found in AAS with id {aasIdentifier}");
            }
        }
    }

    public void DeleteThumbnail(string aasIdentifier)
    {
        var aas = this.ReadAssetAdministrationShellById(null, aasIdentifier);
        if (aas != null)
        {
            if (aas.AssetInformation != null)
            {
                if (aas.AssetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(aas.AssetInformation.DefaultThumbnail.Path))
                {
                    //ToDo: How to delete or reset thumbnail?
                    //_packageEnvService.DeleteAssetInformationThumbnail(packageIndex, aas.AssetInformation.DefaultThumbnail);
                }
                else
                {
                    throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                }
            }
        }
    }

    public void UpdateSubmodelById(string? aasIdentifier, string? submodelIdentifier, ISubmodel newSubmodel)
    {
        var found = IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (found)
        {
            //ToDo submodel service solution, do we really need a different solution for replace and update?
            // Replace
            //_submodelService.ReplaceSubmodelById(submodelIdentifier, newSubmodel);
            //_verificationService.VerifyRequestBody(newSubmodel);
            //_packageEnvService.ReplaceSubmodelById(submodelIdentifier, newSubmodel);


            // Update
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //_submodelService.UpdateSubmodelById(submodelIdentifier, newSubmodel);

            //if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out ISubmodel submodel, out int packageIndex))
            //{
            //    //Verify the body first
            //    _verificationService.VerifyRequestBody(newSubmodel);

            //    Update.ToUpdateObject(submodel, newSubmodel);

            //    submodel.SetTimeStamp(DateTime.UtcNow);

            //    _packageEnvService.setWrite(packageIndex, true);
            //    Program.signalNewData(1);
            //}
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body)
    {
        var found = IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (found)
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //ToDo submodel service solution, do we really need a different solution for replace and update?
            //_submodelService.UpdateSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body)
    {
        //ToDo: Verification, may be in API Controller
        ////Verify the body first
        //_verificationService.VerifyRequestBody(body);

        var found = IsAssetAdministrationShellPresent(body.Id, out IAssetAdministrationShell output);
        if (found)
        {
            //_logger.LogDebug($"Cannot create requested AAS !!");
            throw new DuplicateException($"AssetAdministrationShell with id {body.Id} already exists.");
        }

        //if (EmptyPackageAvailable(out int emptyPackageIndex))
        //{
        //    _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells.Add(body);
        //    var timeStamp = DateTime.UtcNow;
        //    body.TimeStampCreate = timeStamp;
        //    body.SetTimeStamp(timeStamp);
        //    _packages[emptyPackageIndex].setWrite(true);
        //    //Program.signalNewData(2);
        //    output = _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells[0]; //Considering it is the first AAS being added to empty package.
        //}
        //else
        //{
        //    throw new Exception("No empty environment package available in the server.");
        //}
        return output;
    }

    public ISubmodelElement CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, bool first)
    {
        var smFound = IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (smFound)
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //ToDo submodel service solution
            //return _submodelService.CreateSubmodelElement(submodelIdentifier, newSubmodelElement, first);

            return default;
        }
        else
        {
            throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public ISubmodelElement CreateSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, bool first, ISubmodelElement body)
    {
        {
            var smFound = IsSubmodelPresentWithinAAS(null, aasIdentifier, submodelIdentifier, out ISubmodel output);
            if (smFound)
            {
                //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                //return _submodelService.CreateSubmodelElementByPath(submodelIdentifier, idShortPath, first, newSubmodelElement);
                return default;
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }
    }

    public IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier)
    {
        //Verify request body
        //_verificationService.VerifyRequestBody(body);

        IReference output = null;

        //// TODO (jtikekar, 2023-09-04): to check if submodel with requested submodelReference exists in the server
        //var aas = this.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);

        //if (aas != null)
        //{
        //    if (aas.Submodels.IsNullOrEmpty())
        //    {
        //        aas.Submodels = new List<IReference>
        //            {
        //                body
        //            };
        //        output = aas.Submodels.Last();
        //    }
        //    else
        //    {
        //        bool found = false;
        //        //Check if duplicate
        //        foreach (var submodelReference in aas.Submodels)
        //        {
        //            if (submodelReference.Matches(body))
        //            {
        //                found = true;
        //                break;
        //            }
        //        }

        //        if (found)
        //        {
        //            _logger.LogDebug($"Cannot create requested Submodel-Reference in the AAS !!");
        //            throw new DuplicateException($"Requested SubmodelReference already exists in the AAS with Id {aasIdentifier}.");
        //        }
        //        else
        //        {
        //            aas.Submodels.Add(body);
        //            output = aas.Submodels.Last();
        //        }
        //    }
        //    _packages[packageIndex].setWrite(true);
        //}

        return output;

    }

    public void UpdateAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas)
    {
        ////Verify the body first
        //_verificationService.VerifyRequestBody(body);

        //_packageEnvService.UpdateAssetAdministrationShellById(body, aasIdentifier);
    }

    public void UpdateAssetInformation(string aasIdentifier, IAssetInformation newAssetInformation)
    {
        //_verificationService.VerifyRequestBody(body);
        //var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);
        //if (aas != null)
        //{
        //    aas.AssetInformation = body;
        //    Program.signalNewData(0);

        //    _logger.LogDebug($"AssetInformation from AAS with id {aasIdentifier} updated successfully.");
        //}
    }

    public void UpdateSubmodelById(string aasIdentifier, string submodelIdentifier, Submodel newSubmodel) => throw new NotImplementedException();

    public void UpdateFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream) => throw new NotImplementedException();

    public void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, MemoryStream stream) => throw new NotImplementedException();

    //public List<ISubmodel> GetAllSubmodels(string cursor, int limit){

    //}

    private bool IsAssetAdministrationShellPresent(string aasIdentifier, out IAssetAdministrationShell output)
    {
        output = default;

        var assetAdministrationShell = Converter.GetAssetAdministrationShell(null, aasIdentifier);
        if (assetAdministrationShell == null)
        {
            return false;
        }
        else
        {
            output = assetAdministrationShell;

            //ToDo: How to do that?
            //signalNewData(2);

            return true;
        }
    }

    private bool IsSubmodelPresentWithinAAS(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, out ISubmodel output)
    {
        output = default;

        if (!aasIdentifier.IsNullOrEmpty())
        {
            var aas = ReadAssetAdministrationShellById(securityConfig, aasIdentifier);
            if (aas != null)
            {
                foreach (var submodelReference in aas.Submodels)
                {
                    if (submodelReference.GetAsExactlyOneKey().Value.Equals(submodelIdentifier))
                    {
                        output = Converter.GetSubmodel(null, submodelIdentifier);
                        return true;
                    }
                }
            }
        }
        else
        {
            output = Converter.GetSubmodel(null, submodelIdentifier);
            bool isSubmodelPresent = output != null;
            return isSubmodelPresent;
        }

        return false;
    }

    private bool InitSecurity(ISecurityConfig securityConfig, out string securityConditionSM, out string securityConditionSME)
    {
        securityConditionSM = "";
        securityConditionSME = "";
        if (!securityConfig.NoSecurity)
        {
            securityConditionSM = _contractSecurityRules.GetConditionSM();
            securityConditionSME = _contractSecurityRules.GetConditionSME();
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

    public async Task<DbRequestResult> DoDbOperation(DbRequest dbRequest)
    {
        var result = new DbRequestResult();

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
            case DbRequestOp.ReadAllSubmodels:
                var submodels = ReadAllSubmodels(
                    dbRequest.Context.Params.PaginationParameters,
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.SemanticId,
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
                var packageEnv = ReadPackageEnv(dbRequest.Context.Params.AssetAdministrationShellIdentifier
                    , out envFile);
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
                var createdSubmodel = CreateSubmodel(dbRequest.Context.Params.SubmodelBody,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);

                result.Submodels = new List<ISubmodel>
                {
                    createdSubmodel
                };
                break;
            case DbRequestOp.CreateAssetAdministrationShell:
                var createdAas = CreateAssetAdministrationShell(
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
                    dbRequest.Context.Params.First);

                result.SubmodelElements = new List<ISubmodelElement>
                {
                    createdsubmodelElement
                };
                break;
            case DbRequestOp.CreateSubmodelElementByPath:
                var createdsubmodelElementbyPath = CreateSubmodelElementByPath(
                    dbRequest.Context.SecurityConfig,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShort,
                    dbRequest.Context.Params.First,
                    dbRequest.Context.Params.SubmodelElementBody);

                result.SubmodelElements = new List<ISubmodelElement>
                {
                    createdsubmodelElementbyPath
                };
                break;
            case DbRequestOp.CreateSubmodelReference:
                var createdSubmodelReference = CreateSubmodelReferenceInAAS(dbRequest.Context.Params.ReferenceBody,
                    dbRequest.Context.Params.AssetAdministrationShellIdentifier);

                result.References = new List<IReference>
                {
                    createdSubmodelReference
                };
                break;
            case DbRequestOp.UpdateSubmodelById:
                UpdateSubmodelById(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.SubmodelBody);
                break;
            case DbRequestOp.UpdateSubmodelElementByPath:
                UpdateSubmodelElementByPath(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShort,
                    dbRequest.Context.Params.SubmodelElementBody);
                break;
            case DbRequestOp.UpdateAssetInformation:
                UpdateAssetInformation(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.AssetInformation);
                break;
            case DbRequestOp.UpdateFileByPath:
                UpdateFileByPath(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.SubmodelIdentifier,
                    dbRequest.Context.Params.IdShort,
                    dbRequest.Context.Params.FileRequest.File,
                    dbRequest.Context.Params.FileRequest.ContentType,
                    dbRequest.Context.Params.FileRequest.Stream);
                break;
            case DbRequestOp.UpdateThumbnail:
                UpdateThumbnail(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.FileRequest.File,
                    dbRequest.Context.Params.FileRequest.ContentType,
                    dbRequest.Context.Params.FileRequest.Stream);
                break;
            case DbRequestOp.UpdateAssetAdministrationShellById:
                UpdateAssetAdministrationShellById(dbRequest.Context.Params.AssetAdministrationShellIdentifier,
                    dbRequest.Context.Params.AasBody)
                    ;
                break;
            case DbRequestOp.UpdateEventMessages:
                UpdateEventMessages(dbRequest.Context.Params.EventRequest);
                break;
            case DbRequestOp.ReplaceSubmodelById:
                break;
            case DbRequestOp.ReplaceSubmodelElementByPath:
                break;
            case DbRequestOp.ReplaceFileByPath:
                break;
            case DbRequestOp.DeleteAssetAdministrationShellById:
                break;
            case DbRequestOp.DeleteFileByPath:
                break;
            case DbRequestOp.DeleteSubmodelById:
                break;
            case DbRequestOp.DeleteSubmodelElementByPath:
                break;
            case DbRequestOp.DeleteSubmodelReferenceById:
                break;
            case DbRequestOp.DeleteThumbnail:
                break;
            default:
                dbRequest.TaskCompletionSource.SetException(new Exception("Unknown Operation"));
                break;
        }

        dbRequest.TaskCompletionSource.SetResult(result);
        return result;
    }

    public void ReplaceSubmodelById(string submodelIdentifier, ISubmodel body) => throw new NotImplementedException();
    public void ReplaceSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement body) => throw new NotImplementedException();
    public void ReplaceFileByPath(string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream) => throw new NotImplementedException();
    public ISubmodel CreateSubmodel(ISubmodel body, string decodedAasIdentifier) => throw new NotImplementedException();

    public Contracts.Events.EventPayload ReadEventMessages(DbEventRequest dbEventRequest)
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
                eventPayload.Status.LastUpdate = eventData.LastUpdate.Value;
            }
        }
        else
        {
            var timeStamp = DateTime.UtcNow;
            if (eventData.Transmitted != null)
            {
                eventData.Transmitted.Value = eventPayload.Status.Transmitted;
                eventData.Transmitted.SetTimeStamp(DateTime.UtcNow);
            }
            var dt = DateTime.Parse(eventPayload.Status.LastUpdate);
            if (eventData.LastUpdate != null)
            {
                eventData.LastUpdate.Value = eventPayload.Status.LastUpdate;
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

    public void UpdateEventMessages(DbEventRequest eventRequest)
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
        }

        if (eventData.Transmitted != null)
        {
            eventData.Transmitted.Value = transmitted;
            eventData.Transmitted.SetTimeStamp(DateTime.UtcNow);
        }
        var dt = DateTime.Parse(lastDiffValue);
        if (eventData.LastUpdate != null)
        {
            eventData.LastUpdate.Value = lastDiffValue;
            eventData.LastUpdate.SetTimeStamp(dt);
        }
        if (eventData.Message != null && statusValue != null)
        {
            eventData.Message.Value = statusValue;
            eventData.Message.SetTimeStamp(DateTime.UtcNow);
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
}