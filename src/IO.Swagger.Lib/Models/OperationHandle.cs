namespace IO.Swagger.Models
{
    /// <summary>
    /// The returned handle of an operation’s asynchronous invocation used to request the current state of the operation’s execution.
    /// </summary>
    public partial class OperationHandle
    {
        /// <summary>
        /// Client Request Id
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// Handle id
        /// </summary>
        public string HandleId { get; set; }


    }
}
