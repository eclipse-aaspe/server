using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Exceptions
{
    //TODO:jtikekar Move to API project
    public class InvalidOutputModifierException : Exception
    {
        public InvalidOutputModifierException(string modifier) : base($"Invalid output modifier {modifier} for the requested element.")
        {

        }
    }
}
