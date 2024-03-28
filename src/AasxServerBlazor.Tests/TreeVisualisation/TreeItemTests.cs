using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using AutoFixture.AutoMoq;

namespace AasxServerBlazor.Tests.TreeVisualisation;

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

public class TreeItemTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Constructor_WithDefaultValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        // Act
        var treeItem = new TreeItem();

        // Assert
        treeItem.Text.Should().BeNull();
        treeItem.Childs.Should().BeNull();
        treeItem.Parent.Should().BeNull();
        treeItem.Type.Should().BeNull();
        treeItem.Tag.Should().BeNull();
        treeItem.EnvironmentIndex.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string text = "Sample Text";
        var childs = new List<TreeItem>();
        var parent = new object();
        const string type = "Sample Type";
        var tag = new object();
        const int environmentIndex = 1;

        // Act
        var treeItem = new TreeItem
        {
            Text = text,
            Childs = childs,
            Parent = parent,
            Type = type,
            Tag = tag,
            EnvironmentIndex = environmentIndex
        };

        // Assert
        treeItem.Text.Should().Be(text);
        treeItem.Childs.Should().BeEquivalentTo(childs);
        treeItem.Parent.Should().Be(parent);
        treeItem.Type.Should().Be(type);
        treeItem.Tag.Should().Be(tag);
        treeItem.EnvironmentIndex.Should().Be(environmentIndex);
    }

    [Fact]
    public void ToString_ReturnsExpectedString()
    {
        // Arrange
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        var treeItem = _fixture.Create<TreeItem>();
        var childItem1 = _fixture.Create<TreeItem>();
        var childItem2 = _fixture.Create<TreeItem>();
        var childOfChild2 = _fixture.Create<TreeItem>();
        childItem2.Childs = new[] { childOfChild2 };
        treeItem.Childs = new[] { childItem1, childItem2 };

        // Act
        var result = treeItem.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain($"Text: {treeItem.Text}");
        result.Should().Contain($"Type: {treeItem.Type}");
        result.Should().Contain($"EnvironmentIndex: {treeItem.EnvironmentIndex}");

        foreach (var child in treeItem.Childs)
        {
            result.Should().Contain(child.ToString());
        }
    }
}
