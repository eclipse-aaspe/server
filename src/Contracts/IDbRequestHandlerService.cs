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
    Task<DbRequestPackageEnvResult> ReadPackageEnv(string aasIdentifier, string submodelIdentifier);

    Task<List<IAssetAdministrationShell>> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort);
    Task<IAssetAdministrationShell> ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);
    Task<IAssetAdministrationShell> CreateAssetAdministrationShell(ISecurityConfig securityConfig, IAssetAdministrationShell body);
    Task<DbRequestResult> ReplaceAssetAdministrationShellById(ISecurityConfig security, string aasIdentifier, AssetAdministrationShell body);
    Task<DbRequestResult> DeleteAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);

    Task<IReference> CreateSubmodelReferenceInAAS(ISecurityConfig securityConfig, Reference body, string aasIdentifier);
    Task<DbRequestResult> DeleteSubmodelReferenceById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<ISubmodel>> ReadPagedSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference reqSemanticId, string idShort);
    Task<ISubmodel> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    Task<ISubmodel> CreateSubmodel(ISecurityConfig securityConfig, ISubmodel newSubmodel, string aasIdentifier);
    Task<DbRequestResult> UpdateSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body);
    Task<DbRequestResult> ReplaceSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodel body);
    Task<DbRequestResult> DeleteSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    Task<ISubmodelElement> ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    Task<ISubmodelElement> CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, string idShortPath, bool first = true);
    Task<DbRequestResult> UpdateSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    Task<DbRequestResult> ReplaceSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
    Task<DbRequestResult> DeleteSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);

    Task<IAssetInformation> ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier);
    Task<DbRequestResult> ReplaceAssetInformation(ISecurityConfig securityConfig, string aasIdentifier, AssetInformation body);

    Task<DbFileRequestResult> ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);
    Task<DbRequestResult> ReplaceFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);
    Task<DbRequestResult> DeleteFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, string idShortPath);

    Task<DbFileRequestResult> ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier);
    Task<DbRequestResult> ReplaceThumbnail(ISecurityConfig securityConfig, string aasIdentifier, string fileName, string contentType, MemoryStream stream);
    Task<DbRequestResult> DeleteThumbnail(ISecurityConfig securityConfig, string aasIdentifier);

    Task<Events.EventPayload> ReadEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest);
    Task<DbRequestResult> UpdateEventMessages(ISecurityConfig securityConfig, DbEventRequest dbEventRequest);

    Task<List<IConceptDescription>> ReadPagedConceptDescriptions(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string idShort = null, IReference isCaseOf = null, IReference dataSpecificationRef = null);
    Task<IConceptDescription> ReadConceptDescriptionById(ISecurityConfig securityConfig, string cdIdentifier);
    Task<IConceptDescription> CreateConceptDescription(ISecurityConfig securityConfig, IConceptDescription body);
    Task<DbRequestResult> ReplaceConceptDescriptionById(ISecurityConfig securityConfig, IConceptDescription body, string cdIdentifier);
    Task<DbRequestResult> DeleteConceptDescriptionById(ISecurityConfig securityConfig, string cdIdentifier);

    Task<AasCore.Aas3_0.Environment> GenerateSerializationByIds(ISecurityConfig securityConfig, List<string> aasIds = null, List<string> submodelIds = null, bool? includeCD = false);

    Task<QResult> QuerySearchSMs(bool withTotalCount, bool withLastId, string semanticId, string identifier, string diff, string expression);
    Task<int> QueryCountSMs(string semanticId, string identifier, string diff, string expression);
    Task<QResult> QuerySearchSMEs(string requested, bool withTotalCount, bool withLastId, string smSemanticId, string smIdentifier, string semanticId, string diff, string contains, string equal, string lower, string upper, string expression);
    Task<int> QueryCountSMEs(string smSemanticId, string smIdentifier, string semanticId, string diff, string contains, string equal, string lower, string upper, string expression);
}
