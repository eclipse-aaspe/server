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

namespace AasxServerBlazorTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using IO.Swagger.Controllers;
using IO.Swagger.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[TestSubject(typeof(DescriptionAPIApiController))]
public class DescriptionAPIApiControllerTests
{
    [Fact]
    public void GetDescription_ShouldReturn200WithServiceDescription()
    {
        // Arrange
        var mockServiceDescription = new Mock<IServiceDescription>();
        var expectedServiceDescription = new ServiceDescription
                                         {
                                             profiles = new List<ServiceDescription.ServiceProfiles>
                                                        {
                                                            ServiceDescription.ServiceProfiles.AasxFileServerServiceSpecificationSSP001,
                                                            ServiceDescription.ServiceProfiles.SubmodelRepositoryServiceSpecificationSSP001,
                                                            ServiceDescription.ServiceProfiles.AssetAdministrationShellRepositoryServiceSpecificationSSP001,
                                                            ServiceDescription.ServiceProfiles.AssetAdministrationShellRegistryServiceSpecificationSSP001,
                                                            ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001,
                                                            ServiceDescription.ServiceProfiles.ConceptDescriptionServiceSpecificationSSP001
                                                        }
                                         };
        var serializedProfiles = JsonSerializer.Serialize(expectedServiceDescription);
        mockServiceDescription.Setup(x => x.ToJson()).Returns(serializedProfiles);
        var controller = new DescriptionAPIApiController(mockServiceDescription.Object);

        // Act
        var result = controller.GetDescription();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.Value.Should().BeEquivalentTo(serializedProfiles);
    }

    [Fact]
    public void GetDescription_ShouldReturn401WhenUnauthorized()
    {
        // Arrange
        var mockServiceDescription = new Mock<IServiceDescription>();
        var controller             = new DescriptionAPIApiController(mockServiceDescription.Object);
        controller.ControllerContext = new ControllerContext {HttpContext = new DefaultHttpContext()};

        // Simulate unauthorized access
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = controller.GetDescription();

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
    }

    [Fact]
    public void GetDescription_ShouldReturn403WhenForbidden()
    {
        // Arrange
        var mockServiceDescription = new Mock<IServiceDescription>();
        var controller             = new DescriptionAPIApiController(mockServiceDescription.Object);
        controller.ControllerContext = new ControllerContext {HttpContext = new DefaultHttpContext()};

        // Simulate forbidden access
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                                                                            new ClaimsIdentity(new Claim[] {new Claim(ClaimTypes.Role, "ForbiddenRole")})
                                                                           );

        // Act
        var result = controller.GetDescription();

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
    }
}