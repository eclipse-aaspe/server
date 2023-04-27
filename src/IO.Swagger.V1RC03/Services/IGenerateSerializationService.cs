

using System.Collections.Generic;

namespace IO.Swagger.V1RC03.Services
{
    public interface IGenerateSerializationService
    {
        Environment GenerateSerializationByIds(List<string> aasIds, List<string> submodelIds, bool includeConceptDescriptions);
    }
}