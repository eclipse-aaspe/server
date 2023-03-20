using System;

namespace AasxServerStandardBib.Exceptions
{
    public class DuplicateException : Exception
    {
        public DuplicateException(string message) : base(message)
        {

        }
    }
}
