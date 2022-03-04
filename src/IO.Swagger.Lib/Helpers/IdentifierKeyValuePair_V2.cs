using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using IO.Swagger.Models;
using Newtonsoft.Json;

namespace IO.Swagger.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class IdentifierKeyValuePair_V2
    {
        /// <summary>
        /// Gets or Sets Key
        /// </summary>
        [Required]

        [DataMember(Name = "key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [Required]

        [DataMember(Name = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class IdentifierKeyValuePair {\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
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
            return obj.GetType() == GetType() && Equals((IdentifierKeyValuePair_V2)obj);
        }

        /// <summary>
        /// Returns true if IdentifierKeyValuePair instances are equal
        /// </summary>
        /// <param name="other">Instance of IdentifierKeyValuePair to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(IdentifierKeyValuePair other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Key == other.Key ||
                    Key != null &&
                    Key.Equals(other.Key)
                ) &&
                (
                    Value == other.Value ||
                    Value != null &&
                    Value.Equals(other.Value)
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
                if (Key != null)
                    hashCode = hashCode * 59 + Key.GetHashCode();
                if (Value != null)
                    hashCode = hashCode * 59 + Value.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        //public static bool operator ==(IdentifierKeyValuePair left, IdentifierKeyValuePair right)
        //{
        //    return Equals(left, right);
        //}

        //public static bool operator !=(IdentifierKeyValuePair left, IdentifierKeyValuePair right)
        //{
        //    return !Equals(left, right);
        //}

#pragma warning restore 1591
        #endregion Operators
    }
}
