﻿using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;

/*
 * https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
 * 
 * Initial Migration
 * Add-Migration InitialCreate -Context SqliteAasContext -OutputDir Migrations\Sqlite
 * Add-Migration InitialCreate -Context PostgreAasContext -OutputDir Migrations\Postgres
 * 
 * Change database
 * Add-Migration XXX -Context SqliteAasContext
 * Add-Migration XXX -Context PostgreAasContext
 * Update-Database -Context SqliteAasContext
 * Update-Database -Context PostgreAasContext
 */

namespace AasxServerDB
{
    public class AasContext : DbContext
    {
        public static IConfiguration _con { get; set; }

        // --------------- Database Schema ---------------
        public DbSet<DbConfigSet> DbConfigSets { get; set; }
        public DbSet<AASXSet> AASXSets { get; set; }
        public DbSet<AasSet> AasSets { get; set; }
        public DbSet<SubmodelSet> SubmodelSets { get; set; }
        public DbSet<SMESet> SMESets { get; set; }
        public DbSet<StringValue> SValueSets { get; set; }
        public DbSet<IntValue> IValueSets { get; set; }
        public DbSet<DoubleValue> DValueSets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                throw new Exception("No Configuration!");
            }
            string connectionString = _con["DatabaseConnection:ConnectionString"];
            if (connectionString != null)
            {
                if (connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", GlobalDB.DataPath);
                if (connectionString.ToLower().Contains("host")) // Postgres
                {
                    string[] Params = connectionString.Split(";");
                    string dbPassword = System.Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                    string dbUser = System.Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                    for (int i = 0; i < Params.Length; i++)
                    {
                        if (Params[i].Contains("Username") && dbUser != null)
                        {
                            Params[i] = "Username=" + dbUser;
                        }
                        if (Params[i].Contains("Password") && dbPassword != null)
                        {
                            Params[i] = "Password=" + dbPassword;
                        }
                    }
                    GlobalDB.IsPostgres = true;
                    options.UseNpgsql(connectionString);
                }
                else // SQLite
                {
                    GlobalDB.IsPostgres = false;
                    options.UseSqlite(connectionString);
                }
            }
            else
            {
                throw new Exception("No connectionString in appsettings");
            }
        }

        public void clearDB()
        {
            int count = 0;
            var task = Task.Run(async () => count = await DbConfigSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await AASXSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await AasSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await SubmodelSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await SMESets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await IValueSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await SValueSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await DValueSets.ExecuteDeleteAsync());
            task.Wait();

            DbConfigSet dbConfig = null;
            dbConfig = new DbConfigSet
            {
                Id = 1,
                AasCount = 0,
                SubmodelCount = 0,
                AASXCount = 0,
                SMECount = 0
            };
            Add(dbConfig);
            SaveChanges();
        }
    
