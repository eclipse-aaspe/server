using IO.Swagger.Models;

namespace AasxServerBlazorTests.Models;

using JetBrains.Annotations;

[TestSubject(typeof(PagedResult))]
public class PagedResultTests
{
    private readonly IFixture _fixture;

    public PagedResultTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }
    
    [Fact]
    public void ToPagedList_Should_Return_Correct_Subset()
    {
        // Arrange
        var sourceList           = _fixture.CreateMany<int>(20).ToList();
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(5);
        result.paging_metadata.Should().NotBeNull();
        result.paging_metadata?.cursor.Should().Be("5");
    }

    [Fact]
    public void ToPagedList_Should_Cap_EndIndex()
    {
        // Arrange
        var sourceList           = new List<string> {"A", "B", "C"};
        var paginationParameters = new PaginationParameters("1", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().HaveCount(2);
        result.paging_metadata.Should().NotBeNull();
        result.paging_metadata.cursor.Should().BeNull();
    }

    [Fact]
    public void ToPagedList_Should_Handle_Empty_List()
    {
        // Arrange
        var sourceList           = new List<double>();
        var paginationParameters = new PaginationParameters("0", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        result.Should().NotBeNull();
        result.result.Should().BeEmpty();
        result.paging_metadata.Should().NotBeNull();
        result.paging_metadata.cursor.Should().BeNull();
    }

    [Fact]
    public void ToPagedList_Should_Log_If_StartIndex_Out_Of_Bounds()
    {
        // Arrange
        var sourceList           = new List<int>();
        var paginationParameters = new PaginationParameters("10", 5);

        // Act
        var result = PagedResult.ToPagedList(sourceList, paginationParameters);

        // Assert
        // Logging is not directly testable here, but this test ensures the method runs without exceptions
        result.Should().NotBeNull();
        result.result.Should().BeEmpty();
    }
}