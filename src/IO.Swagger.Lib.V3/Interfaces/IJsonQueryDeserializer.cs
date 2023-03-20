using AasCore.Aas3_0_RC02;

namespace IO.Swagger.Lib.V3.Services
{
    public interface IJsonQueryDeserializer
    {
        Reference DeserializeReference(string fieldName, string referenceString);
    }
}