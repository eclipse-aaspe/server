using System;

namespace AasxServerBlazor.DateTimeServices;

/// <inheritdoc cref="IDateTimeProvider"/>
public class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime GetCurrentDateTime()
    {
        return DateTime.Now;
    }

    /// <inheritdoc/>
    public DateTime GetCurrentDate()
    {
        return DateTime.Today;
    }
}