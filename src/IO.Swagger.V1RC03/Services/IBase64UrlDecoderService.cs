namespace IO.Swagger.V1RC03.Services
{
    public interface IBase64UrlDecoderService
    {
        string Decode(string fieldName, string encodedString);
    }
}