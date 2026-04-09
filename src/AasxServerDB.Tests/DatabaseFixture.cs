namespace AasxServerDB.Tests;

using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Creates a temporary SQLite database once per test session, imports all
/// .aasx files from TestData/, and tears down the temp directory afterwards.
/// </summary>
[CollectionDefinition(DatabaseFixture.Collection)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public sealed class DatabaseFixture : IDisposable
{
    public const string Collection = "Database";

    public string TempDir { get; }
    public QueryGrammarJSON Grammar { get; }

    public DatabaseFixture()
    {
        // --- temp directory --------------------------------------------------
        TempDir = Path.Combine(Path.GetTempPath(), $"AasxServerDB.Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(TempDir);
        Directory.CreateDirectory(Path.Combine(TempDir, "files"));
        Directory.CreateDirectory(Path.Combine(TempDir, "files", FileService.ThumnbnailsFolderName));
        Directory.CreateDirectory(Path.Combine(TempDir, "files", FileService.JwsFolderName));
        Directory.CreateDirectory(Path.Combine(TempDir, "files", FileService.XmlFolderName));

        // --- wire up AasContext static state ---------------------------------
        AasContext.DataPath = TempDir;
        AasContext.IsPostgres = false;
        AasContext.Config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseConnection:ConnectionString"] =
                    $"Data Source={Path.Combine(TempDir, "test.db")}"
            })
            .Build();

        // --- create schema ---------------------------------------------------
        using var db = new AasContext();
        db.Database.EnsureCreated();

        // --- import AASX test data ------------------------------------------
        var testDataDir = Path.Combine(
            AppContext.BaseDirectory, "TestData");

        var aasxFiles = Directory.Exists(testDataDir)
            ? Directory.GetFiles(testDataDir, "*.aasx")
            : Array.Empty<string>();

        if (aasxFiles.Length == 0)
        {
            throw new InvalidOperationException(
                $"No .aasx files found in '{testDataDir}'. " +
                $"Place your 7 .aasx files in src/AasxServerDB.Tests/TestData/ " +
                $"and set CopyToOutputDirectory=PreserveNewest.");
        }

        foreach (var file in aasxFiles.OrderBy(f => f))
        {
            Console.WriteLine($"[DatabaseFixture] Importing {Path.GetFileName(file)}");
            VisitorAASX.ImportAASXIntoDB(file, createFilesOnly: false);
        }

        Console.WriteLine("[DatabaseFixture] Import done.");
        using var verify = new AasContext();
        Console.WriteLine($"  SMSets:  {verify.SMSets.Count()}");
        Console.WriteLine($"  SMESets: {verify.SMESets.Count()}");

        // --- grammar (no security) ------------------------------------------
        Grammar = new QueryGrammarJSON(new NoSecurityRules());
    }

    public AasContext CreateDbContext() => new AasContext();

    public void Dispose()
    {
        try { Directory.Delete(TempDir, recursive: true); }
        catch { /* best-effort cleanup */ }
    }
}
