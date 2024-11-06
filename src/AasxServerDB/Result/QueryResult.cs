namespace AasxServerDB.Result
{
    public class QueryResult
    {
        public List<string> Messages { get; set; }

        public List<SMResult> SMResults { get; set; }
        public List<SMEResult> SMEResults { get; set; }
    }
}
