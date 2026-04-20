using Microsoft.Data.Sqlite;

Console.WriteLine("=== SqliteSmoke ===");
Console.WriteLine($"OS              : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
Console.WriteLine($"AASX_SQLITE_PROVIDER = '{Environment.GetEnvironmentVariable("AASX_SQLITE_PROVIDER")}'");

// SqliteProviderInitializer runs via ModuleInitializer when AasxServerDB is loaded.
// Touch it explicitly so the JIT definitely pulls the assembly in.
AasxServerDB.SqliteProviderInitializer.Initialize();

Console.WriteLine($"ActiveProvider  : {AasxServerDB.SqliteProviderInitializer.ActiveProviderName}");
Console.WriteLine($"LibVersion      : {AasxServerDB.SqliteProviderInitializer.ActiveLibraryVersion}");

using var c = new SqliteConnection("Data Source=:memory:");
c.Open();
using var cmd = c.CreateCommand();
cmd.CommandText = "select sqlite_version(), sqlite_source_id();";
using var r = cmd.ExecuteReader();
while (r.Read())
{
    Console.WriteLine($"sqlite_version(): {r.GetString(0)}");
    Console.WriteLine($"sqlite_source_id(): {r.GetString(1)}");
}

Console.WriteLine("OK");
