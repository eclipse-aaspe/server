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

using IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent;

namespace AasxServerBlazorTests.SerializationModifiers.LevelExtent;

using AasCore.Aas3_0;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Models;
using JetBrains.Annotations;

[TestSubject(typeof(LevelExtentTransformer))]
public class LevelExtentTransformerTests
{
    private readonly LevelExtentTransformer _transformer;
    private readonly Mock<IBasicEventElement> _basicEventElementMock;
    private readonly Mock<ICapability> _capabilityMock;
    private readonly Mock<IProperty> _propertyMock;

    private readonly Fixture _fixture;
    private readonly Mock<LevelExtentModifierContext> _contextMock;

    public LevelExtentTransformerTests()
    {
        _transformer                      = new LevelExtentTransformer();
        _basicEventElementMock            = new Mock<IBasicEventElement>();
        _capabilityMock                   = new Mock<ICapability>();
        _propertyMock                     = new Mock<IProperty>();
        _fixture                          = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _contextMock = _fixture.Create<Mock<LevelExtentModifierContext>>();
    }

    [Fact]
    public void TransformBasicEventElement_ShouldThrowInvalidSerializationModifierException_WhenContextIsRootAndExtentWithBlobValue()
    {
        // Arrange
        var context           = new LevelExtentModifierContext(LevelEnum.Deep, ExtentEnum.WithBlobValue) {IsRoot = true};
        var basicEventElement = _basicEventElementMock.Object;

        // Act
        Action act = () => _transformer.TransformBasicEventElement(basicEventElement, context);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>()
           .WithMessage("Invalid serialization modifier Extent for the requested element of type IBasicEventElementProxy.");
    }

    [Fact]
    public void TransformCapability_ShouldThrowInvalidSerializationModifierException_WhenContextIsRootAndExtentWithBlobValue()
    {
        // Arrange
        var context    = new LevelExtentModifierContext(LevelEnum.Deep, ExtentEnum.WithBlobValue) {IsRoot = true};
        var capability = _capabilityMock.Object;

        // Act
        Action act = () => _transformer.TransformCapability(capability, context);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>()
           .WithMessage("Invalid serialization modifier Extent for the requested element of type ICapabilityProxy.");
    }

    [Fact]
    public void TransformProperty_ShouldThrowInvalidSerializationModifierException_WhenContextIsRootAndExtentWithBlobValue()
    {
        // Arrange
        var context  = new LevelExtentModifierContext(LevelEnum.Deep, ExtentEnum.WithBlobValue) {IsRoot = true};
        var property = _propertyMock.Object;

        // Act
        Action act = () => _transformer.TransformProperty(property, context);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>()
           .WithMessage("Invalid serialization modifier Extent for the requested element of type IPropertyProxy.");
    }
}