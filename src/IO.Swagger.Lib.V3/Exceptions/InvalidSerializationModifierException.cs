using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Exceptions
{
    //TODO:jtikekar Move to API project
    public class InvalidSerializationModifierException : Exception
    {
        public InvalidSerializationModifierException(string modifier, string type) : base($"Invalid serialization modifier {modifier} for the requested element of type {type}.")
        {

        }
    }
}
