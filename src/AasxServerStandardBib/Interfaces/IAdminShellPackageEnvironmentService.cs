using System.Collections.Generic;
using System.IO;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAdminShellPackageEnvironmentService
    {
        #region AssetAdministrationShell
        IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body);
        void DeleteAssetAdministrationShell(int packageIndex, IAssetAdministrationShell aas);
        List<IAssetAdministrationShell> GetAllAssetAdministrationShells();
        IAssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier, out int packageIndex);
        Stream GetAssetInformationThumbnail(int packageIndex);
        bool IsAssetAdministrationShellPresent(string aasIdentifier);
        void UpdateAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier);
        void UpdateAssetInformationThumbnail(IResource defaultThumbnail, Stream fileContent, int packageIndex);

        #endregion

        #region Submodel
        void DeleteSubmodelById(string submodelIdentifier);
        ISubmodel GetSubmodelById(string submodelIdentifier, out int packageIndex);

        #endregion

        #region ConceptDescription

        void DeleteConceptDescriptionById(string cdIdentifier);
        IConceptDescription GetConceptDescriptionById(string cdIdentifier, out int packageIndex);
        List<IConceptDescription> GetAllConceptDescriptions();
        bool IsConceptDescriptionPresent(string cdIdentifier);
        IConceptDescription CreateConceptDescription(IConceptDescription body);
        void UpdateConceptDescriptionById(IConceptDescription body, string cdIdentifier);

        #endregion

    }
}
