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
    public interface ISubmodelService
    {
        ISubmodel              CreateSubmodel(ISubmodel newSubmodel);
        ISubmodelElement       CreateSubmodelElement(string submodelIdentifier, ISubmodelElement newSubmodelElement, bool first);
        ISubmodelElement       CreateSubmodelElementByPath(string submodelIdentifier, string idShortPath, bool first, ISubmodelElement newSubmodelElement);
        void                   DeleteFileByPath(string submodelIdentifier, string idShortPath);
        void                   DeleteSubmodelById(string submodelIdentifier);
        void                   DeleteSubmodelElementByPath(string submodelIdentifier, string idShortPath);
        List<ISubmodelElement> GetAllSubmodelElements(string submodelIdentifier);
        List<ISubmodel>        GetAllSubmodels(IReference reqSemanticId = null, string idShort = null);
        string                 GetFileByPath(string submodelIdentifier, string idShortPath, out byte[] byteArray, out long fileSize);
        ISubmodel              GetSubmodelById(string submodelIdentifier);
        ISubmodel GetSubmodelById(string submodelIdentifier, out int packageIndex);
        ISubmodelElement GetSubmodelElementByPath(string submodelIdentifier, string idShortPath);
        bool                   IsSubmodelElementPresent(string submodelIdentifier, string idShortPath);
        void                   ReplaceFileByPath(string submodelIdentifier, string idShortPath, string fileName, string contentType, MemoryStream fileContent);
        void                   ReplaceSubmodelById(string submodelIdentifier, ISubmodel newSubmodel);
        void                   ReplaceSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement newSme);
        void                   UpdateSubmodelById(string submodelIdentifier, ISubmodel newSubmodel);
        void                   UpdateSubmodelElementByPath(string submodelIdentifier, string idShortPath, ISubmodelElement newSme);
    }
}
