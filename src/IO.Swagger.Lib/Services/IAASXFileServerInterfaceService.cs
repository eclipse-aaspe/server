using System.Collections.Generic;
using AdminShellNS;
using IO.Swagger.Lib.Models;
using IO.Swagger.Models;

namespace IO.Swagger.Services
{
    /// <summary>
    /// An interface to deal with "AASX File Server Interface" defines in "Details of Asset Administration Shell Part 2, section 4.5"
    /// </summary>
    public interface IAASXFileServerInterfaceService
    {
        /// <summary>
        /// Returns a list of available AASX packages at the server
        /// </summary>
        List<PackageDescription> GetAllAASXPackageIds();

        /// <summary>
        /// Returns a specific AASX package from the server
        /// </summary>
        /// <param name="packageId">Package Index of the requested package</param>
        /// <param name="byteArray">File content</param>
        /// <param name="fileName">File Name</param>
        /// <param name="fileSize">File size</param>
        /// <returns>PackageBody</returns>
        /// //TODO:May need to remove Package Body
        bool GetAASXByPackageId(string packageId, out byte[] byteArray, out string fileName, out long fileSize);

        /// <summary>
        /// Deletes a specific AASX package from the server
        /// </summary>
        /// <returns>True id successfully deleted</returns>
        bool DeleteAASXByPackageId(string packageId);

        /// <summary>
        /// Updates the AASX package at the server
        /// </summary>
        /// <param name="packageId">Package ID from the package list</param>
        /// <param name="fileContent">New AASX package</param>
        /// <param name="fileName">Filename of the AASX package</param>
        /// <returns></returns>
        bool PutAASXPackageById(string packageId, byte[] fileContent, string fileName);

        /// <summary>
        /// Creates an AASX package at the server
        /// </summary>
        /// <param name="fileContent">New AASX package</param>
        /// <param name="fileName">Filename of the AASX package</param>
        /// <param name="packageIndex">package index at which the new package is added</param>
        /// <returns></returns>
        bool PostAASXPackage(byte[] fileContent, string fileName, out int packageIndex);

        /// <summary>
        /// Retrieves AAS w.r.t. PackageId and aasId
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="aasId"></param>
        /// <returns></returns>
        AssetAdministrationShellAndAsset GetAssetAdministrationShellAndAssetByPackageId(string packageId, string aasId);
    }
}
