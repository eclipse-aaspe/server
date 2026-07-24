namespace AasxServerDB;

using System;
using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Installs and maintains the SQLite FTS5 trigram index used for substring
/// searches on <c>ValueSets.SValue</c> and <c>SMESets.IdShort</c>.
/// </summary>
internal static class SqliteTrigramIndex
{
    internal const string TableName = "ValueSets_fts";
    internal const string IdShortTableName = "SMESets_fts";
    private const int BuildBatchSize = 250_000;

    /// <summary>
    /// Adds the FTS table and its maintenance triggers to an existing database.
    /// Existing values are indexed exactly once, when the FTS table is created.
    /// </summary>
    internal static void Initialize(DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
            db.Database.OpenConnection();
        try
        {
            using var existsCommand = connection.CreateCommand();
            existsCommand.CommandText =
                "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1";
            var nameParameter = existsCommand.CreateParameter();
            nameParameter.ParameterName = "$name";
            nameParameter.Value = TableName;
            existsCommand.Parameters.Add(nameParameter);
            var indexAlreadyExists = existsCommand.ExecuteScalar() is not null;
            nameParameter.Value = IdShortTableName;
            var idShortIndexAlreadyExists = existsCommand.ExecuteScalar() is not null;

            using var transaction = db.Database.BeginTransaction();

            db.Database.ExecuteSqlRaw($$"""
                CREATE VIRTUAL TABLE IF NOT EXISTS "{{TableName}}" USING fts5(
                    "SValue",
                    content='ValueSets',
                    content_rowid='Id',
                    tokenize='trigram',
                    detail='none'
                );
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE TRIGGER IF NOT EXISTS "ValueSets_fts_ai"
                AFTER INSERT ON "ValueSets"
                BEGIN
                    INSERT INTO "{{TableName}}"(rowid, "SValue")
                    VALUES (new."Id", new."SValue");
                END;
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE TRIGGER IF NOT EXISTS "ValueSets_fts_ad"
                AFTER DELETE ON "ValueSets"
                BEGIN
                    INSERT INTO "{{TableName}}"("{{TableName}}", rowid, "SValue")
                    VALUES ('delete', old."Id", old."SValue");
                END;
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE TRIGGER IF NOT EXISTS "ValueSets_fts_au"
                AFTER UPDATE OF "SValue" ON "ValueSets"
                BEGIN
                    INSERT INTO "{{TableName}}"("{{TableName}}", rowid, "SValue")
                    VALUES ('delete', old."Id", old."SValue");
                    INSERT INTO "{{TableName}}"(rowid, "SValue")
                    VALUES (new."Id", new."SValue");
                END;
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE VIRTUAL TABLE IF NOT EXISTS "{{IdShortTableName}}" USING fts5(
                    "IdShort",
                    content='SMESets',
                    content_rowid='Id',
                    tokenize='trigram',
                    detail='none'
                );
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE TRIGGER IF NOT EXISTS "SMESets_fts_ai"
                AFTER INSERT ON "SMESets"
                BEGIN
                    INSERT INTO "{{IdShortTableName}}"(rowid, "IdShort")
                    VALUES (new."Id", new."IdShort");
                END;
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE TRIGGER IF NOT EXISTS "SMESets_fts_ad"
                AFTER DELETE ON "SMESets"
                BEGIN
                    INSERT INTO "{{IdShortTableName}}"("{{IdShortTableName}}", rowid, "IdShort")
                    VALUES ('delete', old."Id", old."IdShort");
                END;
                """);

            db.Database.ExecuteSqlRaw($$"""
                CREATE TRIGGER IF NOT EXISTS "SMESets_fts_au"
                AFTER UPDATE OF "IdShort" ON "SMESets"
                BEGIN
                    INSERT INTO "{{IdShortTableName}}"("{{IdShortTableName}}", rowid, "IdShort")
                    VALUES ('delete', old."Id", old."IdShort");
                    INSERT INTO "{{IdShortTableName}}"(rowid, "IdShort")
                    VALUES (new."Id", new."IdShort");
                END;
                """);

            if (!indexAlreadyExists)
            {
                BuildIndexWithProgress(
                    db, transaction, "ValueSets", TableName, "SValue", "ValueSets.SValue");
            }

            if (!idShortIndexAlreadyExists)
            {
                BuildIndexWithProgress(
                    db, transaction, "SMESets", IdShortTableName, "IdShort", "SMESets.IdShort");
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not initialize the SQLite FTS5 trigram indexes. " +
                "Verify that the selected SQLite library includes FTS5 and the trigram tokenizer.",
                ex);
        }
        finally
        {
            if (shouldCloseConnection)
                db.Database.CloseConnection();
        }
    }

