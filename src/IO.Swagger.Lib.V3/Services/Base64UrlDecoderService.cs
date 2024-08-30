using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;

namespace IO.Swagger.Lib.V3.Services;

/// <inheritdoc />
public class Base64UrlDecoderService : IBase64UrlDecoderService
{
    /// <inheritdoc />
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
                throw new NoIdentifierException(fieldName);
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