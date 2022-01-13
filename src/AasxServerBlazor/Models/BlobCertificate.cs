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
    public partial class BlobCertificate : IEquatable<BlobCertificate>
    {
        /// <summary>
        /// Gets or Sets _BlobCertificate
        /// </summary>

        [DataMember(Name="blobCertificate")]
        public Blob _BlobCertificate { get; set; }

        /// <summary>
        /// Gets or Sets ContainedExtension
        /// </summary>

        [DataMember(Name="containedExtension")]
        public List<Reference> ContainedExtension { get; set; }

        /// <summary>
        /// Gets or Sets LastCertificate
        /// </summary>

        [DataMember(Name="lastCertificate")]
        public bool? LastCertificate { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BlobCertificate {\n");
            sb.Append("  _BlobCertificate: ").Append(_BlobCertificate).Append("\n");
            sb.Append("  ContainedExtension: ").Append(ContainedExtension).Append("\n");
            sb.Append("  LastCertificate: ").Append(LastCertificate).Append("\n");
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
            return obj.GetType() == GetType() && Equals((BlobCertificate)obj);
        }

        /// <summary>
        /// Returns true if BlobCertificate instances are equal
        /// </summary>
        /// <param name="other">Instance of BlobCertificate to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(BlobCertificate other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    _BlobCertificate == other._BlobCertificate ||
                    _BlobCertificate != null &&
                    _BlobCertificate.Equals(other._BlobCertificate)
                ) &&
                (
                    ContainedExtension == other.ContainedExtension ||
                    ContainedExtension != null &&
                    ContainedExtension.SequenceEqual(other.ContainedExtension)
                ) &&
                (
                    LastCertificate == other.LastCertificate ||
                    LastCertificate != null &&
                    LastCertificate.Equals(other.LastCertificate)
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
                    if (_BlobCertificate != null)
                    hashCode = hashCode * 59 + _BlobCertificate.GetHashCode();
                    if (ContainedExtension != null)
                    hashCode = hashCode * 59 + ContainedExtension.GetHashCode();
                    if (LastCertificate != null)
                    hashCode = hashCode * 59 + LastCertificate.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(BlobCertificate left, BlobCertificate right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BlobCertificate left, BlobCertificate right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}