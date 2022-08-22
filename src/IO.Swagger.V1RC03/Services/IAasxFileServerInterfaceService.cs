using IO.Swagger.V1RC03.ApiModel;
using System.Collections.Generic;

namespace IO.Swagger.V1RC03.Services
{
    public interface IAasxFileServerInterfaceService
    {
        void DeleteAASXByPackageId(string packageId);
        string GetAASXByPackageId(string packageId, out byte[] content, out long fileSize);
        List<PackageDescription> GetAllAASXPackageIds(string aasId = null);
        string PostAASXPackage(byte[] fileContent, string fileName);
        void UpdateAASXPackageById(string packageId, byte[] content, string fileName);
    }
}