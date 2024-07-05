namespace TimeStamp
{
    public static class TimeStamp
    {
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
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
