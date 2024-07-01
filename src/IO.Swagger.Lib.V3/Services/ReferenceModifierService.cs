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
