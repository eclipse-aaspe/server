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

namespace IO.Swagger.Models.Tests;

public class PaginationParametersTests
{
    [Fact]
    public void Constructor_WithNullCursor_SetsCursorToZero()
    {
        // Arrange
        string cursor = null;
        int?   limit  = 10;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Cursor.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithEmptyCursor_SetsCursorToZero()
    {
        // Arrange
        string cursor = string.Empty;
        int?   limit  = 10;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Cursor.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithValidCursor_SetsCursorCorrectly()
    {
        // Arrange
        string cursor = "5";
        int?   limit  = 10;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Cursor.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithInvalidCursor_SetsCursorToZero()
    {
        // Arrange
        string cursor = "invalid";
        int?   limit  = 10;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Cursor.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullLimit_SetsLimitToMaxResultSize()
    {
        // Arrange
        string cursor = "5";
        int?   limit  = null;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Limit.Should().Be(500);
    }

    [Fact]
    public void Constructor_WithValidLimit_SetsLimitCorrectly()
    {
        // Arrange
        string cursor = "5";
        int?   limit  = 20;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Limit.Should().Be(20);
    }

    [Fact]
    public void Constructor_WithLimitGreaterThanMaxResultSize_SetsLimitToProvidedValue()
    {
        // Arrange
        string cursor = "5";
        int?   limit  = 600;

        // Act
        var paginationParameters = new PaginationParameters(cursor, limit);

        // Assert
        paginationParameters.Limit.Should().Be(600);
    }

    [Fact]
    public void Limit_Setter_SetsLimitCorrectly()
    {
        // Arrange
        var paginationParameters = new PaginationParameters("5", 10);

        // Act
        paginationParameters.Limit = 30;

        // Assert
        paginationParameters.Limit.Should().Be(30);
    }

    [Fact]
    public void Cursor_Setter_SetsCursorCorrectly()
    {
        // Arrange
        var paginationParameters = new PaginationParameters("5", 10);

        // Act
        paginationParameters.Cursor = 7;

        // Assert
        paginationParameters.Cursor.Should().Be(7);
    }

}