using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Exceptions
{
    internal class NotImplementedException :Exception
    {
        public NotImplementedException(string message) : base(message)
        {

        }
    }
}
