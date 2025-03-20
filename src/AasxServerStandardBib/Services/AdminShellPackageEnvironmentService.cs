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
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Smime;
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
        private readonly Lazy<IAssetAdministrationShellService> _aasService;
        private AdminShellPackageEnv[] _packages;

        public AdminShellPackageEnvironmentService(IAppLogger<AdminShellPackageEnvironmentService> logger, Lazy<IAssetAdministrationShellService> aasService)
        {
            _logger     = logger ?? throw new ArgumentNullException(nameof(logger));
            _aasService = aasService;
            _packages   = Program.env;
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

        public List<IAssetAdministrationShell> GetAllAssetAdministrationShells()
        {
            List<IAssetAdministrationShell> output = new List<IAssetAdministrationShell>();

            if (!Program.withDb)
            {
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
            }
            else
            {
                /*
                var db = new AasContext();
                var timeStamp = DateTime.UtcNow;

                var aasDBList = db.AASSets.ToList();
                foreach (var aasDB in aasDBList)
                {
                    int envId = aasDB.EnvId;

                    var aas = Converter.GetAssetAdministrationShell(aasDB: aasDB);
                    if (aas.TimeStamp == DateTime.MinValue)
                    {
                        aas.TimeStampCreate = timeStamp;
                        aas.SetTimeStamp(timeStamp);
                    }

                    // sm
                    var smAASDBList = db.SMSets.Where(sm => sm.EnvId == envId && sm.AASId == aasDB.Id).ToList();
                    foreach (var sm in smAASDBList)
                    {
                        aas.Submodels?.Add(new Reference(type: ReferenceTypes.ModelReference,
                            keys: new List<IKey>() { new Key(KeyTypes.Submodel, sm.Identifier) }
                            ));
                    }

                    output.Add(aas);
                }
                */

                using (var db = new AasContext())
                {
                    var timeStamp = DateTime.UtcNow;

                    var aasDBList = db.AASSets
                        .Include(aas => aas.SMSets) // Include related SMSets
                        .ToList();

                    foreach (var aasDB in aasDBList)
                    {
                        int envId = aasDB.EnvId;

                        var aas = Converter.GetAssetAdministrationShell(aasDB: aasDB);
                        if (aas.TimeStamp == DateTime.MinValue)
                        {
                            aas.TimeStampCreate = timeStamp;
                            aas.SetTimeStamp(timeStamp);
                        }

                        // sm
                        foreach (var sm in aasDB.SMSets.Where(sm => sm.EnvId == envId))
                        {
                            aas.Submodels?.Add(new Reference(type: ReferenceTypes.ModelReference,
                                keys: new List<IKey>() { new Key(KeyTypes.Submodel, sm.Identifier) }
                            ));
                        }

                        output.Add(aas);
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
                    _aasService.Value.DeleteSubmodelReferenceById(aas.Id, submodelIdentifier);
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
        
        //TODO (jtikekar, 2025-03-20): Refactor, when List<Environment> is removed from the server
        //Following method has complexities due to the current structure of List<Environment>
        public ISubmodel CreateSubmodel(ISubmodel newSubmodel)
        {
            bool found = true;

            //check if submodel is already referenced in any of the shells
            var foundPackages = new Dictionary<int, List<IAssetAdministrationShell>>();
            for(int i = 0;  i< _packages.Length; i++)
            {
                var package = _packages[i];
                if (package != null)
                {
                    var env = package.AasEnv;
                    // Check if the Submodel is already present in the package
                    // If yes, then skip the iteration, it will be handled by Blazor
                    var isSmAlreadyPresent = env.Submodels!.Exists(s => s.Id.Equals(newSubmodel.Id));
                    if (isSmAlreadyPresent)
                    {
                        found = true;
                    }
                    if (env != null && !env.AssetAdministrationShells.IsNullOrEmpty() && !isSmAlreadyPresent)
                    {
                        var foundAAS = new List<IAssetAdministrationShell>();
                        foreach (var aas in env.AssetAdministrationShells!)
                        {
                            if (aas != null && !aas.Submodels.IsNullOrEmpty())
                            {
                                var smFound = aas.Submodels.Exists(s => s.GetAsIdentifier().Equals(newSubmodel.Id));
                                if(smFound)
                                {
                                    foundAAS.Add(aas);
                                }
                            }
                        }
                        if(foundAAS.Any())
                        {
                            foundPackages.Add(i, foundAAS);
                        }
                    }
                }
            }

            if(foundPackages.Count > 0)
            {
                foreach (var package in foundPackages)
                {
                    var packageIndex = package.Key;
                    _packages[packageIndex].AasEnv!.Submodels ??= new List<ISubmodel>();
                    _packages[packageIndex].AasEnv!.Submodels!.Add(newSubmodel);

                    var timeStamp = DateTime.UtcNow;
                    newSubmodel.SetAllParents(timeStamp);
                    newSubmodel.TimeStampCreate = timeStamp;
                    newSubmodel.SetTimeStamp(timeStamp);

                    foreach(var aas in package.Value)
                    {
                        aas.SetTimeStamp(timeStamp);
                    }
                    _packages[packageIndex].setWrite(true);
                }
                Program.signalNewData(2);
                return newSubmodel;
            }

            //No linked AAS found and No package found where submodel is present
            //add the submodel to empty AAS
            if (foundPackages.IsNullOrEmpty() && !found)
            {
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
            //Submodel is already present in one or more packages and is also linked to shells
            //Thus, no need to add to any empty package as well.
            else
            {
                throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
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