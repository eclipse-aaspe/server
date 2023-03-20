using System.Text.Json.Serialization;

namespace IO.Swagger.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LevelEnum
    {
        Deep,
        Core
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContentEnum
    {
        Normal,
        Value,
        Metadata,
        Reference,
        Path
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExtentEnum
    {
        WithoutBlobValue,
        WithBlobValue
    }


}
