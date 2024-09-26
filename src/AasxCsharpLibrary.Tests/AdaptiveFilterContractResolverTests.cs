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

namespace AasxCsharpLibary.Tests;

using AdminShellNS;

public class AdaptiveFilterContractResolverTests
{
    private readonly Fixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdaptiveFilterContractResolverTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _jsonOptions = new JsonSerializerOptions();
    }

    [Fact]
    public void Read_Should_Return_JsonElement_For_Valid_Json()
    {
        // Arrange
        const string jsonString = """{ "key": "value" }""";
        var          bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var          reader     = new Utf8JsonReader(bytes);

        var resolver = new AdminShellConverters.AdaptiveFilterContractResolver();

        // Act
        var result = resolver.Read(ref reader, typeof(JsonElement), _jsonOptions);

        // Assert
        result.Should().NotBeNull();
        result.GetProperty("key").GetString().Should().Be("value");
    }

    [Fact]
    public void Read_Should_Handle_Invalid_Json()
    {
        // Arrange
        const string jsonString = """{ "key": "value" """;
        var          bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var          reader     = new Utf8JsonReader(bytes);

        var resolver = new AdminShellConverters.AdaptiveFilterContractResolver();

        // Act
        try
        {
            resolver.Read(ref reader, typeof(JsonElement), _jsonOptions);
        }
        catch (JsonException e)
        {
            // Assert
            e.Message.Should().Contain("Error while reading JSON");
        }
    }

    [Fact]
    public void Write_Should_Throw_NotImplementedException()
    {
        // Arrange
        var resolver    = new AdminShellConverters.AdaptiveFilterContractResolver();
        var writer      = new Utf8JsonWriter(Stream.Null);
        var jsonElement = JsonDocument.Parse("{}").RootElement;

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => resolver.Write(writer, jsonElement, _jsonOptions));
    }

    [Fact]
    public void AdaptiveFilterContractResolver_Should_Set_Properties_Correctly()
    {
        // Arrange
        var resolver = new AdminShellConverters.AdaptiveFilterContractResolver(false, false, false, false, false);

        // Act & Assert
        resolver.AasHasViews.Should().BeFalse();
        resolver.BlobHasValue.Should().BeFalse();
        resolver.SubmodelHasElements.Should().BeFalse();
        resolver.SmcHasValue.Should().BeFalse();
        resolver.OpHasVariables.Should().BeFalse();
    }

    [Fact]
    public void Read_Should_Return_JsonElement_For_Empty_Json()
    {
        // Arrange
        const string jsonString = "{}";
        var          bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var          reader     = new Utf8JsonReader(bytes);

        var resolver = new AdminShellConverters.AdaptiveFilterContractResolver();

        // Act
        var result = resolver.Read(ref reader, typeof(JsonElement), _jsonOptions);

        // Assert
        result.Should().NotBeNull();
        result.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void Read_Should_Return_JsonElement_For_Nested_Json()
    {
        // Arrange
        const string jsonString = """{ "key1": { "key2": "value" } }""";
        var          bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var          reader     = new Utf8JsonReader(bytes);

        var resolver = new AdminShellConverters.AdaptiveFilterContractResolver();

        // Act
        var result = resolver.Read(ref reader, typeof(JsonElement), _jsonOptions);

        // Assert
        result.Should().NotBeNull();
        result.GetProperty("key1").GetProperty("key2").GetString().Should().Be("value");
    }

    [Fact]
    public void Read_Should_Return_JsonElement_For_Array_Json()
    {
        // Arrange
        const string jsonString = """{ "key": [ "value1", "value2" ] }""";
        var          bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var          reader     = new Utf8JsonReader(bytes);

        var resolver = new AdminShellConverters.AdaptiveFilterContractResolver();

        // Act
        var result = resolver.Read(ref reader, typeof(JsonElement), _jsonOptions);

        // Assert
        result.Should().NotBeNull();
        var array = result.GetProperty("key").EnumerateArray().ToArray();
        array.Should().HaveCount(2);
        array[0].GetString().Should().Be("value1");
        array[1].GetString().Should().Be("value2");
    }
}