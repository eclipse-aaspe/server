using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Exceptions
{
    //TODO: jtikekar change name
    public class InvalidNumberOfChildElementsException : Exception
    {
        public InvalidNumberOfChildElementsException(string resourceName, int sourceCount, int expectedCount) : base($"Invalid child elements in {resourceName}. {expectedCount} child elements are expected, whereas {sourceCount} are found in the request.")
        {

        }
    }
}
