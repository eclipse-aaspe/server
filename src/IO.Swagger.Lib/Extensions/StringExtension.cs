namespace IO.Swagger.Extensions
{
    /// <summary>
    /// Provides Extension Methods over String Class
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Single string value can be compared with multiple values
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compareValues"></param>
        /// <returns></returns>
        public static bool CompareMultiple(this string data, params string[] compareValues)
        {
            foreach (string s in compareValues)
            {
                if (data.Equals(s, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
