namespace AasxServerDB;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AasxServerDB.Context;
using AasxServerDB.Entities;
using AdminShellNS.Extensions;
using AdminShellNS;
using Contracts;
using Microsoft.IdentityModel.Tokens;
using AasCore.Aas3_0;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Contracts.Pagination;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using System.Security.AccessControl;
using System.Security.Claims;

public class EntityFrameworkPersistenceService : IPersistenceService
{
    private IContractSecurityRules _contractSecurityRules;

    public EntityFrameworkPersistenceService(IContractSecurityRules contractSecurityRules)
    {
        this._contractSecurityRules = contractSecurityRules;
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

    public List<IAssetAdministrationShell> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, List<ISpecificAssetId> assetIds, string? idShort)
    {
        var output = Converter.GetPagedAssetAdministrationShells(paginationParameters, assetIds, idShort);

        //Apply filters
        //ToDo: Should this be done during DB access?
        if (output.Count != 0)
        {
            if (!assetIds.IsNullOrEmpty())
            {
                //_logger.LogDebug($"Filtering AASs with requested assetIds.");
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

                if (aasList.Count == 0)
                {
                    //_logger.LogInformation($"No AAS with requested assetId found.");
                }
                output = aasList;
            }
        }
        return output;
    }

    public void DeleteAssetAdministrationShellById(string aasIdentifier)
    {
        var aas = ReadAssetAdministrationShellById(aasIdentifier);

        if (aas != null)
        {
            Edit.DeleteAAS(aasIdentifier);
        }
    }

