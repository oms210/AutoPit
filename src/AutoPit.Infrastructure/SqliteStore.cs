using Dapper;
using Microsoft.Data.Sqlite;
using AutoPit.Core;

namespace AutoPit.Infrastructure;
public sealed class SqliteStore(string connectionString) : IAutoStore
{
    public async Task InitAsync(CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        PRAGMA journal_mode=WAL;
        CREATE TABLE IF NOT EXISTS Cars(
            Vin TEXT PRIMARY KEY, Make TEXT NOT NULL, Model TEXT NOT NULL, Year INTEGER NOT NULL, Trim TEXT NULL
        );
        CREATE TABLE IF NOT EXISTS ServiceRequests(
            Id TEXT PRIMARY KEY, Vin TEXT NOT NULL, Concern TEXT NOT NULL, Priority INTEGER NOT NULL, CreatedUtc TEXT NOT NULL, Status INTEGER NOT NULL, FailureReason TEXT NULL
        );
        CREATE TABLE IF NOT EXISTS ServiceOrders(
            RequestId TEXT PRIMARY KEY, Technician TEXT NOT NULL, Findings TEXT NOT NULL, EstimatedCost REAL NOT NULL, CompletedUtc TEXT NOT NULL
        );";
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task UpsertCarAsync(Car car, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = @"INSERT INTO Cars(Vin, Make, Model, Year, Trim) VALUES(@Vin, @Make, @Model, @Year, @Trim)
                             ON CONFLICT(Vin) DO UPDATE SET Make=@Make, Model=@Model, Year=@Year, Trim=@Trim";
        await conn.ExecuteAsync(new CommandDefinition(sql, car, cancellationToken: ct));
    }
    public async Task<Car?> GetCarAsync(string vin, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM Cars WHERE Vin=@Vin";
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { Vin = vin });
        if (row is null) return null;
        return new Car((string)row.Vin, (string)row.Make, (string)row.Model, (int)(long)row.Year, row.Trim as string);
    }
    public async Task UpsertServiceAsync(ServiceRequest req, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = @"INSERT INTO ServiceRequests(Id, Vin, Concern, Priority, CreatedUtc, Status, FailureReason)
                             VALUES(@Id, @Vin, @Concern, @Priority, @CreatedUtc, @Status, @FailureReason)
                             ON CONFLICT(Id) DO UPDATE SET Status=@Status, FailureReason=@FailureReason";
        await conn.ExecuteAsync(new CommandDefinition(sql, new {
            Id = req.Id.ToString(), req.Vin, req.Concern, req.Priority, CreatedUtc = req.CreatedUtc.ToString("O"), Status = (int)req.Status, req.FailureReason
        }, cancellationToken: ct));
    }
    public async Task<ServiceRequest?> GetServiceAsync(Guid id, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM ServiceRequests WHERE Id=@Id";
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { Id = id.ToString() });
        if (row is null) return null;
        return new ServiceRequest(Guid.Parse((string)row.Id), (string)row.Vin, (string)row.Concern, (int)(long)row.Priority,
            DateTimeOffset.Parse((string)row.CreatedUtc), (ServiceStatus)(long)row.Status, row.FailureReason as string);
    }
    public async Task SaveOrderAsync(ServiceOrder order, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = @"INSERT INTO ServiceOrders(RequestId, Technician, Findings, EstimatedCost, CompletedUtc)
                             VALUES(@RequestId, @Technician, @Findings, @EstimatedCost, @CompletedUtc)
                             ON CONFLICT(RequestId) DO UPDATE SET Technician=@Technician, Findings=@Findings, EstimatedCost=@EstimatedCost, CompletedUtc=@CompletedUtc";
        await conn.ExecuteAsync(new CommandDefinition(sql, new {
            RequestId = order.RequestId.ToString(), order.Technician, order.Findings, order.EstimatedCost, CompletedUtc = order.CompletedUtc.ToString("O")
        }, cancellationToken: ct));
    }
    public async Task<ServiceOrder?> GetOrderAsync(Guid requestId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM ServiceOrders WHERE RequestId=@Id";
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { Id = requestId.ToString() });
        if (row is null) return null;
        return new ServiceOrder(Guid.Parse((string)row.RequestId), (string)row.Technician, (string)row.Findings, (decimal)row.EstimatedCost, DateTimeOffset.Parse((string)row.CompletedUtc));
    }
    public async IAsyncEnumerable<ServiceRequest> GetQueuedAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);
        const string sql = "SELECT * FROM ServiceRequests WHERE Status = @Status ORDER BY Priority DESC, CreatedUtc ASC";
        var rows = await conn.QueryAsync(sql, new { Status = (int)ServiceStatus.Queued });
        foreach (var row in rows)
            yield return new ServiceRequest(Guid.Parse((string)row.Id), (string)row.Vin, (string)row.Concern, (int)(long)row.Priority,
                DateTimeOffset.Parse((string)row.CreatedUtc), (ServiceStatus)(long)row.Status, row.FailureReason as string);
    }
}
