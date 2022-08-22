using AasCore.Aas3_0_RC02;

namespace IO.Swagger.V1RC03.Services
{
    public interface IJsonQueryDeserializer
    {
        Reference DeserializeReference(string fieldName, string referenceString);
    }
}