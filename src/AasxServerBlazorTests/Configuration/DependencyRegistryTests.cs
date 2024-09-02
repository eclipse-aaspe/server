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

using Microsoft.Extensions.DependencyInjection;
using AasxServerBlazor.Configuration;
using AasxServerBlazor.Data;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

public class DependencyRegistryTests
{
    [Fact]
    public void Register_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services

        // Act
        DependencyRegistry.Register(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Verify Singleton Registrations
        serviceProvider.GetService<AASService>().Should().NotBeNull();
        serviceProvider.GetService<CredentialService>().Should().NotBeNull();
        serviceProvider.GetService<IHttpContextAccessor>().Should().NotBeNull();
        serviceProvider.GetService<IRegistryInitializerService>().Should().NotBeNull();
        serviceProvider.GetService<IAuthorizationHandler>().Should().NotBeNull();

        // Verify Scoped Registrations
        serviceProvider.GetService<BlazorSessionService>().Should().NotBeNull();
    }
}
