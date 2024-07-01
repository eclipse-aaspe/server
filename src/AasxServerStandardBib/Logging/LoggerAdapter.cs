/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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

namespace AasxServerStandardBib.Logging
{
    public class LoggerAdapter<T> : IAppLogger<T>
    {
        private readonly ILogger<T> _logger;

        public LoggerAdapter(ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            _logger = loggerFactory.CreateLogger<T>();
            ApplicationLogging.LoggerFactory = loggerFactory;
        }

        public void LogDebug(string message, params object[] args) => _logger.LogDebug(message, args);
        public void LogError(string message, params object[] args) => _logger.LogError(message, args);
        public void LogInformation(string message, params object[] args) => _logger.LogInformation(message, args);
        public void LogWarning(string message, params object[] args) => _logger.LogWarning(message, args);
    }
}
