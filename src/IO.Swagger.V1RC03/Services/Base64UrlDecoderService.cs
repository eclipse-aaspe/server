using AasxServerStandardBib.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Services
{
    public class Base64UrlDecoderService : IBase64UrlDecoderService
    {
        public string Decode(string fieldName, string encodedString)
        {
            try
            {
                if (!string.IsNullOrEmpty(encodedString))
                {
                    return Base64UrlEncoder.Decode(encodedString);
                }
                else
                {
                    return null;
                }
            }
            catch (FormatException)
            {
                throw new Base64UrlDecoderException(fieldName);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
