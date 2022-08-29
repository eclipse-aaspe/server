using System;

namespace IO.Swagger.V1RC03.Services
{
    public interface IInputModifierService
    {
        void ValidateInputModifiers(Type objectType, string level = null, string content = null, string extent = null);
    }
}