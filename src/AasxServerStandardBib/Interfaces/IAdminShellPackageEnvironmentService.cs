using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAdminShellPackageEnvironmentService
    {
        #region AssetAdministrationShell
        AssetAdministrationShell CreateAssetAdministrationShell(AssetAdministrationShell body);
        void DeleteAssetAdministrationShell(int packageIndex, AssetAdministrationShell aas);
        List<AssetAdministrationShell> GetAllAssetAdministrationShells();
        AssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier, out int packageIndex);
        Stream GetAssetInformationThumbnail(int packageIndex);
        bool IsAssetAdministrationShellPresent(string aasIdentifier);
        void UpdateAssetAdministrationShellById(AssetAdministrationShell body, string aasIdentifier);
        void UpdateAssetInformationThumbnail(Resource defaultThumbnail, Stream fileContent, int packageIndex);

        #endregion

        #region Submodel
        void DeleteSubmodelById(string submodelIdentifier);
        Submodel GetSubmodelById(string submodelIdentifier, out int packageIndex);

        #endregion

        #region ConceptDescription

        void DeleteConceptDescriptionById(string cdIdentifier);
        ConceptDescription GetConceptDescriptionById(string cdIdentifier, out int packageIndex);
        List<ConceptDescription> GetAllConceptDescriptions();
        bool IsConceptDescriptionPresent(string cdIdentifier);
        ConceptDescription CreateConceptDescription(ConceptDescription body);
        void UpdateConceptDescriptionById(ConceptDescription body, string cdIdentifier);

        #endregion

    }
}
