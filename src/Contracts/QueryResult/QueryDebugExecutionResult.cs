namespace Contracts.QueryResult;

public class QueryDebugExecutionResult
{
    public List<object> Result { get; set; } = [];

    public List<string> RawSql { get; set; } = [];
}
