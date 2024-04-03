using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using Extensions;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails
{
    public class AnnotatedRelationshipElementDetailsBuilderTests
    {
        private readonly Fixture _fixture;

        public AnnotatedRelationshipElementDetailsBuilderTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
        }

        [Fact]
        public void Build_ShouldReturnSemanticIdHeader_WhenLineIsZeroAndColumnIsZero()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Create<AnnotatedRelationshipElement>();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 0, 0);

            // Assert
            result.Should().Be("Semantic ID");
        }

        [Fact]
        public void Build_ShouldReturnNull_WhenLineIsZeroAndColumnIsOne()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Build<AnnotatedRelationshipElement>()
                .With(a => a.SemanticId, _fixture.Create<IReference>())
                .Create();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 0, 1);

            // Assert
            result.Should().Be("NULL");
        }

        [Fact]
        public void Build_ShouldReturnFirstHeader_WhenLineIsOneAndColumnIsZero()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Create<AnnotatedRelationshipElement>();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 1, 0);

            // Assert
            result.Should().Be("First");
        }

        [Fact]
        public void Build_ShouldReturnFirstValue_WhenLineIsOneAndColumnIsOne()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Build<AnnotatedRelationshipElement>()
                .With(a => a.First, _fixture.Create<IReference>())
                .Create();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 1, 1);

            // Assert
            result.Should().Be(annotatedRelationshipElement.First.Keys.ToStringExtended());
        }

        [Fact]
        public void Build_ShouldReturnSecondHeader_WhenLineIsTwoAndColumnIsZero()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Create<AnnotatedRelationshipElement>();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 2, 0);

            // Assert
            result.Should().Be("Second");
        }

        [Fact]
        public void Build_ShouldReturnSecondValue_WhenLineIsTwoAndColumnIsOne()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Build<AnnotatedRelationshipElement>()
                .With(a => a.Second, _fixture.Create<IReference>())
                .Create();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 2, 1);

            // Assert
            result.Should().Be(annotatedRelationshipElement.Second.Keys.ToStringExtended());
        }

        [Fact]
        public void Build_ShouldReturnQualifiers_WhenLineIsThreeAndColumnIsZero()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var qualifier = _fixture.Create<Qualifier>();
            var qualifiers = new List<IQualifier> {qualifier};
            var annotatedRelationshipElement = _fixture.Build<AnnotatedRelationshipElement>()
                .With(a => a.Qualifiers, qualifiers)
                .Create();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 3, 0);

            // Assert
            result.Should().Contain("Qualifiers, ");
        }

        [Fact]
        public void Build_ShouldReturnEmptyString_WhenLineIsFour()
        {
            // Arrange
            var builder = new AnnotatedRelationshipElementDetailsBuilder();
            var annotatedRelationshipElement = _fixture.Create<AnnotatedRelationshipElement>();
            var treeItem = new TreeItem { Tag = annotatedRelationshipElement };

            // Act
            var result = builder.Build(treeItem, 4, 0);

            // Assert
            result.Should().BeEmpty();
        }
    }
}
