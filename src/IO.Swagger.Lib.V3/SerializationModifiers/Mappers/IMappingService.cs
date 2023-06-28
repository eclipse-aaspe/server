using DataTransferObjects;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers
{
    public interface IMappingService
    {
        //TODO:jtikekar refactor alternative to object
        //TDestination Map(TSource source);
        //object Map(object source);
        IDTO Map(IClass source, string mappingResolverKey);
        List<IDTO> Map(List<IClass> sourceList, string mappingResolverKey);
        IClass Map(IDTO dto, string mappingResolverKey);
    }
}
