using AdminShellNS.Models;
using System.Collections.Generic;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAasxFileServerInterfaceService
    {
        void DeleteAASXByPackageId(string packageId);
        string GetAASXByPackageId(string packageId, out byte[] content, out long fileSize, out IAssetAdministrationShell aas);
        List<PackageDescription> GetAllAASXPackageIds(string aasId = null);
        IAssetAdministrationShell GetAssetAdministrationShellByPackageId(string packageId);
        string PostAASXPackage(byte[] fileContent, string fileName);
        void UpdateAASXPackageById(string packageId, byte[] content, string fileName);
    }
}