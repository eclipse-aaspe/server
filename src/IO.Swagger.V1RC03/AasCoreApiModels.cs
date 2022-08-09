using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Swagger.V1RC03.ApiModel
{
    [DataContract]
    public partial class Result : IEquatable<Result>
    {
        /// <summary>
        /// Gets or Sets Messages
        /// </summary>

        [DataMember(Name = "messages")]
        public List<Message> Messages { get; set; }

        /// <summary>
        /// Gets or Sets Success
        /// </summary>

        [DataMember(Name = "success")]
        public bool? Success { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Result {\n");
            sb.Append("  Messages: ").Append(Messages).Append("\n");
            sb.Append("  Success: ").Append(Success).Append("\n");
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
            return obj.GetType() == GetType() && Equals((Result)obj);
        }

        /// <summary>
        /// Returns true if Result instances are equal
        /// </summary>
        /// <param name="other">Instance of Result to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Result other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Messages == other.Messages ||
                    Messages != null &&
                    Messages.SequenceEqual(other.Messages)
                ) &&
                (
                    Success == other.Success ||
                    Success != null &&
                    Success.Equals(other.Success)
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
                if (Messages != null)
                    hashCode = hashCode * 59 + Messages.GetHashCode();
                if (Success != null)
                    hashCode = hashCode * 59 + Success.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(Result left, Result right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Result left, Result right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }

    [DataContract]
    public partial class Message : IEquatable<Message>
    {
        /// <summary>
        /// Gets or Sets Code
        /// </summary>

        [DataMember(Name = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or Sets MessageType
        /// </summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum MessageTypeEnum
        {
            /// <summary>
            /// Enum UndefinedEnum for Undefined
            /// </summary>
            [EnumMember(Value = "Undefined")]
            UndefinedEnum = 0,
            /// <summary>
            /// Enum InfoEnum for Info
            /// </summary>
            [EnumMember(Value = "Info")]
            InfoEnum = 1,
            /// <summary>
            /// Enum WarningEnum for Warning
            /// </summary>
            [EnumMember(Value = "Warning")]
            WarningEnum = 2,
            /// <summary>
            /// Enum ErrorEnum for Error
            /// </summary>
            [EnumMember(Value = "Error")]
            ErrorEnum = 3,
            /// <summary>
            /// Enum ExceptionEnum for Exception
            /// </summary>
            [EnumMember(Value = "Exception")]
            ExceptionEnum = 4
        }

        /// <summary>
        /// Gets or Sets MessageType
        /// </summary>

        [DataMember(Name = "messageType")]
        public MessageTypeEnum? MessageType { get; set; }

        /// <summary>
        /// Gets or Sets Text
        /// </summary>

        [DataMember(Name = "text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or Sets Timestamp
        /// </summary>

        [DataMember(Name = "timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Message {\n");
            sb.Append("  Code: ").Append(Code).Append("\n");
            sb.Append("  MessageType: ").Append(MessageType).Append("\n");
            sb.Append("  Text: ").Append(Text).Append("\n");
            sb.Append("  Timestamp: ").Append(Timestamp).Append("\n");
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
            return obj.GetType() == GetType() && Equals((Message)obj);
        }

        /// <summary>
        /// Returns true if Message instances are equal
        /// </summary>
        /// <param name="other">Instance of Message to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Message other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Code == other.Code ||
                    Code != null &&
                    Code.Equals(other.Code)
                ) &&
                (
                    MessageType == other.MessageType ||
                    MessageType != null &&
                    MessageType.Equals(other.MessageType)
                ) &&
                (
                    Text == other.Text ||
                    Text != null &&
                    Text.Equals(other.Text)
                ) &&
                (
                    Timestamp == other.Timestamp ||
                    Timestamp != null &&
                    Timestamp.Equals(other.Timestamp)
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
                if (Code != null)
                    hashCode = hashCode * 59 + Code.GetHashCode();
                if (MessageType != null)
                    hashCode = hashCode * 59 + MessageType.GetHashCode();
                if (Text != null)
                    hashCode = hashCode * 59 + Text.GetHashCode();
                if (Timestamp != null)
                    hashCode = hashCode * 59 + Timestamp.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(Message left, Message right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Message left, Message right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }

    [DataContract]
    public partial class OperationRequest : IEquatable<OperationRequest>
    {
        /// <summary>
        /// Gets or Sets InoutputArguments
        /// </summary>

        [DataMember(Name = "inoutputArguments")]
        public List<OperationVariable> InoutputArguments { get; set; }

        /// <summary>
        /// Gets or Sets InputArguments
        /// </summary>

        [DataMember(Name = "inputArguments")]
        public List<OperationVariable> InputArguments { get; set; }

        /// <summary>
        /// Gets or Sets RequestId
        /// </summary>

        [DataMember(Name = "requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or Sets Timeout
        /// </summary>

        [DataMember(Name = "timeout")]
        public int? Timeout { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OperationRequest {\n");
            sb.Append("  InoutputArguments: ").Append(InoutputArguments).Append("\n");
            sb.Append("  InputArguments: ").Append(InputArguments).Append("\n");
            sb.Append("  RequestId: ").Append(RequestId).Append("\n");
            sb.Append("  Timeout: ").Append(Timeout).Append("\n");
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
            return obj.GetType() == GetType() && Equals((OperationRequest)obj);
        }

        /// <summary>
        /// Returns true if OperationRequest instances are equal
        /// </summary>
        /// <param name="other">Instance of OperationRequest to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(OperationRequest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    InoutputArguments == other.InoutputArguments ||
                    InoutputArguments != null &&
                    InoutputArguments.SequenceEqual(other.InoutputArguments)
                ) &&
                (
                    InputArguments == other.InputArguments ||
                    InputArguments != null &&
                    InputArguments.SequenceEqual(other.InputArguments)
                ) &&
                (
                    RequestId == other.RequestId ||
                    RequestId != null &&
                    RequestId.Equals(other.RequestId)
                ) &&
                (
                    Timeout == other.Timeout ||
                    Timeout != null &&
                    Timeout.Equals(other.Timeout)
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
                if (InoutputArguments != null)
                    hashCode = hashCode * 59 + InoutputArguments.GetHashCode();
                if (InputArguments != null)
                    hashCode = hashCode * 59 + InputArguments.GetHashCode();
                if (RequestId != null)
                    hashCode = hashCode * 59 + RequestId.GetHashCode();
                if (Timeout != null)
                    hashCode = hashCode * 59 + Timeout.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(OperationRequest left, OperationRequest right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OperationRequest left, OperationRequest right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }

    [DataContract]
    public partial class OperationVariable : IEquatable<OperationVariable>
    {
        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [Required]

        [DataMember(Name = "value")]
        public AasCore.Aas3_0_RC02.ISubmodelElement Value { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OperationVariable {\n");
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
            return obj.GetType() == GetType() && Equals((OperationVariable)obj);
        }

        /// <summary>
        /// Returns true if OperationVariable instances are equal
        /// </summary>
        /// <param name="other">Instance of OperationVariable to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(OperationVariable other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
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
                if (Value != null)
                    hashCode = hashCode * 59 + Value.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(OperationVariable left, OperationVariable right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OperationVariable left, OperationVariable right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }

    [DataContract]
    public partial class OperationResult : IEquatable<OperationResult>
    {
        /// <summary>
        /// Gets or Sets ExecutionResult
        /// </summary>

        [DataMember(Name = "executionResult")]
        public Result ExecutionResult { get; set; }

        /// <summary>
        /// Gets or Sets ExecutionState
        /// </summary>

        [DataMember(Name = "executionState")]
        public ExecutionState ExecutionState { get; set; }

        /// <summary>
        /// Gets or Sets InoutputArguments
        /// </summary>

        [DataMember(Name = "inoutputArguments")]
        public List<OperationVariable> InoutputArguments { get; set; }

        /// <summary>
        /// Gets or Sets OutputArguments
        /// </summary>

        [DataMember(Name = "outputArguments")]
        public List<OperationVariable> OutputArguments { get; set; }

        /// <summary>
        /// Gets or Sets RequestId
        /// </summary>

        [DataMember(Name = "requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OperationResult {\n");
            sb.Append("  ExecutionResult: ").Append(ExecutionResult).Append("\n");
            sb.Append("  ExecutionState: ").Append(ExecutionState).Append("\n");
            sb.Append("  InoutputArguments: ").Append(InoutputArguments).Append("\n");
            sb.Append("  OutputArguments: ").Append(OutputArguments).Append("\n");
            sb.Append("  RequestId: ").Append(RequestId).Append("\n");
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
            return obj.GetType() == GetType() && Equals((OperationResult)obj);
        }

        /// <summary>
        /// Returns true if OperationResult instances are equal
        /// </summary>
        /// <param name="other">Instance of OperationResult to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(OperationResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    ExecutionResult == other.ExecutionResult ||
                    ExecutionResult != null &&
                    ExecutionResult.Equals(other.ExecutionResult)
                ) &&
                (
                    ExecutionState == other.ExecutionState ||
                    ExecutionState != null &&
                    ExecutionState.Equals(other.ExecutionState)
                ) &&
                (
                    InoutputArguments == other.InoutputArguments ||
                    InoutputArguments != null &&
                    InoutputArguments.SequenceEqual(other.InoutputArguments)
                ) &&
                (
                    OutputArguments == other.OutputArguments ||
                    OutputArguments != null &&
                    OutputArguments.SequenceEqual(other.OutputArguments)
                ) &&
                (
                    RequestId == other.RequestId ||
                    RequestId != null &&
                    RequestId.Equals(other.RequestId)
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
                if (ExecutionResult != null)
                    hashCode = hashCode * 59 + ExecutionResult.GetHashCode();
                if (ExecutionState != null)
                    hashCode = hashCode * 59 + ExecutionState.GetHashCode();
                if (InoutputArguments != null)
                    hashCode = hashCode * 59 + InoutputArguments.GetHashCode();
                if (OutputArguments != null)
                    hashCode = hashCode * 59 + OutputArguments.GetHashCode();
                if (RequestId != null)
                    hashCode = hashCode * 59 + RequestId.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(OperationResult left, OperationResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OperationResult left, OperationResult right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ExecutionState
    {
        /// <summary>
        /// Enum InitiatedEnum for Initiated
        /// </summary>
        [EnumMember(Value = "Initiated")]
        InitiatedEnum = 0,
        /// <summary>
        /// Enum RunningEnum for Running
        /// </summary>
        [EnumMember(Value = "Running")]
        RunningEnum = 1,
        /// <summary>
        /// Enum CompletedEnum for Completed
        /// </summary>
        [EnumMember(Value = "Completed")]
        CompletedEnum = 2,
        /// <summary>
        /// Enum CanceledEnum for Canceled
        /// </summary>
        [EnumMember(Value = "Canceled")]
        CanceledEnum = 3,
        /// <summary>
        /// Enum FailedEnum for Failed
        /// </summary>
        [EnumMember(Value = "Failed")]
        FailedEnum = 4,
        /// <summary>
        /// Enum TimeoutEnum for Timeout
        /// </summary>
        [EnumMember(Value = "Timeout")]
        TimeoutEnum = 5
    }
}
