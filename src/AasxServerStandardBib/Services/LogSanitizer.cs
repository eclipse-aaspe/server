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