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

using System.Collections.Generic;
using System.IO;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAssetAdministrationShellService
    {
        IAssetAdministrationShell CreateAssetAdministrationShell(IAssetAdministrationShell body);
        ISubmodelElement CreateSubmodelElement(string aasIdentifier, string submodelIdentifier, bool first, ISubmodelElement newSubmodelElement);
        ISubmodelElement CreateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, bool first, ISubmodelElement newSubmodelElement);
        IReference CreateSubmodelReferenceInAAS(IReference body, string aasIdentifier);
        void DeleteAssetAdministrationShellById(string aasIdentifier);
        void DeleteFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        void DeleteSubmodelById(string aasIdentifier, string submodelIdentifier);
        void DeleteSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        void DeleteSubmodelReferenceById(string aasIdentifier, string submodelIdentifier);
        void DeleteThumbnail(string aasIdentifier);
        List<IAssetAdministrationShell> GetAllAssetAdministrationShells(List<ISpecificAssetId>? assetIds = null, string? idShort = null);
        List<ISubmodelElement> GetAllSubmodelElements(string aasIdentifier, string submodelIdentifier);
        List<IReference> GetAllSubmodelReferencesFromAas(string aasIdentifier);
        IAssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier);
        IAssetInformation GetAssetInformation(string aasIdentifier);
        string GetFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, out byte[] content, out long fileSize);
        ISubmodel GetSubmodelById(string aasIdentifier, string submodelIdentifier);
        ISubmodelElement GetSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath);
        string GetThumbnail(string aasIdentifier, out byte[] content, out long fileSize);
        void ReplaceAssetAdministrationShellById(string aasIdentifier, IAssetAdministrationShell newAas);
        void ReplaceAssetInformation(string aasIdentifier, IAssetInformation newAssetInformation);
        void ReplaceFileByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream stream);
        void ReplaceSubmodelById(string aasIdentifier, string submodelIdentifier, Submodel newSubmodel);
        void ReplaceSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement body);
        void UpdateAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier);
        void UpdateAssetInformation(AssetInformation body, string aasIdentifier);
        void UpdateSubmodelById(string aasIdentifier, string submodelIdentifier, ISubmodel newSubmodel);
        void UpdateSubmodelElementByPath(string aasIdentifier, string submodelIdentifier, string idShortPath, ISubmodelElement newSme);
        void UpdateThumbnail(string aasIdentifier, string fileName, string contentType, Stream stream);
    }
}