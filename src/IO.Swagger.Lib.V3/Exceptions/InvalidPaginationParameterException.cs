using System;

namespace IO.Swagger.Lib.V3.Exceptions
{
    public class InvalidPaginationParameterException : Exception
    {
        public InvalidPaginationParameterException(string paramName, int? value) : base($"Invalid pagination parameter {paramName} value = {value}.")
        {

        }
    }
}
