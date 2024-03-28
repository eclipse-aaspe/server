using System;
using FluentAssertions;
using Moq;
using Xunit;

namespace AasxServerBlazor.DateTimeServices.Tests;

public class SystemDateTimeProviderTests
{
    [Fact]
    public void GetCurrentDateTime_ReturnsCurrentDateTime()
    {
        // Arrange
        var dateTimeProvider = new SystemDateTimeProvider();

        // Act
        var currentDateTime = dateTimeProvider.GetCurrentDateTime();

        // Assert
        currentDateTime.Should().BeCloseTo(DateTime.Now, precision: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetCurrentDate_ReturnsCurrentDate()
    {
        // Arrange
        var dateTimeProvider = new SystemDateTimeProvider();

        // Act
        var currentDate = dateTimeProvider.GetCurrentDate();

        // Assert
        currentDate.Should().Be(DateTime.Today);
    }
}