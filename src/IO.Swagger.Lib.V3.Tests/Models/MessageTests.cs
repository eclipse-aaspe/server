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

namespace IO.Swagger.Lib.V3.Tests.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Swagger.Models;

[TestSubject(typeof(Message))]
public class MessageTests
{
    [Fact]
    public void ToString_ReturnsExpectedString()
    {
        // Arrange
        var message = new Message
                      {
                          Code          = "123",
                          CorrelationId = "abc",
                          MessageType   = MessageTypeEnum.Info,
                          Text          = "Sample message",
                          Timestamp     = "2023-07-02T14:30:00Z"
                      };

        var expectedString
            = $"class Message {{\n  Code: {message.Code}\n  CorrelationId: {message.CorrelationId}\n  MessageType: Info\n  Text: {message.Text}\n  Timestamp: {message.Timestamp}\n}}\n";

        // Act
        var result = message.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    [Fact]
    public void ToJson_ReturnsValidJsonString()
    {
        // Arrange
        var message = new Message
                      {
                          Code          = "123",
                          CorrelationId = "abc",
                          MessageType   = MessageTypeEnum.Info,
                          Text          = "Sample message",
                          Timestamp     = "2023-07-02T14:30:00Z"
                      };

        var expectedJson = JsonSerializer.Serialize(message, new JsonSerializerOptions {WriteIndented = true, Converters = {new JsonStringEnumConverter()}});

        // Act
        var result = message.ToJson();

        // Assert
        result.Should().Be(expectedJson);
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenMessagesAreEqual()
    {
        // Arrange
        var message1 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        var message2 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_WhenMessagesAreEqual()
    {
        // Arrange
        var message1 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        var message2 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        // Act
        var hashCode1 = message1.GetHashCode();
        var hashCode2 = message2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void Operators_Equality_ReturnsTrue_WhenMessagesAreEqual()
    {
        // Arrange
        var message1 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        var message2 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        // Act
        var isEqual = message1 == message2;

        // Assert
        isEqual.Should().BeTrue();
    }

    [Fact]
    public void Operators_Inequality_ReturnsFalse_WhenMessagesAreEqual()
    {
        // Arrange
        var message1 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        var message2 = new Message
                       {
                           Code          = "123",
                           CorrelationId = "abc",
                           MessageType   = MessageTypeEnum.Info,
                           Text          = "Sample message",
                           Timestamp     = "2023-07-02T14:30:00Z"
                       };

        // Act
        var isNotEqual = message1 != message2;

        // Assert
        isNotEqual.Should().BeFalse();
    }
}