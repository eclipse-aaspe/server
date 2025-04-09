namespace Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.DbRequests;
using Contracts.Pagination;

public interface IDbRequestHandlerService
{
    Task<List<IAssetAdministrationShell>> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort);

    Task<ISubmodel> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

    Task<List<ISubmodel>> ReadPagedSubmodels(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, Reference reqSemanticId, string idShort);

    Task<IAssetAdministrationShell> ReadAssetAdministrationShellById(ISecurityConfig securityConfig, string aasIdentifier);

    Task<ISubmodelElement> ReadSubmodelElementByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements);

    Task<DbFileRequestResult> ReadFileByPath(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, List<object> idShortPathElements);

    Task<IAssetInformation> ReadAssetInformation(ISecurityConfig securityConfig, string aasIdentifier);

    Task<DbFileRequestResult> ReadThumbnail(ISecurityConfig securityConfig, string aasIdentifier);

    Task<DbRequestPackageEnvResult> ReadPackageEnv(string aasIdentifier, string submodelIdentifier);

    Task<Events.EventPayload> ReadEventMessages(DbEventRequest dbEventRequest);

    Task<ISubmodel> CreateSubmodel(ISecurityConfig securityConfig, ISubmodel newSubmodel, string aasIdentifier);

    Task<ISubmodelElement> CreateSubmodelElement(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier, ISubmodelElement body, string idShortPath, bool first);

    Task<IAssetAdministrationShell> CreateAssetAdministrationShell(ISecurityConfig securityConfig, IAssetAdministrationShell body);

    Task<IReference> CreateSubmodelReferenceInAAS(ISecurityConfig securityConfig, Reference body, string aasIdentifier);


    Task UpdateEventMessages(DbEventRequest dbEventRequest);
}
