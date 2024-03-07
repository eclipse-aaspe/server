using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Exceptions
{
    public class InvalidIdShortPathException:Exception
    {
        public InvalidIdShortPathException(string idShortPath) : base($"Invalid segment {idShortPath} in IdShortPath.")
        {

        }
    }
}
