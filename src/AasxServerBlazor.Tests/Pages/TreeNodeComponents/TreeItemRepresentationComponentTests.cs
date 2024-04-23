using System.IO;
using System.Linq;
using AasCore.Aas3_0;
using AasSecurity;
using AasxServer;
using AasxServerBlazor.Pages.TreeNodeComponents;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders;
using AutoFixture;
using AutoFixture.AutoMoq;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AasxServerBlazor.Tests.Pages.TreeNodeComponents
{
    public class TreeItemRepresentationComponentTests : TestContext
    {
        private readonly Fixture _fixture;
        private readonly Mock<ISecurityService> _securityServiceMock;

        public TreeItemRepresentationComponentTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _securityServiceMock = new Mock<ISecurityService>();

            // Add the SecurityService mock to the service collection
            Services.AddSingleton(_securityServiceMock.Object);
        }

        [Fact]
        public void Render_SelectedNode_Tag_Not_String_Should_Render_NodeRepresentation_And_Identifier()
        {
            // Arrange
            var selectedNode = _fixture.Create<TreeItem>();
            selectedNode.Tag = _fixture.Create<object>();
            var expectedNodeRepresentation = selectedNode.BuildNodeRepresentation();
            var expectedIdentifier = selectedNode.GetIdentifier();
            var cut = RenderComponent<TreeItemRepresentationComponent>(parameters => parameters
                .Add(p => p.SelectedNode, selectedNode));

            // Act - Rendering is done in Arrange phase

            // Assert
            cut.Find("#SelectedNodeInfoType").TextContent.Should().Be(expectedNodeRepresentation);
            cut.Find("#SelectedNodeInfoId").TextContent.Should().Be(expectedIdentifier);
        }

        [Fact(Skip = "cannot be tested without mockable file reading")]
        public void Render_SelectedNode_Text_Contains_Readme_Should_Render_FileContents()
        {
            // Arrange
            var selectedNode = _fixture.Create<TreeItem>();
            selectedNode.Text = "/some/path/readme.txt";
            var fileContents = "Sample readme content";
            selectedNode.Tag = fileContents;
            _securityServiceMock.Setup(x => x.GetSecurityRules()).Returns("Sample\tReadme\tContent");
            var expectedMarkupString = new MarkupString(fileContents);
            var cut = RenderComponent<TreeItemRepresentationComponent>(parameters => parameters
                .Add(p => p.SelectedNode, selectedNode));

            // Act - Rendering is done in Arrange phase

            // Assert
            cut.Find("span").TextContent.Should().Contain(fileContents);
        }

        [Fact(Skip = "Cannot test because of System.IO.File.ReadAllText")]
        public void Render_SelectedNode_Tag_Is_String_Should_Render_AccessRules_Table()
        {
            // Arrange
            var selectedNode = _fixture.Create<TreeItem>();
            selectedNode.Tag = "example_tag";
            var accessRules = "/readme/%ACCESSRULES%";
            selectedNode.Text = accessRules;
            _securityServiceMock.Setup(x => x.GetSecurityRules()).Returns(accessRules);
            var cut = RenderComponent<TreeItemRepresentationComponent>(parameters => parameters
                .Add(p => p.SelectedNode, selectedNode));

            // Act - Rendering is done in Arrange phase

            // Assert
            var table = cut.Find("table");
            table.MarkupMatches($"<table.*></table>");
        }

        [Fact]
        public void Render_SelectedNode_Tag_Is_String_Without_Readme_Should_Render_Tag_Text()
        {
            // Arrange
            var selectedNode = _fixture.Create<TreeItem>();
            selectedNode.Tag = _fixture.Create<AssetAdministrationShell>();
            var cut = RenderComponent<TreeItemRepresentationComponent>(parameters => parameters
                .Add(p => p.SelectedNode, selectedNode));

            // Act - Rendering is done in Arrange phase

            // Assert
            cut.Find("span").TextContent.Should().Contain("AAS");
        }

        [Fact(Skip = "Cannot test because of System.IO.File.ReadAllText")]
        public void Render_SelectedNode_Tag_Is_Null_Should_Not_Render_Anything()
        {
            // Arrange
            var selectedNode = _fixture.Create<TreeItem>();
            selectedNode.Tag = null;
            var cut = RenderComponent<TreeItemRepresentationComponent>(parameters => parameters
                .Add(p => p.SelectedNode, selectedNode));

            // Act - Rendering is done in Arrange phase

            // Assert
            cut.MarkupMatches(""); // Assert that no markup is rendered
        }
    }
}