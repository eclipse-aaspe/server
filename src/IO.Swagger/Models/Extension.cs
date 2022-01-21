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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Swagger.Models
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class Extension : HasSemantics, IEquatable<Extension>
    {
        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [Required]

        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets RefersTo
        /// </summary>

        [DataMember(Name = "refersTo")]
        public Reference RefersTo { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>

        [DataMember(Name = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or Sets ValueType
        /// </summary>

        [DataMember(Name = "valueType")]
        public ValueTypeEnum ValueType { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Extension {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  RefersTo: ").Append(RefersTo).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  ValueType: ").Append(ValueType).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public new string ToJson()
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
            return obj.GetType() == GetType() && Equals((Extension)obj);
        }

        /// <summary>
        /// Returns true if Extension instances are equal
        /// </summary>
        /// <param name="other">Instance of Extension to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Extension other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Name == other.Name ||
                    Name != null &&
                    Name.Equals(other.Name)
                ) &&
                (
                    RefersTo == other.RefersTo ||
                    RefersTo != null &&
                    RefersTo.Equals(other.RefersTo)
                ) &&
                (
                    Value == other.Value ||
                    Value != null &&
                    Value.Equals(other.Value)
                ) &&
                (
                    ValueType == other.ValueType ||
                    ValueType != null &&
                    ValueType.Equals(other.ValueType)
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
                if (Name != null)
                    hashCode = hashCode * 59 + Name.GetHashCode();
                if (RefersTo != null)
                    hashCode = hashCode * 59 + RefersTo.GetHashCode();
                if (Value != null)
                    hashCode = hashCode * 59 + Value.GetHashCode();
                if (ValueType != null)
                    hashCode = hashCode * 59 + ValueType.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(Extension left, Extension right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Extension left, Extension right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }
}
