namespace Contracts.DbRequests;
public class DbQueryRequest
{
    public bool WithTotalCount { get; set; }

    public bool WithLastId { get; set; }

    public string SemanticId { get; set; }

    public string Identifier { get; set; }

    public string Diff { get; set; }

    public int PageFrom { get; set; }
    public int PageSize { get; set; }

    public string Expression { get; set; }

    //Submodel Element Request
    public string Requested { get; set; }
    public string SmSemanticId { get; set; }
    public string Contains { get; set; }
    public string Equal { get; set; }
    public string Lower { get; set; }
    public string Upper { get; set; }
}