        public void LoadAASXInDB(string filePath, bool createFilesOnly, bool withDbFiles)
        {
            using (var asp = new AdminShellPackageEnv(filePath, false, true))
            {
                if (!createFilesOnly)
                {
                    var configDBList = DbConfigSets.Where(d => true);
                    var dbConfig = configDBList.FirstOrDefault();

                    long aasxNum = ++dbConfig.AASXCount;
                    var aasxDB = new AASXSet
                    {
                        AASXNum = aasxNum,
                        AASX = filePath
                    };
                    Add(aasxDB);

                    var aas = asp.AasEnv.AssetAdministrationShells[0];
                    var aasId = aas.Id;
                    var assetId = aas.AssetInformation.GlobalAssetId;

                    // Check security
                    if (aas.IdShort.ToLower().Contains("globalsecurity"))
                    {
                        // AasxHttpContextHelper.securityInit(); // read users and access rights form AASX Security
                        // AasxHttpContextHelper.serverCertsInit(); // load certificates of auth servers
                    }
                    else
                    {
                        if (aasId != null && aasId != "" && assetId != null && assetId != "")
                        {
                            VisitorAASX.LoadAASInDB(this, aas, aasxNum, asp, dbConfig);
                        }
                    }
                    SaveChanges();
                    /*
                    Task t = db.SaveChangesAsync();
                    if (saveTasks.Count == maxTasks)
                    {
                        // search for completed task
                        int i = 0;
                        while (!saveTasks[i].IsCompleted && i < maxTasks)
                        {
                            i++;
                        }
                        if (i < maxTasks)
                        {
                            taskIndex = i;
                        }
                        else
                        {
                            await saveTasks[taskIndex];
                        }
                    }
                    if (taskIndex <= saveTasks.Count)
                    {
                        saveTasks.Add(t);
                    }
                    else
                    {
                        saveTasks[taskIndex] = t;
                    }
                    taskIndex++;
                    if (taskIndex >= maxTasks)
                        taskIndex = 0;
                    */
                }

                if (withDbFiles)
                {
                    string name = Path.GetFileName(filePath);
                    try
                    {
                        string fcopyt = name + "__thumbnail";
                        fcopyt = fcopyt.Replace("/", "_");
                        fcopyt = fcopyt.Replace(".", "_");
                        Uri dummy = null;
                        using (var st = asp.GetLocalThumbnailStream(ref dummy, init: true))
                        {
                            Console.WriteLine("Copy " + GlobalDB.DataPath + "/files/" + fcopyt + ".dat");
                            var fst = System.IO.File.Create(GlobalDB.DataPath + "/files/" + fcopyt + ".dat");
                            if (st != null)
                            {
                                st.CopyTo(fst);
                            }
                        }
                    }
                    catch { }

                    using (var fileStream = new FileStream(GlobalDB.DataPath + "/files/" + name + ".zip", FileMode.Create))
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                    {
                        var files = asp.GetListOfSupplementaryFiles();
                        foreach (var f in files)
                        {
                            try
                            {
                                // string fcopy = name + "__" + f.Uri.OriginalString;
                                // fcopy = fcopy.Replace("/", "_");
                                // fcopy = fcopy.Replace(".", "_");
                                using (var s = asp.GetLocalStreamFromPackage(f.Uri.OriginalString, init: true))
                                {
                                    var archiveFile = archive.CreateEntry(f.Uri.OriginalString);
                                    Console.WriteLine("Copy " + GlobalDB.DataPath + "/" + name + "/" + f.Uri.OriginalString);

                                    using (var archiveStream = archiveFile.Open())
                                    {
                                        // Console.WriteLine("Copy " + AasxHttpContextHelper.GlobalPath.DataPath + "/files/" + fcopy + ".dat");
                                        // var fs = System.IO.File.Create(AasxHttpContextHelper.GlobalPath.DataPath + "/files/" + fcopy + ".dat");
                                        s.CopyTo(archiveStream);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public string getAASXPath(string aasId = "", string submodelId = "")
        {
            long aasxNum = 0;
            if (!submodelId.Equals(""))
            { 
                var submodelDBList = SubmodelSets.Where(s => s.SubmodelId == submodelId);
                if (submodelDBList.Count() > 0)
                {
                    var submodelDB = submodelDBList.First();
                    aasxNum = submodelDB.AASXNum;
                }
            }
            if (!aasId.Equals(""))
            {
                var aasDBList = AasSets.Where(a => a.AasId == aasId);
                if (aasDBList.Any())
                {
                    var aasDB = aasDBList.First();
                    aasxNum = aasDB.AASXNum;
                }
            }
            if (aasxNum == 0 )
                return null;
            var aasxDBList = AASXSets.Where(a => a.AASXNum == aasxNum);
            if (!aasxDBList.Any())
                return null;
            var aasxDB = aasxDBList.First();
            return aasxDB.AASX;
        }

        public AdminShellPackageEnv AASXToPackageEnv(string path, AasSet aasDB)
        {
            if (path == null || path.Equals("") || aasDB == null)
                return null;

            AssetAdministrationShell aas = new AssetAdministrationShell(
                id: aasDB.AasId,
                idShort: aasDB.Idshort,
                assetInformation: new AssetInformation(AssetKind.Type, aasDB.AssetId),
                submodels: new List<AasCore.Aas3_0.IReference>());

            AdminShellPackageEnv aasEnv = new AdminShellPackageEnv();
            aasEnv.SetFilename(path);
            aasEnv.AasEnv.AssetAdministrationShells.Add(aas);

            var submodelDBList = SubmodelSets
                .OrderBy(sm => sm.SubmodelNum)
                .Where(sm => sm.AasNum == aasDB.AasNum)
                .ToList();
            foreach (var submodelDB in submodelDBList)
            {
                var sm = DBRead.getSubmodel(submodelDB.SubmodelId);
                aas.Submodels.Add(sm.GetReference());
                aasEnv.AasEnv.Submodels.Add(sm);
            }

            return aasEnv;
        }
    }

    public class SqliteAasContext : AasContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                options.UseSqlite("");
            }
            else
            {
                var connectionString = _con["DatabaseConnection:ConnectionString"];
                if (connectionString != null && connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", GlobalDB.DataPath);
                options.UseSqlite(connectionString);
            }
        }
    }

    public class PostgreAasContext : AasContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                options.UseNpgsql("");
            }
            else
            {
                var connectionString = _con["DatabaseConnection:ConnectionString"];
                if (connectionString != null && connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", GlobalDB.DataPath);
                options.UseNpgsql(connectionString);
            }
        }
    }



    // --------------- Database Schema ---------------
    public class DbConfigSet
    {
        public int Id { get; set; }
        public long AASXCount { get; set; }
        public long AasCount { get; set; }
        public long SubmodelCount { get; set; }
        public long SMECount { get; set; }
    }

    [Index(nameof(AASXNum))]
    public class AASXSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public string AASX { get; set; }
    }

    [Index(nameof(AasNum))]
    public class AasSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public long AasNum { get; set; }
        public string AasId { get; set; }
        public string Idshort { get; set; }
        public string AssetId { get; set; }
        public string AssetKind { get; set; }
    }

    [Index(nameof(SubmodelNum))]
    public class SubmodelSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public long AasNum { get; set; }
        public long SubmodelNum { get; set; }
        public string SubmodelId { get; set; }
        public string Idshort { get; set; }
        public string SemanticId { get; set; }
    }

