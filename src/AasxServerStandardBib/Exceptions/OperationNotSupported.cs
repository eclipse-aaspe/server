using System;

namespace AasxServerStandardBib.Exceptions
{
    public class OperationNotSupported : Exception
    {
        public OperationNotSupported(string message) : base(message)
        {

        }
    }
}
