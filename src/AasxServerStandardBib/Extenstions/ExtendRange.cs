using AasCore.Aas3_0_RC02;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendRange
    {
        public static string ValueAsText(this Range range)
        {
            return "" + range.Min + " .. " + range.Max;
        }
    }
}
