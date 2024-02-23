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
    public class AdminShellPackageEnvironmentServiceDB : IAdminShellPackageEnvironmentService
    {
        private readonly IAppLogger<AdminShellPackageEnvironmentService> _logger;
        private readonly Lazy<IAssetAdministrationShellService> _aasService;
        private IDatabase _database;
        private AdminShellPackageEnv[] _packages;

        public AdminShellPackageEnvironmentServiceDB(IAppLogger<AdminShellPackageEnvironmentService> logger, Lazy<IAssetAdministrationShellService> aasService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aasService = aasService;
            //_packages = Program.env;
            _database = Program.database;
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
            var timeStamp = DateTime.UtcNow;
            body.TimeStampCreate = timeStamp;
            body.SetTimeStamp(timeStamp);
            _database.WriteDBAssetAdministrationShell(body);
            Program.signalNewData(2);
            return body;
        }

        public void DeleteAssetAdministrationShell(int packageIndex, IAssetAdministrationShell aas)
        {
            bool deleted = _database.DeleteDBAssetAdministrationShell(aas);
            if (deleted)
            {
                _logger.LogDebug($"Deleted Asset Administration Shell with id {aas.Id}");
                Program.signalNewData(2);
            }
            else
            {
                _logger.LogError($"Could not delete Asset Administration Shell with id {aas.Id}");
            }
        }

        public List<IAssetAdministrationShell> GetAllAssetAdministrationShells()
        {
            return _database.getLINQAssetAdministrationShell().Cast<IAssetAdministrationShell>().ToList();
        }

        public IAssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier, out int packageIndex)
        {
            packageIndex = -1; //TODO unused -> remove in the future
            var output = _database.getLINQAssetAdministrationShell()
                .Where(r => r.Id.Equals(aasIdentifier))
                .ToList();

            if (output.Any())
            {
                _logger.LogDebug($"Asset Administration Shell with id {aasIdentifier} found.");
                return output.First();
            }
            else
            {
                throw new NotFoundException($"Asset Administration Shell with id {aasIdentifier} not found.");
            }
        }

        public bool IsAssetAdministrationShellPresent(string aasIdentifier)
        {
            return _database.getLINQAssetAdministrationShell()
                .Where(r => r.Id.Equals(aasIdentifier))
                .ToList().Any();
        }

        private bool IsAssetAdministrationShellPresent(string aasIdentifier, out IAssetAdministrationShell output, out int packageIndex)
        {
            output = null; packageIndex = -1;
            //TODO unused -> remove in the future
            throw new NotImplementedException();

            Program.loadPackageForAas(aasIdentifier, out output, out packageIndex);

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
            //TODO Test -> should work -> function unused
            var timeStamp = DateTime.UtcNow;
            body.TimeStampCreate = timeStamp;
            body.SetTimeStamp(timeStamp); 
            
            _database.UpdateDBAssetAdministrationShellById(body, aasIdentifier);

            Program.signalNewData(1); //0 not working, hence 1 = same tree, structure may change
            _logger.LogDebug($"Successfully updated the AAS with requested AAS");
        }

        public void DeleteAssetInformationThumbnail(int packageIndex, IResource defaultThumbnail)
        {
            //TODO
            throw new NotImplementedException();
            //_packages[packageIndex].DeleteAssetInformationThumbnail(defaultThumbnail);
            Program.signalNewData(0);
        }

        public Stream GetAssetInformationThumbnail(int packageIndex)
        {
            //TODO
            throw new NotImplementedException(); 
            return _packages[packageIndex].GetLocalThumbnailStream();
        }

        public void UpdateAssetInformationThumbnail(IResource defaultThumbnail, Stream fileContent, int packageIndex)
        {
            //TODO
            throw new NotImplementedException();
            _packages[packageIndex].EmbeddAssetInformationThumbnail(defaultThumbnail, fileContent);
            Program.signalNewData(0);
        }
        #endregion

        #region Submodel
        public ISubmodel CreateSubmodel(ISubmodel newSubmodel, string aasIdentifier = null)
        {
            //Check if Submodel exists
            var found = IsSubmodelPresent(newSubmodel.Id);
            if (found)
            {
                throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
            }
            DateTime timeStamp = DateTime.UtcNow;
            //Check if corresponding AAS exist. If yes, then add to the same environment
            if (!string.IsNullOrEmpty(aasIdentifier))
            {
                IAssetAdministrationShell aas = GetAssetAdministrationShellById(aasIdentifier, out int packageIndex); //Throws Exception if not found
                newSubmodel.SetAllParents(timeStamp);

                aas.Submodels ??= new List<IReference>();
                aas.Submodels.Add(newSubmodel.GetReference());
                aas.SetTimeStamp(timeStamp);
                _database.UpdateDBAssetAdministrationShellById(aas, aasIdentifier); //Save aas Changes in db

                newSubmodel.TimeStampCreate = timeStamp;
                newSubmodel.SetTimeStamp(timeStamp);
                _database.WriteDBSubmodel(newSubmodel);

                AasxServer.Program.signalNewData(2);
                return newSubmodel; // TODO: jtikekar find proper solution
            }

            newSubmodel.TimeStampCreate = timeStamp;
            newSubmodel.SetTimeStamp(timeStamp);
            _database.WriteDBSubmodel(newSubmodel);
            Program.signalNewData(2);
            return newSubmodel;
        }
        public List<ISubmodel> GetAllSubmodels(IReference reqSemanticId = null, string idShort = null)
        {
            //Get All Submodels
            List<ISubmodel> output = _database.getLINQSubmodel().ToList().Cast<ISubmodel>().ToList();
            // TODO (jtikekar, 2023-09-04): uncomment and support
            //if (SecurityCheckTestOnly(s.IdShort, "", s))

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
        public ISubmodel GetSubmodelById(string submodelIdentifier, out int packageIndex)
        {
            packageIndex = -1; //TODO unused -> remove in the future
            List<Submodel> output = _database.getLINQSubmodel()
                .Where(r => r.Id.Equals(submodelIdentifier))
                .ToList();

            if (output.Any())
            {
                _logger.LogDebug($"Found the submodel with Id {submodelIdentifier}");
                return output.First();
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found.");
            }
        }
        public bool IsSubmodelPresent(string submodelIdentifier)
        {
            return _database.getLINQSubmodel()
                .Where(r => r.Id.Equals(submodelIdentifier))
                .ToList().Any();
        }
        public void ReplaceSubmodelById(string submodelIdentifier, ISubmodel newSubmodel)
        {
            var timeStamp = DateTime.UtcNow;
            newSubmodel.TimeStampCreate = timeStamp;
            newSubmodel.SetParentAndTimestamp(timeStamp);
            _database.UpdateDBSubmodelById(submodelIdentifier, newSubmodel);
            Program.signalNewData(1);
        }
        public void DeleteSubmodelById(string submodelIdentifier)
        {
            // Get all Submodels that Reference the given submodelIdentifier
            List<AssetAdministrationShell> aasToModify = _database.getLINQAssetAdministrationShell()
                .Where(aas => aas.Submodels
                    .Any(s => s.Keys
                        .Any(k => k.Value.Equals(submodelIdentifier))))
                .ToList();

            foreach (var aas in aasToModify)
            {
                _aasService.Value.DeleteSubmodelReferenceById(aas.Id, submodelIdentifier);
                //TODO Jonas Graubner make sure, that reference is deleted in DB
            }
            _database.DeleteDBSubmodelById(submodelIdentifier);
            _logger.LogDebug($"Deleted submodel with id {submodelIdentifier}.");
            AasxServer.Program.signalNewData(1);
        }
        public void DeleteSupplementaryFileInPackage(string submodelIdentifier, string filePath)
        {
            //TODO
            throw new NotImplementedException();
            _ = GetSubmodelById(submodelIdentifier, out int packageIndex);
            if (packageIndex != -1)
            {
                _packages[packageIndex].DeleteSupplementaryFile(filePath);
            }
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
                var timeStamp = DateTime.UtcNow; 
                body.TimeStampCreate = timeStamp;
                body.SetTimeStamp(timeStamp);
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
                var timeStamp = DateTime.UtcNow;
                newAas.TimeStampCreate = timeStamp;
                newAas.SetTimeStamp(timeStamp);
                Program.signalNewData(1);
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
