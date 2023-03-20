using AasCore.Aas3_0_RC02;
using System.Collections.Generic;
using System.IO;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAssetAdministrationShellService
    {
        AssetAdministrationShell CreateAssetAdministrationShell(AssetAdministrationShell body);
        Reference CreateSubmodelReferenceInAAS(Reference body, string aasIdentifier);
        void DeleteAssetAdministrationShellById(string aasIdentifier);
        void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);
        List<AssetAdministrationShell> GetAllAssetAdministrationShells(List<SpecificAssetId> assetIds = null, string idShort = null);
        List<ISubmodelElement> GetAllSubmodelElements(string aasIdentifier, string submodelIdentifier);
        List<Reference> GetAllSubmodelReferencesFromAas(string aasIdentifier);
        AssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier);
        AssetInformation GetAssetInformation(string aasIdentifier);
        string GetThumbnail(string aasIdentifier, out byte[] content, out long fileSize);
        void UpdateAssetAdministrationShellById(AssetAdministrationShell body, string aasIdentifier);
        void UpdateAssetInformation(AssetInformation body, string aasIdentifier);
        void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, Stream stream);
    }
}
