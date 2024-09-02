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

using AasCore.Aas3_0;
using IO.Swagger.Models;
using JetBrains.Annotations;

[TestSubject(typeof(LevelExtentModifierService))]
public class LevelExtentModifierServiceTests
{
    private readonly IFixture _fixture;
    private readonly LevelExtentModifierService _service;
    private readonly Mock<IClass> _classMock;

    public LevelExtentModifierServiceTests()
    {
        _fixture   = new Fixture().Customize(new AutoMoqCustomization());
        _classMock = _fixture.Freeze<Mock<IClass>>();
        _service   = _fixture.Create<LevelExtentModifierService>();
    }

    [Fact]
    public void ApplyLevelExtent_ListInstance_ThrowsArgumentNullException_WhenInputIsNull()
    {
        // Act
        Action act = () => _service.ApplyLevelExtent((List<IClass>)null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyLevelExtent_Instance_ThrowsArgumentNullException_WhenInputIsNull()
    {
        // Act
        Action act = () => _service.ApplyLevelExtent((IClass)null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyLevelExtent_Instance_ReturnsTransformedInstance()
    {
        // Arrange
        var testClass = _classMock.Object;
        var level     = LevelEnum.Deep;
        var extent    = ExtentEnum.WithoutBlobValue;

        // Act
        var result = _service.ApplyLevelExtent(testClass, level, extent);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IClass>();
    }

    [Fact]
    public void ApplyLevelExtent_ListInstance_ReturnsTransformedList()
    {
        // Arrange
        var testClasses = new List<IClass> {_classMock.Object, _classMock.Object};
        var level       = LevelEnum.Deep;
        var extent      = ExtentEnum.WithoutBlobValue;

        // Act
        var result = _service.ApplyLevelExtent(testClasses, level, extent);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(testClasses.Count);
    }

    [Fact]
    public void ApplyLevelExtent_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<IClass>();
        var level     = LevelEnum.Deep;
        var extent    = ExtentEnum.WithoutBlobValue;

        // Act
        var result = _service.ApplyLevelExtent(emptyList, level, extent);

        // Assert
        result.Should().BeEmpty();
    }
}