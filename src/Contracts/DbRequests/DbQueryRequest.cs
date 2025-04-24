namespace Contracts.DbRequests;
public class DbQueryRequest
{
    public bool WithTotalCount { get; set; }

    public bool WithLastId { get; set; }

    public string SemanticId { get; set; }

    public string Identifier { get; set; }

    public string Diff { get; set; }

    public string Expression { get; set; }
}
