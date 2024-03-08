namespace AasxServerDB
{
    public static class GlobalDB
    {
        private static string _dataPath = "";
        public static string DataPath
        {
            get
            {
                return _dataPath;
            }
            set
            {
                _dataPath = value;
            }
        }

        private static string _externalBlazor = "";
        public static string ExternalBlazor
        {
            get
            {
                return _externalBlazor;
            }
            set
            {
                _externalBlazor = value;
            }
        }
    }
}
