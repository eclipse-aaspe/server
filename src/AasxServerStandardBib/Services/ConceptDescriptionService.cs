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


using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AasxServerStandardBib.Services
{
    public class ConceptDescriptionService : IConceptDescriptionService
    {
        private readonly IAppLogger<ConceptDescriptionService> _logger;
        private readonly IAdminShellPackageEnvironmentService _packageEnvService;
        private readonly IMetamodelVerificationService _verificationService;

        public ConceptDescriptionService(IAppLogger<ConceptDescriptionService> logger, IAdminShellPackageEnvironmentService packageEnvService, IMetamodelVerificationService verificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _packageEnvService = packageEnvService;
            _verificationService = verificationService;
        }

        public IConceptDescription CreateConceptDescription(IConceptDescription body)
        {
            //Verify the body first
            //_verificationService.VerifyRequestBody(body);

            var found = _packageEnvService.IsConceptDescriptionPresent(body.Id);
            if (found)
            {
                _logger.LogDebug($"Cannot create requested ConceptDescription !!");
                throw new DuplicateException($"ConceptDescription with id {body.Id} already exists.");
            }

            var output = _packageEnvService.CreateConceptDescription(body);

            return output;
        }

        public void DeleteConceptDescriptionById(string cdIdentifier)
        {
            _packageEnvService.DeleteConceptDescriptionById(cdIdentifier);
        }

        public List<IConceptDescription> GetAllConceptDescriptions(string idShort = null, Reference isCaseOf = null, Reference dataSpecificationRef = null)
        {
            //Get All Concept descriptions
            var output = _packageEnvService.GetAllConceptDescriptions();

            if (output.Any())
            {
                //Filter AASs based on IdShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    var cdList = output.Where(cd => cd.IdShort.Equals(idShort)).ToList();
                    if (cdList.IsNullOrEmpty())
                    {
                        _logger.LogDebug($"No Concept Description with IdShort {idShort} Found.");
                    }
                    else
                    {
                        output = cdList;
                    }
                }

                //Filter based on IsCaseOf
                if (isCaseOf != null)
                {
                    var cdList = new List<IConceptDescription>();
                    foreach (var conceptDescription in output)
                    {
                        if (!conceptDescription.IsCaseOf.IsNullOrEmpty())
                        {
                            foreach (var reference in conceptDescription.IsCaseOf)
                            {
                                if (reference != null && reference.Matches(isCaseOf))
                                {
                                    cdList.Add(conceptDescription);
                                    break;
                                }
                            }
                        }
                    }
                    if (cdList.IsNullOrEmpty())
                    {
                        _logger.LogDebug($"No Concept Description with requested IsCaseOf found.");
                    }
                    else
                    {
                        output = cdList;
                    }

                }

                //Filter based on DataSpecificationRef
                if (dataSpecificationRef != null)
                {
                    var cdList = new List<IConceptDescription>();
                    foreach (var conceptDescription in output)
                    {
                        if (!conceptDescription.EmbeddedDataSpecifications.IsNullOrEmpty())
                        {
                            foreach (var reference in conceptDescription.EmbeddedDataSpecifications)
                            {
                                if (reference != null && reference.DataSpecification.Matches(dataSpecificationRef))
                                {
                                    cdList.Add(conceptDescription);
                                    break;
                                }
                            }
                        }
                    }
                    if (cdList.IsNullOrEmpty())
                    {
                        _logger.LogDebug($"No Concept Description with requested DataSpecificationReference found.");
                    }
                    else
                    {
                        output = cdList;
                    }
                }
            }

            return output;
        }

        public IConceptDescription GetConceptDescriptionById(string cdIdentifier)
        {
            return _packageEnvService.GetConceptDescriptionById(cdIdentifier, out _);
        }

        public void UpdateConceptDescriptionById(ConceptDescription body, string cdIdentifier)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(body);

            _packageEnvService.UpdateConceptDescriptionById(body, cdIdentifier);
        }
    }
}
