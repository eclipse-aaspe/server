using System;

namespace IO.Swagger.Lib.V3.Exceptions
{
    public class AuthorizationException : Exception
    {
        public AuthorizationException(string message) : base(message)
        {

        }
    }
}
