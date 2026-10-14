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

using System.Linq;

namespace AasxServerStandardBib.Services;

/// <summary>
/// Provides methods for sanitizing log entries to prevent log forging attacks.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes the input string for plain text logs by removing all control characters
    /// (new lines, tabs, escape sequences, ...). This helps prevent log forging by ensuring
    /// user input cannot inject new log entries. It does not hide secrets: never log those.
    /// </summary>
    /// <param name="input">The user input string to sanitize, may be null.</param>
    /// <returns>The sanitized string without control characters, or an empty string for null.</returns>
    public static string Sanitize(string? input) => input == null ? "" : string.Concat(input.Where(c => !char.IsControl(c)));
}