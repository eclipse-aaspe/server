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
using IO.Swagger.Registry.Lib.V3.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    using System.Threading.Tasks;

    public interface IRegistryInitializerService
    {
        void       CreateAssetAdministrationShellDescriptor(AssetAdministrationShellDescriptor newAasDesc, DateTime timestamp, bool initial = false);
        void       CreateMultipleAssetAdministrationShellDescriptor(List<AssetAdministrationShellDescriptor> body, DateTime timestamp);
        ISubmodel? GetAasRegistry();

        List<AssetAdministrationShellDescriptor> GetAasDescriptorsForSubmodelView();
        List<string>                             GetRegistryList();
        Task                                     InitRegistry(List<AasxCredentialsEntry> cList, DateTime timestamp, bool initAgain = false);
    }
}