using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using AasxServerBlazor.TreeVisualisation;
using Moq.Protected;
using Xunit;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class BaseTreeDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public BaseTreeDetailsBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void Build_ShouldReturnNull_WhenTreeItemIsNull()
    {
        // Arrange
        var builder = new Mock<BaseTreeDetailsBuilder>() { CallBase = true };
        TreeItem treeItem = null!;
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Object.Build(treeItem, line, column);

        // Assert
        result.Should().BeNull();
    }
}