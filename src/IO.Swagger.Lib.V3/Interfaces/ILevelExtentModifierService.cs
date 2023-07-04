using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface ILevelExtentModifierService
    {
        IClass ApplyLevelExtent(IClass that, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue);
        List<IClass> ApplyLevelExtent(List<IClass> submodelElements, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue);
    }
}
