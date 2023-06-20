using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IPathModifierService
    {
        List<string> ToIdShortPath(IClass that);
        List<List<string>> ToIdShortPath(List<ISubmodel> submodelList);
        List<List<string>> ToIdShortPath(List<ISubmodelElement> submodelElementList);
    }
}
