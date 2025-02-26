/*  Copyright (c) 2019-2023 Fraunhofer IOSB-INA Lemgo,
eine rechtlich nicht selbstaendige Einrichtung der Fraunhofer-Gesellschaft
zur Foerderung der angewandten Forschung e.V.
 */

using AasxServer;
using AasxServerDB;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AdminShellNS;
using Contracts;
using Contracts.Pagination;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TimeStamp;

namespace AasxServerStandardBib.Services
{
    public class AdminShellPackageEnvironmentService : IAdminShellPackageEnvironmentService
    {
        private readonly IAppLogger<AdminShellPackageEnvironmentService> _logger;
        private readonly IPersistenceService _persistenceService;   
        private AdminShellPackageEnv[] _packages;

        public AdminShellPackageEnvironmentService(IAppLogger<AdminShellPackageEnvironmentService> logger, IPersistenceService persistenceService)
        {
            _logger     = logger ?? throw new ArgumentNullException(nameof(logger));
            _packages   = Program.env;
            _persistenceService = persistenceService;
        }

        #region Others

        private bool EmptyPackageAvailable(out int emptyPackageIndex)
        {
            emptyPackageIndex = -1;

            for (int envi = 0; envi < _packages.Length; envi++)
            {
                if (_packages[envi] == null)
                {
                    emptyPackageIndex            = envi;
                    _packages[emptyPackageIndex] = new AdminShellPackageEnv();
                    return true;
                }
            }

            return false;
        }

        public void setWrite(int packageIndex, bool status)
        {
            _packages[packageIndex].setWrite(status);
        }

        #endregion

        #region AssetAdministrationShell

        public IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body)
        {
            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {
                _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells.Add(body);
                var timeStamp = DateTime.UtcNow;
                body.TimeStampCreate = timeStamp;
                body.SetTimeStamp(timeStamp);
                _packages[emptyPackageIndex].setWrite(true);
                Program.signalNewData(2);
                return _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells[0]; //Considering it is the first AAS being added to empty package.
            }
            else
            {
                throw new Exception("No empty environment package available in the server.");
            }
        }

        public void DeleteAssetAdministrationShell(int packageIndex, IAssetAdministrationShell aas)
        {
            if (aas != null && packageIndex != -1)
            {
                bool deleted = (bool)(_packages[packageIndex].AasEnv?.AssetAdministrationShells.Remove(aas));
                if (deleted)
                {
                    _logger.LogDebug($"Deleted Asset Administration Shell with id {aas.Id}");
                    //if no more shells in the environment, then remove the environment
                    // TODO (jtikekar, 2023-09-04): what about submodels and concept descriptions for the same environment
                    if (_packages[packageIndex].AasEnv.AssetAdministrationShells.Count == 0)
                    {
                        _packages[packageIndex] = null;
                    }

                    //_packages[packageIndex].setWrite(true);
                    Program.signalNewData(2);
                }
                else
                {
                    _logger.LogError($"Could not delete Asset Administration Shell with id {aas.Id}");
                }
            }
        }


