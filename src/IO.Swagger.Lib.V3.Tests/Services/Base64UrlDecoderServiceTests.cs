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

using IO.Swagger.Lib.V3.Services;

namespace AasxServerBlazorTests.Services;

using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

[TestSubject(typeof(Base64UrlDecoderService))]
public class Base64UrlDecoderServiceTests
{
    private readonly IFixture _fixture;
    private readonly Base64UrlDecoderService _service;

    public Base64UrlDecoderServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _service = _fixture.Create<Base64UrlDecoderService>();
    }

    [Fact]
    public void Decode_WithValidBase64UrlEncodedString_ReturnsDecodedString()
    {
        // Arrange
        var originalString = _fixture.Create<string>();
        var encodedString  = Base64UrlEncoder.Encode(originalString);

        // Act
        var result = _service.Decode("testField", encodedString);

        // Assert
        result.Should().Be(originalString);
    }

    [Fact]
    public void Decode_WithNullEncodedString_ReturnsNull()
    {
        // Act
        var result = _service.Decode("testField", null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_WithEmptyEncodedString_ReturnsNull()
    {
        // Act
        var result = _service.Decode("testField", string.Empty);

        // Assert
        result.Should().BeNull();
    }
}