    public void DeleteFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        if (IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output))
        {
            //ToDo: Decide how to deal with files
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //_submodelService.DeleteFileByPath(submodelIdentifier, idShortPath);
        }
        else
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier)
    {
        if (IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output))
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");

            //ToDo Delete in DB
            //delete the submodel first, this should eventually delete the submodel reference from all the AASs
            //_submodelService.DeleteSubmodelById(submodelIdentifier);
        }
        else
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
    {
        if (IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output))
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //_submodelService.DeleteSubmodelElementByPath(submodelIdentifier, idShortPath);
        }
        else
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier)
    {
        var aas = this.ReadAssetAdministrationShellById(aasIdentifier);

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
                throw new Exception($"SubmodelReference with id {submodelIdentifier} not found in AAS with id {aasIdentifier}");
            }
        }
    }

    public void DeleteThumbnail(string aasIdentifier)
    {
        var aas = this.ReadAssetAdministrationShellById(aasIdentifier);
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
                    throw new Exception($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                }
            }
        }
    }
    public List<ISubmodelElement> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier)
    {
        /*
        bool found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (found)
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");

            //Need to load all first, otherwise recursion might not work
            return output.SubmodelElements
                    .Skip(paginationParameters.Cursor)
                    .Take(paginationParameters.Limit).ToList();
        }
        */
        var securityConditionSM = "";
        var securityConditionSME = "";
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
            if (!authResult)
            {
                throw new Exception($"NOT ALLOWED: Submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            }
        }

        var output = Converter.GetPagedSubmodelElements(paginationParameters, securityConditionSM, securityConditionSME, aasIdentifier, submodelIdentifier);
        if (output == null)
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
        return output;
    }


    public List<IReference> ReadAllSubmodelReferencesFromAas(string aasIdentifier)
    {
        List<IReference> output = new List<IReference>();
        var aas = this.ReadAssetAdministrationShellById(aasIdentifier);

        if (aas != null)
        {
            if (aas.Submodels.IsNullOrEmpty())
            {
                //_logger.LogDebug($"No submodels present in the AAS with Id {aasIdentifier}");
            }

            output = aas.Submodels;
        }

        return output;
    }

    public IAssetAdministrationShell ReadAssetAdministrationShellById(string aasIdentifier)
    {
        bool found = IsAssetAdministrationShellPresent(aasIdentifier, out IAssetAdministrationShell output);
        if (found)
        {
            //_logger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
            return output;
        }
        else
        {
            throw new Exception($"Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    public ISubmodel ReadSubmodelById(string aasIdentifier, string submodelIdentifier)
    {
        bool found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (found)
        {
            //_logger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
            return output;
        }
        else
        {
            throw new Exception($"Submodel wit id {submodelIdentifier} in Asset Administration Shell with id {aasIdentifier} not found.");
        }
    }

    public IAssetInformation ReadAssetInformation(string aasIdentifier)
    {
        var aas = ReadAssetAdministrationShellById(aasIdentifier);
        return aas.AssetInformation;
    }

    public string ReadFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize)
    {
        content = null;
        fileSize = 0;
        var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (found)
        {
            //ToDo submodel service solution
            //return _submodelService.GetFileByPath(submodelIdentifier, idShortPath, out content, out fileSize);
            return "dummy";
        }
        else
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public ISubmodelElement ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements)
    {
        var output = Converter.GetSubmodelElementByPath(aasIdentifier, submodelIdentifier, idShortPathElements);
        if (output == null)
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
        return output;
    }

    public string ReadThumbnail(string aasIdentifier, out byte[] byteArray, out long fileSize)
    {
        string fileName = null;
        byteArray = null;
        fileSize = 0;
        var aas = ReadAssetAdministrationShellById(aasIdentifier);
        if (aas != null)
        {
            if (aas.AssetInformation != null)
            {
                if (aas.AssetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(aas.AssetInformation.DefaultThumbnail.Path))
                {
                    fileName = aas.AssetInformation.DefaultThumbnail.Path;

                    //ToDo: Soultion for thumbnail stream
                    //Stream stream = _packageEnvService.GetAssetInformationThumbnail(packageIndex);
                    //byteArray = stream.ToByteArray();
                    //fileSize = byteArray.Length;

                    //_logger.LogDebug($"Updated the thumbnail in AAS with Id {aasIdentifier}");
                }
                else
                {
                    throw new Exception($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                }
            }
            else
            {
                throw new Exception($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
            }
        }

        return fileName;
    }

    public void UpdateSubmodelById(string? aasIdentifier, string? submodelIdentifier, ISubmodel newSubmodel)
    {
        var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
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
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body)
    {
        var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (found)
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //ToDo submodel service solution, do we really need a different solution for replace and update?
            //_submodelService.UpdateSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
        }
        else
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
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
            throw new Exception($"AssetAdministrationShell with id {body.Id} already exists.");
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

    public ISubmodelElement CreateSubmodelElement(string aasIdentifier, string submodelIdentifier, bool first, ISubmodelElement newSubmodelElement)
    {
        var smFound = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
        if (smFound)
        {
            //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
            //ToDo submodel service solution
            //return _submodelService.CreateSubmodelElement(submodelIdentifier, newSubmodelElement, first);

            return default;
        }
        else
        {
            throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
        }
    }

    public ISubmodelElement CreateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, bool first, ISubmodelElement body)
    {
        {
            var smFound = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier, out ISubmodel output);
            if (smFound)
            {
                //_logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                //return _submodelService.CreateSubmodelElementByPath(submodelIdentifier, idShortPath, first, newSubmodelElement);
                return default;
            }
            else
            {
                throw new Exception($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
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

    public void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, Stream stream) => throw new NotImplementedException();

    //public List<ISubmodel> GetAllSubmodels(string cursor, int limit){

    //}

    private bool IsAssetAdministrationShellPresent(string aasIdentifier, out IAssetAdministrationShell output)
    {
        output = default;

        var assetAdministrationShell = Converter.GetAssetAdministrationShell(null,aasIdentifier);
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

    private bool IsSubmodelPresentWithinAAS(string aasIdentifier, string submodelIdentifier, out ISubmodel output)
    {
        output = default;

        var aas = ReadAssetAdministrationShellById(aasIdentifier);
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

        return false;
    }

    public ISubmodel ReadSubmodelById(ISecurityConfig securityConfig, string submodelIdentifier) => throw new NotImplementedException();
    public List<ISubmodelElement> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string submodelIdentifier) => throw new NotImplementedException();
    public List<ISubmodel> ReadAllSubmodels(Reference? reqSemanticId, string? idShort) => throw new NotImplementedException();
    public string ReadFileByPath(string decodedSubmodelIdentifier, string idShortPath, out byte[]? content, out long? fileSize) => throw new NotImplementedException();
    public ISubmodelElement ReadSubmodelElementByPath(ISecurityConfig securityConfig, string decodedSubmodelIdentifier, string idShortPath) => throw new NotImplementedException();
    public ISubmodel CreateSubmodel(Submodel body, string decodedAasIdentifier) => throw new NotImplementedException();
    public ISubmodelElement CreateSubmodelElementByPath(ISecurityConfig securityConfig, string decodedSubmodelIdentifier, string idShortPath, bool first, ISubmodelElement body) => throw new NotImplementedException();
    public ISubmodelElement CreateSubmodelElement(ISecurityConfig securityConfig, string decodedSubmodelIdentifier, ISubmodelElement body, bool first) => throw new NotImplementedException();
    public void UpdateSubmodelById(string decodedSubmodelIdentifier, ISubmodel submodel) => throw new NotImplementedException();
    public void UpdateSubmodelElementByPath(string decodedSubmodelIdentifier, string idShortPath, ISubmodelElement submodelElement) => throw new NotImplementedException();
    public void DeleteFileByPath(ISecurityConfig securityConfig, string decodedSubmodelIdentifier, string idShortPath) => throw new NotImplementedException();
    public void DeleteSubmodelById(string decodedSubmodelIdentifier) => throw new NotImplementedException();
    public void DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string decodedSubmodelIdentifier, string idShortPath) => throw new NotImplementedException();
    public void ReplaceSubmodelById(string decodedSubmodelIdentifier, Submodel body) => throw new NotImplementedException();
    public void ReplaceSubmodelElementByPath(string decodedSubmodelIdentifier, string idShortPath, ISubmodelElement body) => throw new NotImplementedException();
    public void ReplaceFileByPath(string decodedSubmodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream) => throw new NotImplementedException();
}
