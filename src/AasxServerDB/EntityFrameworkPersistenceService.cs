namespace AasxServerDB;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AasxServerDB.Context;
using AasxServerDB.Entities;
using AdminShellNS;
using Contracts;
using Microsoft.IdentityModel.Tokens;
using AasCore.Aas3_0;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Contracts.Pagination;

public class EntityFrameworkPersistenceService : IPersistenceService
{

    public void InitDB(bool reloadDB, string dataPath)
    {
        AasContext.DataPath = dataPath;

        //Provoke OnConfiguring so that IsPostgres is set
        var db = new AasContext();

        bool isPostgres = AasContext.IsPostgres;

        // Get database
        Console.WriteLine($"Use {(isPostgres ? "POSTGRES" : "SQLITE")}");

        db = isPostgres ? new PostgreAasContext() : new SqliteAasContext();

        // Get path
        var connectionString = db.Database.GetConnectionString();
        Console.WriteLine($"Database connection string: {connectionString}");
        if (connectionString.IsNullOrEmpty())
        {
            throw new Exception("Database connection string is empty.");
        }
        if (!isPostgres && !Directory.Exists(Path.GetDirectoryName(connectionString.Replace("Data Source=", string.Empty))))
        {
            throw new Exception($"Directory to the database does not exist. Check appsettings.json. Connection string: {connectionString}");
        }

        // Check if db exists
        var canConnect = db.Database.CanConnect();
        if (!canConnect)
        {
            Console.WriteLine($"Unable to connect to the database.");
        }

        // Delete database
        if (canConnect && reloadDB)
        {
            Console.WriteLine("Clear database.");
            db.Database.EnsureDeleted();
        }

        // Create the database if it does not exist
        // Applies any pending migrations
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            throw new Exception($"Migration failed: {ex.Message}\nTry deleting the database.");
        }
    }

    public void ImportAASXIntoDB(string filePath, bool createFilesOnly, bool withDbFiles)
    {
        VisitorAASX.ImportAASXIntoDB(filePath, createFilesOnly, withDbFiles);
    }

    public List<string> GetFilteredPackages(string filterPath, List<AdminShellPackageEnv> list)
    {
        return Converter.GetFilteredPackages(filterPath, list);
    }

    public AdminShellPackageEnv? GetPackageEnv(int envId)
    {
        return Converter.GetPackageEnv(envId);
    }

    public string GetAASXPath(int? envId = null, string cdId = "", string aasId = "", string smId = "")
    {
        return Converter.GetAASXPath(envId,cdId, aasId, smId);
    }

    public List<IAssetAdministrationShell> GetPagedAssetAdministrationShells(IPaginationParameters paginationParameters, List<ISpecificAssetId> assetIds)
    {
       return Converter.GetPagedAssetAdministrationShells(paginationParameters, assetIds);
    }

    //public List<ISubmodel> GetAllSubmodels(string cursor, int limit){

    //}
}
