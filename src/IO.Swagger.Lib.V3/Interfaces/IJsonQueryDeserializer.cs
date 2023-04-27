

namespace IO.Swagger.Lib.V3.Services
{
    public interface IJsonQueryDeserializer
    {
        Reference DeserializeReference(string fieldName, string referenceString);
    }
}