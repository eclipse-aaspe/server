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

/// <summary>
/// Static class for managing application-wide logging and creating logger instances.
/// Use this class where direct injection of <see cref="IAppLogger{T}"/> is not feasible.
/// <example>
/// <code>
/// // Using ApplicationLogging to create logger instances
/// var logger = ApplicationLogging.CreateLogger -MyClass-();
/// logger.LogDebug("Debug message");
/// logger.LogError("Error message");
/// 
/// // Using ApplicationLogging to create a logger with a specific category
/// var customLogger = ApplicationLogging.CreateLogger("CustomCategory");
/// customLogger.LogInformation("Information message");
/// customLogger.LogWarning("Warning message");
/// </code>
/// </example>
/// </summary>
public static class ApplicationLogging
{
    private static ILoggerFactory _loggerFactory { get; set; }

    static ApplicationLogging() => _loggerFactory = new LoggerFactory();

    /// <summary>
    /// Gets or sets the factory for creating ILogger instances.
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        get => _loggerFactory;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _loggerFactory = value;
        }
    }

    /// <summary>
    /// Creates a logger instance for the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>An instance of logger for the specified type.</returns>
    public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

    /// <summary>
    /// Creates a logger instance with the specified category name.
    /// </summary>
    /// <param name="categoryName">The name of the category for the logger.</param>
    /// <returns>An instance of logger for the specified category.</returns>
    public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
}