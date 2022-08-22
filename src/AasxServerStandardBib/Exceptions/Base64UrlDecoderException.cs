using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Exceptions
{
    public class Base64UrlDecoderException : Exception
    {
        public Base64UrlDecoderException(string fieldName) : base($"Unable decode {fieldName}. Incorrect Base64Url encoded string.")
        {

        }
    }
}
