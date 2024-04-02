using System.Text;
using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation.Builders;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using File = AasCore.Aas3_0.File;
using Range = AasCore.Aas3_0.Range;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders;

public class NodeDescriptionBuilderTests
{
    private readonly Fixture _fixture;

    public NodeDescriptionBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void AppendSubmodelInfo_WhenQualifiersNotNull_ShouldAppendQualifiersString()
    {
        // Arrange
        var submodel = _fixture.Build<Submodel>().With(s => s.Qualifiers, _fixture.CreateMany<IQualifier>(2).ToList()).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendSubmodelInfo(submodel, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain("@QUALIFIERS");
    }

    [Fact]
    public void AppendSubmodelInfo_WhenQualifiersNull_ShouldNotAppendQualifiersString()
    {
        // Arrange
        var submodel = _fixture.Build<Submodel>().With(s => s.Qualifiers, (List<IQualifier>) null).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendSubmodelInfo(submodel, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("@QUALIFIERS");
    }

    [Fact]
    public void AppendCollectionInfo_WhenValueCollectionNotNull_ShouldAppendCountString()
    {
        // Arrange
        var collection = _fixture.Build<SubmodelElementCollection>().With(c => c.Value, _fixture.CreateMany<ISubmodelElement>(3).ToList()).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendCollectionInfo(collection, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain("#3");
    }

    [Fact]
    public void AppendCollectionInfo_WhenValueCollectionNull_ShouldNotAppendCountString()
    {
        // Arrange
        var collection = _fixture.Build<SubmodelElementCollection>().With(c => c.Value, (List<ISubmodelElement>) null).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendCollectionInfo(collection, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("#");
    }

    [Fact]
    public void AppendPropertyInfo_WhenValueNotNull_ShouldAppendValueString()
    {
        // Arrange
        var property = _fixture.Build<Property>().With(p => p.Value, "SomeValue").Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendPropertyInfo(property, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain(" = SomeValue");
    }

    [Fact]
    public void AppendPropertyInfo_WhenValueNull_ShouldNotAppendValueString()
    {
        // Arrange
        var property = _fixture.Build<Property>().With(p => p.Value, (string) null).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendPropertyInfo(property, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("=");
    }

    [Fact]
    public void AppendFileInfo_WhenValueNotNull_ShouldAppendValueString()
    {
        // Arrange
        var file = _fixture.Build<File>().With(f => f.Value, "FileValue").Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendFileInfo(file, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain(" = FileValue");
    }

    [Fact]
    public void AppendFileInfo_WhenValueNull_ShouldNotAppendValueString()
    {
        // Arrange
        var file = _fixture.Build<File>().With(f => f.Value, (string) null).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendFileInfo(file, nodeInfoBuilder);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("=");
    }

    [Fact]
    public void AppendAdditionalInfo_WhenTagIsRangeAndMinAndMaxNotNull_ShouldAppendRangeString()
    {
        // Arrange
        var rangeObject = _fixture.Build<Range>().With(r => r.Min, "MinValue").With(r => r.Max, "MaxValue").Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, rangeObject);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain(" = MinValue .. MaxValue");
    }

    [Fact]
    public void AppendAdditionalInfo_WhenTagIsRangeAndMinOrMaxNull_ShouldNotAppendRangeString()
    {
        // Arrange
        var rangeObject = _fixture.Build<Range>().With(r => r.Min, (string) null).With(r => r.Max, "MaxValue").Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, rangeObject);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("=");
    }

    [Fact]
    public void AppendAdditionalInfo_WhenTagIsMultiLanguageProperty_ShouldAppendMultiLanguagePropertyString()
    {
        // Arrange
        var langStringTextType = new LangStringTextType("en", "English");
        var multiLanguageProperty = _fixture.Build<MultiLanguageProperty>()
            .With(p => p.Value, new List<ILangStringTextType> {langStringTextType})
            .Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, multiLanguageProperty);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain(" = en English");
    }

    [Fact]
    public void AppendAdditionalInfo_WhenTagIsMultiLanguagePropertyAndValueNull_ShouldNotAppendMultiLanguagePropertyString()
    {
        // Arrange
        var multiLanguageProperty = _fixture.Build<MultiLanguageProperty>().With(p => p.Value, (List<ILangStringTextType>) null).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, multiLanguageProperty);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("=");
    }


    [Fact]
    public void AppendAdditionalInfo_WhenTagIsMultiLanguagePropertyAndQualifiersNotNull_ShouldAppendQualifiersString()
    {
        // Arrange
        var multiLanguageProperty = _fixture.Build<MultiLanguageProperty>()
            .With(p => p.Qualifiers, _fixture.CreateMany<IQualifier>(2).ToList())
            .Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, multiLanguageProperty);

        // Assert
        nodeInfoBuilder.ToString().Should().Contain("@QUALIFIERS");
    }

    [Fact]
    public void AppendAdditionalInfo_WhenTagIsMultiLanguagePropertyAndQualifiersNull_ShouldNotAppendQualifiersString()
    {
        // Arrange
        var multiLanguageProperty = _fixture.Build<MultiLanguageProperty>().With(p => p.Qualifiers, (List<IQualifier>) null).Create();
        var nodeInfoBuilder = new StringBuilder();

        // Act
        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, multiLanguageProperty);

        // Assert
        nodeInfoBuilder.ToString().Should().NotContain("@QUALIFIERS");
    }
}