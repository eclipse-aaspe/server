
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers
{
    public static class SerializationModifiersValidator
    {
        //As per new APIs, content is not handled here
        public static void Validate(IClass resource, LevelEnum level, ExtentEnum extent)
        {
            switch (resource)
            {
                case BasicEventElement:
                case Capability:
                case Operation:
                    {
                        if (level == LevelEnum.Core)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }

                        if (extent == ExtentEnum.WithBlobValue)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }
                        break;
                    }
                case Blob:
                    {
                        if (level == LevelEnum.Core)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }
                        break;
                    }
                case IDataElement:
                    {
                        if (level == LevelEnum.Core)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }
                        if (extent == ExtentEnum.WithBlobValue)
                        {
                            throw new InvalidSerializationModifierException(level.ToString(), resource.GetType().Name);
                        }
                        break;
                    }
            }
        }

        public static void Validate(List<IClass> resources, LevelEnum level, ExtentEnum extent)
        {
            foreach (IClass resource in resources)
                Validate(resource, level, extent);
        }
    }
}
