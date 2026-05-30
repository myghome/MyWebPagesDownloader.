using System.Data.SQLite;
using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.Entities;
using MyWebPagesDownloader.Core.Enums;
using Microsoft.Extensions.Logging;

namespace MyWebPagesDownloader.Infrastructure.Persistence;

public sealed class SqliteDownloadRepository : IDownloadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteDownloadRepository> _logger;

    public SqliteDownloadRepository(string databasePath, ILogger<SqliteDownloadRepository> logger)
    {
        _connectionString = $"Data Source={databasePath};Version=3;";
        _logger = logger;
    }

    public async Task<DownloadRecord?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Downloads WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id);

            var record = await ReadRecordAsync(await command.ExecuteReaderAsync(cancellationToken));
            connection.Close();
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record by ID: {Id}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<DownloadRecord>> GetFailedAsync(CancellationToken cancellationToken)
    {
        return await GetByStatusAsync(DownloadStatus.Failed, cancellationToken);
    }

    public async Task<IReadOnlyList<DownloadRecord>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Downloads ORDER BY CreatedAt DESC LIMIT @Count;";
            command.Parameters.AddWithValue("@Count", count);

            var records = new List<DownloadRecord>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(MapRecord(reader));
            }

            connection.Close();
            return records.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent records.");
            throw;
        }
    }

    public async Task<IReadOnlyList<DownloadRecord>> GetByStatusAsync(DownloadStatus status, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Downloads WHERE Status = @Status;";
            command.Parameters.AddWithValue("@Status", (int)status);

            var records = new List<DownloadRecord>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(MapRecord(reader));
            }

            connection.Close();
            return records.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting records by status: {Status}", status);
            throw;
        }
    }

    public async Task<IReadOnlyList<DownloadRecord>> GetIncompleteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Downloads 
                WHERE Status IN (@Pending, @Queued, @Running, @Retrying);";
            command.Parameters.AddWithValue("@Pending", (int)DownloadStatus.Pending);
            command.Parameters.AddWithValue("@Queued", (int)DownloadStatus.Queued);
            command.Parameters.AddWithValue("@Running", (int)DownloadStatus.Running);
            command.Parameters.AddWithValue("@Retrying", (int)DownloadStatus.Retrying);

            var records = new List<DownloadRecord>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(MapRecord(reader));
            }

            connection.Close();
            return records.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting incomplete records.");
            throw;
        }
    }

    public async Task SaveAsync(DownloadRecord record, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Downloads 
                (Id, Url, Status, Priority, AttemptCount, CorrelationId, CreatedAt, StartedAt, CompletedAt, HttpStatusCode, ContentLength, ElapsedMilliseconds, FilePath, ErrorMessage)
                VALUES 
                (@Id, @Url, @Status, @Priority, @AttemptCount, @CorrelationId, @CreatedAt, @StartedAt, @CompletedAt, @HttpStatusCode, @ContentLength, @ElapsedMilliseconds, @FilePath, @ErrorMessage);";
            
            AddParameters(command, record);
            await command.ExecuteNonQueryAsync(cancellationToken);
            connection.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving record: {Id}", record.Id);
            throw;
        }
    }

    public async Task UpdateAsync(DownloadRecord record, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Downloads 
                SET Status = @Status, Priority = @Priority, AttemptCount = @AttemptCount, StartedAt = @StartedAt, 
                    CompletedAt = @CompletedAt, HttpStatusCode = @HttpStatusCode, ContentLength = @ContentLength, 
                    ElapsedMilliseconds = @ElapsedMilliseconds, FilePath = @FilePath, ErrorMessage = @ErrorMessage
                WHERE Id = @Id;";
            
            AddParameters(command, record);
            await command.ExecuteNonQueryAsync(cancellationToken);
            connection.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating record: {Id}", record.Id);
            throw;
        }
    }

    private void AddParameters(SQLiteCommand command, DownloadRecord record)
    {
        command.Parameters.AddWithValue("@Id", record.Id);
        command.Parameters.AddWithValue("@Url", record.Url);
        command.Parameters.AddWithValue("@Status", (int)record.Status);
        command.Parameters.AddWithValue("@Priority", (int)record.Priority);
        command.Parameters.AddWithValue("@AttemptCount", record.AttemptCount);
        command.Parameters.AddWithValue("@CorrelationId", record.CorrelationId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", record.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@StartedAt", record.StartedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletedAt", record.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@HttpStatusCode", record.HttpStatusCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ContentLength", record.ContentLength ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ElapsedMilliseconds", record.ElapsedMilliseconds ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FilePath", record.FilePath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", record.ErrorMessage ?? (object)DBNull.Value);
    }

    private async Task<DownloadRecord?> ReadRecordAsync(System.Data.Common.DbDataReader reader)
    {
        using (reader)
        {
            if (await reader.ReadAsync())
                return MapRecord(reader);
        }
        return null;
    }

    private static DownloadRecord MapRecord(System.Data.Common.DbDataReader reader)
    {
        return new DownloadRecord
        {
            Id = reader["Id"].ToString() ?? "",
            Url = reader["Url"].ToString() ?? "",
            Status = (DownloadStatus)(int)reader["Status"],
            Priority = (DownloadPriority)(int)reader["Priority"],
            AttemptCount = (int)reader["AttemptCount"],
            CorrelationId = reader["CorrelationId"] as string,
            CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.UtcNow.ToString("O")),
            StartedAt = reader["StartedAt"] != DBNull.Value ? DateTime.Parse((string)reader["StartedAt"]) : null,
            CompletedAt = reader["CompletedAt"] != DBNull.Value ? DateTime.Parse((string)reader["CompletedAt"]) : null,
            HttpStatusCode = reader["HttpStatusCode"] != DBNull.Value ? (int)reader["HttpStatusCode"] : null,
            ContentLength = reader["ContentLength"] != DBNull.Value ? (long)reader["ContentLength"] : null,
            ElapsedMilliseconds = reader["ElapsedMilliseconds"] != DBNull.Value ? (long)reader["ElapsedMilliseconds"] : null,
            FilePath = reader["FilePath"] as string,
            ErrorMessage = reader["ErrorMessage"] as string
        };
    }
}
