namespace Contracts;

using System.Collections.Generic;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.Pagination;

public interface IPersistenceService
{
    void InitDB(bool reloadDB, string dataPath);

    void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles);

    List<string> GetFilteredPackages(string filterPath, List<AdminShellPackageEnv> list);

    AdminShellPackageEnv? GetPackageEnv(int envId);

    //ConceptDescription? GetConceptDescription(string cdIdentifier = "");

    List<IAssetAdministrationShell> GetPagedAssetAdministrationShells(IPaginationParameters paginationParameters, List<ISpecificAssetId> assetIds);

    //AssetAdministrationShell? GetAssetAdministrationShell(string aasIdentifier = "");

    //Submodel? GetSubmodel(string smIdentifier = "");

    //List<ISubmodel> GetAllSubmodels(string cursor, int limit);

    //Files, siehe API /attachment

    string GetAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "");

}
