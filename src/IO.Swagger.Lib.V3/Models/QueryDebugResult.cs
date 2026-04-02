namespace IO.Swagger.Lib.V3.Models;

using System.Collections.Generic;

public class QueryDebugResult : QueryResult
{
    public List<string> raw_sql { get; set; } = [];
    public List<List<string>> raw_sql_lines { get; set; } = [];
}
