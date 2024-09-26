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

using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;
using File = System.IO.File;
using Console = System.Console;
using Environment = System.Environment;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Process = System.Diagnostics.Process;
using StringSplitOptions = System.StringSplitOptions;
using Directory = System.IO.Directory;

namespace Script.CheckHelpInReadme
{
    internal static class Program
    {
        /// <summary>
        /// Adds quotes around the text and escapes a couple of common special characters.
        /// </summary>
        /// <remarks>Do not use <see cref="System.Text.Json.JsonSerializer"/> since it escapes so many common
        /// characters that the output is unreadable.
        /// See <a href="https://github.com/dotnet/runtime/issues/1564">this GitHub issue</a></remarks>
        private static string Quote(string text)
        {
            string escaped =
                text
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("\a", "\\b")
                    .Replace("\b", "\\b")
                    .Replace("\v", "\\v")
                    .Replace("\f", "\\f");

            return $"\"{escaped}\"";
        }

        private static void Main(string[] args)
        {
            // Read the README

            string readmePath = Path.GetFullPath(Path.Combine("..", "README.md"));
            if (!File.Exists(readmePath))
            {
                Console.Error.WriteLine($"Could not find the readme file: {readmePath}");
                Environment.ExitCode = 1;
                return;
            }

            string text = File.ReadAllText(readmePath);

            string buildDir = Path.GetFullPath(Path.Combine(
                "..", "artefacts", "build", "Release", "AasxServerCore"));
            if (!Directory.Exists(buildDir))
            {
                Console.Error.WriteLine($"Could not find the build directory: {buildDir}");
                Environment.ExitCode = 1;
                return;
            }

            // Capture the --help

            var exeCandidates = new List<string>
            {
                Path.Combine(buildDir, "AasxServerCore"),
                Path.Combine(buildDir, "AasxServerCore.exe")
            };
            var exe = exeCandidates.FirstOrDefault(File.Exists);
            if (exe == null)
            {
                Console.Error.WriteLine(
                    $"Could not find any of the executable candidates: {string.Join(" or ", exeCandidates)};" +
                    "did you `dotnet publish -c Release` to it?");
                Environment.ExitCode = 1;
                return;
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--help",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (process == null)
            {
                Console.Error.WriteLine(
                    $"Failed to start the process: {exe}; the result of the Process.Start was null");
                Environment.ExitCode = 1;
                return;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Trim the help so that the message does not take unnecessary space in the
            // Readme
            output = output.Trim();

            List<string> outputLines = new List<string>(
                output.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None));


            // Find the help in the Readme

            var textLines = new List<string>(
                text.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None));

            const string startMarker = "<!--- Help starts. -->";
            var startMarkerIdx = textLines.IndexOf(startMarker);
            if (startMarkerIdx == -1)
            {
                Console.Error.WriteLine(
                    $"Failed to find the start marker in the readme {readmePath}: {startMarker}");
                Environment.ExitCode = 1;
                return;
            }

            const string endMarker = "<!--- Help ends. -->";
            var endMarkerIdx = textLines.IndexOf(endMarker);
            if (endMarkerIdx == -1)
            {
                Console.Error.WriteLine(
                    $"Failed to find the end marker in the readme {readmePath}: {endMarker}");
                Environment.ExitCode = 1;
                return;
            }

            if (endMarkerIdx - startMarkerIdx - 1 < 2)
            {
                Console.Error.WriteLine(
                    $"Too few lines between the start marker ({startMarker}) and the end marker ({endMarker} " +
                    $"in the readme {readmePath}: expected at least two lines for the code directives (```).");
                Environment.ExitCode = 1;
                return;
            }

            if (textLines[startMarkerIdx + 1] != "```")
            {
                Console.Error.WriteLine(
                    $"Expected the code directive (```) after the start marker ({startMarker}) " +
                    $"in the readme {readmePath} on line {startMarkerIdx + 2}, " +
                    $"but found: {Quote(textLines[startMarkerIdx + 1])}");
                Environment.ExitCode = 1;
                return;
            }

            if (textLines[endMarkerIdx - 1] != "```")
            {
                Console.Error.WriteLine(
                    $"Expected the code directive (```) before the end marker ({endMarker}) " +
                    $"in the readme {readmePath} on line {endMarkerIdx}, but found: {Quote(textLines[endMarkerIdx - 1])}");
                Environment.ExitCode = 1;
                return;
            }

            var helpStart = startMarkerIdx + 2; // inclusive
            var helpEnd = endMarkerIdx - 1; // exclusive

            var helpLength = helpEnd - helpStart;

            // Check if empty

            if (outputLines.Count == 0)
            {
                Console.Error.WriteLine(
                    $"The --help of {exe} is unexpectedly empty.");
                Environment.ExitCode = 1;
                return;
            }

            if (helpLength == 0)
            {
                Console.Error.WriteLine(
                    $"The help section in the readme {readmePath} on line {startMarkerIdx + 1} is unexpectedly empty.");
                Environment.ExitCode = 1;
                return;
            }

            // Check if lengths differ

            if (outputLines.Count != helpLength)
            {
                Console.Error.WriteLine(
                    $"The --help of {exe} and the documented help in {readmePath} differ in length " +
                    $"({outputLines} line(s) and {helpLength} line(s), respectively).");
                Console.Error.WriteLine();
                Console.Error.WriteLine("$The --help of {exe} is:");
                for (var i = 0; i < outputLines.Count; i++)
                {
                    Console.Error.WriteLine($"{i + 1,3}:{outputLines[i]}");
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine($"The documented help in {readmePath} is:");
                for (var i = helpStart; i < helpEnd; i++)
                {
                    Console.Error.WriteLine($"{i + 1,3}:{textLines[i]}");
                }

                Environment.ExitCode = 1;
                return;
            }

            // Check if content different

            var firstDifferent = -1;
            for (var i = 0; i < outputLines.Count; i++)
            {
                if (outputLines[i] != textLines[helpStart + i])
                {
                    firstDifferent = i;
                    break;
                }
            }

            if (firstDifferent >= 0)
            {
                Console.Error.WriteLine(
                    $"The --help of {exe} at the line {firstDifferent + 1} and " +
                    $"the documented help in {readmePath} at line {helpStart + firstDifferent + 1} differ. ");
                Console.Error.WriteLine();
                Console.Error.WriteLine(
                    $"The line {firstDifferent + 1} in --help is: {Quote(outputLines[firstDifferent])}");
                Console.Error.WriteLine();
                Console.Error.WriteLine(
                    $"The line {helpStart + firstDifferent + 1} of the documented help " +
                    $"is: {Quote(textLines[helpStart + firstDifferent])}");
                
                Console.Error.WriteLine();
                Console.Error.WriteLine($"The --help of {exe} is:");
                for (var i = 0; i < outputLines.Count; i++)
                {
                    string prefix = (i != firstDifferent) ? "    " : ">>> ";
                    Console.Error.WriteLine($"{prefix}{i + 1,3}:{outputLines[i]}");
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine($"The documented help in {readmePath} is:");
                for (var i = helpStart; i < helpEnd; i++)
                {
                    string prefix = (i - helpStart != firstDifferent) ? "    " : ">>> ";
                    Console.Error.WriteLine($"{prefix}{i + 1,3}:{textLines[i]}");
                }

                Environment.ExitCode = 1;
                return;
            }

            Console.Out.WriteLine($"OK: the --help of {exe} and the documented help in {readmePath} coincide.");
            Environment.ExitCode = 0;
        }
    }
}