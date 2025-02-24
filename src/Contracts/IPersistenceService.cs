namespace Contracts;

using System.Collections.Generic;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.Pagination;

public interface IPersistenceService
{
    void InitDB(bool reloadDB, string dataPath);

    void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles);

    List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list);

    AdminShellPackageEnv ReadPackageEnv(int envId);

    //ConceptDescription GetConceptDescription(string cdIdentifier = "");

    List<IAssetAdministrationShell> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, List<ISpecificAssetId> assetIds, string? idShort);


    //List<ISubmodel> GetAllSubmodels(string cursor, int limit);

    //Files, siehe API /attachment

    string ReadAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "");

    void DeleteAssetAdministrationShellById(string aasIdentifier);

    ISubmodel ReadSubmodelById(string aasIdentifier, string submodelIdentifier);

    void DeleteFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);

    void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier);

    void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);

    void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);

    void DeleteThumbnail(string aasIdentifier);

    List<ISubmodelElement> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    List<IReference> ReadAllSubmodelReferencesFromAas(string aasIdentifier);
    IAssetAdministrationShell ReadAssetAdministrationShellById(string aasIdentifier);
    IAssetInformation ReadAssetInformation(string aasIdentifier);
    string ReadFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize);
    ISubmodelElement ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathELements);
    string ReadThumbnail(string aasIdentifier, out byte[] byteArray, out long fileSize);
    void UpdateSubmodelById(string? aasIdentifier, string? submodelIdentifier, ISubmodel body);
    void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body);
    ISubmodelElement CreateSubmodelElement(string aasIdentifier, string submodelIdentifier, bool first, ISubmodelElement body);
    ISubmodelElement CreateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, bool first, ISubmodelElement body);
    IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier);
    void UpdateAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas);
    void UpdateAssetInformation(string aasIdentifier, IAssetInformation newAssetInformation);
    void UpdateSubmodelById(string aasIdentifier, string submodelIdentifier, Submodel newSubmodel);

    //void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    void UpdateFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);
    void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, Stream stream);
}
