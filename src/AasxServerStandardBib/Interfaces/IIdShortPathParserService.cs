using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Interfaces
{
    public interface IIdShortPathParserService
    {
        List<object> ParseIdShortPath(string idShortPath);
    }
}
