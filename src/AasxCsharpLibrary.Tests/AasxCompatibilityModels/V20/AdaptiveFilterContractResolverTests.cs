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

namespace AasxCsharpLibary.Tests.AasxCompatibilityModels.V20;

using System.Text.Json;
using global::AasxCompatibilityModels;

public class AdaptiveFilterContractResolverTests
{
    private readonly Fixture _fixture;

    public AdaptiveFilterContractResolverTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void AdaptiveFilterContractResolver_CanConvert_Should_Return_True_For_AdministrationShell()
    {
        // Arrange
        var resolver      = new AdminShell_V20.AdminShellConverters.AdaptiveFilterContractResolver();
        var typeToConvert = typeof(AdminShellV20.AdministrationShell);

        // Act
        var result = resolver.CanConvert(typeToConvert);

        // Assert
        result.Should().BeTrue();
    }
}