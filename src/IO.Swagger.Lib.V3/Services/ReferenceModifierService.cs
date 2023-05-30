using AasxServerStandardBib.Logging;
using Extensions;
using IO.Swagger.Lib.V3.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services
{
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
                foreach (var referable in referables)
                {
                    Reference reference = referable.GetReference();
                    output.Add(reference);
                }
            }

            return output;
        }

        public IReference GetReferenceResult(IReferable referable)
        {
            return referable.GetReference();
        }
    }
}
