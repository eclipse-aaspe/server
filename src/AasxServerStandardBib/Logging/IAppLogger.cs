namespace AasxServerStandardBib.Logging;

/// <summary>
/// Represents a generic interface for logging messages of different levels.
/// </summary>
/// <typeparam name="T">The type associated with the logger.</typeparam>
public interface IAppLogger<T>
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments to format into the message.</param>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments to format into the message.</param>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments to format into the message.</param>
    void LogError(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments to format into the message.</param>
    void LogDebug(string message, params object[] args);
}