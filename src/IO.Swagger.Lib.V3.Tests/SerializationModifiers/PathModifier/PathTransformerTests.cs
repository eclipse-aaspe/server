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

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.PathModifier;

using AasCore.Aas3_0;
using Exceptions;
using V3.SerializationModifiers.PathModifier;

public class PathTransformerTests
{
    private readonly Fixture _fixture;
        private readonly PathTransformer _pathTransformer;

        public PathTransformerTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
            _pathTransformer = new PathTransformer();
        }

        [Fact]
        public void TransformAnnotatedRelationshipElement_WithEmptyContext_ShouldReturnEmptyStringList()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IAnnotatedRelationshipElement>>();
            mockElement.Setup(e => e.IdShort).Returns("idShort");
            mockElement.Setup(e => e.Annotations).Returns(new List<IDataElement>());

            var context = new PathModifierContext { IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformAnnotatedRelationshipElement(mockElement.Object, context);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("idShort");
        }

        [Fact]
        public void TransformBasicEventElement_WithNullParentPath_ShouldThrowInvalidSerializationModifierException()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IBasicEventElement>>();
            var context = new PathModifierContext { ParentPath = null };

            // Act
            Action act = () => _pathTransformer.TransformBasicEventElement(mockElement.Object, context);

            // Assert
            act.Should().Throw<InvalidSerializationModifierException>();
        }

        [Fact]
        public void TransformBlob_WithValidParentPath_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IBlob>>();
            mockElement.Setup(e => e.IdShort).Returns("blobId");

            var context = new PathModifierContext { ParentPath = "parentPath", IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformBlob(mockElement.Object, context);

            // Assert
            result.Should().Contain("parentPath.blobId");
        }

        [Fact]
        public void TransformCapability_WithNullParentPath_ShouldThrowInvalidSerializationModifierException()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<ICapability>>();
            var context = new PathModifierContext { ParentPath = null };

            // Act
            Action act = () => _pathTransformer.TransformCapability(mockElement.Object, context);

            // Assert
            act.Should().Throw<InvalidSerializationModifierException>();
        }

        [Fact]
        public void TransformEntity_WithEmptyContext_ShouldReturnEmptyStringList()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IEntity>>();
            mockElement.Setup(e => e.IdShort).Returns("entityId");
            mockElement.Setup(e => e.Statements).Returns(new List<ISubmodelElement>());

            var context = new PathModifierContext { IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformEntity(mockElement.Object, context);

            // Assert
            result.Should().Contain("entityId");
        }

        [Fact]
        public void TransformFile_WithValidParentPath_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IFile>>();
            mockElement.Setup(e => e.IdShort).Returns("fileId");

            var context = new PathModifierContext { ParentPath = "parentPath", IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformFile(mockElement.Object, context);

            // Assert
            result.Should().Contain("parentPath.fileId");
        }

        [Fact]
        public void TransformMultiLanguageProperty_WithNullParentPath_ShouldThrowInvalidSerializationModifierException()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IMultiLanguageProperty>>();
            var context = new PathModifierContext { ParentPath = null };

            // Act
            Action act = () => _pathTransformer.TransformMultiLanguageProperty(mockElement.Object, context);

            // Assert
            act.Should().Throw<InvalidSerializationModifierException>();
        }

        [Fact]
        public void TransformOperation_WithValidIdShort_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IOperation>>();
            mockElement.Setup(e => e.IdShort).Returns("operationId");
            mockElement.Setup(e => e.InputVariables).Returns(new List<IOperationVariable>());

            var context = new PathModifierContext { IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformOperation(mockElement.Object, context);

            // Assert
            result.Should().Contain("operationId");
        }

        [Fact]
        public void TransformOperationVariable_WithNullParentPath_ShouldThrowInvalidSerializationModifierException()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IOperationVariable>>();
            var mockSubmodelElement = _fixture.Freeze<Mock<ISubmodelElement>>();
            mockElement.Setup(e => e.Value).Returns(mockSubmodelElement.Object);

            var context = new PathModifierContext { ParentPath = null };

            // Act
            Action act = () => _pathTransformer.TransformOperationVariable(mockElement.Object, context);

            // Assert
            act.Should().Throw<InvalidSerializationModifierException>();
        }

        [Fact]
        public void TransformProperty_WithValidParentPath_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IProperty>>();
            mockElement.Setup(e => e.IdShort).Returns("propertyId");

            var context = new PathModifierContext { ParentPath = "parentPath", IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformProperty(mockElement.Object, context);

            // Assert
            result.Should().Contain("parentPath.propertyId");
        }

        [Fact]
        public void TransformRange_WithNullParentPath_ShouldThrowInvalidSerializationModifierException()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IRange>>();
            var context = new PathModifierContext { ParentPath = null };

            // Act
            Action act = () => _pathTransformer.TransformRange(mockElement.Object, context);

            // Assert
            act.Should().Throw<InvalidSerializationModifierException>();
        }

        [Fact]
        public void TransformReferenceElement_WithValidParentPath_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IReferenceElement>>();
            mockElement.Setup(e => e.IdShort).Returns("referenceElementId");

            var context = new PathModifierContext { ParentPath = "parentPath", IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformReferenceElement(mockElement.Object, context);

            // Assert
            result.Should().Contain("parentPath.referenceElementId");
        }

        [Fact]
        public void TransformRelationshipElement_WithNullParentPath_ShouldThrowInvalidSerializationModifierException()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<IRelationshipElement>>();
            var context = new PathModifierContext { ParentPath = null };

            // Act
            Action act = () => _pathTransformer.TransformRelationshipElement(mockElement.Object, context);

            // Assert
            act.Should().Throw<InvalidSerializationModifierException>();
        }

        [Fact]
        public void TransformSubmodel_WithValidIdShort_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<ISubmodel>>();
            mockElement.Setup(e => e.IdShort).Returns("submodelId");
            mockElement.Setup(e => e.SubmodelElements).Returns(new List<ISubmodelElement>());

            var context = new PathModifierContext { IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformSubmodel(mockElement.Object, context);

            // Assert
            result.Should().Contain("submodelId");
        }

        [Fact]
        public void TransformSubmodelElementCollection_WithValidParentPath_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<ISubmodelElementCollection>>();
            mockElement.Setup(e => e.IdShort).Returns("elementCollectionId");
            mockElement.Setup(e => e.Value).Returns(new List<ISubmodelElement>());

            var context = new PathModifierContext { ParentPath = "parentPath", IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformSubmodelElementCollection(mockElement.Object, context);

            // Assert
            result.Should().Contain("elementCollectionId");
        }

        [Fact]
        public void TransformSubmodelElementList_WithValidParentPath_ShouldReturnTransformedPaths()
        {
            // Arrange
            var mockElement = _fixture.Freeze<Mock<ISubmodelElementList>>();
            mockElement.Setup(e => e.IdShort).Returns("elementListId");
            mockElement.Setup(e => e.Value).Returns(new List<ISubmodelElement>
                                                    {
                                                        _fixture.Create<ISubmodelElement>(),
                                                        _fixture.Create<ISubmodelElement>()
                                                    });

            var context = new PathModifierContext { ParentPath = "parentPath", IdShortPaths = new List<string>() };

            // Act
            var result = _pathTransformer.TransformSubmodelElementList(mockElement.Object, context);

            // Assert
            result.Should().Contain("parentPath.elementListId[0]");
            result.Should().Contain("parentPath.elementListId[1]");
        }
}