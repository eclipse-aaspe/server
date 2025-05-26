namespace AasRegistryDiscovery.WebApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AasRegistryDiscovery.WebApi.Models;

public interface IPersistenceService
{
    #region AAS Registry
    AssetAdministrationShellDescriptor CreateAssetAdminitrationShellDescriptor(AssetAdministrationShellDescriptor aasDescriptor);
    SubmodelDescriptor CreateSubmodelDescriptorWithinAasDescriptor(string aasIdentifier, SubmodelDescriptor smDescriptor);
    void DeleteAssetAdministrationShellDescriptorById(string aasIdentifier);
    void DeleteSmDescriptorWithinAasDescriptorById(string aasIdentifier, string smIdentifier);
    AasDescriptorPagedResult GetAllAsssetAdministrationShellDescriptors(int? limit, string? cursor, AssetKind? assetKind, string? assetType);
    SubmodelDescriptorPagedResult GetAllSmDescriptorsWithinAasDescriptor(string aasIdentifier, int? limit, string? cursor);
    AssetAdministrationShellDescriptor GetAssetAdministrationShellDescriptorById(string aasIdentifier);
    SubmodelDescriptor GetSubmodelDescriptorWithinAasDescriptor(string aasIdentifier, string smIdentifier);
    void UpdateAssetAdministrationShellDescriptor(string aasIdentifier, AssetAdministrationShellDescriptor newAasDescriptor);
    void UpdateSmDescriptorWithinAasDescriptor(string aasIdentifier, string smIdentifier, SubmodelDescriptor newSmDescriptor);
    #endregion

    #region Submodel Registry
    SubmodelDescriptor CreateSubmodelDescriptor(SubmodelDescriptor submodelDescriptor);
    SubmodelDescriptor GetSubmodelDescriptorById(string smIdentifier);
    void DeleteSubmodeldescriptorById(string smIdentifier);
    SubmodelDescriptorPagedResult GetAllSubmodelDescriptors(int? limit, string? cursor);
    void UpdateSubmodelDescriptorById(string smIdentifier, SubmodelDescriptor newSmDescriptor);

    #endregion

    #region Discovery

    GetAllAssetAdministrationShellIdsResult GetAllAssetAdministrationShellIdsByAssetLink(List<ISpecificAssetId>? reqAssetIds, int? limit, string cursor);
    List<ISpecificAssetId> GetAllAssetLinksById(string aasIdentifier);
    void DeleteAllAssetLinksById(string aasIdentifier);
    List<ISpecificAssetId> AddAllAsetLinkById(string aasIdentifier, List<ISpecificAssetId> newAssetLinks);
    #endregion
}
