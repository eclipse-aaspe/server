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
    Task<DbRequestResult> DoDbOperation(DbRequest dbRequest);

    void ImportAASXIntoDB(string filePath, bool createFilesOnly);

    List<string> ReadFilteredPackages(string filterPath, List<AdminShellPackageEnv> list);
}

