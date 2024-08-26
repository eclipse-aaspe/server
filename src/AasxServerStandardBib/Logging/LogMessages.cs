/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace AasxServerStandardBib.Logging;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Static class defining LoggerMessage delegates for various log levels.
/// </summary>
internal static class LogMessages
{
    private static readonly Action<ILogger, string, Exception?> _logDebugMessage =
        LoggerMessage.Define<string>(
                                     LogLevel.Debug,
                                     new EventId(1, nameof(LogDebugMessage)),
                                     "{Message}"
                                    );

    private static readonly Action<ILogger, string, Exception?> _logErrorMessage =
        LoggerMessage.Define<string>(
                                     LogLevel.Error,
                                     new EventId(2, nameof(LogErrorMessage)),
                                     "{Message}"
                                    );

    private static readonly Action<ILogger, string, Exception?> _logInformationMessage =
        LoggerMessage.Define<string>(
                                     LogLevel.Information,
                                     new EventId(3, nameof(LogInformationMessage)),
                                     "{Message}"
                                    );

    private static readonly Action<ILogger, string, Exception?> _logWarningMessage =
        LoggerMessage.Define<string>(
                                     LogLevel.Warning,
                                     new EventId(4, nameof(LogWarningMessage)),
                                     "{Message}"
                                    );

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception associated with the log message.</param>
    public static void LogDebugMessage(ILogger logger, string message, Exception? exception = null) => _logDebugMessage(logger, message, exception);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception associated with the log message.</param>
    public static void LogErrorMessage(ILogger logger, string message, Exception? exception = null) => _logErrorMessage(logger, message, exception);

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception associated with the log message.</param>
    public static void LogInformationMessage(ILogger logger, string message, Exception? exception = null) => _logInformationMessage(logger, message, exception);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception associated with the log message.</param>
    public static void LogWarningMessage(ILogger logger, string message, Exception? exception = null) => _logWarningMessage(logger, message, exception);
}