        public IAssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier, out int packageIndex)
        {
            bool found = IsAssetAdministrationShellPresent(aasIdentifier, out IAssetAdministrationShell output, out packageIndex);
            if (found)
            {
                _logger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
                return output;
            }
            else
            {
                throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
            }
        }

        public bool IsAssetAdministrationShellPresent(string aasIdentifier)
        {
            return IsAssetAdministrationShellPresent(aasIdentifier, out _, out _);
        }

        public bool IsAssetAdministrationShellPresent(string aasIdentifier, out IAssetAdministrationShell output, out int packageIndex)
        {
            if (Program.withDb)
            {
                var success = false;
                output = Program.LoadPackage<IAssetAdministrationShell>(success: out success, packageIndex: out packageIndex, aasIdentifier: aasIdentifier);
                return success;
            }

            output = null;
            packageIndex = -1;

            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        var aas = env.AssetAdministrationShells.Where(a => a.Id.Equals(aasIdentifier));
                        if (aas.Any())
                        {
                            output = aas.First();
                            packageIndex = Array.IndexOf(_packages, package);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void UpdateAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null && packageIndex != -1)
            {
                var aasIndex = _packages[packageIndex].AasEnv.AssetAdministrationShells.IndexOf(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Remove(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Insert(aasIndex, body);
                var timeStamp = DateTime.UtcNow;
                body.TimeStampCreate = timeStamp;
                body.SetTimeStamp(timeStamp);
                _packages[packageIndex].setWrite(true);
                Program.signalNewData(1); //0 not working, hence 1 = same tree, structure may change

                _logger.LogDebug($"Successfully updated the AAS with requested AAS");
            }
        }

        public void DeleteAssetInformationThumbnail(int packageIndex, IResource defaultThumbnail)
        {
            _packages[packageIndex].DeleteAssetInformationThumbnail(defaultThumbnail);
            Program.signalNewData(0);
        }

        public Stream GetAssetInformationThumbnail(int packageIndex)
        {
            return _packages[packageIndex].GetLocalThumbnailStream();
        }

        public void UpdateAssetInformationThumbnail(IResource defaultThumbnail, Stream fileContent, int packageIndex)
        {
            _packages[packageIndex].EmbedAssetInformationThumbnail(defaultThumbnail, fileContent);
            Program.signalNewData(0);
        }

        #endregion

        #region Submodel

        public void DeleteSubmodelById(string submodelIdentifier)
        {
            var submodel = GetSubmodelById(submodelIdentifier, out int packageIndex);
            if (submodel != null && packageIndex != -1)
            {
                foreach (var aas in _packages[packageIndex].AasEnv.AssetAdministrationShells)
                {
                    //_aasService.Value.DeleteSubmodelReferenceById(aas.Id, submodelIdentifier);
                }

                _packages[packageIndex].AasEnv.Submodels.Remove(submodel);
                _logger.LogDebug($"Deleted submodel with id {submodelIdentifier}.");
                _packages[packageIndex].setWrite(true);
                Program.signalNewData(1);
            }
        }

        public ISubmodel GetSubmodelById(string submodelIdentifier, out int packageIndex)
        {
            var found = IsSubmodelPresent(submodelIdentifier, out ISubmodel submodel, out packageIndex);
            if (found)
            {
                _logger.LogDebug($"Found the submodel with Id {submodelIdentifier}");
                return submodel;
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found.");
            }
        }

        public void DeleteSupplementaryFileInPackage(string submodelIdentifier, string filePath)
        {
            _ = GetSubmodelById(submodelIdentifier, out int packageIndex);
            if (packageIndex != -1)
            {
                _packages[packageIndex].DeleteSupplementaryFile(filePath);
            }
        }

        public bool IsSubmodelPresent(string submodelIdentifier)
        {
            return IsSubmodelPresent(submodelIdentifier, out _, out _);
        }

        public bool IsSubmodelPresent(string submodelIdentifier, out ISubmodel output, out int packageIndex)
        {
            if (Program.withDb)
            {
                var success = false;
                output = Program.LoadPackage<ISubmodel>(success: out success, packageIndex: out packageIndex, smIdentifier: submodelIdentifier);
                return success;
            }

            output = null;
            packageIndex = -1;

            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        var submodels = env.Submodels.Where(a => a.Id.Equals(submodelIdentifier));
                        if (submodels.Any())
                        {
                            output = submodels.First();
                            packageIndex = Array.IndexOf(_packages, package);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region ConceptDescription

        public void DeleteConceptDescriptionById(string cdIdentifier)
        {
            var conceptDescription = GetConceptDescriptionById(cdIdentifier, out int packageIndex);

            if ((conceptDescription != null) && (packageIndex != -1))
            {
                _packages[packageIndex].AasEnv.ConceptDescriptions.Remove(conceptDescription);
                _logger.LogDebug($"Delete ConceptDescription with id {cdIdentifier}");
                Program.signalNewData(1);
            }
        }

        public IConceptDescription GetConceptDescriptionById(string cdIdentifier, out int packageIndex)
        {
            var found = IsConceptDescriptionPresent(cdIdentifier, out IConceptDescription output, out packageIndex);
            if (found)
            {
                _logger.LogDebug($"Found the conceptDescription with id {cdIdentifier}");
                return output;
            }
            else
            {
                throw new NotFoundException($"ConceptDescription with id {cdIdentifier} NOT found.");
            }
        }

        private bool IsConceptDescriptionPresent(string cdIdentifier, out IConceptDescription output, out int packageIndex)
        {
            if (Program.withDb)
            {
                var success = false;
                output = Program.LoadPackage<IConceptDescription>(success: out success, packageIndex: out packageIndex, cdIdentifier: cdIdentifier);
                return success;
            }

            output = null;
            packageIndex = -1;
            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        var conceptDescriptions = env.ConceptDescriptions.Where(c => c.Id.Equals(cdIdentifier));
                        if (conceptDescriptions.Any())
                        {
                            output = conceptDescriptions.First();
                            packageIndex = Array.IndexOf(_packages, package);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public List<IConceptDescription> GetAllConceptDescriptions()
        {
            var output = new List<IConceptDescription>();

            //Get All Concept descriptions
            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        output.AddRange(env.ConceptDescriptions);
                    }
                }
            }

            return output;
        }

        public bool IsConceptDescriptionPresent(string cdIdentifier)
        {
            return IsConceptDescriptionPresent(cdIdentifier, out _, out _);
        }

        public IConceptDescription CreateConceptDescription(IConceptDescription body)
        {
            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {
                _packages[emptyPackageIndex].AasEnv.ConceptDescriptions.Add(body);
                var timeStamp = DateTime.UtcNow;
                body.TimeStampCreate = timeStamp;
                body.SetTimeStamp(timeStamp);
                // _packages[emptyPackageIndex].setWrite(true); this api is currently not connected to the database
                Program.signalNewData(2);
                return _packages[emptyPackageIndex].AasEnv.ConceptDescriptions[0]; //Considering it is the first AAS being added to empty package.
            }
            else
            {
                throw new Exception("No empty environment package available in the server.");
            }
        }

        public void UpdateConceptDescriptionById(IConceptDescription body, string cdIdentifier)
        {
            var conceptDescription = GetConceptDescriptionById(cdIdentifier, out int packageIndex);
            if (conceptDescription != null && packageIndex != -1)
            {
                var cdIndex = _packages[packageIndex].AasEnv.ConceptDescriptions.IndexOf(conceptDescription);
                _packages[packageIndex].AasEnv.ConceptDescriptions.Remove(conceptDescription);
                _packages[packageIndex].AasEnv.ConceptDescriptions.Insert(cdIndex, body);
                var timeStamp = DateTime.UtcNow;
                body.TimeStampCreate = timeStamp;
                body.SetTimeStamp(timeStamp);
                // _packages[packageIndex].setWrite(true); this api is currently not connected to the database
                Program.signalNewData(1); //0 not working, hence 1 = same tree, structure may change

                _logger.LogDebug($"Successfully updated the ConceptDescription.");
            }
        }

        public Stream GetFileFromPackage(string submodelIdentifier, string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                var _ = GetSubmodelById(submodelIdentifier, out int packageIndex);
                return _packages[packageIndex].GetLocalStreamFromPackage(fileName);
            }
            else
            {
                _logger.LogError($"File name is empty.");
                throw new UnprocessableEntityException($"File name is empty.");
            }
        }

        public void ReplaceAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas)
        {
            if (!aasIdentifier.Equals(newAas.Id))
                throw new UnprocessableEntityException($"The AAS ID can currently not be changed.");

            var aas = GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null && packageIndex != -1)
            {
                var existingIndex = _packages[packageIndex].AasEnv.AssetAdministrationShells.IndexOf(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Remove(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Insert(existingIndex, newAas);
                var timeStamp = DateTime.UtcNow;
                newAas.TimeStampCreate = timeStamp;
                newAas.SetTimeStamp(timeStamp);
                _packages[packageIndex].setWrite(true);
                Program.signalNewData(1);
            }
        }

        public void ReplaceSubmodelById(string submodelIdentifier, ISubmodel newSubmodel)
        {
            var submodel = GetSubmodelById(submodelIdentifier, out int packageIndex);
            if (submodel != null && packageIndex != -1)
            {
                var existingIndex = _packages[packageIndex].AasEnv.Submodels.IndexOf(submodel);
                _packages[packageIndex].AasEnv.Submodels.Remove(submodel);
                _packages[packageIndex].AasEnv.Submodels.Insert(existingIndex, newSubmodel);
                var timeStamp = DateTime.UtcNow;
                newSubmodel.TimeStampCreate = timeStamp;
                newSubmodel.SetParentAndTimestamp(timeStamp);
                _packages[packageIndex].setWrite(true);
                Program.signalNewData(1);
            }
        }
        public List<ISubmodel> GetSubmodelsBySemanticId(IReference reqSemanticId)
        {
            var output = GetAllSubmodels();

            if (output.Count == 0)
            {
                return output;
            }

            var submodels = output.Where(s => s.SemanticId.Matches(reqSemanticId)).ToList();
            if (submodels.IsNullOrEmpty())
            {
                _logger.LogInformation("Submodels with requested SemanticId not found.");
            }

            return submodels;

        }

        public List<ISubmodel> GetSubmodelsByIdShort(string idShort)
        {
            var output = GetAllSubmodels();

            if (string.IsNullOrEmpty(idShort) || output.Count == 0)
            {
                return output;
            }

            var submodels = output.Where(s => s.IdShort != null && s.IdShort.Equals(idShort, StringComparison.Ordinal)).ToList();
            if (submodels.IsNullOrEmpty())
            {
                _logger.LogInformation($"Submodels with IdShort {idShort} not found.");
            }

            return submodels;

        }

        /// <summary>
        /// Gets all submodels from the packages.
        /// </summary>
        /// <returns>A list of all submodels.</returns>
        public List<ISubmodel> GetAllSubmodels()
        {
            if (Program.withDb)
            {
                // workround to have submodels in memory
                // will only work if all packages fit into memory
                Program.LoadAllPackages();
            }

            var output = new List<ISubmodel>();

            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        if (env?.Submodels != null)
                        {
                            output.AddRange(env.Submodels);
                        }  
                    }
                }
            }

            return output;
        }

        public ISubmodel CreateSubmodel(ISubmodel newSubmodel, string aasIdentifier = null)
        {
            //Check if Submodel exists
            var found = IsSubmodelPresent(newSubmodel.Id, out _, out _);
            if (found)
            {
                throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
            }

            //Check if corresponding AAS exist. If yes, then add to the same environment
            if (!string.IsNullOrEmpty(aasIdentifier))
            {
                var aasFound = IsAssetAdministrationShellPresent(aasIdentifier, out IAssetAdministrationShell aas, out int packageIndex);
                if (aasFound)
                {
                    newSubmodel.SetAllParents(DateTime.UtcNow);
                    aas.Submodels ??= new List<IReference>();
                    aas.Submodels.Add(newSubmodel.GetReference());
                    _packages[packageIndex].AasEnv.Submodels.Add(newSubmodel);
                    var timeStamp = DateTime.UtcNow;
                    aas.SetTimeStamp(timeStamp);
                    newSubmodel.TimeStampCreate = timeStamp;
                    newSubmodel.SetTimeStamp(timeStamp);
                    _packages[packageIndex].setWrite(true);
                    Program.signalNewData(2);
                    return newSubmodel; // TODO: jtikekar find proper solution
                }
            }

            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {
                _packages[emptyPackageIndex].AasEnv.Submodels.Add(newSubmodel);
                var timeStamp = DateTime.UtcNow;
                newSubmodel.TimeStampCreate = timeStamp;
                newSubmodel.SetTimeStamp(timeStamp);
                _packages[emptyPackageIndex].setWrite(true);
                Program.signalNewData(2);
                return _packages[emptyPackageIndex].AasEnv.Submodels[0]; //Considering it is the first AAS being added to empty package.
            }
            else
            {
                throw new Exception("No empty environment package available in the server.");
            }
        }

        public Task ReplaceSupplementaryFileInPackage(string submodelIdentifier, string sourceFile, string targetFile, string contentType, MemoryStream fileContent)
        {
            var submodel = GetSubmodelById(submodelIdentifier, out int packageIndex);
            return _packages[packageIndex].ReplaceSupplementaryFileInPackageAsync(sourceFile, targetFile, contentType, fileContent);
        }

        public void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier)
        {
            var aas = this.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);

            if (aas != null)
            {
                var submodelReference = aas.Submodels.Where(s => s.Matches(submodelIdentifier));
                if (submodelReference.Any())
                {
                    _logger.LogDebug($"Found requested submodel reference in the aas.");
                    bool deleted = aas.Submodels.Remove(submodelReference.First());
                    if (deleted)
                    {
                        _logger.LogDebug($"Deleted submodel reference with id {submodelIdentifier} from the AAS with id {aasIdentifier}.");
                        _packages[packageIndex].setWrite(true);
                        Program.signalNewData(1);
                    }
                    else
                    {
                        _logger.LogError($"Could not delete submodel reference with id {submodelIdentifier} from the AAS with id {aasIdentifier}.");
                    }
                }
                else
                {
                    throw new NotFoundException($"SubmodelReference with id {submodelIdentifier} not found in AAS with id {aasIdentifier}");
                }
            }
        }

        public IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier)
        {
            IReference output = null;

            // TODO (jtikekar, 2023-09-04): to check if submodel with requested submodelReference exists in the server
            var aas = this.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);

            if (aas != null)
            {
                if (aas.Submodels.IsNullOrEmpty())
                {
                    aas.Submodels = new List<IReference>
                    {
                        body
                    };
                    output = aas.Submodels.Last();
                }
                else
                {
                    bool found = false;
                    //Check if duplicate
                    foreach (var submodelReference in aas.Submodels)
                    {
                        if (submodelReference.Matches(body))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        _logger.LogDebug($"Cannot create requested Submodel-Reference in the AAS !!");
                        throw new DuplicateException($"Requested SubmodelReference already exists in the AAS with Id {aasIdentifier}.");
                    }
                    else
                    {
                        aas.Submodels.Add(body);
                        output = aas.Submodels.Last();
                    }
                }
                _packages[packageIndex].setWrite(true);
            }

            return output;
        }

        #endregion
    }
}