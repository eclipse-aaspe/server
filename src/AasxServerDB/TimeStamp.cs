namespace TimeStamp
{
    public static class TimeStamp
    {
        private const string FormatString = "yyyy-MM-dd HH:mm:ss.fff";
        private const string FormatStringSQL = "%Y-%m-%d %H:%M:%f";

        public static DateTime StringToDateTime(string stringDateTime)
        {
            try
            {
                return DateTime.Parse(stringDateTime).ToUniversalTime();
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public static string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString(FormatString);
        }

        public static string GetFormatStringSQL()
        {
            return FormatStringSQL;
        }
    }
}
