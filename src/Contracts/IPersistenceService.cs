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

namespace Contracts;

using System.Collections.Generic;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.DbRequests;
using Contracts.Pagination;

public class Running
{
    private static bool IsRunning;

    public static void SetRunning() => IsRunning = true;
    public static bool GetRunning() => IsRunning;
}

public interface IPersistenceService
{
    void InitDB(bool reloadDB, string dataPath);

    void InitDBFiles(bool reloadDBFiles, string dataPath);

    Task<DbRequestResult> DoDbOperation(DbRequest dbRequest);

    void ImportAASXIntoDB(string filePath, bool createFilesOnly);

    List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list);
}


