using System.Reflection;
using AasxServerBlazor.DateTimeServices;
using AasxServerBlazor.TreeVisualisation;
using AasxServerStandardBib;
using AutoFixture;
using AutoFixture.AutoMoq;
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
        filter.StartDate.Date.Should().Be(expectedStartDate.Date);
        filter.EndDate.Date.Should().Be(expectedEndDate.Date);
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

    [Fact]
    public void SetFilterDates_SetsCorrectDates()
    {
        // Arrange
        _fixture.Customize(new AutoMoqCustomization());
        var filter = _fixture.Create<PlotFilter>();
        var startDate = _fixture.Create<DateTime>().Date;
        var startTime = DateTime.MinValue.Date.Add(_fixture.Create<TimeSpan>());
        var endDate = _fixture.Create<DateTime>().Date;
        var endTime = DateTime.MinValue.Date.Add(_fixture.Create<TimeSpan>());

        // Act
        var methodInfo = typeof(PlotFilter).GetMethod("SetFilterDates", BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo!.Invoke(filter, new object[] { startDate, startTime, endDate, endTime });

        // Assert
        filter.StartDate.Date.Should().Be(startDate);
        filter.EndDate.Date.Should().Be(endDate);
    }

    [Fact]
    public void ResetTimeOfDates_SetsCorrectTime()
    {
        // Arrange
        _fixture.Customize(new AutoMoqCustomization());
        var filter = _fixture.Create<PlotFilter>();
        var startDate = _fixture.Create<DateTime>().Date;
        var endDate = startDate.AddDays(1).AddTicks(-1);
        filter.StartDate = startDate;
        filter.EndDate = endDate;

        // Act
        var methodInfo = typeof(PlotFilter).GetMethod("ResetTimeOfDates", BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo!.Invoke(filter, null);

        // Assert
        filter.StartDate.Should().Be(startDate);
        filter.EndDate.Should().Be(endDate);
        filter.CombinedStartDateTime.Date.Should().Be(startDate);
        filter.CombinedEndDateTime.Date.Should().BeCloseTo(endDate,precision: TimeSpan.FromHours(6) );
    }

    [Fact]
    public void ApplyFilterToTimeSeriesData_DoesNotThrowIfTimeSeriesDataIsNull()
    {
        // Arrange
        _fixture.Customize(new AutoMoqCustomization());
        var filter = _fixture.Create<PlotFilter>();

        // Act
        var act = () => filter.ApplyFilterToTimeSeriesData(null, 1);

        // Assert
        act.Should().NotThrow();
    }
}