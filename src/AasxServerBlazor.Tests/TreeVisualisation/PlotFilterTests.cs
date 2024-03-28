using AasxServerBlazor.DateTimeServices;
using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using FluentAssertions;
using Moq;

namespace AasxServerBlazor.Tests.TreeVisualisation;

public class PlotFilterTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void SetInitialFilterState_SetsCorrectInitialState()
    {
        // Arrange
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var currentDateTime = _fixture.Create<DateTime>();
        dateTimeProviderMock.Setup(x => x.GetCurrentDateTime()).Returns(currentDateTime);

        var filter = new PlotFilter(dateTimeProviderMock.Object);

        // Act
        filter.SetInitialFilterState();

        // Assert
        var expectedStartDate = currentDateTime.AddYears(-3).Date;
        var expectedEndDate = currentDateTime.AddYears(3).Date.AddDays(1).AddTicks(-1);
        filter.StartDate.Should().Be(expectedStartDate);
        filter.EndDate.Should().Be(expectedEndDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public void SetFilterStateForDay_SetsCorrectStateForGivenOffset(int offset)
    {
        // Arrange
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var currentDateTime = _fixture.Create<DateTime>();
        dateTimeProviderMock.Setup(x => x.GetCurrentDate()).Returns(currentDateTime.Date);
        var filter = new PlotFilter(dateTimeProviderMock.Object);

        // Act
        filter.SetFilterStateForDay(offset);

        // Assert
        var expectedStartDate = currentDateTime.AddDays(offset).Date;
        var expectedEndDate = expectedStartDate.Date.AddDays(1).AddTicks(-1);
        filter.StartDate.Should().Be(expectedStartDate);
        filter.EndDate.Should().Be(expectedEndDate);
    }

    [Fact]
    public void ApplyFilterToTimeSeriesData_DoesNotRenderIfTimeSeriesDataIsNull()
    {
        // Arrange
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var filter = new PlotFilter(dateTimeProviderMock.Object);

        // Act
        var action = () => filter.ApplyFilterToTimeSeriesData(null, 1);

        // Assert
        action.Should().NotThrow();
    }
}