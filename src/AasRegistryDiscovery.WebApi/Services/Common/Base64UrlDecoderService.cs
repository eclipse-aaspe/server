/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;

namespace AasRegistryDiscovery.WebApi.Services.Common;

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