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

using AdminShellNS.Models;
using System.Collections.Generic;

namespace AasxServerStandardBib.Interfaces
{
    public interface IAasxFileServerInterfaceService
    {
        void DeleteAASXByPackageId(string packageId);
        string GetAASXByPackageId(string packageId, out byte[] content, out long fileSize, out IAssetAdministrationShell aas);
        List<PackageDescription> GetAllAASXPackageIds(string aasId = null);
        IAssetAdministrationShell GetAssetAdministrationShellByPackageId(string packageId);
        string PostAASXPackage(byte[] fileContent, string fileName);
        void UpdateAASXPackageById(string packageId, byte[] content, string fileName);
    }
}