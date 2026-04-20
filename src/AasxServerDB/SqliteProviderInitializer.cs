/********************************************************************************
* Copyright (c) {2019 - 2026} Contributors to the Eclipse Foundation
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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using SQLitePCL;

namespace AasxServerDB
{
    /// <summary>
    /// Selects the SQLite native provider at process startup.
    ///
    /// Single artifact, decision at startup:
    ///   - Windows: always embedded (bundled e_sqlite3). ENV is ignored.
    ///   - Linux / macOS: embedded by default; opt-in to the distro libsqlite3
    ///     via the AASX_SQLITE_PROVIDER environment variable.
    ///
    /// Values for AASX_SQLITE_PROVIDER (case-insensitive):
    ///   embedded        use the bundled e_sqlite3 native (default)
    ///   system | distro use the OS libsqlite3; auto-fallback to embedded if missing
    ///   strict-system   use the OS libsqlite3; hard-fail if it cannot be loaded
    ///
    /// Called from a ModuleInitializer so it runs as soon as the AasxServerDB
    /// assembly is loaded – before any DbContext / SqliteConnection is opened.
    /// Calling Initialize() explicitly at program start is also supported and
    /// idempotent (first call wins).
    /// </summary>
    public static class SqliteProviderInitializer
    {
        private const string EnvVarName = "AASX_SQLITE_PROVIDER";

        private static int _initialized;

        public static string? ActiveProviderName { get; private set; }
        public static string? ActiveLibraryVersion { get; private set; }

        [ModuleInitializer]
        internal static void AutoInit() => Initialize();

        /// <summary>
        /// Idempotent provider selection. Safe to invoke multiple times.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            var envValue = Environment.GetEnvironmentVariable(EnvVarName);
            var requested = (envValue ?? string.Empty).Trim().ToLowerInvariant();

            var useSystem = false;

            if (OperatingSystem.IsWindows())
            {
                if (requested is "system" or "distro" or "strict-system" or "system-required")
                {
                    Console.Error.WriteLine(
                        $"[SQLite] {EnvVarName}={requested} requested on Windows – " +
                        "ignored, using embedded (no stock sqlite3.dll on Windows).");
                }
            }
            else
            {
                switch (requested)
                {
                    case "system":
                    case "distro":
                        useSystem = TryProbeSystemLib(out var probedPath);
                        if (useSystem)
                        {
                            Console.WriteLine($"[SQLite] system libsqlite3 located: {probedPath}");
                        }
                        else
                        {
                            Console.Error.WriteLine(
                                $"[SQLite] {EnvVarName}=system requested, but libsqlite3 could not be " +
                                "loaded – falling back to embedded.");
                        }
                        break;

                    case "strict-system":
                    case "system-required":
                        useSystem = TryProbeSystemLib(out var strictPath);
                        if (!useSystem)
                        {
                            throw new DllNotFoundException(
                                $"{EnvVarName}={requested} requested, but libsqlite3 is not available. " +
                                "Install libsqlite3-0 (Debian/Ubuntu) or set AASX_SQLITE_PROVIDER=embedded.");
                        }
                        Console.WriteLine($"[SQLite] system libsqlite3 located: {strictPath}");
                        break;

                    case "":
                    case "embedded":
                        break;

                    default:
                        Console.Error.WriteLine(
                            $"[SQLite] Unknown {EnvVarName}='{envValue}' – using embedded.");
                        break;
                }
            }

            if (useSystem)
            {
                raw.SetProvider(new SQLite3Provider_sqlite3());
                ActiveProviderName = "system (sqlite3)";
            }
            else
            {
                raw.SetProvider(new SQLite3Provider_e_sqlite3());
                ActiveProviderName = "embedded (e_sqlite3)";
            }

            try
            {
                ActiveLibraryVersion = raw.sqlite3_libversion().utf8_to_string();
            }
            catch
            {
                ActiveLibraryVersion = "unknown";
            }

            Console.WriteLine(
                $"[SQLite] provider: {ActiveProviderName}, libversion: {ActiveLibraryVersion}");
        }

        private static bool TryProbeSystemLib(out string? resolvedName)
        {
            // Try the common sonames. .NET's NativeLibrary.TryLoad also consults
            // LD_LIBRARY_PATH / DYLD_LIBRARY_PATH, so a non-standard install works too.
            string[] candidates = OperatingSystem.IsMacOS()
                ? new[] { "libsqlite3.dylib", "libsqlite3.0.dylib" }
                : new[] { "libsqlite3.so.0", "libsqlite3.so" };

            foreach (var name in candidates)
            {
                if (NativeLibrary.TryLoad(name, out var handle))
                {
                    NativeLibrary.Free(handle);
                    resolvedName = name;
                    return true;
                }
            }

            resolvedName = null;
            return false;
        }
    }
}
