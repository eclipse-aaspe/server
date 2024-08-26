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


using AasxServerStandardBib.Logging;
using Extensions;
using IO.Swagger.Lib.V3.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services
{
    public class AasRepositoryApiHelperService : IAasRepositoryApiHelperService
    {
        private readonly IAppLogger<AasRepositoryApiHelperService> _logger;

        public AasRepositoryApiHelperService(IAppLogger<AasRepositoryApiHelperService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
        }
        public List<Reference>? GetAllAssetAdministrationShellReference(List<AssetAdministrationShell> aasList)
        {
            if (aasList.IsNullOrEmpty())
            {
                _logger.LogDebug($"No asset administrations shells present.");
                return null;
            }

            var result = new List<Reference?>();
            foreach (var aas in aasList)
            {
                result.Add(aas.GetReference());
            }

            return result;
        }

        public Reference? GetAssetAdministrationShellReference(AssetAdministrationShell aas)
        {
            if (aas == null)
            {
                _logger.LogDebug($"Retrieved AAS is null");
                return null;
            }

            return aas.GetReference();
        }

        public List<Reference?>? GetAllReferences(List<IReferable> referables)
        {
            if (referables.IsNullOrEmpty())
            {
                _logger.LogDebug($"No asset administrations shells present.");
                return null;
            }

            var result = new List<Reference?>();
            foreach (var referable in referables)
            {
                result.Add(GetReference(referable));
            }

            return result;
        }


        public Reference? GetReference(IReferable referable)
        {
            if (referable == null)
            {
                _logger.LogDebug($"Retrieved AAS is null");
                return null;
            }

            return referable.GetReference();
        }
    }
}
