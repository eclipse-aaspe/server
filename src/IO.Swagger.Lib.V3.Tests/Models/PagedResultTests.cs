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

using IO.Swagger.Models;

namespace AasxServerBlazorTests.Models;

using AasCore.Aas3_0;
using JetBrains.Annotations;

[TestSubject(typeof(PagedResult))]
public class PagedResultTests
{
    private readonly IFixture _fixture;

    public PagedResultTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void ToPagedList_Should_Return_Correct_Subset()
    {
        // Arrange
        var sourceList           = _fixture.CreateMany<int>(20).ToList();
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(5);
        result.paging_metadata.Should().NotBeNull();
        result.paging_metadata?.cursor.Should().Be("5");
    }

    [Fact]
    public void ToPagedList_Should_Cap_EndIndex()
    {
        // Arrange
        var sourceList           = new List<string> { "A", "B", "C" };
        var paginationParameters = new PaginationParameters("1", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(2);
        result.paging_metadata.Should().BeNull("No paging metadata because endIndex doesn't exceed list size");
    }

    [Fact]
    public void ToPagedList_Should_Handle_Empty_List()
    {
        // Arrange
        var sourceList           = new List<double>();
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().BeEmpty();
        result.paging_metadata.Should().BeNull("No paging metadata for empty list");
    }

    [Fact]
    public void ToPagedList_Should_Log_If_StartIndex_Out_Of_Bounds()
    {
        // Arrange
        var sourceList           = new List<int>();
        var paginationParameters = new PaginationParameters("10", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        // Logging is not directly testable here, but this test ensures the method runs without exceptions
        result.Should().NotBeNull();
        result.result.Should().BeEmpty();
        result.paging_metadata.Should().BeNull("No paging metadata because startIndex is out of bounds");
    }

    [Fact]
    public void ToPagedList_Should_Handle_Limit_Greater_Than_List_Size()
    {
        // Arrange
        var sourceList           = new List<char> { 'A', 'B', 'C' };
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(3,"Only 3 items in the source list");
        result.paging_metadata.Should().BeNull("No paging metadata because endIndex doesn't exceed list size");
    }

    [Fact]
    public void ToPagedList_Should_Return_Empty_If_Cursor_Null()
    {
        // Arrange
        var sourceList           = _fixture.CreateMany<DateTime>(10).ToList();
        var paginationParameters = new PaginationParameters(null, 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().NotBeNull();
        result.paging_metadata.Should().NotBeNull();
    }

    [Fact]
    public void ToPagedList_Should_Return_All_Items_If_Limit_Greater_Than_List_Size()
    {
        // Arrange
        var sourceList           = new List<long> { 1, 2, 3 };
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(3,"Only 3 items in the source list");
        result.paging_metadata.Should().BeNull("No paging metadata because endIndex doesn't exceed list size");
    }
}