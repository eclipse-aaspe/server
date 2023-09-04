using System;

namespace AasxServerStandardBib.Exceptions
{
    public class InvalidNumberOfChildElementsException : Exception
    {
        public InvalidNumberOfChildElementsException(string resourceName, int sourceCount, int expectedCount) : base($"Invalid child elements in {resourceName}. {expectedCount} child elements are expected, whereas {sourceCount} are found in the request.")
        {

        }
    }
}
