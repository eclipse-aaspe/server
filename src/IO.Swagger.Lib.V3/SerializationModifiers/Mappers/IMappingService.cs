using DataTransferObjects;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers
{
    public interface IMappingService
    {
        IDTO Map(IClass source, string mappingResolverKey);
        List<IDTO> Map(List<IClass> sourceList, string mappingResolverKey);
        IClass Map(IDTO dto, string mappingResolverKey);
    }
}
