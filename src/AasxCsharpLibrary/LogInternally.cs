/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

using Exception = System.Exception;

namespace AdminShellNS
{
    public static class Logging
    {
        // see: https://stackoverflow.com/questions/9314172/getting-all-messages-from-innerexceptions
        private static string GetExceptionMessages(this Exception e, string msgs = "")
        {
            if (e == null) return string.Empty;
            if (msgs == "") msgs = e.Message;
            if (e.InnerException != null)
                msgs += "\r\nInnerException: " + GetExceptionMessages(e.InnerException);
            return msgs;
        }

        public static string FormatError(Exception ex, string where)
        {
            var res = string.Format("Error: {0}: {1} {2} at {3}.",
                where,
                ex.Message,
                ex.GetExceptionMessages(),
                ex.StackTrace);

            var inner = ex.InnerException;
            while (inner != null)
            {
                res += $"Inner message: {inner.Message}" + System.Environment.NewLine;
                inner = inner.InnerException;
            }

            return res;
        }
    }

    public class InternalLog
    {
        /// <summary>
        /// Logs the exception to STDERR.
        /// </summary>
        public void Error(Exception ex, string where)
        {
            System.Console.Error.WriteLine(Logging.FormatError(ex, where));
        }

        /// <summary>
        /// Logs that the exception is silently ignored to STDERR.
        /// </summary>
        public void SilentlyIgnoredError(Exception ex)
        {
            System.Console.Error.WriteLine("The exception is silently ignored: {0} {1} at {2}.",
                ex.Message,
                ((ex.InnerException != null) ? ex.InnerException.Message : ""),
                ex.StackTrace);
        }

        /// <summary>
        /// Does no logging at all. Allows to have non-empty catch clauses.
        /// </summary>
        public void CompletelyIgnoredError(Exception ex)
        {
        }
    }

    /// <summary>
    /// Handles logging meant to be read by developers (*i.e*, not by the users of the software).
    /// </summary>
    /// <remarks>Please see AasxIntegrationBase\LogInstance.cs for how to keep logs intended
    /// for the user.</remarks>
    public static class LogInternally
    {
        public static readonly InternalLog That = new InternalLog();
    }
}
