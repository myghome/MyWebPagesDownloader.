using System.Data.SQLite;
using Microsoft.Extensions.Logging;

namespace MyWebPagesDownloader.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(string databasePath, ILogger<DatabaseInitializer> logger)
    {
        _connectionString = $"Data Source={databasePath};Version=3;";
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            
            // Schema versioning
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SchemaVersion (
                    Version INTEGER PRIMARY KEY,
                    AppliedAt TEXT NOT NULL
                );
            ";
            await command.ExecuteNonQueryAsync();

            // Main downloads table
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Downloads (
                    Id TEXT PRIMARY KEY,
                    Url TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    Priority INTEGER NOT NULL DEFAULT 0,
                    AttemptCount INTEGER NOT NULL DEFAULT 0,
                    CorrelationId TEXT,
                    CreatedAt TEXT NOT NULL,
                    StartedAt TEXT,
                    CompletedAt TEXT,
                    HttpStatusCode INTEGER,
                    ContentLength LONG,
                    ElapsedMilliseconds LONG,
                    FilePath TEXT,
                    ErrorMessage TEXT
                );
            ";
            await command.ExecuteNonQueryAsync();

            // Indices
            command.CommandText = "CREATE INDEX IF NOT EXISTS idx_status ON Downloads(Status);";
            await command.ExecuteNonQueryAsync();

            command.CommandText = "CREATE INDEX IF NOT EXISTS idx_priority ON Downloads(Priority);";
            await command.ExecuteNonQueryAsync();

            command.CommandText = "CREATE INDEX IF NOT EXISTS idx_created ON Downloads(CreatedAt);";
            await command.ExecuteNonQueryAsync();

            command.CommandText = "CREATE INDEX IF NOT EXISTS idx_correlation_id ON Downloads(CorrelationId);";
            await command.ExecuteNonQueryAsync();

            connection.Close();
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database.");
            throw;
        }
    }
}
