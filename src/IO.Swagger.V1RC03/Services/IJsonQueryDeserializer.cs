

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IJsonQueryDeserializer
    {
        Reference DeserializeReference(string fieldName, string referenceString);
    }
}