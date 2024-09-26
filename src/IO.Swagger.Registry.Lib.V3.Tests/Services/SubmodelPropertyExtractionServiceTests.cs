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

namespace IO.Swagger.Registry.Lib.V3.Tests.Services;

using AasCore.Aas3_0;
using JetBrains.Annotations;
using V3.Services;

[TestSubject(typeof(SubmodelPropertyExtractionService))]
public class SubmodelPropertyExtractionServiceTests
{
    private readonly IFixture _fixture;
    private SubmodelPropertyExtractionService _submodelPropertyExtractionService;

    public SubmodelPropertyExtractionServiceTests()
    {
        _fixture                           = new Fixture().Customize(new AutoMoqCustomization());
        _submodelPropertyExtractionService = new SubmodelPropertyExtractionService();
    }

    [Fact]
    public void FindMatchingProperties_ElementCollectionWithNullValue_ReturnsZeroAndNulls()
    {
        // Arrange
        var elementCollection = _fixture.Build<SubmodelElementCollection>()
                                        .With(x => x.Value, (List<ISubmodelElement>?)null)
                                        .Create();

        // Act
        var result = _submodelPropertyExtractionService.FindMatchingProperties(elementCollection, null, null);

        // Assert
        result.found.Should().Be(0);
        result.jsonProperty.Should().BeNull();
        result.endpointProperty.Should().BeNull();
    }

    [Fact]
    public void FindMatchingProperties_NoMatchingProperties_ReturnsZeroAndNulls()
    {
        // Arrange
        var properties = new List<ISubmodelElement>
                         {
                             _fixture.Build<Property>().With(x => x.IdShort, "someId").With(x => x.Value, "someValue").Create(),
                             _fixture.Build<Property>().With(x => x.IdShort, "anotherId").With(x => x.Value, "anotherValue").Create()
                         };
        var elementCollection = _fixture.Build<SubmodelElementCollection>()
                                        .With(x => x.Value, properties)
                                        .Create();

        // Act
        var result = _submodelPropertyExtractionService.FindMatchingProperties(elementCollection, "nonexistentAasID", "nonexistentAssetID");

        // Assert
        result.found.Should().Be(0);
        result.jsonProperty.Should().BeNull();
        result.endpointProperty.Should().BeNull();
    }
    
    [Fact]
    public void FindMatchingProperties_WithMatchingProperties_ReturnsCorrectValues()
    {
        // Arrange
        var properties = new List<ISubmodelElement>
                         {
                             _fixture.Build<Property>().With(x => x.IdShort, "aasID").With(x => x.Value, "expectedAasID").Create(),
                             _fixture.Build<Property>().With(x => x.IdShort, "assetID").With(x => x.Value, "expectedAssetID").Create(),
                             _fixture.Build<Property>().With(x => x.IdShort, "descriptorJSON").With(x => x.Value, "jsonValue").Create(),
                             _fixture.Build<Property>().With(x => x.IdShort, "endpoint").With(x => x.Value, "endpointValue").Create()
                         };
        var elementCollection = _fixture.Build<SubmodelElementCollection>()
                                        .With(x => x.Value, properties)
                                        .Create();

        // Act
        var result = _submodelPropertyExtractionService.FindMatchingProperties(elementCollection, "expectedAasID", "expectedAssetID");

        // Assert
        result.found.Should().Be(2);
        result.jsonProperty.Should().NotBeNull();
        result.jsonProperty.Value.Should().Be("jsonValue");
        result.endpointProperty.Should().NotBeNull();
        result.endpointProperty.Value.Should().Be("endpointValue");
    }

    [Fact]
    public void AddPropertyToCollection_CheckEmptyTrueAndEmptyValue_DoesNotAddProperty()
    {
        // Arrange
        var collection   = _fixture.Create<SubmodelElementCollection>();
        int initialCount = collection.Value.Count;

        // Act
        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "testId", "", DateTime.UtcNow, checkEmpty: true);

        // Assert
        collection.Value.Count.Should().Be(initialCount);
    }

    [Fact]
    public void AddPropertyToCollection_ValidValue_AddsProperty()
    {
        // Arrange
        var collection   = _fixture.Create<SubmodelElementCollection>();
        int initialCount = collection.Value.Count;

        // Act
        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "testId", "testValue", DateTime.UtcNow);

        // Assert
        collection.Value.Count.Should().Be(initialCount + 1);
        var addedProperty = collection.Value.Last() as Property;
        addedProperty.Should().NotBeNull();
        addedProperty.IdShort.Should().Be("testId");
        addedProperty.Value.Should().Be("testValue");
    }
    
    [Fact]
    public void CreateSubmodelElementCollection_ValidInputs_CreatesCollection()
    {
        // Arrange
        var idShort   = "testCollection";
        var timestamp = DateTime.UtcNow;

        // Act
        var collection = _submodelPropertyExtractionService.CreateSubmodelElementCollection(idShort, timestamp);

        // Assert
        collection.Should().NotBeNull();
        collection.IdShort.Should().Be(idShort);
        collection.TimeStampCreate.Should().Be(timestamp);
        collection.TimeStamp.Should().Be(timestamp);
        collection.Value.Should().NotBeNull().And.BeEmpty();
    }

}