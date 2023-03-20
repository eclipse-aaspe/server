namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IBase64UrlDecoderService
    {
        string Decode(string fieldName, string encodedString);
    }
}