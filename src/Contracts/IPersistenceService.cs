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

    void DeleteAssetAdministrationShellById(string aasIdentifier);
    void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier);
    void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);
    void DeleteThumbnail(string aasIdentifier);
    void DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    void DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
}

