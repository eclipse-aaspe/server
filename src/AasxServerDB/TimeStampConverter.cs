namespace TimeStampConverter
{
    static public class TimeStampConverter
    {
        static public DateTime StringToDateTime(string stringDateTime)
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

        static public string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
