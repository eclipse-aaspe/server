using System.Collections.Generic;
using System.Linq;
using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Xunit;

namespace AasxServerBlazor.Tests.TreeVisualisation;

public class TreePathTests
{
    private readonly Fixture _fixture;

    public TreePathTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenPathIsNull()
    {
        // Arrange
        IReadOnlyList<string> path = null;
        var items = new List<TreeItem>();

        // Act
        var result = TreePath.Find(path, items);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenPathIsEmpty()
    {
        // Arrange
        var path = new List<string>();
        var items = new List<TreeItem>();

        // Act
        var result = TreePath.Find(path, items);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenItemsListIsEmpty()
    {
        // Arrange
        var path = new List<string> {"A", "B", "C"};
        var items = new List<TreeItem>();

        // Act
        var result = TreePath.Find(path, items);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Find_ShouldReturnCorrectItem_WhenItemExistsInPath()
    {
        // Arrange
        var itemC = _fixture.Build<TreeItem>().With(i => i.Text, "C").Create();
        var itemB = _fixture.Build<TreeItem>().With(i => i.Text, "B").With(i => i.Childs, new List<TreeItem> {itemC}).Create();
        var itemA = _fixture.Build<TreeItem>().With(i => i.Text, "A").With(i => i.Childs, new List<TreeItem> {itemB}).Create();
        var items = new List<TreeItem> {itemA};
        var path = new List<string> {"A", "B", "C"};

        // Act
        var result = TreePath.Find(path, items);

        // Assert
        result.Should().Be(itemC);
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenPathDoesNotExist()
    {
        // Arrange
        var itemC = _fixture.Build<TreeItem>().With(i => i.Text, "C").Create();
        var itemB = _fixture.Build<TreeItem>().With(i => i.Text, "B").With(i => i.Childs, new List<TreeItem> {itemC}).Create();
        var itemA = _fixture.Build<TreeItem>().With(i => i.Text, "A").With(i => i.Childs, new List<TreeItem> {itemB}).Create();
        var items = new List<TreeItem> {itemA};
        var path = new List<string> {"A", "B", "D"}; // "D" doesn't exist

        // Act
        var result = TreePath.Find(path, items);

        // Assert
        result.Should().BeNull();
    }
}