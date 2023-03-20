using AasCore.Aas3_0_RC02;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace AasxServerStandardBib.Services
{
    public class ConceptDescriptionService : IConceptDescriptionService
    {
        private readonly IAppLogger<ConceptDescriptionService> _logger;
        private readonly IAdminShellPackageEnvironmentService _packageEnvService;
        private readonly IMetamodelVerificationService _verificationService;

        public ConceptDescriptionService(IAppLogger<ConceptDescriptionService> logger, IAdminShellPackageEnvironmentService packageEnvService, IMetamodelVerificationService verificationService) 
        {
            _logger = logger;
            _packageEnvService = packageEnvService;
            _verificationService = verificationService;
        }

        public ConceptDescription CreateConceptDescription(ConceptDescription body)
        {
            //Verify the body first
            _verificationService.VerifyRequestBody(body);

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

        public List<ConceptDescription> GetAllConceptDescriptions(string idShort, Reference isCaseOf, Reference dataSpecificationRef)
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
                    var cdList = new List<ConceptDescription>();
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
                    var cdList = new List<ConceptDescription>();
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

        public ConceptDescription GetConceptDescriptionById(string cdIdentifier)
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
