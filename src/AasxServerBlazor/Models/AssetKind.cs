/*
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace IO.Swagger.Models
{
    /// <summary>
    /// Gets or Sets AssetKind
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum AssetKind
        {
            /// <summary>
            /// Enum TypeEnum for Type
            /// </summary>
            [EnumMember(Value = "Type")]
            TypeEnum = 0,
            /// <summary>
            /// Enum InstanceEnum for Instance
            /// </summary>
            [EnumMember(Value = "Instance")]
            InstanceEnum = 1
        }
}
