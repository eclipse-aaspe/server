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


using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AdminShellNS.Exceptions;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace AasxServerStandardBib.Services
{
    public class MetamodelVerificationService : IMetamodelVerificationService
    {
        private readonly IAppLogger<MetamodelVerificationService> _logger;
        private readonly IConfiguration _configuration;

        public MetamodelVerificationService(IAppLogger<MetamodelVerificationService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration;
        }
        public void VerifyRequestBody(IClass body)
        {
            if (_configuration.GetValue<bool>("IsMetamodelVerificationStrict"))
            {
                var errorList = Verification.Verify(body).ToList();
                if (errorList.Any())
                {
                    throw new MetamodelVerificationException(errorList);
                }

                _logger.LogDebug($"The request body is conformant with the metamodel.");
            }
            else
            {
                _logger.LogDebug("Metamodel verification is not strict. Therefore, skipping the verification.");
            }
        }
    }
}
