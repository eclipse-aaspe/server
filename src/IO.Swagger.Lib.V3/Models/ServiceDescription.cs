/*
 * DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification
 *
 * The Full Profile of the Asset Administration Shell Registry Service Specification as part of the [Specification of the Asset Administration Shell: Part 2](http://industrialdigitaltwin.org/en/content-hub).   Publisher: Industrial Digital Twin Association (IDTA) 2023
 *
 * OpenAPI spec version: V3.0.1_SSP-001
 * Contact: info@idtwin.org
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
    /// The Description object enables servers to present their capabilities to the clients, in particular which profiles they implement. At least one defined profile is required. Additional, proprietary attributes might be included. Nevertheless, the server must not expect that a regular client understands them.
    /// </summary>
    [DataContract]
    public partial class ServiceDescription : IEquatable<ServiceDescription>
    {
        /// <summary>
        /// Gets or Sets Profiles
        /// </summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum ProfilesEnum
        {
            /// <summary>
            /// Enum AssetAdministrationShellServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-001")]
            AssetAdministrationShellServiceSpecificationSSP001Enum = 0,
            /// <summary>
            /// Enum AssetAdministrationShellServiceSpecificationSSP002Enum for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-002
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-002")]
            AssetAdministrationShellServiceSpecificationSSP002Enum = 1,
            /// <summary>
            /// Enum SubmodelServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001")]
            SubmodelServiceSpecificationSSP001Enum = 2,
            /// <summary>
            /// Enum SubmodelServiceSpecificationSSP002Enum for https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-002
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-002")]
            SubmodelServiceSpecificationSSP002Enum = 3,
            /// <summary>
            /// Enum SubmodelServiceSpecificationSSP003Enum for https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-003
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-003")]
            SubmodelServiceSpecificationSSP003Enum = 4,
            /// <summary>
            /// Enum AasxFileServerServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/AasxFileServerServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AasxFileServerServiceSpecification/SSP-001")]
            AasxFileServerServiceSpecificationSSP001Enum = 5,
            /// <summary>
            /// Enum AssetAdministrationShellRegistryServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-001")]
            AssetAdministrationShellRegistryServiceSpecificationSSP001Enum = 6,
            /// <summary>
            /// Enum AssetAdministrationShellRegistryServiceSpecificationSSP002Enum for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-002
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-002")]
            AssetAdministrationShellRegistryServiceSpecificationSSP002Enum = 7,
            /// <summary>
            /// Enum SubmodelRegistryServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-001")]
            SubmodelRegistryServiceSpecificationSSP001Enum = 8,
            /// <summary>
            /// Enum SubmodelRegistryServiceSpecificationSSP002Enum for https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-002
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-002")]
            SubmodelRegistryServiceSpecificationSSP002Enum = 9,
            /// <summary>
            /// Enum DiscoveryServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/DiscoveryServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/DiscoveryServiceSpecification/SSP-001")]
            DiscoveryServiceSpecificationSSP001Enum = 10,
            /// <summary>
            /// Enum AssetAdministrationShellRepositoryServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-001")]
            AssetAdministrationShellRepositoryServiceSpecificationSSP001Enum = 11,
            /// <summary>
            /// Enum AssetAdministrationShellRepositoryServiceSpecificationSSP002Enum for https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-002
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-002")]
            AssetAdministrationShellRepositoryServiceSpecificationSSP002Enum = 12,
            /// <summary>
            /// Enum SubmodelRepositoryServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-001")]
            SubmodelRepositoryServiceSpecificationSSP001Enum = 13,
            /// <summary>
            /// Enum SubmodelRepositoryServiceSpecificationSSP002Enum for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-002
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-002")]
            SubmodelRepositoryServiceSpecificationSSP002Enum = 14,
            /// <summary>
            /// Enum SubmodelRepositoryServiceSpecificationSSP003Enum for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-003
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-003")]
            SubmodelRepositoryServiceSpecificationSSP003Enum = 15,
            /// <summary>
            /// Enum SubmodelRepositoryServiceSpecificationSSP004Enum for https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-004
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/SubmodelRepositoryServiceSpecification/SSP-004")]
            SubmodelRepositoryServiceSpecificationSSP004Enum = 16,
            /// <summary>
            /// Enum ConceptDescriptionServiceSpecificationSSP001Enum for https://admin-shell.io/aas/API/3/0/ConceptDescriptionServiceSpecification/SSP-001
            /// </summary>
            [EnumMember(Value = "https://admin-shell.io/aas/API/3/0/ConceptDescriptionServiceSpecification/SSP-001")]
            ConceptDescriptionServiceSpecificationSSP001Enum = 17
        }

        /// <summary>
        /// Gets or Sets Profiles
        /// </summary>

        [DataMember(Name = "profiles")]
        public List<ProfilesEnum> Profiles { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ServiceDescription {\n");
            sb.Append("  Profiles: ").Append(Profiles).Append("\n");
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
            return obj.GetType() == GetType() && Equals((ServiceDescription)obj);
        }

        /// <summary>
        /// Returns true if ServiceDescription instances are equal
        /// </summary>
        /// <param name="other">Instance of ServiceDescription to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ServiceDescription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return

                    Profiles == other.Profiles ||
                    Profiles != null &&
                    Profiles.SequenceEqual(other.Profiles)
                ;
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
                if (Profiles != null)
                    hashCode = hashCode * 59 + Profiles.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(ServiceDescription left, ServiceDescription right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ServiceDescription left, ServiceDescription right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }
}
