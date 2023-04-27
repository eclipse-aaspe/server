
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IAasRepositoryApiHelperService
    {
        List<Reference> GetAllReferences(List<IReferable> referables);
        Reference GetReference(IReferable referable);
    }
}
