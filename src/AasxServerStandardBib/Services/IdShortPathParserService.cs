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

using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using System;
using System.Collections.Generic;

namespace AasxServerStandardBib.Services
{
    public class IdShortPathParserService : IIdShortPathParserService
    {
        private IAppLogger<SubmodelService> _logger;

        public IdShortPathParserService(IAppLogger<SubmodelService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<object> ParseIdShortPath(string idShortPath)
        {
            var idShorts = idShortPath.Split('.');
            var output = new List<object>();
            foreach (var idShort in idShorts)
            {
                CheckIfEmptyIdShort(idShort);
                CheckIfIdShortStartsWithOpeningBracket(idShort);
                CheckIfIdShortStartsWithClosingBracket(idShort);

                if (idShort.Contains('[') || idShort.Contains(']'))
                {
                    GetSmlIdShorts(idShort, out string smlIdShort, out int index);
                    output.Add(smlIdShort);
                    output.Add(index);
                }
                else
                    output.Add(idShort);
            }

            return output;
        }

        private static void GetSmlIdShorts(string idShort, out string smlIdShort, out int index)
        {
            int startIndex = idShort.IndexOf("[");
            int endIndex = idShort.IndexOf("]");

            //opening bracket missing
            CheckIfOpeningBracketMissing(startIndex, idShort);
            CheckIfClosingBracketMissing(endIndex, idShort);
            CheckIfClosingBracketBeforeOpeningBracket(startIndex, endIndex, idShort);
            CheckIfIndexMissing(startIndex, endIndex, idShort);
            CheckIfCharAfterClosingBracket(endIndex, idShort.Length, idShort);

            smlIdShort = idShort.Substring(0, startIndex);
            bool success = int.TryParse(idShort.AsSpan(startIndex + 1, endIndex - startIndex - 1), out index);
            if (!success)
            {
                index = -1;
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfIdShortStartsWithClosingBracket(string idShort)
        {
            if (idShort.StartsWith(']'))
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfIdShortStartsWithOpeningBracket(string idShort)
        {
            if (idShort.StartsWith('['))
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfEmptyIdShort(string idShort)
        {
            if (string.IsNullOrEmpty(idShort))
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfCharAfterClosingBracket(int endIndex, int length, string idShort)
        {
            if (endIndex + 1 < length)
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfIndexMissing(int startIndex, int endIndex, string idShort)
        {
            if (startIndex + 1 == endIndex)
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfClosingBracketBeforeOpeningBracket(int startIndex, int endIndex, string idShort)
        {
            if (endIndex < startIndex)
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfClosingBracketMissing(int endIndex, string idShort)
        {
            if (endIndex == -1)
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }

        private static void CheckIfOpeningBracketMissing(int startIndex, string idShort)
        {
            if (startIndex == -1)
            {
                throw new InvalidIdShortPathException(idShort);
            }
        }
    }
}