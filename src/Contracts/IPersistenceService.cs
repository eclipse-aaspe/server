namespace Contracts;

using System.Collections.Generic;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.DbRequests;
using Contracts.Pagination;

public interface IPersistenceService
{
    void InitDB(bool reloadDB, string dataPath);
    Task<DbRequestResult> DoDbOperation(DbRequest dbRequest);

    void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles);

    List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list);


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

