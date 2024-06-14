using Environment = System.Environment;

namespace AasxServerStandardBib.Services;

/// <summary>
/// Provides methods for sanitizing log entries to prevent log forging attacks.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes the input string for plain text logs by removing new line characters.
    /// This helps prevent log forging by ensuring user input cannot inject new log entries.
    /// </summary>
    /// <param name="input">The user input string to sanitize.</param>
    /// <returns>The sanitized string with new line characters removed.</returns>
    public static string Sanitize(string input) => input.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
}