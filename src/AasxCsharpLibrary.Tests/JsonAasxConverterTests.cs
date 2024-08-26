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

using System.Text.Json;
using AdminShellNS;

namespace AasxCsharpLibary.Tests;

using AasCore.Aas3_0;

public class JsonAasxConverterTests
{
    private readonly Fixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonAasxConverterTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _jsonOptions = new JsonSerializerOptions();
    }

    [Fact]
    public void Read_Should_Deserialize_JsonWithCategoryAndIdShort()
    {
        // Arrange
        const string jsonString = @"
            {
                ""upperClassProperty"": {
                    ""lowerClassProperty"": ""Submodel""
                },
                ""category"": ""test"",
                ""idShort"": ""123abc""
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Submodel>();
        result.Category.Should().Be("test");
        result.IdShort.Should().Be("123abc");
    }

    [Fact]
    public void Read_Should_Return_Null_For_Missing_UpperClassProperty()
    {
        // Arrange
        const string jsonString = @"
            {
                ""category"": ""test"",
                ""idShort"": ""123abc""
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions);

        // Assert
        result.Should().BeNull();
    }


    [Fact]
    public void Read_Should_Return_Null_For_Invalid_Json()
    {
        // Arrange
        const string jsonString = @"{ ""invalidJson"": ""test"" }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Read_Should_Deserialize_JsonWithDisplayName()
    {
        // Arrange
        const string jsonString = @"
            {
                ""upperClassProperty"": {
                    ""lowerClassProperty"": ""Submodel""
                },
                ""displayName"": [
                    { ""lang"": ""en"", ""value"": ""Test DisplayName"" }
                ]
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions) as Submodel;

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(new LangStringNameType("en", "Test DisplayName"));
    }

    [Fact]
    public void Read_Should_Deserialize_JsonWithDescription()
    {
        // Arrange
        const string jsonString = @"
            {
                ""upperClassProperty"": {
                    ""lowerClassProperty"": ""Submodel""
                },
                ""description"": [
                    { ""lang"": ""en"", ""value"": ""Test Description"" }
                ]
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions) as Submodel;

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(new LangStringTextType("en", "Test Description"));
    }

    [Fact]
    public void Read_Should_Handle_Missing_OptionalProperties()
    {
        // Arrange
        const string jsonString = @"
            {
                ""upperClassProperty"": {
                    ""lowerClassProperty"": ""Submodel""
                }
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions) as Submodel;

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().BeNull();
        result.IdShort.Should().BeNull();
        result.DisplayName.Should().BeNull();
        result.Description.Should().BeNull();
    }

    [Fact]
    public void Write_Should_Throw_NotImplementedException()
    {
        // Arrange
        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");
        var writer    = new Utf8JsonWriter(Stream.Null);
        var referable = new Mock<IReferable>().Object;

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => converter.Write(writer, referable, _jsonOptions));
    }

    [Fact]
    public void CanConvert_Should_Return_True_For_IReferable()
    {
        // Arrange
        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var canConvert = converter.CanConvert(typeof(IReferable));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void CanConvert_Should_Return_False_For_Non_IReferable()
    {
        // Arrange
        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var canConvert = converter.CanConvert(typeof(string));

        // Assert
        canConvert.Should().BeFalse();
    }
}