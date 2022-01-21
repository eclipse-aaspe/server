/*
 * DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository
 *
 * An exemplary interface combination for the use case of an Asset Administration Shell Repository
 *
 * OpenAPI spec version: Final-Draft
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Swagger.Models
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class HasDataSpecification : IEquatable<HasDataSpecification>
    {
        /// <summary>
        /// Gets or Sets EmbeddedDataSpecifications
        /// </summary>

        [DataMember(Name = "embeddedDataSpecifications")]
        public List<EmbeddedDataSpecification> EmbeddedDataSpecifications { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class HasDataSpecification {\n");
            sb.Append("  EmbeddedDataSpecifications: ").Append(EmbeddedDataSpecifications).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((HasDataSpecification)obj);
        }

        /// <summary>
        /// Returns true if HasDataSpecification instances are equal
        /// </summary>
        /// <param name="other">Instance of HasDataSpecification to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(HasDataSpecification other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    EmbeddedDataSpecifications == other.EmbeddedDataSpecifications ||
                    EmbeddedDataSpecifications != null &&
                    EmbeddedDataSpecifications.SequenceEqual(other.EmbeddedDataSpecifications)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                if (EmbeddedDataSpecifications != null)
                    hashCode = hashCode * 59 + EmbeddedDataSpecifications.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(HasDataSpecification left, HasDataSpecification right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HasDataSpecification left, HasDataSpecification right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }
}
