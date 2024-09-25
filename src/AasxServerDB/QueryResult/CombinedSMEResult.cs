namespace AasxServerDB
{
    public partial class Query
    {
        private class CombinedSMEResult
        {
            public string? Identifier { get; set; }
            public string? IdShortPath { get; set; }
            public DateTime TimeStamp { get; set; }
            public string? Value { get; set; }
        }
    }
}