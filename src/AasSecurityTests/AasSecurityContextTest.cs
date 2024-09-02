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

using AasSecurity.Models;

namespace AasSecurity;

using Exceptions;

public class AasSecurityContextTests
{
    private readonly IFixture _fixture;

    public AasSecurityContextTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    [Theory]
    [InlineData("post", AccessRights.CREATE)]
    [InlineData("head", AccessRights.READ)]
    [InlineData("get", AccessRights.READ)]
    [InlineData("put", AccessRights.UPDATE)]
    [InlineData("delete", AccessRights.DELETE)]
    [InlineData("patch", AccessRights.UPDATE)]
    public void Constructor_Should_SetNeededRights_Correctly(string httpOperation, AccessRights expectedRights)
    {
        // Arrange
        var accessRole = _fixture.Create<string>();
        var route      = _fixture.Create<string>();

        // Act
        var context = new AasSecurityContext(accessRole, route, httpOperation);

        // Assert
        context.NeededRights.Should().Be(expectedRights);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData(" ")]
    [InlineData("")]
    public void Constructor_Should_ThrowAuthorizationException_ForUnsupportedHttpOperation(string unsupportedHttpOperation)
    {
        // Arrange
        var accessRole = _fixture.Create<string>();
        var route      = _fixture.Create<string>();

        // Act
        Action act = () => new AasSecurityContext(accessRole, route, unsupportedHttpOperation);

        // Assert
        act.Should().Throw<AuthorizationException>().WithMessage($"Unsupported HTTP Operation {unsupportedHttpOperation}");
    }
    
    [Theory]
    [InlineData(null)]
    public void Constructor_Should_ThrowNullReferenceException_ForUnsupportedHttpOperation(string unsupportedHttpOperation)
    {
        // Arrange
        var accessRole = _fixture.Create<string>();
        var route      = _fixture.Create<string>();

        // Act
        Action act = () => new AasSecurityContext(accessRole, route, unsupportedHttpOperation);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Constructor_Should_SetAccessRoleAndRoute_Correctly()
    {
        // Arrange
        var accessRole    = _fixture.Create<string>();
        var route         = _fixture.Create<string>();
        var httpOperation = "get";

        // Act
        var context = new AasSecurityContext(accessRole, route, httpOperation);

        // Assert
        context.AccessRole.Should().Be(accessRole);
        context.Route.Should().Be(route);
    }

    [Theory]
    [InlineData("PoSt", AccessRights.CREATE)]
    [InlineData("hEaD", AccessRights.READ)]
    [InlineData("gEt", AccessRights.READ)]
    [InlineData("pUt", AccessRights.UPDATE)]
    [InlineData("dEleTe", AccessRights.DELETE)]
    [InlineData("PaTcH", AccessRights.UPDATE)]
    public void Constructor_Should_HandleCaseInsensitiveHttpOperations(string httpOperation, AccessRights expectedRights)
    {
        // Arrange
        var accessRole = _fixture.Create<string>();
        var route      = _fixture.Create<string>();

        // Act
        var context = new AasSecurityContext(accessRole, route, httpOperation);

        // Assert
        context.NeededRights.Should().Be(expectedRights);
    }
}