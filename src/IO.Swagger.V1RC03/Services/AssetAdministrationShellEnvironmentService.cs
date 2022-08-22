using AasCore.Aas3_0_RC02;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Extenstions;
using AdminShellNS;
using Extenstions;
using IO.Swagger.V1RC03.ApiModel;
using IO.Swagger.V1RC03.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using File = AasCore.Aas3_0_RC02.File;

namespace IO.Swagger.V1RC03.Services
{
    public class AssetAdministrationShellEnvironmentService : IAssetAdministrationShellEnvironmentService
    {
        private readonly IAppLogger<AssetAdministrationShellEnvironmentService> _logger;
        private AdminShellPackageEnv[] _packages;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        public AssetAdministrationShellEnvironmentService(IAppLogger<AssetAdministrationShellEnvironmentService> logger)
        {
            _logger = logger;
            _packages = AasxServer.Program.env;
        }

        #region AssetAdministrationShell

        public void UpdateSubmodelElementByPath(ISubmodelElement body, string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            if (string.IsNullOrEmpty(body.IdShort))
            {
                throw new NoIdentifierException("SubmodelElement");
            }

            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    UpdateSubmodelElementByPathSubmodelRepo(body, submodelIdentifier, idShortPath);
                }
            }
        }

        public void UpdateSubmodel(Submodel body, string aasIdentifier, string submodelIdentifier)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("Submodel");
            }

            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    UpdateSubmodelById(body, submodelIdentifier);
                }
            }
        }

        private bool IsSubmodelPresentInAAS(AssetAdministrationShell aas, string submodelIdentifier)
        {
            if (aas.Submodels.Any(s => s.Matches(submodelIdentifier)))
            {
                return true;
            }
            else
            {
                throw new NotFoundException($"SubmodelReference with id {submodelIdentifier} not found in AAS with id {aas.Id}");
            }
        }

        public void UpdateAssetInformation(AssetInformation body, string aasIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                aas.AssetInformation = body;
                AasxServer.Program.signalNewData(1);
            }
        }

        public void UpdateAssetAdministrationShellById(AssetAdministrationShell body, string aasIdentifier)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("AssetAdministrationShell");
            }

            var aas = GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null && packageIndex != -1)
            {
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Remove(aas);
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Add(body);
                AasxServer.Program.signalNewData(1);
            }
        }

        public Reference CreateSubmodelReference(Reference body, string aasIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);

            if (aas != null)
            {
                var found = aas.Submodels.Any(s => s.Matches(body));
                if (found)
                {
                    throw new DuplicateException($"Requested submodel reference already exists in AAS with id {aasIdentifier}");
                }
                else
                {
                    aas.Submodels ??= new List<Reference>();
                    aas.Submodels.Add(body);
                    return body;
                }
            }

            return null;
        }

        public ISubmodelElement CreateSubmodelElementByPath(ISubmodelElement body, string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            if (string.IsNullOrEmpty(body.IdShort))
            {
                throw new NoIdentifierException("SubmodelElement");
            }

            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    return CreateSubmodelElementByPathSubmodelRepo(body, submodelIdentifier, idShortPath);
                }
            }

            return null;
        }

        public ISubmodelElement CreateSubmodelElement(ISubmodelElement body, string aasIdentifier, string submodelIdentifier)
        {
            if (string.IsNullOrEmpty(body.IdShort))
            {
                throw new NoIdentifierException("SubmodelElement");
            }

            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    return CreateSubmodelElementSubmodelRepo(body, submodelIdentifier);
                }
            }

            return null;
        }

        public AssetAdministrationShell CreateAssetAdministrationShell(AssetAdministrationShell body)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("AssetAdministrationShell");
            }

            //Check if AAS exists
            var found = IsAssetAdministrationShellPresent(body.Id, out _, out _);
            if (found)
            {
                throw new DuplicateException($"AssetAdministrationShell with id {body.Id} already exists.");
            }

            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {

                _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells.Add(body);
                AasxServer.Program.signalNewData(2);
                return _packages[emptyPackageIndex].AasEnv.AssetAdministrationShells[0]; //Considering it is being added to empty package.
            }
            else
            {
                throw new Exception("No empty environment package available in the server.");
            }
        }



        public OperationResult GetOperationAsyncResult(string aasIdentifier, string submodelIdentifier, string idShortPath, string handleId)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    return GetOperationAsyncResultSubmodelRepo(submodelIdentifier, idShortPath, handleId);
                }
            }

            return null;
        }

        public string GetFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize)
        {
            content = null;
            fileSize = 0;
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    return GetFileByPathSubmodelRepo(submodelIdentifier, idShortPath, out content, out fileSize);
                }
            }

            return null;
        }

        public void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    DeleteSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath);
                }
            }
        }

        public ISubmodelElement GetSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                if (IsSubmodelPresentInAAS(aas, submodelIdentifier))
                {
                    var output = GetSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath, out _);
                    return output;
                }
            }

            return null;
        }

        public Submodel GetSubmodel(string aasIdentifier, string submodelIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                var submodelRefs = aas.Submodels.Where(s => s.Matches(submodelIdentifier));
                if (submodelRefs.Any())
                {
                    return GetSubmodelById(submodelIdentifier, out _);
                }
                else
                {
                    throw new NotFoundException($"SubmodelReference with id {submodelIdentifier} not found in AAS with id {aasIdentifier}");
                }
            }

            return null;
        }

        public void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                var submodelRefs = aas.Submodels.Where(s => s.Matches(submodelIdentifier));
                if (submodelRefs.Any())
                {
                    aas.Submodels.Remove(submodelRefs.First());
                    AasxServer.Program.signalNewData(1);
                }
                else
                {
                    throw new NotFoundException($"SubmodelReference with id {submodelIdentifier} not found in AAS with id {aasIdentifier}");
                }
            }
        }

        public void DeleteAssetAdministrationShellById(string aasIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if ((aas != null) && (packageIndex != -1))
            {
                _packages[packageIndex].AasEnv.AssetAdministrationShells.Remove(aas);
                if (_packages[packageIndex].AasEnv.AssetAdministrationShells.Count == 0)
                {
                    _packages[packageIndex] = null;             //TODO: jtikekar what about submodels?
                }
                AasxServer.Program.signalNewData(2);
            }
            else
            {
                throw new Exception("Unexpected error occurred.");
            }
        }

        public AssetInformation GetAssetInformationFromAas(string aasIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                return aas.AssetInformation;
            }

            return null;
        }

        public List<Reference> GetAllSubmodelReferences(string decodedAasId)
        {
            var aas = GetAssetAdministrationShellById(decodedAasId, out _);

            if (aas != null)
            {
                return aas.Submodels;
            }

            return null;
        }

        /// <summary>
        /// Retrieves all AASs from the server
        /// </summary>
        /// <returns></returns>
        public List<AssetAdministrationShell> GetAllAssetAdministrationShells(List<SpecificAssetId> assetIds = null, string idShort = null)
        {
            var output = new List<AssetAdministrationShell>();

            //Get All AASs
            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        output.AddRange(env.AssetAdministrationShells);
                    }
                }
            }

            if (output.Any())
            {
                //Filter AASs based on IdShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    output = output.Where(a => a.IdShort.Equals(idShort)).ToList();
                    if (output.IsNullOrEmpty())
                    {
                        throw new NotFoundException($"AssetAdministrationShells with IdShort {idShort} Not Found.");
                    }
                }

                //Filter based on AssetId
                if (assetIds != null && assetIds.Count != 0)
                {
                    var aasList = new List<AssetAdministrationShell>();
                    foreach (var assetId in assetIds)
                    {
                        aasList.AddRange(output.Where(a => a.AssetInformation.SpecificAssetIds.Contains(assetId)).ToList());
                    }

                    if (aasList.Any())
                    {
                        return aasList;
                    }
                    else
                    {
                        throw new NotFoundException($"AssetAdministrationShells with requested SpecificAssetIds Not Found.");
                    }
                }
            }

            return output;
        }

        public List<ISubmodelElement> GetAllSubmodelElements(string aasIdentifier, string submodelIdentifier)
        {
            List<ISubmodelElement> output = null;
            //Find AAS
            var aas = GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                //Check if AAS consist the requested submodel
                if (aas.Submodels.Where(s => s.Matches(submodelIdentifier)).IsNullOrEmpty())
                {
                    throw new NotFoundException($"Requested submodel: {submodelIdentifier} not found in AAS: {aasIdentifier}");
                }

                output = GetAllSubmodelElementsFromSubmodel(submodelIdentifier);
            }

            return output;
        }

        public AssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier, out int packageIndex)
        {
            bool found = IsAssetAdministrationShellPresent(aasIdentifier, out AssetAdministrationShell output, out packageIndex);

            if (found)
            {
                return output;
            }
            else
            {
                throw new NotFoundException($"AssetAdministrationShell with id {aasIdentifier} not found.");
            }
        }

        private bool IsAssetAdministrationShellPresent(string aasIdentifier, out AssetAdministrationShell output, out int packageIndex)
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

        #endregion

        #region ConceptDescription

        public void UpdateConceptDescriptionById(ConceptDescription body, string cdIdentifier)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("ConceptDescription");
            }

            var conceptDescription = GetConceptDescriptionById(cdIdentifier, out int packageIndex);
            if (conceptDescription != null)
            {
                int cdIndex = _packages[packageIndex].AasEnv.ConceptDescriptions.IndexOf(conceptDescription);
                _packages[packageIndex].AasEnv.ConceptDescriptions.Remove(conceptDescription);
                _packages[packageIndex].AasEnv.ConceptDescriptions.Insert(cdIndex, body);
                AasxServer.Program.signalNewData(0);
            }
        }

        public ConceptDescription CreateConceptDescription(ConceptDescription body)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("ConceptDescription");
            }

            //Check if AAS exists
            var found = IsConceptDescriptionPresent(body.Id, out _, out _);
            if (found)
            {
                throw new DuplicateException($"ConceptDescription with id {body.Id} already exists.");
            }

            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {

                _packages[emptyPackageIndex].AasEnv.ConceptDescriptions.Add(body);
                AasxServer.Program.signalNewData(2);
                return _packages[emptyPackageIndex].AasEnv.ConceptDescriptions[0]; //Considering it is being added to empty package.
            }
            else
            {
                throw new Exception("No empty environment package available in the server.");
            }
        }

        public void DeleteConceptDescriptionById(string cdIdentifier)
        {
            var conceptDescription = GetConceptDescriptionById(cdIdentifier, out int packageIndex);
            if ((conceptDescription != null) && (packageIndex != -1))
            {
                _packages[packageIndex].AasEnv.ConceptDescriptions.Remove(conceptDescription);
                AasxServer.Program.signalNewData(1);
            }
            else
            {
                throw new Exception("Unexpected error occurred.");
            }
        }

        public ConceptDescription GetConceptDescriptionById(string cdIdentifier, out int packageIndex)
        {
            bool found = IsConceptDescriptionPresent(cdIdentifier, out ConceptDescription output, out packageIndex);
            if (found)
            {
                return output;
            }
            else
            {
                throw new NotFoundException($"ConceptDescription with id {cdIdentifier} not found.");
            }
        }

        private bool IsConceptDescriptionPresent(string cdIdentifier, out ConceptDescription output, out int packageIndex)
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

        /// <summary>
        /// Retrieves all concept descriptions
        /// </summary>
        /// <param name="idShort"></param>
        /// <param name="reqIsCaseOf"></param>
        /// <param name="reqDataSpecificationRef"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<ConceptDescription> GetAllConceptDescriptions(string idShort = null, Reference reqIsCaseOf = null, Reference reqDataSpecificationRef = null)
        {
            var output = new List<ConceptDescription>();

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

            if (output.Any())
            {
                //Filter AASs based on IdShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    var cdList = output.Where(cd => cd.IdShort.Equals(idShort)).ToList();
                    if (cdList.IsNullOrEmpty())
                    {
                        throw new NotFoundException($"Concept Description with IdShort {idShort} Not Found.");
                    }
                    else
                    {
                        output = cdList;
                    }
                }

                //Filter based on IsCaseOf
                if (reqIsCaseOf != null)
                {
                    var cdList = new List<ConceptDescription>();
                    foreach (var conceptDescription in output)
                    {
                        if (!conceptDescription.IsCaseOf.IsNullOrEmpty())
                        {
                            foreach (var reference in conceptDescription.IsCaseOf)
                            {
                                if (reference != null && reference.Matches(reqIsCaseOf))
                                {
                                    cdList.Add(conceptDescription);
                                    break;
                                }
                            }
                        }
                    }
                    if (cdList.IsNullOrEmpty())
                    {
                        throw new NotFoundException($"Concept Description with requested IsCaseOf Not Found.");
                    }
                    else
                    {
                        output = cdList;
                    }

                }

                //Filter based on DataSpecificationRef
                if (reqDataSpecificationRef != null)
                {
                    var cdList = new List<ConceptDescription>();
                    foreach (var conceptDescription in output)
                    {
                        if (!conceptDescription.DataSpecifications.IsNullOrEmpty())
                        {
                            foreach (var reference in conceptDescription.DataSpecifications)
                            {
                                if (reference != null && reference.Matches(reqDataSpecificationRef))
                                {
                                    cdList.Add(conceptDescription);
                                    break;
                                }
                            }
                        }
                    }
                    if (cdList.IsNullOrEmpty())
                    {
                        throw new NotFoundException($"Concept Description with requested DataSpecificationReference Not Found.");
                    }
                    else
                    {
                        output = cdList;
                    }
                }
            }

            return output;
        }

        #endregion


        #region Submodel

        public void UpdateSubmodelElementByPathSubmodelRepo(ISubmodelElement body, string submodelIdentifier, string idShortPath)
        {
            if (string.IsNullOrEmpty(body.IdShort))
            {
                throw new NoIdentifierException("SubmodelElement");
            }

            var submodelElement = GetSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath, out object smeParent);
            if (submodelElement != null && smeParent != null)
            {
                if (smeParent is SubmodelElementCollection collection)
                {
                    var smeIndex = collection.Value.IndexOf(submodelElement);
                    collection.Value.Remove(submodelElement);
                    collection.Value.Insert(smeIndex, body);
                }
                else if (smeParent is SubmodelElementList list)
                {
                    var smeIndex = list.Value.IndexOf(submodelElement);
                    list.Value.Remove(submodelElement);
                    list.Value.Insert(smeIndex, body);
                }
                //Added support for submodel here, as no other api found for this functionality
                else if (smeParent is Submodel submodel)
                {
                    var smeIndex = submodel.SubmodelElements.IndexOf(submodelElement);
                    submodel.SubmodelElements.Remove(submodelElement);
                    submodel.SubmodelElements.Insert(smeIndex, body);
                }

                AasxServer.Program.signalNewData(1);
            }
        }

        public void UpdateSubmodelById(Submodel body, string submodelIdentifier)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("Submodel");
            }

            var submodel = GetSubmodelById(submodelIdentifier, out int packageIndex);
            if (submodel != null)
            {
                _packages[packageIndex].AasEnv.Submodels.Remove(submodel);
                _packages[packageIndex].AasEnv.Submodels.Add(body);
                AasxServer.Program.signalNewData(1);
            }
        }

        public ISubmodelElement CreateSubmodelElementByPathSubmodelRepo(ISubmodelElement body, string submodelIdentifier, string idShortPath)
        {
            if (string.IsNullOrEmpty(body.IdShort))
            {
                throw new NoIdentifierException("SubmodelElement");
            }

            var newIdShortPath = idShortPath + "." + body.IdShort;
            var found = IsSubmodelElementPresent(submodelIdentifier, newIdShortPath, out _, out object smeParent);
            if (found)
            {
                throw new DuplicateException($"SubmodelElement with IdShort {body.IdShort} already exists.");
            }
            else
            {
                if (smeParent != null && smeParent is Submodel submodel)
                {
                    submodel.SubmodelElements ??= new List<ISubmodelElement>();

                    submodel.SubmodelElements.Add(body);
                }
                else if (smeParent != null && smeParent is SubmodelElementCollection collection)
                {
                    collection.Value ??= new List<ISubmodelElement>();

                    collection.Value.Add(body);
                }
                else if (smeParent != null && smeParent is SubmodelElementList list)
                {
                    list.Value ??= new List<ISubmodelElement>();

                    list.Value.Add(body);
                }
                else if (smeParent != null && smeParent is Entity entity)
                {
                    entity.Statements ??= new List<ISubmodelElement>();

                    entity.Statements.Add(body);
                }
                else if (smeParent != null && smeParent is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    annotatedRelationshipElement.Annotations ??= new List<IDataElement>();

                    annotatedRelationshipElement.Annotations.Add((IDataElement)body);
                }

                AasxServer.Program.signalNewData(1);

                return body;
            }

            return null;
        }

        public ISubmodelElement CreateSubmodelElementSubmodelRepo(ISubmodelElement body, string submodelIdentifier)
        {
            if (string.IsNullOrEmpty(body.IdShort))
            {
                throw new NoIdentifierException("SubmodelElement");
            }

            var found = IsSubmodelElementPresent(submodelIdentifier, body.IdShort, out _, out object smeParent);
            if (found)
            {
                throw new DuplicateException($"SubmodelElement with IdShort {body.IdShort} already exists.");
            }
            else
            {
                if (smeParent != null && smeParent is Submodel submodel)
                {
                    submodel.SubmodelElements ??= new List<ISubmodelElement>();

                    submodel.SubmodelElements.Add(body);

                    AasxServer.Program.signalNewData(1);

                    return body;
                }
            }

            return null;

        }

        public Submodel CreateSubmodel(Submodel body)
        {
            if (string.IsNullOrEmpty(body.Id))
            {
                throw new NoIdentifierException("Submodel");
            }

            //Check if AAS exists
            var found = IsSubmodelPresent(body.Id, out _, out _);
            if (found)
            {
                throw new DuplicateException($"Submodel with id {body.Id} already exists.");
            }

            if (EmptyPackageAvailable(out int emptyPackageIndex))
            {

                _packages[emptyPackageIndex].AasEnv.Submodels.Add(body);
                AasxServer.Program.signalNewData(2);
                return _packages[emptyPackageIndex].AasEnv.Submodels[0]; //Considering it is being added to empty package.
            }
            else
            {
                throw new Exception("No empty environment package available in the server.");
            }
        }

        public Submodel GetSubmodelById(string submodelIdentifier, out int packageIndex)
        {
            bool found = IsSubmodelPresent(submodelIdentifier, out Submodel output, out packageIndex);
            if (found)
            {
                return output;
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} not found.");
            }
        }

        private bool IsSubmodelPresent(string submodelIdentifier, out Submodel output, out int packageIndex)
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

        public List<ISubmodelElement> GetAllSubmodelElementsFromSubmodel(string submodelIdentifier)
        {
            List<ISubmodelElement> output = null;
            //Find Submodel
            var submodel = GetSubmodelById(submodelIdentifier, out _);
            if (submodel != null)
            {
                output = submodel.SubmodelElements;
            }

            return output;
        }

        public List<Submodel> GetAllSubmodels(Reference reqSemanticId = null, string idShort = null)
        {
            List<Submodel> output = new List<Submodel>();

            //Get All Submodels
            foreach (var package in _packages)
            {
                if (package != null)
                {
                    var env = package.AasEnv;
                    if (env != null)
                    {
                        output.AddRange(env.Submodels);
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

        public void DeleteSubmodelById(string submodelIdentifier)
        {
            var submodel = GetSubmodelById(submodelIdentifier, out int packageIndex);
            if ((submodel != null) && (packageIndex != -1))
            {
                _packages[packageIndex].AasEnv.Submodels.Remove(submodel);

                //Delete submodel reference from AAS
                foreach (var aas in _packages[packageIndex].AasEnv.AssetAdministrationShells)
                {
                    DeleteSubmodelReferenceById(aas.Id, submodelIdentifier);
                }

                AasxServer.Program.signalNewData(1);  //TODO jtikekar : may be not needed
            }
            else
            {
                throw new Exception("Unexpected error occurred.");
            }
        }

        public ISubmodelElement GetSubmodelElementByPathSubmodelRepo(string submodelIdentifier, string idShortPath, out object smeParent)
        {
            bool found = IsSubmodelElementPresent(submodelIdentifier, idShortPath, out ISubmodelElement output, out smeParent);

            if (found)
            {
                return output;
            }
            else
            {
                throw new NotFoundException($"Requested submodel element {idShortPath} NOT found.");
            }
        }

        private bool IsSubmodelElementPresent(string submodelIdentifier, string idShortPath, out ISubmodelElement output, out object smeParent)
        {
            output = null;
            smeParent = null;
            var submodel = GetSubmodelById(submodelIdentifier, out _);

            if (submodel != null)
            {
                output = GetSubmodelElementByPath(submodel, idShortPath, out object parent);
                smeParent = parent;
                if (output != null)
                {
                    return true;
                }

            }

            return false;
        }

        //TODO:jtikekar refactor
        private ISubmodelElement GetSubmodelElementByPath(object parent, string idShortPath, out object outParent)
        {
            outParent = parent;
            if (idShortPath.Contains('.'))
            {
                string[] idShorts = idShortPath.Split('.', 2);
                if (parent is Submodel submodel)
                {
                    var submodelElement = submodel.FindSubmodelElementByIdShort(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    var submodelElement = collection.FindFirstIdShortAs<ISubmodelElement>(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
                else if (parent is SubmodelElementList list)
                {
                    var submodelElement = list.FindFirstIdShortAs<ISubmodelElement>(idShorts[0]);
                    if (submodelElement != null)
                    {
                        return GetSubmodelElementByPath(submodelElement, idShorts[1], out outParent);
                    }
                }
            }
            else
            {
                if (parent is Submodel submodel)
                {
                    var submodelElement = submodel.FindSubmodelElementByIdShort(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else if (parent is SubmodelElementCollection collection)
                {
                    var submodelElement = collection.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
                else if (parent is SubmodelElementList list)
                {
                    var submodelElement = list.FindFirstIdShortAs<ISubmodelElement>(idShortPath);
                    if (submodelElement != null)
                    {
                        return submodelElement;
                    }
                }
            }
            return null;
        }

        public void DeleteSubmodelElementByPathSubmodelRepo(string submodelIdentifier, string idShortPath)
        {
            var submodelElement = GetSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath, out object smeParent);
            if (submodelElement != null)
            {
                if (smeParent is SubmodelElementCollection parentCollection)
                {
                    parentCollection.Value.Remove(submodelElement);
                }
                else if (smeParent is SubmodelElementList parentList)
                {
                    parentList.Value.Remove(submodelElement);
                }
                else if (smeParent is Submodel parentSubmodel)
                {
                    parentSubmodel.SubmodelElements.Remove(submodelElement);
                }

                AasxServer.Program.signalNewData(1);
            }
        }

        public string GetFileByPathSubmodelRepo(string submodelIdentifier, string idShortPath, out byte[] byteArray, out long fileSize)
        {
            byteArray = null;
            string fileName = null;
            fileSize = 0;

            _ = GetSubmodelById(submodelIdentifier, out int packageIndex);

            var fileElement = GetSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath, out _);

            if (fileElement != null)
            {
                if (fileElement is File file)
                {
                    fileName = file.Value;

                    Stream stream = _packages[packageIndex].GetLocalStreamFromPackage(fileName);
                    byteArray = stream.ToByteArray();
                    fileSize = byteArray.Length;
                }
                else
                {
                    throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                }
            }

            return fileName;
        }

        public OperationResult GetOperationAsyncResultSubmodelRepo(string decodedSubmodelId, string idShortPath, string handleId)
        {
            var operationElement = GetSubmodelElementByPathSubmodelRepo(decodedSubmodelId, idShortPath, out _);

            if (operationElement != null)
            {
                if (operationElement is Operation)
                {
                    TestData.opResultAsyncDict.TryGetValue(handleId, out OperationResult operationResult);
                    return operationResult;
                }
                else
                {
                    throw new NotFoundException($"Submodel element {operationElement.IdShort} is not of type Operation.");
                }
            }

            return null;
        }

        public OperationResult InvokeOperationSubmodelRepo(string submodelIdentifier, string idShortPath, OperationRequest operationRequest)
        {
            var operationElement = GetSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath, out _);

            if (operationElement != null)
            {
                if (operationElement is Operation operation)
                {
                    CheckOperationVariables(operation, operationRequest);
                    OperationResult operationResult = new OperationResult();
                    //Check the qualifier for demo
                    if (operation.FindQualifierOfType("Demo") != null)
                    {
                        operationResult.OutputArguments = new List<OperationVariable>();
                        operationResult.OutputArguments.Add(new OperationVariable(new Property(DataTypeDefXsd.String, idShort: "DemoOutputArgument")));
                        operationResult.ExecutionState = ExecutionState.CompletedEnum;
                        Result result = new Result();
                        result.Success = true;
                        operationResult.ExecutionResult = result;
                        operationResult.RequestId = operationRequest.RequestId;
                    }
                    return operationResult;
                }
                else
                {
                    throw new NotFoundException($"Submodel element {operationElement.IdShort} is not of type Operation.");
                }
            }

            return null;
        }

        private void CheckOperationVariables(Operation operation, OperationRequest operationRequest)
        {
            if (operation.InputVariables.Count != operationRequest.InputArguments.Count)
            {
                throw new OperationVariableException($"Incorrect number of InputVariables in OperationRequest.");
            }
            else if (operation.InoutputVariables.Count != operationRequest.InoutputArguments.Count)
            {
                throw new OperationVariableException($"Incorrect number of InOutputVariables in OperationRequest.");
            }
        }

        public OperationHandle InvokeOperationAsyncSubmodelRepo(string submodelIdentifier, string idShortPath, OperationRequest operationRequest)
        {
            var operationElement = GetSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath, out _);

            if (operationElement != null)
            {
                if (operationElement is Operation operation)
                {
                    CheckOperationVariables(operation, operationRequest);
                    OperationHandle operationHandle = new OperationHandle();
                    //Check the qualifier for demo
                    if (operation.FindQualifierOfType("Demo") != null)
                    {
                        operationHandle.RequestId = operationRequest.RequestId;
                        operationHandle.HandleId = Guid.NewGuid().ToString();
                        TestData.InvokeTestOperation(operationHandle);
                    }

                    return operationHandle;
                }
                else
                {
                    throw new NotFoundException($"Submodel element {operationElement.IdShort} is not of type Operation.");
                }
            }

            return null;
        }





        #endregion

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











    }
}
