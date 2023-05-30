using System;

namespace AasxServerStandardBib.Exceptions
{
    public class UnprocessableEntityException : Exception
    {
        public UnprocessableEntityException(string message) : base(message)
        {

        }
    }
}
