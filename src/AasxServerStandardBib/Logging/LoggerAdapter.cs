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

using Microsoft.Extensions.Logging;
using System;

namespace AasxServerStandardBib.Logging;

using System.Globalization;

/// <summary>
/// Adapter class that implements <see cref="IAppLogger{T}"/> for logging messages using Microsoft.Extensions.Logging.
/// </summary>
/// <typeparam name="T">The type of the class for which the logger is being created.</typeparam>
public class LoggerAdapter<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerAdapter{T}"/> class.
    /// </summary>
    /// <param name="loggerFactory">The factory for creating ILogger instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerFactory"/> is null.</exception>
    public LoggerAdapter(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _logger                          = loggerFactory.CreateLogger<T>();
        ApplicationLogging.LoggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object[] args)
    {
        //var formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
        LogMessages.LogDebugMessage(_logger, message);
    }

    /// <inheritdoc />
    public void LogError(string message, params object[] args)
    {
        //var formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
        LogMessages.LogErrorMessage(_logger, message);
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object[] args)
    {
        //var formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
        LogMessages.LogInformationMessage(_logger, message);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] args)
    {
        //var formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
        LogMessages.LogWarningMessage(_logger, message);
    }
}