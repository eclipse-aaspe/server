namespace Contracts;

using System;
using System.Globalization;
using System.Linq;
using System.Threading;

public static class DiagnosticsLog
{
    private static readonly AsyncLocal<int> McpScopeDepth = new();
    private static int GlobalMcpScopeDepth;

    public static bool QueryEnabled =>
        EnvEnabled("AAS_QUERY_LOG") ||
        EnvEnabled("AAS_QUERY_DIAG");

    public static bool McpEnabled =>
        EnvEnabled("AAS_MCP_LOG");

    public static IDisposable BeginMcpScope()
    {
        McpScopeDepth.Value++;
        Interlocked.Increment(ref GlobalMcpScopeDepth);
        return new Scope(() =>
        {
            McpScopeDepth.Value = Math.Max(0, McpScopeDepth.Value - 1);
            if (Interlocked.Decrement(ref GlobalMcpScopeDepth) < 0)
            {
                Volatile.Write(ref GlobalMcpScopeDepth, 0);
            }
        });
    }

    public static void WriteMcp(string message, bool timestamp = false)
    {
        if (!McpEnabled)
        {
            return;
        }

        if (message.Length == 0)
        {
            Console.WriteLine();
            return;
        }

        var prefix = timestamp
            ? $"[MCP {Timestamp()}] "
            : "[MCP] ";
        Console.WriteLine(prefix + message);
    }

    public static void WriteQuery(string message, bool timestamp = false)
    {
        if (!QueryEnabled)
        {
            return;
        }

        var inMcpScope = McpEnabled &&
            (McpScopeDepth.Value > 0 || Volatile.Read(ref GlobalMcpScopeDepth) > 0);
        if (inMcpScope)
        {
            message = message.TrimStart('\r', '\n');
        }

        var indent = inMcpScope ? "  " : string.Empty;
        var firstNonEmpty = true;
        foreach (var line in SplitLines(message))
        {
            if (line.Length == 0)
            {
                Console.WriteLine();
                continue;
            }

            var time = timestamp && firstNonEmpty
                ? $"[{Timestamp()}] "
                : string.Empty;
            Console.WriteLine(indent + time + line);
            firstNonEmpty = false;
        }
    }

    private static bool EnvEnabled(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return value != null &&
            (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
    }

    private static string[] SplitLines(string message) =>
        message.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

    private static string Timestamp() =>
        DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    private sealed class Scope : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Scope(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dispose();
        }
    }
}
