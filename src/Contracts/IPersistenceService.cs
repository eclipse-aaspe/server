namespace Contracts;

using System.Collections.Generic;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.DbRequests;
using Contracts.Pagination;

public interface IPersistenceService
{
    void InitDB(bool reloadDB, string dataPath);
    void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles);

    List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list);
    AdminShellPackageEnv ReadPackageEnv(int envId);
    string ReadAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "");

    Task<DbRequestResult> DoDbOperation(DbRequest dbRequest);

    List<IAssetAdministrationShell> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort);
    ISubmodel ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    List<ISubmodelElement> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    ISubmodelElement ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathELements);
    List<ISubmodel> ReadAllSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference? reqSemanticId, string? idShort);
    IAssetAdministrationShell ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);
    string ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathELements, out byte[] content, out long fileSize);
    IAssetInformation ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier);
    string ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier, out byte[] byteArray, out long fileSize);
    AdminShellPackageEnv ReadPackageEnv(string aasIdentifier, out string envFileName);
    Events.EventPayload ReadEventMessages(DbEventRequest eventRequest);

    ISubmodel CreateSubmodel(ISubmodel body, string decodedAasIdentifier);
    IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body);
    IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier);
    ISubmodelElement CreateSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, bool first, ISubmodelElement body);
    ISubmodelElement CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, bool first);

    void UpdateSubmodelById(string? aasIdentifier, string? submodelIdentifier, ISubmodel body);
    void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    void UpdateAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas);
    void UpdateAssetInformation(string aasIdentifier, IAssetInformation newAssetInformation);
    //void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    void UpdateFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);
    void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, MemoryStream stream);

    void UpdateEventMessages(DbEventRequest eventRequest);

    void ReplaceSubmodelById(string submodelIdentifier, ISubmodel body);
    void ReplaceSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement body);
    void ReplaceFileByPath(string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);

    void DeleteAssetAdministrationShellById(string aasIdentifier);
    void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier);
    void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);
    void DeleteThumbnail(string aasIdentifier);
    void DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    void DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
}

