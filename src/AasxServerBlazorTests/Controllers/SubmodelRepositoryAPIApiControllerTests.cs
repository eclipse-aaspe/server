using IO.Swagger.Controllers;

namespace AasxServerBlazorTests.Controllers;

using System.Security.Claims;
using AasCore.Aas3_0;
using AasxServer;
using AasxServerStandardBib.Interfaces;
using IO.Swagger.Lib.V3.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Claim = System.IdentityModel.Claims.Claim;

[TestSubject(typeof(SubmodelRepositoryAPIApiController))]
public class SubmodelRepositoryAPIApiControllerTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IBase64UrlDecoderService> _decoderServiceMock;
    private readonly Mock<ISubmodelService> _submodelServiceMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly SubmodelRepositoryAPIApiController _controller;

    public SubmodelRepositoryAPIApiControllerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());

        _decoderServiceMock       = _fixture.Freeze<Mock<IBase64UrlDecoderService>>();
        _submodelServiceMock      = _fixture.Freeze<Mock<ISubmodelService>>();
        _authorizationServiceMock = _fixture.Freeze<Mock<IAuthorizationService>>();

        _controller = _fixture.Create<SubmodelRepositoryAPIApiController>();
    }

    [Theory]
    [InlineData("asdf1234")]
    [InlineData("")]
    [InlineData(" ")]
    public void DeleteFileByPathSubmodelRepo_ValidRequest_ReturnsNoContent(string submodelIdentifier)
    {
        // Arrange
        var decodedSubmodelIdentifier = _fixture.Create<string>();
        var idShortPath        = _fixture.Create<string>();

        Program.noSecurity = true;

        _decoderServiceMock.Setup(x => x.Decode("submodelIdentifier", submodelIdentifier)).Returns(decodedSubmodelIdentifier);

        // Act
        var result = _controller.DeleteFileByPathSubmodelRepo(submodelIdentifier, idShortPath);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _submodelServiceMock.Verify(x => x.DeleteFileByPath(decodedSubmodelIdentifier, idShortPath), Times.Once);
    }
}