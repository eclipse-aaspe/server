/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/


using AasxServer;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AdminShellNS;
using AdminShellNS.Extensions;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AasxServerStandardBib.Services
{
    public class AssetAdministrationShellService : IAssetAdministrationShellService
    {
        private readonly IAppLogger<AssetAdministrationShellService> _logger;
        private readonly IAdminShellPackageEnvironmentService _packageEnvService;
        private readonly IMetamodelVerificationService _verificationService;
        private readonly ISubmodelService _submodelService;
        private AdminShellPackageEnv[] _packages;

        public AssetAdministrationShellService(IAppLogger<AssetAdministrationShellService> logger, IAdminShellPackageEnvironmentService packageEnvService, IMetamodelVerificationService verificationService, ISubmodelService submodelService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _packageEnvService = packageEnvService;
            _verificationService = verificationService;
            _submodelService = submodelService;
            _packages = Program.env;
        }

        public IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(body);

            var found = _packageEnvService.IsAssetAdministrationShellPresent(body.Id);
            if (found)
            {
                _logger.LogDebug($"Cannot create requested AAS !!");
                throw new DuplicateException($"AssetAdministrationShell with id {body.Id} already exists.");
            }

            var output = _packageEnvService.CreateAssetAdministrationShell(body);

            return output;
        }

        public IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier)
        {
            IReference output = null;
            //Verify request body
            _verificationService.VerifyRequestBody(body);

            // TODO (jtikekar, 2023-09-04): to check if submodel with requested submodelReference exists in the server
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);

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



        public void DeleteAssetAdministrationShellById(string aasIdentifier)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);

            if (aas != null && packageIndex != -1)
            {
                _packageEnvService.DeleteAssetAdministrationShell(packageIndex, aas);
            }
        }

        public void DeleteFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            if (IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier))
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.DeleteFileByPath(submodelIdentifier, idShortPath);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier)
        {
            if (IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier))
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                //delete the submodel first, this should eventually delete the submodel reference from all the AASs
                _submodelService.DeleteSubmodelById(submodelIdentifier);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            if (IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier))
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.DeleteSubmodelElementByPath(submodelIdentifier, idShortPath);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }

        }



        public void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);

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



        public List<IAssetAdministrationShell> GetAllAssetAdministrationShells(List<ISpecificAssetId> assetIds = null, string? idShort = null)
        {
            var output = _packageEnvService.GetAllAssetAdministrationShells();

            //Apply filters

            if (output.Count != 0)
            {
                if (!string.IsNullOrEmpty(idShort))
                {
                    _logger.LogDebug($"Filtering AASs with idShort {idShort}.");
                    output = output.Where(a => a.IdShort.Equals(idShort)).ToList();
                    if (output.IsNullOrEmpty())
                    {
                        _logger.LogInformation($"No AAS with idShhort {idShort} found.");
                    }
                }

                if (!assetIds.IsNullOrEmpty())
                {
                    _logger.LogDebug($"Filtering AASs with requested assetIds.");
                    var aasList = new List<IAssetAdministrationShell>();
                    foreach (var assetId in assetIds)
                    {
                        var result = new List<IAssetAdministrationShell>();
                        if (!string.IsNullOrEmpty(assetId.Name))
                        {
                            if (assetId.Name.Equals("globalAssetId", StringComparison.OrdinalIgnoreCase))
                            {
                                result = output.Where(a => a.AssetInformation.GlobalAssetId!.Equals(assetId.Value)).ToList();
                            }
                            else
                            {
                                result = output.Where(a => a.AssetInformation.SpecificAssetIds!.ContainsSpecificAssetId(assetId)).ToList();
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("The attribute Name cannot be null in the requested assetId.");
                        }

                        if (result.Count != 0)
                        {
                            aasList.AddRange(result);
                        }
                    }

                    if (aasList.Count == 0)
                    {
                        _logger.LogInformation($"No AAS with requested assetId found.");
                    }
                    output = aasList;
                }
            }

            return output;
        }

        public List<ISubmodelElement> GetAllSubmodelElements(string aasIdentifier, string submodelIdentifier)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                return _submodelService.GetAllSubmodelElements(submodelIdentifier);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public List<IReference> GetAllSubmodelReferencesFromAas(string aasIdentifier)
        {
            List<IReference> output = new List<IReference>();
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);

            if (aas != null)
            {
                if (aas.Submodels.IsNullOrEmpty())
                {
                    _logger.LogDebug($"No submodels present in the AAS with Id {aasIdentifier}");
                }

                output = aas.Submodels;
            }

            return output;
        }

        public IAssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier)
        {
            return _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);
        }

        public IAssetInformation GetAssetInformation(string aasIdentifier)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);
            return aas.AssetInformation;
        }

        public void DeleteThumbnail(string aasIdentifier)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null)
            {
                if (aas.AssetInformation != null)
                {
                    if (aas.AssetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(aas.AssetInformation.DefaultThumbnail.Path))
                    {
                        _packageEnvService.DeleteAssetInformationThumbnail(packageIndex, aas.AssetInformation.DefaultThumbnail);
                    }
                    else
                    {
                        throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                    }
                }
            }
        }

        public string GetThumbnail(string aasIdentifier, out byte[] byteArray, out long fileSize)
        {
            string fileName = null;
            byteArray = null;
            fileSize = 0;
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null)
            {
                if (aas.AssetInformation != null)
                {
                    if (aas.AssetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(aas.AssetInformation.DefaultThumbnail.Path))
                    {
                        fileName = aas.AssetInformation.DefaultThumbnail.Path;

                        Stream stream = _packageEnvService.GetAssetInformationThumbnail(packageIndex);
                        byteArray = stream.ToByteArray();
                        fileSize = byteArray.Length;

                        _logger.LogDebug($"Updated the thumbnail in AAS with Id {aasIdentifier}");
                    }
                    else
                    {
                        throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
                    }
                }
                else
                {
                    throw new NotFoundException($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
                }
            }

            return fileName;
        }

        public void UpdateAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(body);

            _packageEnvService.UpdateAssetAdministrationShellById(body, aasIdentifier);

        }

        public void UpdateAssetInformation(AssetInformation body, string aasIdentifier)
        {
            _verificationService.VerifyRequestBody(body);
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                aas.AssetInformation = body;
                Program.signalNewData(0);

                _logger.LogDebug($"AssetInformation from AAS with id {aasIdentifier} updated successfully.");
            }
        }

        public void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, Stream fileContent)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out int packageIndex);
            if (aas != null)
            {
                if (aas.AssetInformation != null)
                {
                    var asset = aas.AssetInformation;

                    if (string.IsNullOrEmpty(contentType))
                    {
                        contentType = "application/octet-stream";
                    }

                    if (asset.DefaultThumbnail == null)
                    {
                        //If thumbnail is not set, set to default path 
                        asset.DefaultThumbnail ??= new Resource(Path.Combine("/aasx/files", fileName).Replace('/', Path.DirectorySeparatorChar), contentType);
                    }
                    else
                    {
                        asset.DefaultThumbnail.Path = asset.DefaultThumbnail.Path.Replace('/', Path.DirectorySeparatorChar);
                    }

                    _packageEnvService.UpdateAssetInformationThumbnail(asset.DefaultThumbnail, fileContent, packageIndex);


                }
                else
                {
                    throw new NotFoundException($"AssetInformation is NULL in requested AAS with id {aasIdentifier}");
                }
            }
        }

        #region PrivateMethods

        private bool IsSubmodelPresentWithinAAS(string aasIdentifier, string submodelIdentifier)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                foreach (var submodelReference in aas.Submodels)
                {
                    if (submodelReference.GetAsExactlyOneKey().Value.Equals(submodelIdentifier))
                    { return true; }
                }
            }

            return false;
        }

        public string GetFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize)
        {
            content = null;
            fileSize = 0;
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                return _submodelService.GetFileByPath(submodelIdentifier, idShortPath, out content, out fileSize);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }

        }

        public ISubmodel GetSubmodelById(string aasIdentifier, string submodelIdentifier)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                return _submodelService.GetSubmodelById(submodelIdentifier);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public ISubmodelElement GetSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                return _submodelService.GetSubmodelElementByPath(submodelIdentifier, idShortPath);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public ISubmodelElement CreateSubmodelElement(string aasIdentifier, string submodelIdentifier, bool first, ISubmodelElement newSubmodelElement)
        {
            var smFound = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (smFound)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                return _submodelService.CreateSubmodelElement(submodelIdentifier, newSubmodelElement, first);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public ISubmodelElement CreateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, bool first, ISubmodelElement newSubmodelElement)
        {
            var smFound = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (smFound)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                return _submodelService.CreateSubmodelElementByPath(submodelIdentifier, idShortPath, first, newSubmodelElement);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public void ReplaceAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas)
        {
            _verificationService.VerifyRequestBody(newAas);
            _packageEnvService.ReplaceAssetAdministrationShellById(aasIdentifier, newAas);
        }

        public void ReplaceAssetInformation(string aasIdentifier, IAssetInformation newAssetInformation)
        {
            var aas = _packageEnvService.GetAssetAdministrationShellById(aasIdentifier, out _);
            if (aas != null)
            {
                _verificationService.VerifyRequestBody(newAssetInformation);
                aas.AssetInformation = newAssetInformation;
                Program.signalNewData(0);
            }
        }

        public void ReplaceSubmodelById(string aasIdentifier, string submodelIdentifier, Submodel newSubmodel)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.ReplaceSubmodelById(submodelIdentifier, newSubmodel);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }

        }

        public void ReplaceSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement newSme)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.ReplaceSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public void UpdateSubmodelById(string aasIdentifier, string submodelIdentifier, ISubmodel newSubmodel)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.UpdateSubmodelById(submodelIdentifier, newSubmodel);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement newSme)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.UpdateSubmodelElementByPath(submodelIdentifier, idShortPath, newSme);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        public void ReplaceFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream fileContent)
        {
            var found = IsSubmodelPresentWithinAAS(aasIdentifier, submodelIdentifier);
            if (found)
            {
                _logger.LogDebug($"Found submodel with id {submodelIdentifier} in AAS with id {aasIdentifier}");
                _submodelService.ReplaceFileByPath(submodelIdentifier, idShortPath, fileName, contentType, fileContent);
            }
            else
            {
                throw new NotFoundException($"Submodel with id {submodelIdentifier} NOT found in AAS with id {aasIdentifier}");
            }
        }

        #endregion
    }
}
