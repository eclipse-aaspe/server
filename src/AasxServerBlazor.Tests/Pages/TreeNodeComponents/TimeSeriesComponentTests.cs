using AasCore.Aas3_0;
using AasxServerBlazor.Data;
using AasxServerBlazor.Pages.TreeNodeComponents;
using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using AutoFixture.AutoMoq;
using Bunit;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Key = AasCore.Aas3_0.Key;

namespace AasxServerBlazor.Tests.Pages.TreeNodeComponents
{
    public class TimeSeriesComponentTests : TestContext
    {
        private readonly Fixture _fixture;
        private readonly Mock<BlazorSessionService> _blazorSessionServiceMock;

        public TimeSeriesComponentTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _blazorSessionServiceMock = new Mock<BlazorSessionService>();

            // Add the BlazorSessionService mock to the service collection
            Services.AddSingleton(_blazorSessionServiceMock.Object);
        }

        [Fact(Skip = "We need interface we can Mock before we can test correctly")]
        public void Render_SelectedNode_Tag_Is_SubmodelElementCollection_Should_Render_TimeSeriesData()
        {
            // Arrange
            var selectedNode = _fixture.Create<TreeItem>();
            var submodelElementCollection = _fixture.Create<SubmodelElementCollection>();
            var reference = _fixture.Create<IReference>();
            reference.Keys = new List<IKey>(new[] {_fixture.Create<Key>()});
            submodelElementCollection.SemanticId = reference;
            selectedNode.Tag = submodelElementCollection;
            var cut = RenderComponent<TimeSeriesComponent>(parameters => parameters
                .Add(p => p.SelectedNode, selectedNode));

            // Act - Rendering is done in Arrange phase

            // Assert
            cut.MarkupMatches(@"
<EditForm>
    <!-- Add expected HTML markup here -->
</EditForm>
<div>
    <!-- Add expected HTML markup here -->
</div>
<div class=""modal modal-fullscreen d-none"" tabindex=""-1"">
    <!-- Add expected HTML markup here -->
</div>
");
        }
    }
}