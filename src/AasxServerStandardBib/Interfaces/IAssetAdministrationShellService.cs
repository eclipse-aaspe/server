
using System.Collections.Generic;
using System.IO;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAssetAdministrationShellService
    {
        IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body);
        IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier);
        void DeleteAssetAdministrationShellById(string aasIdentifier);
        void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);
        List<IAssetAdministrationShell> GetAllAssetAdministrationShells(List<SpecificAssetId> assetIds = null, string idShort = null);
        List<ISubmodelElement> GetAllSubmodelElements(string aasIdentifier, string submodelIdentifier);
        List<IReference> GetAllSubmodelReferencesFromAas(string aasIdentifier);
        IAssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier);
        IAssetInformation GetAssetInformation(string aasIdentifier);
        string GetThumbnail(string aasIdentifier, out byte[] content, out long fileSize);
        void UpdateAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier);
        void UpdateAssetInformation(AssetInformation body, string aasIdentifier);
        void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, Stream stream);
    }
}
