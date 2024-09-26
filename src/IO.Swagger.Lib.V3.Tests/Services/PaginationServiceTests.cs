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

using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using IO.Swagger.Models;
using JetBrains.Annotations;

[TestSubject(typeof(PaginationService))]
public class PaginationServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IAppLogger<PaginationService>> _loggerMock;

    public PaginationServiceTests()
    {
        _fixture    = new Fixture();
        _loggerMock = new Mock<IAppLogger<PaginationService>>();
    }

    [Fact]
    public void GetPaginatedList_Should_Return_Correct_Subset()
    {
        // Arrange
        var service              = new PaginationService(_loggerMock.Object);
        var sourceList           = _fixture.CreateMany<string>(10);
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = service.GetPaginatedList(sourceList.ToList(), paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(5);
    }

    [Fact]
    public void GetPaginatedList_Should_Log_Error_If_StartIndex_Out_Of_Bounds()
    {
        // Arrange
        var service              = new PaginationService(_loggerMock.Object);
        var sourceList           = new List<int>();
        var paginationParameters = new PaginationParameters("10", 5);

        // Act
        var result = service.GetPaginatedList(sourceList, paginationParameters);

        // Assert
        _loggerMock.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void GetPaginatedPackageDescriptionList_Should_Return_Correct_Subset()
    {
        // Arrange
        var service              = new PaginationService(_loggerMock.Object);
        var sourceList           = _fixture.CreateMany<PackageDescription>(10);
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = service.GetPaginatedPackageDescriptionList(sourceList.ToList(), paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(5);
    }
}