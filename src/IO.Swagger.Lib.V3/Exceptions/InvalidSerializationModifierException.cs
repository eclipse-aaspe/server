using System;

namespace IO.Swagger.Lib.V3.Exceptions
{
    public class InvalidSerializationModifierException : Exception
    {
        public InvalidSerializationModifierException(string modifier, string type) : base($"Invalid serialization modifier {modifier} for the requested element of type {type}.")
        {

        }
    }
}
