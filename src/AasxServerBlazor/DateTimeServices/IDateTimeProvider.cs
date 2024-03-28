using System;

namespace AasxServerBlazor.DateTimeServices;

/// <summary>
/// Represents a service for providing access to the current date and time from the system clock.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current date and time from the system clock.
    /// </summary>
    /// <returns>The current date and time.</returns>
    DateTime GetCurrentDateTime();

    /// <summary>
    /// Gets the current date from the system clock.
    /// </summary>
    /// <returns>The current date.</returns>
    DateTime GetCurrentDate();
}