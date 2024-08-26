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

namespace IO.Swagger.Lib.V3.Interfaces;

using Exceptions;

/// <summary>
/// Service for decoding Base64Url encoded strings.
/// </summary>
public interface IBase64UrlDecoderService
{
    /// <summary>
    /// Decodes a Base64Url encoded string.
    /// </summary>
    /// <param name="fieldName">The name of the field being decoded. Used for exception handling.</param>
    /// <param name="encodedString">The Base64Url encoded string to decode.</param>
    /// <returns>The decoded string, or null if the input string is null or empty.</returns>
    /// <exception cref="Base64UrlDecoderException">Thrown when the input string is not a valid Base64Url encoded string.</exception>
    string? Decode(string fieldName, string? encodedString);
}