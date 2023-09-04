/*  Copyright (c) 2019-2023 Fraunhofer IOSB-INA Lemgo,
    eine rechtlich nicht selbstaendige Einrichtung der Fraunhofer-Gesellschaft
    zur Foerderung der angewandten Forschung e.V.
 */
using AasxServer;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AdminShellNS;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Services
{
    public class AdminShellPackageEnvironmentService : IAdminShellPackageEnvironmentService
    {
        private readonly IAppLogger<AdminShellPackageEnvironmentService> _logger;
        private readonly Lazy<IAssetAdministrationShellService> _aasService;
        private AdminShellPackageEnv[] _packages;

        public AdminShellPackageEnvironmentService(IAppLogger<AdminShellPackageEnvironmentService> logger, Lazy<IAssetAdministrationShellService> aasService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aasService = aasService;
            _packages = Program.env;
        }

        #region Others

        private bool EmptyPackageAvailable(out int emptyPackageIndex)
        {
            emptyPackageIndex = -1;

            for (int envi = 0; envi < _packages.Length; envi++)
            {
                if (_packages[envi] == null)
                {
                    emptyPackageIndex = envi;
                    _packages[emptyPackageIndex] = new AdminShellPackageEnv();
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region AssetAdministrationShell
        public IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body)
        {
            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {

                _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells.Add(body);
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

                    Program.signalNewData(2);
                }
                else
                {
                    _logger.LogError($"Could not delete Asset Administration Shell with id {aas.Id}");
                }
            }
        }

        public List<IAssetAdministrationShell> GetAllAssetAdministrationShells()
        {
            List<IAssetAdministrationShell> output = new List<IAssetAdministrationShell>();

            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null && env.AssetAdministrationShells != null && env.AssetAdministrationShells.Count != 0)
                    {
                        output.AddRange(env.AssetAdministrationShells);
                    }
                }
            }

            return output;
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



        private bool IsAssetAdministrationShellPresent(string aasIdentifier, out IAssetAdministrationShell output, out int packageIndex)
        {
            output = null; packageIndex = -1;
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
            _packages[packageIndex].EmbeddAssetInformationThumbnail(defaultThumbnail, fileContent);
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
                    _aasService.Value.DeleteSubmodelReferenceById(aas.Id, submodelIdentifier);
                }
                _packages[packageIndex].AasEnv.Submodels.Remove(submodel);
                _logger.LogDebug($"Deleted submodel with id {submodelIdentifier}.");
                AasxServer.Program.signalNewData(1);
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

        private bool IsSubmodelPresent(string submodelIdentifier, out ISubmodel output, out int packageIndex)
        {
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
                AasxServer.Program.signalNewData(1);
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
            var aas = GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null && packageIndex != -1)
            {
                var existingIndex = _packages[packageIndex].AasEnv.AssetAdministrationShells.IndexOf(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Remove(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Insert(existingIndex, newAas);
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
                Program.signalNewData(1);
            }
        }

        public List<ISubmodel> GetAllSubmodels(IReference reqSemanticId = null, string idShort = null)
        {
            List<ISubmodel> output = new List<ISubmodel>();

            //Get All Submodels
            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        foreach (var s in env.Submodels)
                        {
                            // TODO (jtikekar, 2023-09-04): uncomment and support
                            //if (SecurityCheckTestOnly(s.IdShort, "", s))
                            output.Add(s);
                        }
                    }
                }
            }

            //Apply filters
            if (output.Any())
            {
                //Filter w.r.t idShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    var submodels = output.Where(s => s.IdShort.Equals(idShort)).ToList();
                    if (submodels.IsNullOrEmpty())
                    {
                        _logger.LogInformation($"Submodels with IdShort {idShort} Not Found.");
                    }

                    output = submodels;
                }

                //Filter w.r.t. SemanticId
                if (reqSemanticId != null)
                {
                    if (output.Any())
                    {
                        var submodels = output.Where(s => s.SemanticId.Matches(reqSemanticId)).ToList();
                        if (submodels.IsNullOrEmpty())
                        {
                            _logger.LogInformation($"Submodels with requested SemnaticId Not Found.");
                        }

                        output = submodels;
                    }

                }
            }

            return output;
        }

        public ISubmodel CreateSubmodel(ISubmodel newSubmodel)
        {
            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {

                _packages[emptyPackageIndex].AasEnv.Submodels.Add(newSubmodel);
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

        #endregion
    }
}
