using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;

namespace IO.Swagger.Lib.V3.Services;

/// <inheritdoc />
public class Base64UrlDecoderService : IBase64UrlDecoderService
{
    /// <inheritdoc />
    public string? Decode(string fieldName, string? encodedString)
    {
        if (string.IsNullOrEmpty(encodedString))
        {
            return null;
        }

        try
        {
            return Base64UrlEncoder.Decode(encodedString);
        }
        catch (FormatException)
        {
            throw new Base64UrlDecoderException(fieldName);
        }
    }
}