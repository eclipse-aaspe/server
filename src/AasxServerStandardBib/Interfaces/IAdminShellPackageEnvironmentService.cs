using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
        void DeleteAssetInformationThumbnail(int packageIndex, IResource defaultThumbnail);
        void ReplaceAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas);

        #endregion

        #region Submodel
        void DeleteSubmodelById(string submodelIdentifier);
        ISubmodel GetSubmodelById(string submodelIdentifier, out int packageIndex);

        void DeleteSupplementaryFileInPackage(string submodelIdentifier, string filePath);
        Stream GetFileFromPackage(string submodelIdentifier, string fileName1);
        void ReplaceSubmodelById(string submodelIdentifier, ISubmodel newSubmodel);
        List<ISubmodel> GetAllSubmodels(IReference reqSemanticId = null, string idShort = null);
        bool IsSubmodelPresent(string submodelIdentifier);
        ISubmodel CreateSubmodel(ISubmodel newSubmodel);

        #endregion

        #region ConceptDescription

        void DeleteConceptDescriptionById(string cdIdentifier);
        IConceptDescription GetConceptDescriptionById(string cdIdentifier, out int packageIndex);
        List<IConceptDescription> GetAllConceptDescriptions();
        bool IsConceptDescriptionPresent(string cdIdentifier);
        IConceptDescription CreateConceptDescription(IConceptDescription body);
        void UpdateConceptDescriptionById(IConceptDescription body, string cdIdentifier);
        Task ReplaceSupplementaryFileInPackage(string submodelIdentifier, string sourceFile, string targetFile, string contentType, MemoryStream fileContent);



        #endregion

    }
}
