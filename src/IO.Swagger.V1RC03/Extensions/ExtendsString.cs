using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Extensions
{
    internal static class ExtendsString
    {
        /// <summary>
        /// Single string value can be compared with multiple values
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compareValues"></param>
        /// <returns></returns>
        public static bool EqualsAny(this string data, params string[] compareValues)
        {
            foreach (string s in compareValues)
            {
                if (data.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
