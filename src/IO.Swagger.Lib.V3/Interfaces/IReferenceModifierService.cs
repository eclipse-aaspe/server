using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IReferenceModifierService
    {
        List<IReference> GetReferenceResult(List<IReferable> referables);
        IReference GetReferenceResult(IReferable referable);
    }
}
