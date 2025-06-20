namespace IO.Swagger.Lib.V3.Models;

public enum ResultType
{
    Identifier,
    Submodel
}

public partial class QueryResultPagingMetadata
{
    /// <summary>
    /// Gets or Sets Cursor
    /// </summary>
    public string? cursor { get; set; }

    /// <summary>
    /// Gets or Sets Result Type
    /// </summary>
    public string? resultType { get; set; }

}
