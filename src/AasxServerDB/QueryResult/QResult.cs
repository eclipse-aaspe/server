using AasxServerDB.Result;

namespace AasxServerDB
{
    public class QResult
    {
        public static int DefaultPageSize = 1000;
        public int Count { get; set; }
        public int TotalCount { get; set; }
        public int PageFrom { get; set; }
        public int PageSize { get; set; }
        public int? LastID { get; set; }
        public List<string> Messages { get; set; }
        public List<SMResult> SMResults { get; set; }
        public List<SMEResult> SMEResults { get; set; }
        public List<string> SQL { get; set; }
    }
}
