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
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services
{
    using System.Linq;

    public class ReferenceModifierService : IReferenceModifierService
    {
        private readonly IAppLogger<ReferenceModifierService> _logger;

        public ReferenceModifierService(IAppLogger<ReferenceModifierService> logger)
        {
            _logger = logger;
        }

        public List<IReference> GetReferenceResult(List<IReferable> referables)
        {
            var output = new List<IReference>();

            if (!referables.IsNullOrEmpty())
            {
                output.AddRange(referables.Select(referable => referable.GetReference()).Cast<IReference>());
            }

            return output;
        }

        public IReference? GetReferenceResult(IReferable referable)
        {
            return referable.GetReference();
        }
    }
}
