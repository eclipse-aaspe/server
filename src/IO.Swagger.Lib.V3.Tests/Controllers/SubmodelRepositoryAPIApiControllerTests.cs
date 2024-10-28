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

using IO.Swagger.Controllers;

namespace AasxServerBlazorTests.Controllers;

using System.Reflection.Emit;
using System.Security.Claims;
using System.Security.Principal;
using System.Xml.Linq;
using AasCore.Aas3_0;
using AasxServer;
using AasxServerStandardBib.Interfaces;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

[TestSubject(typeof(SubmodelRepositoryAPIApiController))]
public class SubmodelRepositoryAPIApiControllerTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IBase64UrlDecoderService> _decoderServiceMock;
    private readonly Mock<ISubmodelService> _submodelServiceMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IPaginationService> _paginationServiceMock;
    private readonly SubmodelRepositoryAPIApiController _controller;

    public SubmodelRepositoryAPIApiControllerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _decoderServiceMock       = _fixture.Freeze<Mock<IBase64UrlDecoderService>>();
        _submodelServiceMock      = _fixture.Freeze<Mock<ISubmodelService>>();
        _authorizationServiceMock = _fixture.Freeze<Mock<IAuthorizationService>>();
        _paginationServiceMock    = _fixture.Freeze<Mock<IPaginationService>>();

        _controller = _fixture.Create<SubmodelRepositoryAPIApiController>();
    }

    #region DeleteFileByPathSubmodelRepo

    [Theory]
    [InlineData("asdf1234")]
    [InlineData("")]
    [InlineData(" ")]
    public void DeleteFileByPathSubmodelRepo_WithValidRequest_CorrectlyCallsSubmodelDeletionPath(string submodelIdentifier)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();
        var idShortPath               = _fixture.Create<string>();

        Program.noSecurity = true;

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);

        // Act
        var result = _controller.DeleteFileByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _submodelServiceMock.Verify(x => x.DeleteFileByPath(decodedSubmodelIdentifier, idShortPath), Times.Once);
    }

    [Theory]
    [InlineData("asdf1234")]
    [InlineData("")]
    [InlineData(" ")]
    public void DeleteFileByPathSubmodelRepo_WithValidRequestAndNoSecurity_CorrectlyCallsSubmodelDeletionPath(string submodelIdentifier)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();
        var idShortPath               = _fixture.Create<string>();
        var submodel                  = _fixture.Create<ISubmodel>();

        _fixture.Freeze<HttpContext>();

        Program.noSecurity = false;

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);

        _submodelServiceMock.Setup(x => x.GetSubmodelById(submodelIdentifier)).Returns(submodel);
        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<GenericPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var result = _controller.DeleteFileByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _submodelServiceMock.Verify(x => x.DeleteFileByPath(decodedSubmodelIdentifier, idShortPath), Times.Once);
    }

    [Theory]
    [InlineData("asdf1234")]
    [InlineData("")]
    [InlineData(" ")]
    public void DeleteFileByPathSubmodelRepo_WhenAuthorizationResultIsFailed_WillThrowNotAllowed(string submodelIdentifier)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();
        var idShortPath               = _fixture.Create<string>();
        var submodel                  = _fixture.Create<ISubmodel>();

        _fixture.Freeze<HttpContext>();

        Program.noSecurity = false;

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);

        _submodelServiceMock.Setup(x => x.GetSubmodelById(submodelIdentifier)).Returns(submodel);

        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<GenericPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        Action action = () => _controller.DeleteFileByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>();
        _submodelServiceMock.Verify(x => x.DeleteFileByPath(decodedSubmodelIdentifier, idShortPath), Times.Never);
    }

    [Fact]
    public void DeleteFileByPathSubmodelRepo_WhenDecodingReturnedNull_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var idShortPath        = _fixture.Create<string>();
        var submodel           = _fixture.Create<ISubmodel>();

        _fixture.Freeze<HttpContext>();

        Program.noSecurity = false;

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns((string)null);

        _submodelServiceMock.Setup(x => x.GetSubmodelById(submodelIdentifier)).Returns(submodel);

        // Act
        Action action = () => _controller.DeleteFileByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>().WithMessage($"Decoding {submodelIdentifier} returned null");
        _submodelServiceMock.Verify(x => x.DeleteFileByPath(It.IsAny<string>(), idShortPath), Times.Never);
    }

    #endregion

    #region DeleteSubmodelById

    [Theory]
    [InlineData("identifier")]
    [InlineData(" ")]
    [InlineData("")]
    public void DeleteSubmodelById_CallsDeleteSubmodelById_ForAnySubmodeIdentifier(string submodelIdentifier)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);

        // Act
        var result = _controller.DeleteSubmodelById(submodelIdentifier);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _submodelServiceMock.Verify(x => x.DeleteSubmodelById(decodedSubmodelIdentifier), Times.Once);
    }

    [Fact]
    public void DeleteSubmodelById_WhenDecodingReturnedNull_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns((string)null);

        // Act
        Action action = () => _controller.DeleteSubmodelById(submodelIdentifier);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>().WithMessage($"Decoding {submodelIdentifier} returned null");
        _submodelServiceMock.Verify(x => x.DeleteSubmodelById(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region DeleteSubmodelElementByPathSubmodelRepo

    [Theory]
    [InlineData("asdf1234", "path123")]
    [InlineData("", "path456")]
    [InlineData(" ", "")]
    public void DeleteSubmodelElementByPathSubmodelRepo_WithValidRequest_CorrectlyCallsSubmodelElementDeletion(string submodelIdentifier, string idShortPath)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);
        Program.noSecurity = true; // Ensure security setting is appropriate for the test

        // Act
        var result = _controller.DeleteSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _submodelServiceMock.Verify(x => x.DeleteSubmodelElementByPath(decodedSubmodelIdentifier, idShortPath), Times.Once);
    }

    [Fact]
    public void DeleteSubmodelElementByPathSubmodelRepo_WhenDecodingReturnedNull_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var idShortPath        = _fixture.Create<string>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns((string)null);

        // Act
        Action action = () => _controller.DeleteSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>().WithMessage($"Decoding {submodelIdentifier} returned null");
        _submodelServiceMock.Verify(x => x.DeleteSubmodelElementByPath(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void DeleteSubmodelElementByPathSubmodelRepo_WhenAuthorizationFails_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var idShortPath        = _fixture.Create<string>();
        var submodel           = _fixture.Create<ISubmodel>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(submodelIdentifier);

        _submodelServiceMock.Setup(x => x.GetSubmodelById(submodelIdentifier)).Returns(submodel);

        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<GenericPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Failed()));

        // Act
        Action action = () => _controller.DeleteSubmodelElementByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>();
        _submodelServiceMock.Verify(x => x.DeleteSubmodelElementByPath(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region GetAllSubmodelElements

    [Theory(Skip = "Need a way to instanciate the page result")]
    [InlineData("asdf1234", 10, "cursor123", LevelEnum.Deep, ExtentEnum.WithoutBlobValue, "2023-01-01T00:00:00Z")]
    [InlineData("", null, null, LevelEnum.Deep, ExtentEnum.WithBlobValue, null)]
    [InlineData(" ", 5, "cursor456", LevelEnum.Core, ExtentEnum.WithBlobValue, "2022-06-01T00:00:00Z")]
    public void GetAllSubmodelElements_WithValidRequest_ReturnsPagedResult(string submodelIdentifier, int? limit, string cursor, LevelEnum level, ExtentEnum extent, string diff)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();
        var submodelElements          = new List<ISubmodelElement>(); // mock or actual data as needed

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);
        _submodelServiceMock.Setup(x => x.GetAllSubmodelElements(decodedSubmodelIdentifier)).Returns(submodelElements);


        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Success()));
        var levelString = nameof(level);
        var extentString = nameof(extent);
        // Act
        var result = _controller.GetAllSubmodelElements(submodelIdentifier, limit, cursor, levelString, extentString, diff);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Value.Should().BeOfType<PagedResult>();

        var pagedResult = objectResult.Value as PagedResult;
        pagedResult.result.Should().NotBeNull();
    }

    [Fact]
    public void GetAllSubmodelElements_WhenDecodingReturnedNull_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns((string)null);

        var levelString = nameof(LevelEnum.Deep);
        var extentString = nameof(ExtentEnum.WithoutBlobValue);

        // Act
        Action action = () => _controller.GetAllSubmodelElements(submodelIdentifier, null, null, levelString, extentString, null);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>().WithMessage($"Decoding {submodelIdentifier} returned null");
        _submodelServiceMock.Verify(x => x.GetAllSubmodelElements(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetAllSubmodelElements_WhenAuthorizationFails_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var submodelElements   = new List<ISubmodelElement>(); // mock or actual data as needed

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(submodelIdentifier);
        _submodelServiceMock.Setup(x => x.GetAllSubmodelElements(submodelIdentifier)).Returns(submodelElements);
        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Failed()));

        Program.noSecurity = false;

        var levelString = nameof(LevelEnum.Core);
        var extentString = nameof(ExtentEnum.WithBlobValue);

        // Act
        Action action = () => _controller.GetAllSubmodelElements(submodelIdentifier, null, null, levelString, extentString, null);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>();
        _submodelServiceMock.Verify(x => x.GetAllSubmodelElements(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetAllSubmodelElements_WithValidRequestAndDiffParameter_FiltersSubmodelElements()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var submodelElements   = new List<ISubmodelElement>(); // mock or actual data as needed

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(submodelIdentifier);
        _submodelServiceMock.Setup(x => x.GetAllSubmodelElements(submodelIdentifier)).Returns(submodelElements);

        var diffDateTime       = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var diffParameterValue = diffDateTime; // Set the diff parameter value as per test needs


        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Success()));

        //_paginationServiceMock.Setup(x => x.GetPaginatedList(It.IsAny<List<ISubmodelElement>>(), It.IsAny<PaginationParameters>()))
        //                      .Returns((List<ISubmodelElement> elements, PaginationParameters parameters) => new List<ISubmodelElement>(elements));

        var levelString = nameof(LevelEnum.Deep);
        var extentString = nameof(ExtentEnum.WithoutBlobValue);

        // Act
        Action action = () => _controller.GetAllSubmodelElements(submodelIdentifier, null, null, levelString, extentString, diffParameterValue);

        // Assert
        action.Should().NotThrow(); // Ensure no exception is thrown
        _submodelServiceMock.Verify(x => x.GetAllSubmodelElements(submodelIdentifier), Times.Once);
    }

    #endregion

    #region GetAllSubmodelElementsPathSubmodelRepo

    [Theory]
    [InlineData("asdf1234", 10, "cursor123", "Deep", "2023-01-01T00:00:00Z")]
    [InlineData("", null, null, "Deep", null)]
    [InlineData(" ", 5, "cursor456", "Core", "2022-06-01T00:00:00Z")]
    public void GetAllSubmodelElementsPathSubmodelRepo_WithValidRequest_ReturnsSubmodelElements(string submodelIdentifier, int? limit, string cursor, string level, string diff)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();
        var submodelElements          = new List<ISubmodelElement>();
        submodelElements.Add(_fixture.Create<ISubmodelElement>());

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);
        _submodelServiceMock.Setup(x => x.GetAllSubmodelElements(decodedSubmodelIdentifier)).Returns(submodelElements);

        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var result = _controller.GetAllSubmodelElementsPathSubmodelRepo(submodelIdentifier, limit, cursor, level, diff);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Value.Should().BeAssignableTo<List<List<string>>>();

        var pathList = objectResult.Value as List<List<string>>;
        pathList.Should().NotBeNull();
    }

    [Fact]
    public void GetAllSubmodelElementsPathSubmodelRepo_WhenDecodingReturnedNull_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns((string)null);

        // Act
        Action action = () => _controller.GetAllSubmodelElementsPathSubmodelRepo(submodelIdentifier, null, null, "Deep", null);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>().WithMessage($"Decoding {submodelIdentifier} returned null");
        _submodelServiceMock.Verify(x => x.GetAllSubmodelElements(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetAllSubmodelElementsPathSubmodelRepo_WhenAuthorizationFails_ThrowsNotAllowedException()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var submodelElements   = new List<ISubmodelElement>(); // mock or actual data as needed

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(submodelIdentifier);
        _submodelServiceMock.Setup(x => x.GetAllSubmodelElements(submodelIdentifier)).Returns(submodelElements);
        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Failed()));

        Program.noSecurity = false;

        // Act
        Action action = () => _controller.GetAllSubmodelElementsPathSubmodelRepo(submodelIdentifier, null, null, "Core", null);

        // Assert
        action.Should().ThrowExactly<AasSecurity.Exceptions.NotAllowed>();
        _submodelServiceMock.Verify(x => x.GetAllSubmodelElements(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetAllSubmodelElementsPathSubmodelRepo_WithValidRequestAndDiffParameter_FiltersSubmodelElements()
    {
        // Arrange
        var submodelIdentifier = _fixture.Create<string>();
        var submodelElements   = new List<ISubmodelElement>(); // mock or actual data as needed

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(submodelIdentifier);
        _submodelServiceMock.Setup(x => x.GetAllSubmodelElements(submodelIdentifier)).Returns(submodelElements);

        var diffDateTime       = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var diffParameterValue = diffDateTime; // Set the diff parameter value as per test needs

        _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISubmodel>(), "SecurityPolicy"))
                                 .Returns(Task.FromResult(AuthorizationResult.Success()));

        // Act
        var result = _controller.GetAllSubmodelElementsPathSubmodelRepo(submodelIdentifier, null, null, "Deep", diffParameterValue);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Value.Should().BeAssignableTo<List<List<string>>>();
        
        var pathList = objectResult.Value as List<List<string>>;
        pathList.Should().NotBeNull();
    }

    #endregion
}