    [Index(nameof(SubmodelNum))]
    [Index(nameof(SMENum))]
    public class SMESet
    {
        public int Id { get; set; }
        public long SubmodelNum { get; set; }
        public long ParentSMENum { get; set; }
        public long SMENum { get; set; }
        public string SMEType { get; set; }
        public string Idshort { get; set; }
        public string SemanticId { get; set; }
        public string ValueType { get; set; }

        public string getValue()
        {
            using (AasContext db = new AasContext())
            {
                switch (ValueType)
                {
                    case "S":
                        var ls = db.SValueSets.Where(s => s.ParentSMENum == SMENum).Select(s => s.Value).ToList();
                        if (ls.Count != 0)
                            return ls.First() + "";
                        break;
                    case "I":
                        var li = db.IValueSets.Where(s => s.ParentSMENum == SMENum).Select(s => s.Value).ToList();
                        if (li.Count != 0)
                            return li.First() + "";
                        break;
                    case "F":
                        var ld = db.DValueSets.Where(s => s.ParentSMENum == SMENum).Select(s => s.Value).ToList();
                        if (ld.Count != 0)
                            return ld.First() + "";
                        break;
                }
            }
            return "";
        }

        public List<string> getMLPValue()
        {
            var list = new List<String>();
            if (SMEType == "MLP")
            {
                using (AasContext db = new AasContext())
                {
                    var MLPValueSetList = db.SValueSets
                        .Where(s => s.ParentSMENum == SMENum)
                        .ToList();
                    foreach (var MLPValue in MLPValueSetList)
                    {
                        list.Add(MLPValue.Annotation);
                        list.Add(MLPValue.Value);
                    }
                    return list;
                }
            }
            return new List<string>();
        }

        public static List<StringValue> getValueList(List<SMESet> smesets)
        {
            var smeNums = smesets.OrderBy(s => s.SMENum).Select(s => s.SMENum).ToList();
            long first = smeNums.First();
            long last = smeNums.Last();
            List<StringValue> valueList = null;
            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                valueList = db.SValueSets.FromSqlRaw("SELECT * FROM SValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last + " UNION SELECT * FROM IValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last + " UNION SELECT * FROM DValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last)
                    .Where(v => smeNums.Contains(v.ParentSMENum))
                    .OrderBy(v => v.ParentSMENum)
                    .ToList();
                watch.Stop();
                Console.WriteLine("Getting the value list took this time: " + watch.ElapsedMilliseconds);
            }
            return valueList;
        }
    }

    [Index(nameof(ParentSMENum))]
    public class IntValue
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public long Value { get; set; }
        public string Annotation { get; set; }
        public StringValue asStringValue()
        {
            return new StringValue
            {
                Id = Id,
                ParentSMENum = ParentSMENum,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }

    [Index(nameof(ParentSMENum))]
    public class StringValue
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public string Value { get; set; }
        public string Annotation { get; set; }
    }

    [Index(nameof(ParentSMENum))]
    public class DoubleValue
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public double Value { get; set; }
        public string Annotation { get; set; }
        public StringValue asStringValue()
        {
            return new StringValue
            {
                Id = Id,
                ParentSMENum = ParentSMENum,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }
}