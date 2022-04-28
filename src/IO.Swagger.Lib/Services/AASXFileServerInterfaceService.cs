using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AasxRestServerLibrary;
using AdminShellNS;
using IO.Swagger.Lib.Models;
using IO.Swagger.Models;
using Microsoft.Extensions.Logging;

namespace IO.Swagger.Services
{
    /// <summary>
    /// This is a helper class to deal with "AASX File Server Interface" defines in "Details of Asset Administration Shell Part 2, section 4.5"
    /// </summary>
    public class AASXFileServerInterfaceService : IAASXFileServerInterfaceService
    {
        private AdminShellPackageEnv[] _packages;
        private string[] _envFileNames;

        /// <summary>
        /// Constructor
        /// </summary>
        public AASXFileServerInterfaceService()
        {
            _packages = AasxServer.Program.env;
            _envFileNames = AasxServer.Program.envFileName;
        }

        /// <summary>
        /// Deletes a specific AASX package from the server
        /// </summary>
        /// <returns>True, if successfully deleted</returns>
        public bool DeleteAASXByPackageId(string packageId)
        {
            int packageIndex = int.Parse(packageId);
            var requestedPackage = _packages[packageIndex];
            if (requestedPackage != null)
            {
                //Move the file to archive
                try
                {
                    var archivePath = Path.Combine(AasxHttpContextHelper.DataPath, "AasxFileArchive");
                    if (!Directory.Exists(archivePath))
                    {
                        Directory.CreateDirectory(archivePath);
                    }
                    var fileName = Path.Combine(archivePath, Path.GetFileName(_envFileNames[packageIndex]));
                    System.IO.File.Move(_envFileNames[packageIndex], fileName);
                }
                catch (Exception ex)
                {
                    new Exception($"File could not be moved to the archive directory. Exception found : {ex.Message}");
                }

                _packages[packageIndex] = null;
                _envFileNames[packageIndex] = null;
                AasxServer.Program.signalNewData(2);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a specific AASX package from the server
        /// </summary>
        /// <param name="packageId">Package index of the found package</param>
        /// <param name="byteArray">File content</param>
        /// <param name="fileName">File Name</param>
        /// <param name="fileSize">Size of the file</param>
        /// <returns></returns>
        public bool GetAASXByPackageId(string packageId, out byte[] byteArray, out string fileName, out long fileSize)
        {
            byteArray = null;
            fileName = null;
            fileSize = 0;
            int packageIndex = int.Parse(packageId);
            var requestedPackage = _packages[packageIndex];
            var requestedFileName = _envFileNames[packageIndex];
            if (!string.IsNullOrEmpty(requestedFileName) && requestedPackage != null)
            {
                //Create Temp file
                string copyFileName;
                try
                {
                    copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
                    System.IO.File.Copy(requestedFileName, copyFileName, true);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while creating temp file for {requestedFileName} : {ex.Message}");
                }

                byteArray = System.IO.File.ReadAllBytes(copyFileName);
                fileName = Path.GetFileName(requestedFileName);
                fileSize = byteArray.Length;

                //Delete Temp file
                System.IO.File.Delete(copyFileName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a list of available AASX packages at the server
        /// </summary>
        public List<PackageDescription> GetAllAASXPackageIds()
        {
            var packageDescriptionList = new List<PackageDescription>();
            for (int i = 0; i < _packages.Length; i++)
            {
                if (_packages[i] != null)
                {
                    PackageDescription packageDescription = new PackageDescription();
                    packageDescription.PackageId = i.ToString();
                    var aasIdList = new List<string>();
                    foreach (var aas in _packages[i].AasEnv.AdministrationShells)
                    {
                        aasIdList.Add(aas.identification.id);
                    }
                    packageDescription.AasIds = aasIdList;
                    packageDescriptionList.Add(packageDescription);
                }
            }
            return packageDescriptionList;
        }

        /// <summary>
        /// Creates an AASX package at the server
        /// </summary>
        /// <param name="fileContent">New AASX package</param>
        /// <param name="fileName">Filename of the AASX package</param>
        /// <param name="packageIndex">package index at which the new package is added</param>
        /// <returns></returns>
        public bool PostAASXPackage(byte[] fileContent, string fileName, out int packageIndex)
        {
            packageIndex = -1;
            var newFileName = Path.Combine(AasxHttpContextHelper.DataPath, fileName);
            //Check if file already exists
            if (System.IO.File.Exists(newFileName))
            {
                throw new Exception($"File already exists");
            }

            //TODO:Check file extentsion ".aasx"
            try
            {
                //Write the received file content to this temp file
                //var content = Convert.FromBase64String(body);
                System.IO.File.WriteAllBytes(newFileName, fileContent);
            }
            catch (Exception e)
            {
                throw new Exception($"Cannot create AASX file {fileName}. Aborting. Exception: {e.Message}");
            }
            try
            {
                // open again
                var newAasx = new AdminShellPackageEnv(newFileName, true);
                if (newAasx != null)
                {
                    var emptyPackageIndex = FindEmptyPackageIndex();
                    if (emptyPackageIndex == -1)
                    {
                        throw new Exception($"Could not create the file as the detastructure is completely filled.");
                    }
                    _packages[emptyPackageIndex] = newAasx;
                    _envFileNames[emptyPackageIndex] = newFileName;
                    packageIndex = emptyPackageIndex;
                    return true;
                }
                else
                {
                    throw new Exception($"Cannot load new package {fileName}. Aborting.");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not create the file. Aborting. Exception: {e.Message}");
            }

        }

        private int FindEmptyPackageIndex()
        {
            var packageIndex = -1;
            for (int i = 0; i < _packages.Length; i++)
            {
                if (_packages[i] == null)
                {
                    return i;
                }
            }

            return packageIndex;
        }

        /// <summary>
        /// Updates the AASX package at the server
        /// </summary>
        /// <param name="packageId">Package ID from the package list</param>
        /// <param name="fileContent">New AASX package</param>
        /// <param name="newFileName">Filename of the AASX package</param>
        /// <returns></returns>
        public bool PutAASXPackageById(string packageId, byte[] fileContent, string newFileName)
        {
            int packageIndex = int.Parse(packageId);
            var package = _packages[packageIndex];
            if (package != null)
            {
                var originalFile = _packages[packageIndex].Filename;

                //Create temporary file
                var tempNewFile = Path.GetTempFileName().Replace(".tmp", ".aasx");
                try
                {
                    //Write the received file content to this temp file
                    //var content = Convert.FromBase64String(fileContent);
                    System.IO.File.WriteAllBytes(tempNewFile, fileContent);
                }
                catch (Exception e)
                {
                    throw new Exception($"Cannot save AASX temporarily in {tempNewFile}. Aborting. Exception: {e.Message}");
                }

                lock (AasxServer.Program.changeAasxFile)
                {
                    try
                    {
                        _packages[packageIndex].Close();
                        //Create back up of existing file
                        System.IO.File.Copy(originalFile, originalFile + ".bak", overwrite: true);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Cannot close/ backup old AASX {originalFile}. Aborting. Exception: {e.Message}");
                    }
                    try
                    {
                        //Replace existing file with temp file
                        originalFile = newFileName;
                        //Copy tempFile into originalFile location
                        System.IO.File.Copy(tempNewFile, originalFile, overwrite: true);
                        // open again
                        var newAasx = new AdminShellPackageEnv(originalFile, true);
                        if (newAasx != null)
                        {
                            _packages[packageIndex] = newAasx;
                        }
                        else
                        {
                            throw new Exception($"Cannot load new package {originalFile} for replacing via PUT. Aborting.");
                        }
                        //now delete tempFile
                        System.IO.File.Delete(tempNewFile);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Cannot replace AASX {originalFile} with new {tempNewFile}. Aborting. Exception: {e.Message}");
                    }
                }

                AasxServer.Program.signalNewData(2);

                return true;
            }
            else
            {
                throw new Exception($"Requested package not found.");
            }
        }

        /// <summary>
        ///  Retrieves AAS w.r.t. PackageId and aasId
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="aasId"></param>
        /// <returns></returns>
        public AssetAdministrationShellAndAsset GetAssetAdministrationShellAndAssetByPackageId(string packageId, string aasId)
        {
            int packageIndex = int.Parse(packageId);
            var package = _packages[packageIndex];
            if (package != null)
            {
                foreach (var aas in package.AasEnv.AdministrationShells)
                {
                    if (!string.IsNullOrEmpty(aas.identification.id) && aas.identification.id.Equals(aasId))
                    {
                        var output = new AssetAdministrationShellAndAsset
                        {
                            aas = aas
                        };
                        var asset = package.AasEnv.FindAsset(aas.assetRef);
                        if (asset != null)
                        {
                            output.asset = asset;
                        }
                        return output;
                    }
                }
            }

            return null;
        }
    }
}
