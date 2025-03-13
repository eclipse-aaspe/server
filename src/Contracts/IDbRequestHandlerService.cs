namespace AasxServerStandardBib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using Contracts.Pagination;
using Contracts;

public interface IDbRequestHandlerService
{
    Task<IDbRequestResult> ReadPagedAssetAdministrationShells(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, List<ISpecificAssetId> assetIds, string idShort);
    Task<IDbRequestResult> ReadSubmodelById(ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);
    Task<List<ISubmodelElement>> ReadPagedSubmodelElements(IPaginationParameters paginationParameters, ISecurityConfig securityConfig, string aasIdentifier, string submodelIdentifier);

}
