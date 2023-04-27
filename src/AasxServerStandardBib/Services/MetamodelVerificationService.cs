
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AdminShellNS.Exceptions;
using System;
using System.Linq;

namespace AasxServerStandardBib.Services
{
    public class MetamodelVerificationService : IMetamodelVerificationService
    {
        private readonly IAppLogger<MetamodelVerificationService> _logger;

        public MetamodelVerificationService(IAppLogger<MetamodelVerificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
        }
        public void VerifyRequestBody(IClass body)
        {
            var errorList = Verification.Verify(body).ToList();
            if (errorList.Any())
            {
                throw new MetamodelVerificationException(errorList);
            }

            _logger.LogDebug($"The request body is conformant with the metamodel.");
        }
    }
}
