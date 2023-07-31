using Microsoft.Extensions.Logging;

//This class has been added to use the logger in static classes, without DI
namespace AasxServerStandardBib.Logging
{
    public static class ApplicationLogging
    {
        public static ILoggerFactory LoggerFactory { get; set; }
        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
        public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);

    }
}
