using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels
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
