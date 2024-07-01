/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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
using AasxServerStandardBib.Transformers;
using Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Services
{
    public class SubmodelService : ISubmodelService
    {
        private readonly IAppLogger<SubmodelService> _logger;
        private readonly IAdminShellPackageEnvironmentService _packageEnvService;
        private readonly IMetamodelVerificationService _verificationService;
        private readonly IIdShortPathParserService _pathParserService;
        private const string SML_IdShortPath_Regex = @"\[(?<numbers>[\d]+)\]";

        public SubmodelService(IAppLogger<SubmodelService> logger, IAdminShellPackageEnvironmentService packageEnvService, IMetamodelVerificationService verificationService, 
        IIdShortPathParserService pathParserService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _packageEnvService = packageEnvService ?? throw new ArgumentNullException(nameof(_packageEnvService));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(_verificationService));
            _pathParserService = pathParserService ?? throw new ArgumentNullException(nameof(pathParserService));
        }

        #region PrivateMethods

        private bool IsSubmodelElementPresent(string submodelIdentifier, string idShortPath, out ISubmodelElement output, out IReferable smeParent)
        {
            output = null;
            smeParent = null;
            var submodel = _packageEnvService.GetSubmodelById(submodelIdentifier, out _);

            if (submodel != null)
            {
                output = GetSubmodelElementByPath(submodel, idShortPath, out IReferable parent);
                smeParent = parent;
                if (output != null)
                {
                    _logger.LogInformation($"Found SubmodelElement at {idShortPath} in submodel with Id {submodelIdentifier}");
                    return true;
                }
            }
            return false;
        }

        private ISubmodelElement GetSubmodelElementByPath(IReferable parent, string idShortPath, out IReferable outParent)
        {
            ISubmodelElement output = null;
            outParent = parent;

            var idShorts = _pathParserService.ParseIdShortPath(idShortPath);

            if (idShorts.Count == 1)
            {
                return parent.FindSubmodelElementByIdShort((string) idShorts[ 0 ]);
            }

            foreach (var idShortObject in idShorts)
            {
                if (output != null)
                {
                    outParent = output;
                }

                if (idShortObject is string idShortStr)
                {
                    output = outParent.FindSubmodelElementByIdShort(idShortStr);
                    if (output == null)
                    {
                        return null;
                    }
                }
                else if (idShortObject is int idShortInt)
                {
                    if (outParent is ISubmodelElementList smeList)
                    {
                        try
                        {
                            output = smeList.Value?[ idShortInt ];
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            throw new InvalidIdShortPathException(smeList.IdShort + "[" + idShortInt + "]");
                        }

                        if (output == null)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        throw new InvalidIdShortPathException(idShortPath);
                    }
                }
                else
                {
                    throw new Exception($"IdShort of {idShortObject.GetType} not supported.");
                }
            }

            return output;
        }

        private ISubmodelElement CreateSubmodelElementByPath(string submodelIdentifier, IReferable smeParent, ISubmodelElement newSubmodelElement, bool first)
        {
            //Create new SME
            _logger.LogDebug("Create the new submodel element.");
            if (smeParent != null && smeParent is Submodel submodel)
            {
                submodel.SubmodelElements ??= new List<ISubmodelElement>();
                if (first)
                {
                    submodel.SubmodelElements.Insert(0, newSubmodelElement);
                }
                else
                {
                    submodel.SubmodelElements.Add(newSubmodelElement);
                }
            }
            else if (smeParent != null && smeParent is SubmodelElementCollection collection)
            {
                collection.Value ??= new List<ISubmodelElement>();
                if (first)
                {
                    collection.Value.Insert(0, newSubmodelElement);
                }
                else
                {
                    collection.Value.Add(newSubmodelElement);
                }
            }
            else if (smeParent != null && smeParent is SubmodelElementList list)
            {
                list.Value ??= new List<ISubmodelElement>();
                if (first)
                {
                    list.Value.Insert(0, newSubmodelElement);
                }
                else
                {
                    list.Value.Add(newSubmodelElement);
                }
            }
            else if (smeParent != null && smeParent is Entity entity)
            {
                entity.Statements ??= new List<ISubmodelElement>();
                if (first)
                {
                    entity.Statements.Insert(0, newSubmodelElement);
                }
                else
                {
                    entity.Statements.Add(newSubmodelElement);
                }
            }
            else if (smeParent != null && smeParent is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                annotatedRelationshipElement.Annotations ??= new List<IDataElement>();
                if (first)
                {
                    annotatedRelationshipElement.Annotations.Insert(0, (IDataElement) newSubmodelElement);
                }
                else
                {
                    annotatedRelationshipElement.Annotations.Add((IDataElement) newSubmodelElement);
                }
            }
            else
            {
                throw new Exception($"The submodel element {smeParent.IdShort} does not support child elements.");
            }

            var timeStamp = DateTime.UtcNow;
            newSubmodelElement.SetAllParentsAndTimestamps(smeParent, timeStamp, timeStamp);
            newSubmodelElement.SetTimeStamp(timeStamp);
            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                _packageEnvService.setWrite(packageIndex, true);
            Program.signalNewData(1);
            return newSubmodelElement;
        }

        #endregion

        public bool IsSubmodelElementPresent(string submodelIdentifier, string idShortPath)
        {
            return IsSubmodelElementPresent(submodelIdentifier, idShortPath, out _, out _);
        }

        public void DeleteSubmodelById(string submodelIdentifier)
        {
            _packageEnvService.DeleteSubmodelById(submodelIdentifier);
        }

        public void DeleteSubmodelElementByPath(string submodelIdentifier, string idShortPath)
        {
            if (IsSubmodelElementPresent(submodelIdentifier, idShortPath, out ISubmodelElement submodelElement, out IReferable smeParent))
            {
                if (smeParent is SubmodelElementCollection parentCollection)
                {
                    parentCollection.Value.Remove(submodelElement);
                }
                else if (smeParent is SubmodelElementList parentList)
                {
                    parentList.Value.Remove(submodelElement);
                }
                else if (smeParent is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    annotatedRelationshipElement.Annotations.Remove((IDataElement) submodelElement);
                }
                else if (smeParent is Entity entity)
                {
                    entity.Statements.Remove(submodelElement);
                }
                else if (smeParent is Submodel parentSubmodel)
                {
                    parentSubmodel.SubmodelElements.Remove(submodelElement);
                }
                else
                {
                    _logger.LogDebug($"Could not delete SubmodelElement {submodelElement.IdShort}");
                    throw new Exception($"Unsupported data type of parent {smeParent.IdShort} for delete operation.");
                }
            }
            else
            {
                throw new NotFoundException($"Requested SubmodelElement NOT found in submodel with Id {submodelIdentifier}");
            }

            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                _packageEnvService.setWrite(packageIndex, true);
            Program.signalNewData(1);
            _logger.LogDebug($"Deleted SubmodelElement at {idShortPath} from submodel with Id {submodelIdentifier}");
        }

        public List<ISubmodelElement> GetAllSubmodelElements(string submodelIdentifier)
        {
            var submodel = _packageEnvService.GetSubmodelById(submodelIdentifier, out _);
            return submodel.SubmodelElements;
        }

        public void DeleteFileByPath(string submodelIdentifier, string idShortPath)
        {
            if (IsSubmodelElementPresent(submodelIdentifier, idShortPath, out ISubmodelElement submodelElement, out _))
            {
                if (submodelElement is AasCore.Aas3_0.File file)
                {
                    if (!string.IsNullOrEmpty(file.Value))
                    {
                        //check if it is external location
                        if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
                        {
                            _logger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
                            throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
                        }
                        //Check if a directory
                        else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
                        {
                            _logger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

                            _packageEnvService.DeleteSupplementaryFileInPackage(submodelIdentifier, file.Value);

                            file.Value = string.Empty;

                            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                                _packageEnvService.setWrite(packageIndex, true);
                            Program.signalNewData(1);
                            _logger.LogDebug($"Deleted the file at {idShortPath} from submodel with Id {submodelIdentifier}");
                        }
                        // incorrect value
                        else
                        {
                            _logger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
                            throw new OperationNotSupported($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
                        }
                    }
                    else
                    {
                        throw new OperationNotSupported($"Cannot delete the file. SubmodelElement {idShortPath} does not have a file attached.");
                    }
                }
                else
                {
                    throw new OperationNotSupported($"SubmodelElement found at {idShortPath} is not of type File");
                }
            }
            else
            {
                throw new NotFoundException($"Requested SubmodelElement NOT found in submodel with Id {submodelIdentifier}");
            }
        }

        public string GetFileByPath(string submodelIdentifier, string idShortPath, out byte[] byteArray, out long fileSize)
        {
            byteArray = null;
            string fileName = null;
            fileSize = 0;

            var fileElement = GetSubmodelElementByPath(submodelIdentifier, idShortPath);

            if (fileElement != null)
            {
                if (fileElement is AasCore.Aas3_0.File file)
                {
                    fileName = file.Value;

                    if (string.IsNullOrEmpty(fileName))
                    {
                        _logger.LogError($"File name is empty. Cannot fetch the file.");
                        throw new UnprocessableEntityException($"File value Null!!");
                    }

                    //check if it is external location
                    if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
                    {
                        _logger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
                        throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
                    }
                    //Check if a directory
                    else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
                    {
                        _logger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");
                        Stream stream = _packageEnvService.GetFileFromPackage(submodelIdentifier, fileName);
                        byteArray = stream.ToByteArray();
                        fileSize = byteArray.Length;
                    }
                    // incorrect value
                    else
                    {
                        _logger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
                        throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
                    }
                }
                else
                {
                    throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                }
            }

            return fileName;
        }

        public ISubmodel GetSubmodelById(string submodelIdentifier)
        {
            return _packageEnvService.GetSubmodelById(submodelIdentifier, out _);
        }

        public ISubmodelElement GetSubmodelElementByPath(string submodelIdentifier, string idShortPath)
        {
            var found = IsSubmodelElementPresent(submodelIdentifier, idShortPath, out ISubmodelElement output, out _);
            if (found)
            {
                return output;
            }
            else
            {
                throw new NotFoundException($"Submodel Element at {idShortPath} not found in the submodel with id {submodelIdentifier}");
            }
        }

        public ISubmodelElement CreateSubmodelElement(string submodelIdentifier, ISubmodelElement newSubmodelElement, bool first)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(newSubmodelElement);

            var smeFound = IsSubmodelElementPresent(submodelIdentifier, newSubmodelElement.IdShort, out _, out IReferable smeParent);
            if (smeFound)
            {
                _logger.LogDebug($"Cannot create requested submodel element !!");
                throw new DuplicateException($"SubmodelElement with idShort {newSubmodelElement.IdShort} already exists in the submodel.");
            }

            //Create new SME
            var submodel = GetSubmodelById(submodelIdentifier);

            submodel.SubmodelElements ??= new List<ISubmodelElement>();
            if (first)
            {
                submodel.SubmodelElements.Insert(0, newSubmodelElement);
            }
            else
            {
                submodel.SubmodelElements.Add(newSubmodelElement);
            }

            var timeStamp = DateTime.UtcNow;
            newSubmodelElement.SetAllParentsAndTimestamps(submodel, timeStamp, timeStamp);
            newSubmodelElement.SetTimeStamp(timeStamp);
            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                _packageEnvService.setWrite(packageIndex, true);
            Program.signalNewData(1);
            return newSubmodelElement;
        }

        public ISubmodelElement CreateSubmodelElementByPath(string submodelIdentifier, string idShortPath, bool first, ISubmodelElement newSubmodelElement)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(newSubmodelElement);

            var newIdShortPath = idShortPath + "." + newSubmodelElement.IdShort;
            bool smeFound = IsSubmodelElementPresent(submodelIdentifier, newIdShortPath, out _, out IReferable smeParent);
            if (smeFound)
            {
                _logger.LogDebug($"Cannot create requested submodel element !!");
                throw new DuplicateException($"SubmodelElement with idShort {newSubmodelElement.IdShort} already exists at {idShortPath} in the submodel.");
            }
            else
            {
                return CreateSubmodelElementByPath(submodelIdentifier, smeParent, newSubmodelElement, first);
            }
        }

        public void ReplaceSubmodelById(string submodelIdentifier, ISubmodel newSubmodel)
        {
            _verificationService.VerifyRequestBody(newSubmodel);
            _packageEnvService.ReplaceSubmodelById(submodelIdentifier, newSubmodel);
        }

        public void ReplaceSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement newSme)
        {
            _verificationService.VerifyRequestBody(newSme);
            var submodel = GetSubmodelById(submodelIdentifier);
            var existingSme = GetSubmodelElementByPath(submodel, idShortPath, out IReferable smeParent);

            if (existingSme != null && smeParent != null)
            {
                if (smeParent is Submodel)
                {
                    var existingIndex = submodel.SubmodelElements.IndexOf(existingSme);
                    submodel.SubmodelElements.Remove(existingSme);
                    submodel.SubmodelElements.Insert(existingIndex, newSme);
                }
                else if (smeParent is SubmodelElementCollection collection)
                {
                    var existingIndex = collection.Value.IndexOf(existingSme);
                    collection.Value.Remove(existingSme);
                    collection.Value.Insert(existingIndex, newSme);
                }
                else if (smeParent is SubmodelElementList smeList)
                {
                    var existingIndex = smeList.Value.IndexOf(existingSme);
                    smeList.Value.Remove(existingSme);
                    smeList.Value.Insert(existingIndex, newSme);
                }
                else if (smeParent is Entity entity)
                {
                    var existingIndex = entity.Statements.IndexOf(existingSme);
                    entity.Statements.Remove(existingSme);
                    entity.Statements.Insert(existingIndex, newSme);
                }
                else if (smeParent is AnnotatedRelationshipElement annotatedRelElement)
                {
                    var existingIndex = annotatedRelElement.Annotations.IndexOf((IDataElement) existingSme);
                    annotatedRelElement.Annotations.Remove((IDataElement) existingSme);
                    annotatedRelElement.Annotations.Insert(existingIndex, (IDataElement) newSme);
                }
                else
                {
                    throw new Exception($"The submodel element {idShortPath} does not support child elements.");
                }

                var timeStamp = DateTime.UtcNow;
                newSme.SetAllParentsAndTimestamps(smeParent, timeStamp, timeStamp);
                newSme.SetTimeStamp(timeStamp);
                if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                    _packageEnvService.setWrite(packageIndex, true);
                Program.signalNewData(1);
            }
        }

        public List<ISubmodel> GetAllSubmodels(IReference reqSemanticId = null, string idShort = null)
        {
            return _packageEnvService.GetAllSubmodels(reqSemanticId, idShort);
        }

        public ISubmodel CreateSubmodel(ISubmodel newSubmodel, string aasIdentifier)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(newSubmodel);

            if (_packageEnvService.IsSubmodelPresent(newSubmodel.Id))
            {
                _logger.LogDebug($"Cannot create requested Submodel !!");
                throw new DuplicateException($"Submodel with id {newSubmodel.Id} already exists.");
            }

            var output = _packageEnvService.CreateSubmodel(newSubmodel, aasIdentifier);

            return output;
        }

        public void UpdateSubmodelById(string submodelIdentifier, ISubmodel newSubmodel)
        {
            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out ISubmodel submodel, out int packageIndex))
            {
                //Verify the body first
                _verificationService.VerifyRequestBody(newSubmodel);

                Update.ToUpdateObject(submodel, newSubmodel);

                submodel.SetTimeStamp(DateTime.UtcNow);

                _packageEnvService.setWrite(packageIndex, true);
                Program.signalNewData(1);
            }
        }

        public void UpdateSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement newSme)
        {
            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out ISubmodel _, out int packageIndex))
            {
                var submodelElement = GetSubmodelElementByPath(submodelIdentifier, idShortPath);

                //Verify the body first
                _verificationService.VerifyRequestBody(newSme);

                Update.ToUpdateObject(submodelElement, newSme);

                submodelElement.SetTimeStamp(DateTime.UtcNow);

                _packageEnvService.setWrite(packageIndex, true);
                Program.signalNewData(1);
            }
        }

        public void ReplaceFileByPath(string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream fileContent)
        {
            var fileElement = GetSubmodelElementByPath(submodelIdentifier, idShortPath);

            if (fileElement != null)
            {
                if (fileElement is AasCore.Aas3_0.File file)
                {
                    //Check if file has location
                    if (!string.IsNullOrEmpty(file.Value))
                    {
                        //check if it is external location
                        if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
                        {
                            _logger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
                            throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
                        }
                        //Check if a directory
                        else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
                        {
                            _logger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");
                            //check if the value consists file extension
                            string sourcePath;
                            if (Path.HasExtension(file.Value))
                            {
                                sourcePath = Path.GetDirectoryName(file.Value); //This should get platform specific path, without file name
                            }
                            else
                            {
                                sourcePath = Path.Combine(file.Value);
                            }

                            var targetFile = Path.Combine(sourcePath, fileName);
                            targetFile = targetFile.Replace('/', Path.DirectorySeparatorChar); //TODO:jtikekar: better way to handle
                            Task task = _packageEnvService.ReplaceSupplementaryFileInPackage(submodelIdentifier, file.Value, targetFile, contentType, fileContent);
                            file.Value = FormatFileName(targetFile);
                            if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                                _packageEnvService.setWrite(packageIndex, true);
                            Program.signalNewData(2);
                        }
                        // incorrect value
                        else
                        {
                            _logger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
                            throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
                        }
                    }
                    else
                    {
                        //The value is null, so store the file to default location "/aasx/files"
                        _logger.LogError($"Null Value of the Submodel-Element File with IdShort {file.IdShort}");
                        var targetFile = Path.Combine("/aasx/files", fileName);
                        targetFile = targetFile.Replace('/', Path.DirectorySeparatorChar);
                        Task task = _packageEnvService.ReplaceSupplementaryFileInPackage(submodelIdentifier, file.Value, targetFile, contentType, fileContent);
                        file.Value = FormatFileName(targetFile);
                        if (_packageEnvService.IsSubmodelPresent(submodelIdentifier, out _, out int packageIndex))
                            _packageEnvService.setWrite(packageIndex, true);
                        Program.signalNewData(2);
                    }
                }
                else
                {
                    throw new NotFoundException($"Submodel element {fileElement.IdShort} is not of type File.");
                }
            }
        }

        private string FormatFileName(string fileName)
        {
            string fileNameTemp = fileName;

            string output = Regex.Replace(fileNameTemp, @"\\", "/");

            return output;
        }
    }
}