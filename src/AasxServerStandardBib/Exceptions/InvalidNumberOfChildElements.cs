using System;

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
