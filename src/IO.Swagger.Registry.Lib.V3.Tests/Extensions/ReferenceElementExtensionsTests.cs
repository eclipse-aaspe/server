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

namespace IO.Swagger.Registry.Lib.V3.Tests.Extensions;

using AasCore.Aas3_0;
using V3.Extensions;

public class ReferenceElementExtensionsTests
{
    private readonly IFixture _fixture;

    public ReferenceElementExtensionsTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void ReverseReferenceKeys_NullReferenceElement_DoesNothing()
    {
        // Arrange
        ReferenceElement referenceElement = null;

        // Act
        referenceElement.ReverseReferenceKeys();

        // Assert
        referenceElement.Should().BeNull();
    }

    [Fact]
    public void ReverseReferenceKeys_NullValueProperty_DoesNothing()
    {
        // Arrange
        var referenceElement = _fixture.Build<ReferenceElement>()
                                       .With(x => x.Value, (Reference?)null)
                                       .Create();

        // Act
        referenceElement.ReverseReferenceKeys();

        // Assert
        referenceElement.Value.Should().BeNull();
    }

    [Fact]
    public void ReverseReferenceKeys_NullKeysProperty_DoesNothing()
    {
        // Arrange
        var reference = _fixture.Build<Reference>()
                                .With(x => x.Keys, (List<IKey>?)null)
                                .Create();
        var referenceElement = _fixture.Build<ReferenceElement>()
                                       .With(x => x.Value, reference)
                                       .Create();

        // Act
        referenceElement.ReverseReferenceKeys();

        // Assert
        referenceElement.Value.Keys.Should().BeNull();
    }

    [Fact]
    public void ReverseReferenceKeys_EmptyKeysList_DoesNothing()
    {
        // Arrange
        var reference = _fixture.Build<Reference>()
                                .With(x => x.Keys, new List<IKey>())
                                .Create();
        var referenceElement = _fixture.Build<ReferenceElement>()
                                       .With(x => x.Value, reference)
                                       .Create();

        // Act
        referenceElement.ReverseReferenceKeys();

        // Assert
        referenceElement.Value.Keys.Should().BeEmpty();
    }

    [Fact]
    public void ReverseReferenceKeys_NonEmptyKeysList_ReversesKeys()
    {
        // Arrange
        var keys = new List<IKey> {new Key(KeyTypes.ReferenceElement, "key1"), new Key(KeyTypes.ReferenceElement, "key2"), new Key(KeyTypes.ReferenceElement, "key3")};
        var reference = _fixture.Build<Reference>()
                                .With(x => x.Keys, keys)
                                .Create();
        var referenceElement = _fixture.Build<ReferenceElement>()
                                       .With(x => x.Value, reference)
                                       .Create();

        // Act
        referenceElement.ReverseReferenceKeys();

        // Assert
        referenceElement.Value.Keys.Should().HaveCount(3);
        referenceElement.Value.Keys[0].Value.Should().Be("key3");
        referenceElement.Value.Keys[1].Value.Should().Be("key2");
        referenceElement.Value.Keys[2].Value.Should().Be("key1");
    }
}