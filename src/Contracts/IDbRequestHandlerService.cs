namespace Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.DbRequests;
using Contracts.Pagination;
using Contracts.QueryResult;

public interface IDbRequestHandlerService
{
    Task<List<IAssetAdministrationShell>> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort);

    Task<ISubmodel> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<ISubmodel>> ReadPagedSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference reqSemanticId, string idShort);

    Task<IAssetAdministrationShell> ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);

    Task<ISubmodelElement> ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);

    Task<DbFileRequestResult> ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);

    Task<IAssetInformation> ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier);

    Task<DbFileRequestResult> ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier);

    Task<DbRequestPackageEnvResult> ReadPackageEnv(string aasIdentifier, string submodelIdentifier);

    Task<Events.EventPayload> ReadEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest);

    Task<ISubmodel> CreateSubmodel(ISecurityConfig securityConfig, ISubmodel newSubmodel, string aasIdentifier);

    Task<ISubmodelElement> CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, string idShortPath, bool first = true);

    Task<IAssetAdministrationShell> CreateAssetAdministrationShell(ISecurityConfig securityConfig, IAssetAdministrationShell body);

    Task<IReference> CreateSubmodelReferenceInAAS(ISecurityConfig securityConfig, Reference body, string aasIdentifier);

    Task<DbRequestResult> UpdateSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body);
    Task<DbRequestResult> UpdateEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest);
    Task<DbRequestResult> UpdateSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);

    Task<DbRequestResult> ReplaceSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body);
    Task<DbRequestResult> ReplaceAssetAdministrationShellById(ISecurityConfig security, string aasIdentifier, AssetAdministrationShell body);
    Task<DbRequestResult> ReplaceAssetInformation(ISecurityConfig securityConfig, string aasIdentifier, AssetInformation body);
    Task<DbRequestResult> ReplaceSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    Task<DbRequestResult> ReplaceFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);
    Task<DbRequestResult> ReplaceThumbnail(ISecurityConfig securityConfig, string aasIdentifier, string fileName, string contentType, MemoryStream stream);

    Task<DbRequestResult> DeleteAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);
    Task<DbRequestResult> DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    Task<DbRequestResult> DeleteSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    Task<DbRequestResult> DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    Task<DbRequestResult> DeleteSubmodelReferenceById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    Task<DbRequestResult> DeleteThumbnail(ISecurityConfig securityConfig, string aasIdentifier);

    Task<QResult> QuerySearchSMs(QueryGrammarJSON grammar, bool withTotalCount, bool withLastId, string semanticId, string identifier, string diff, string expression);
    Task<int> QueryCountSMs(QueryGrammarJSON grammar, string semanticId, string identifier, string diff, string expression);
    Task<QResult> QuerySearchSMEs(QueryGrammarJSON grammar, string requested, bool withTotalCount, bool withLastId, string smSemanticId, string smIdentifier, string semanticId, string diff, string contains, string equal, string lower, string upper, string expression);
    Task<int> QueryCountSMEs(QueryGrammarJSON grammar, string smSemanticId, string smIdentifier, string semanticId, string diff, string contains, string equal, string lower, string upper, string expression);
}
