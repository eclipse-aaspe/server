namespace IO.Swagger.V1RC03.Services
{
    public interface IOutputModifiersService
    {
        object ApplyOutputModifiers(object obj, string level = null, string content = null, string extent = null);
        void ValidateOutputModifiers(object obj, string level = null, string content = null, string extent = null);
    }
}