    private static void BuildIndexWithProgress(
        DbContext db,
        IDbContextTransaction transaction,
        string contentTable,
        string ftsTable,
        string column,
        string label)
    {
        var connection = db.Database.GetDbConnection();
        var adoTransaction = transaction.GetDbTransaction();

        long ExecuteScalarLong(string sql, params (string Name, object Value)[] parameters)
        {
            using var command = connection.CreateCommand();
            command.Transaction = adoTransaction;
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }

            return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }

        // MAX(Id) uses the INTEGER PRIMARY KEY b-tree and is effectively immediate,
        // unlike COUNT(*) which has to visit every content-table row in SQLite.
        var maximumId = ExecuteScalarLong(
            $"SELECT COALESCE(MAX(\"Id\"), -1) FROM \"{contentTable}\"");
        Console.WriteLine(
            $"[SQLite] Building {label} trigram index up to Id {maximumId:N0} " +
            $"in batches of {BuildBatchSize:N0} (progress by primary-key range)...");

        if (maximumId < 0)
        {
            Console.WriteLine($"[SQLite] {label} trigram index built (database is empty).");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        long processedRows = 0;
        long lastId = -1;

        while (true)
        {
            long batchRows;
            long batchLastId;
            using (var boundaryCommand = connection.CreateCommand())
            {
                boundaryCommand.Transaction = adoTransaction;
                boundaryCommand.CommandText = $"""
                    SELECT COUNT(*), COALESCE(MAX("Id"), -1)
                    FROM (
                        SELECT "Id"
                        FROM "{contentTable}"
                        WHERE "{column}" IS NOT NULL AND "Id" > $lastId
                        ORDER BY "Id"
                        LIMIT $batchSize
                    )
                    """;
                var lastIdParameter = boundaryCommand.CreateParameter();
                lastIdParameter.ParameterName = "$lastId";
                lastIdParameter.Value = lastId;
                boundaryCommand.Parameters.Add(lastIdParameter);
                var batchSizeParameter = boundaryCommand.CreateParameter();
                batchSizeParameter.ParameterName = "$batchSize";
                batchSizeParameter.Value = BuildBatchSize;
                boundaryCommand.Parameters.Add(batchSizeParameter);

                using var reader = boundaryCommand.ExecuteReader();
                reader.Read();
                batchRows = reader.GetInt64(0);
                batchLastId = reader.GetInt64(1);
            }

            if (batchRows == 0)
                break;

            using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = adoTransaction;
                insertCommand.CommandText = $"""
                    INSERT INTO "{ftsTable}"(rowid, "{column}")
                    SELECT "Id", "{column}"
                    FROM "{contentTable}"
                    WHERE "{column}" IS NOT NULL
                      AND "Id" > $lastId
                      AND "Id" <= $batchLastId
                    ORDER BY "Id"
                    """;
                var lastIdParameter = insertCommand.CreateParameter();
                lastIdParameter.ParameterName = "$lastId";
                lastIdParameter.Value = lastId;
                insertCommand.Parameters.Add(lastIdParameter);
                var batchLastIdParameter = insertCommand.CreateParameter();
                batchLastIdParameter.ParameterName = "$batchLastId";
                batchLastIdParameter.Value = batchLastId;
                insertCommand.Parameters.Add(batchLastIdParameter);
                insertCommand.ExecuteNonQuery();
            }

            processedRows += batchRows;
            lastId = batchLastId;

            var elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            var rowsPerSecond = processedRows / elapsedSeconds;
            var idsPerSecond = (lastId + 1) / elapsedSeconds;
            var remainingSeconds = idsPerSecond > 0
                ? Math.Max(0, maximumId - lastId) / idsPerSecond
                : 0;
            var percentage = maximumId > 0
                ? Math.Clamp(lastId * 100.0 / maximumId, 0, 100)
                : 100;
            Console.WriteLine(
                $"[SQLite] {label}: {processedRows:N0} rows indexed, Id {lastId:N0} / {maximumId:N0} " +
                $"(~{percentage:F1} %) - {rowsPerSecond:N0} rows/s - " +
                $"elapsed {FormatDuration(stopwatch.Elapsed)} - " +
                $"ETA {FormatDuration(TimeSpan.FromSeconds(remainingSeconds))}");
        }

        stopwatch.Stop();
        Console.WriteLine(
            $"[SQLite] {label} trigram index built: {processedRows:N0} rows " +
            $"in {FormatDuration(stopwatch.Elapsed)}.");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalHours = (long)duration.TotalHours;
        return $"{totalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
}
