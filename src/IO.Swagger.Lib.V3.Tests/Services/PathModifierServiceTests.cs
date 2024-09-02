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

namespace AasxServerBlazorTests.Services;

using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using System.Linq;
using AasCore.Aas3_0;
using IO.Swagger.Lib.V3.Services;
using Xunit;

public class PathModifierServiceTests
{
    private readonly Fixture _fixture;
    private readonly PathModifierService _pathModifierService;

    public PathModifierServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _pathModifierService = _fixture.Create<PathModifierService>();
    }

    [Fact]
    public void ToIdShortPath_WithListOfISubmodel_ShouldTransformAndReturnListOfLists()
    {
        // Arrange
        var submodels = _fixture.CreateMany<ISubmodel>(4).ToList();

        // Act
        var result = _pathModifierService.ToIdShortPath(submodels);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(submodels.Count);
    }

    [Fact]
    public void ToIdShortPath_WithListOfISubmodelElement_ShouldTransformAndReturnListOfLists()
    {
        // Arrange
        var submodelElements = _fixture.CreateMany<ISubmodelElement>(4).ToList();

        // Act
        var result = _pathModifierService.ToIdShortPath(submodelElements);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(submodelElements.Count);
    }
}