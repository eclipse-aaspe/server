using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IGenerateSerializationService
    {
        Environment GenerateSerializationByIds(List<string> aasIds = null, List<string> submodelIds = null, bool includeConceptDescriptions = false);
    }
}