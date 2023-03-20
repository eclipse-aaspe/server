using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Exceptions
{
    public class JsonDeserializationException : Exception
    {
        public JsonDeserializationException(string fieldName, string message) : base($"Unable to deserialize {fieldName}. Reason: {message}")
        {

        }
    }
}
