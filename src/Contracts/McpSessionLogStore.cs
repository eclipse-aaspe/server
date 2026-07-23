namespace Contracts;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public sealed record McpSessionLogEntry(long Sequence, DateTimeOffset Timestamp, string Line);

public static class McpSessionLogStore
{
    private const int MaxSessions = 64;
    private const int MaxEntriesPerSession = 2000;
    private const int MaxLineLength = 8000;

    private static readonly ConcurrentDictionary<string, SessionBuffer> Sessions = new(StringComparer.Ordinal);
    private static long NextSequence;

    public static bool IsValidSessionId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 128)
        {
            return false;
        }

        foreach (var c in sessionId)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }

    public static void Append(string sessionId, string line)
    {
        if (!IsValidSessionId(sessionId))
        {
            return;
        }

        TrimSessionCount();

        var buffer = Sessions.GetOrAdd(sessionId, _ => new SessionBuffer());
        var trimmedLine = line.Length <= MaxLineLength ? line : line[..MaxLineLength] + " ...";
        buffer.Append(new McpSessionLogEntry(
            System.Threading.Interlocked.Increment(ref NextSequence),
            DateTimeOffset.Now,
            trimmedLine));
    }

    public static IReadOnlyList<McpSessionLogEntry> Read(string sessionId, long afterSequence = 0, int limit = 500)
    {
        if (!IsValidSessionId(sessionId) || !Sessions.TryGetValue(sessionId, out var buffer))
        {
            return Array.Empty<McpSessionLogEntry>();
        }

        return buffer.Read(afterSequence, Math.Clamp(limit, 1, 2000));
    }

    public static void Clear(string sessionId)
    {
        if (IsValidSessionId(sessionId))
        {
            Sessions.TryRemove(sessionId, out _);
        }
    }

    private static void TrimSessionCount()
    {
        if (Sessions.Count < MaxSessions)
        {
            return;
        }

        foreach (var key in Sessions
            .OrderBy(kv => kv.Value.LastWrite)
            .Take(Math.Max(1, Sessions.Count - MaxSessions + 1))
            .Select(kv => kv.Key))
        {
            Sessions.TryRemove(key, out _);
        }
    }

    private sealed class SessionBuffer
    {
        private readonly Queue<McpSessionLogEntry> _entries = new();
        private readonly object _gate = new();

        public DateTimeOffset LastWrite { get; private set; } = DateTimeOffset.Now;

        public void Append(McpSessionLogEntry entry)
        {
            lock (_gate)
            {
                _entries.Enqueue(entry);
                LastWrite = entry.Timestamp;

                while (_entries.Count > MaxEntriesPerSession)
                {
                    _entries.Dequeue();
                }
            }
        }

        public IReadOnlyList<McpSessionLogEntry> Read(long afterSequence, int limit)
        {
            lock (_gate)
            {
                return _entries
                    .Where(entry => entry.Sequence > afterSequence)
                    .Take(limit)
                    .ToArray();
            }
        }
    }
}
