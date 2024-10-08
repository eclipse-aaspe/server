/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

/*
 * DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification
 *
 * The Full Profile of the Asset Administration Shell Registry Service Specification as part of the [Specification of the Asset Administration Shell: Part 2](http://industrialdigitaltwin.org/en/content-hub).   Publisher: Industrial Digital Twin Association (IDTA) 2023
 *
 * OpenAPI spec version: V3.0.1_SSP-001
 * Contact: info@idtwin.org
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Swagger.Registry.Lib.V3.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class ProtocolInformationSecurityAttributes : IEquatable<ProtocolInformationSecurityAttributes>
    {
        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        public enum TypeEnum
        {
            /// <summary>
            /// Enum NONE for NONE
            /// </summary>
            [EnumMember(Value = "NONE")] NONE = 0,

            /// <summary>
            /// Enum RFCTLSA for RFC_TLSA
            /// </summary>
            [EnumMember(Value = "RFC_TLSA")] RFCTLSA = 1,

            /// <summary>
            /// Enum W3CDID for W3C_DID
            /// </summary>
            [EnumMember(Value = "W3C_DID")] W3CDID = 2
        }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [Required]
        [DataMember(Name = "type")]
        public TypeEnum? Type { get; set; }

        /// <summary>
        /// Gets or Sets Key
        /// </summary>
        [Required]
        [DataMember(Name = "key")]
        public string? Key { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [Required]
        [DataMember(Name = "value")]
        public string Value { get; set; }

        public ProtocolInformationSecurityAttributes(TypeEnum type, string? key, string value)
        {
            Type  = type;
            Key   = key;
            Value = value;
        }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ProtocolInformationSecurityAttributes {\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson() => JsonSerializer.Serialize(this, options);

        private static readonly JsonSerializerOptions options = new() {WriteIndented = true, IgnoreNullValues = true, Converters = {new JsonStringEnumConverter()}};

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ProtocolInformationSecurityAttributes)obj);
        }

        /// <summary>
        /// Returns true if ProtocolInformationSecurityAttributes instances are equal
        /// </summary>
        /// <param name="other">Instance of ProtocolInformationSecurityAttributes to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ProtocolInformationSecurityAttributes? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Type == other.Type ||
                    Type != null &&
                    Type.Equals(other.Type)
                ) &&
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
                if (Type != null)
                    hashCode = hashCode * 59 + Type.GetHashCode();
                if (Key != null)
                    hashCode = hashCode * 59 + Key.GetHashCode();
                if (Value != null)
                    hashCode = hashCode * 59 + Value.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(ProtocolInformationSecurityAttributes left, ProtocolInformationSecurityAttributes right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProtocolInformationSecurityAttributes left, ProtocolInformationSecurityAttributes right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}