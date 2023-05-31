using System;

namespace AasxServerStandardBib.Exceptions
{
    public class InvalidUpdateResourceException : Exception
    {
        public InvalidUpdateResourceException(string expectedType, string receivedType) : base($"Expected Type : {expectedType}; Received Type : {receivedType}")
        {

        }
    }
}
