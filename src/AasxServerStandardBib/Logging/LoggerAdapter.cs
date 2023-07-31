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
