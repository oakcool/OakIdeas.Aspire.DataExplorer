using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

/// <summary>
/// SQL Server implementation of <see cref="IQueryPerformanceService"/> that reads from
/// the SQL Server Query Store (<c>sys.query_store_*</c> catalog views).
/// Each discovered SQL Server database is queried independently and results are merged.
/// A database is silently skipped when Query Store is disabled or the caller lacks access.
/// </summary>
public sealed class SqlServerQueryPerformanceService : IQueryPerformanceService
{
    private const string ConnectionStringKey = "connectionString";
    private const string ConnectionStringEnvVarKey = "connectionStringEnvironmentVariable";

    private const string GetTopQueriesSql = """
        SELECT TOP (@Limit)
            CAST(q.query_id AS NVARCHAR(20))                                                   AS query_id,
            qt.query_sql_text,
            DB_NAME()                                                                           AS database_name,
            CASE
                WHEN q.object_id <> 0
                THEN OBJECT_SCHEMA_NAME(q.object_id) + '.' + OBJECT_NAME(q.object_id)
                ELSE NULL
            END                                                                                 AS object_name,
            SUM(rs.count_executions)                                                            AS execution_count,
            ISNULL(AVG(rs.avg_duration) / 1000.0, 0)                                          AS avg_duration_ms,
            ISNULL(MAX(rs.max_duration) / 1000.0, 0)                                          AS max_duration_ms,
            ISNULL(SUM(CAST(rs.count_executions AS BIGINT) * rs.avg_duration) / 1000.0, 0)   AS total_duration_ms,
            ISNULL(AVG(rs.avg_logical_io_reads), 0)                                            AS avg_logical_reads,
            ISNULL(SUM(CAST(rs.count_executions AS BIGINT) * rs.avg_logical_io_reads), 0)     AS total_logical_reads,
            ISNULL(AVG(rs.avg_logical_io_writes), 0)                                           AS avg_logical_writes,
            ISNULL(SUM(CAST(rs.count_executions AS BIGINT) * rs.avg_logical_io_writes), 0)    AS total_logical_writes,
            ISNULL(SUM(rs.count_exceptions), 0)                                                AS failure_count,
            ISNULL(AVG(rs.avg_rowcount), 0)                                                    AS avg_row_count,
            MIN(q.last_compile_start_time)                                                     AS first_seen_at,
            MAX(rs.last_execution_time)                                                        AS last_seen_at,
            COUNT(DISTINCT p.plan_id)                                                          AS plan_count
        FROM sys.query_store_query         AS q
        INNER JOIN sys.query_store_query_text  AS qt ON q.query_text_id = qt.query_text_id
        INNER JOIN sys.query_store_plan        AS p  ON q.query_id      = p.query_id
        INNER JOIN sys.query_store_runtime_stats AS rs ON p.plan_id     = rs.plan_id
        WHERE (@QueryTextFilter IS NULL OR qt.query_sql_text LIKE N'%' + @QueryTextFilter + N'%')
        GROUP BY q.query_id, qt.query_sql_text, q.object_id
        ORDER BY
            CASE WHEN @SortBy = 0 THEN AVG(rs.avg_duration)                                        END DESC,
            CASE WHEN @SortBy = 1 THEN SUM(CAST(rs.count_executions AS BIGINT) * rs.avg_duration)  END DESC,
            CASE WHEN @SortBy = 2 THEN SUM(rs.count_executions)                                     END DESC,
            CASE WHEN @SortBy = 3 THEN SUM(CAST(rs.count_executions AS BIGINT) * rs.avg_logical_io_reads) END DESC,
            CASE WHEN @SortBy = 4 THEN SUM(rs.count_exceptions)                                     END DESC,
            CASE WHEN @SortBy = 5 THEN MAX(rs.last_execution_time)                                  END DESC;
        """;

    private readonly IAspireResourceDiscovery _resourceDiscovery;

    public SqlServerQueryPerformanceService(IAspireResourceDiscovery resourceDiscovery)
    {
        ArgumentNullException.ThrowIfNull(resourceDiscovery);
        _resourceDiscovery = resourceDiscovery;
    }

    /// <inheritdoc />
    public async Task<GetQueryPerformanceResponse> GetTopQueriesAsync(
        GetQueryPerformanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var discovered = await _resourceDiscovery.DiscoverResourcesAsync(
            new DiscoverResourcesRequest(IncludeUnavailableResources: false),
            cancellationToken);

        var targets = discovered.Resources
            .Where(r => r.ProviderType == DatabaseProviderType.SqlServer && r.IsAvailable)
            .Where(r => string.IsNullOrEmpty(request.DatabaseName)
                || string.Equals(r.DatabaseName, request.DatabaseName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (targets.Count == 0)
        {
            return new GetQueryPerformanceResponse
            {
                Entries = [],
                TotalCount = 0,
                IsSupported = false,
                UnsupportedReason = "No available SQL Server databases were found. Connect to a SQL Server database with Query Store enabled to use this feature.",
                DataSource = null,
            };
        }

        var allEntries = new List<QueryPerformanceEntry>();

        foreach (var resource in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connectionString = ResolveConnectionString(resource.ConnectionMetadata);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                continue;
            }

            var entries = await QueryDatabaseAsync(connectionString, resource.DatabaseName, request, cancellationToken);
            allEntries.AddRange(entries);
        }

        var sorted = SortAndLimit(allEntries, request);

        return new GetQueryPerformanceResponse
        {
            Entries = sorted,
            TotalCount = sorted.Count,
            IsSupported = true,
            UnsupportedReason = null,
            DataSource = "SQL Server Query Store",
        };
    }

    private static async Task<IReadOnlyList<QueryPerformanceEntry>> QueryDatabaseAsync(
        string connectionString,
        string databaseName,
        GetQueryPerformanceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(GetTopQueriesSql, connection);
            command.Parameters.AddWithValue("@Limit", request.Limit > 0 ? request.Limit : 50);
            command.Parameters.AddWithValue(
                "@QueryTextFilter",
                string.IsNullOrWhiteSpace(request.QueryTextFilter) ? DBNull.Value : (object)request.QueryTextFilter);
            command.Parameters.AddWithValue("@SortBy", (int)request.SortBy);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await ReadEntriesAsync(reader, databaseName, cancellationToken);
        }
        catch (SqlException)
        {
            // Query Store may be disabled or the caller may lack VIEW DATABASE STATE.
            // Return empty rather than surfacing provider errors.
            return [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return [];
        }
    }

    internal static async Task<IReadOnlyList<QueryPerformanceEntry>> ReadEntriesAsync(
        SqlDataReader reader,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var entries = new List<QueryPerformanceEntry>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var entry = MapRow(reader, databaseName);
            entries.Add(entry);
        }

        return entries;
    }

    internal static QueryPerformanceEntry MapRow(SqlDataReader reader, string databaseName)
    {
        var avgDurationMs = reader.GetDouble(reader.GetOrdinal("avg_duration_ms"));
        var maxDurationMs = reader.GetDouble(reader.GetOrdinal("max_duration_ms"));
        var planCount = reader.GetInt32(reader.GetOrdinal("plan_count"));

        return new QueryPerformanceEntry
        {
            QueryId = reader.GetString(reader.GetOrdinal("query_id")),
            QueryText = reader.GetString(reader.GetOrdinal("query_sql_text")),
            DatabaseName = reader.IsDBNull(reader.GetOrdinal("database_name"))
                ? databaseName
                : reader.GetString(reader.GetOrdinal("database_name")),
            ObjectName = reader.IsDBNull(reader.GetOrdinal("object_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("object_name")),
            ExecutionCount = reader.GetInt64(reader.GetOrdinal("execution_count")),
            AvgDurationMs = avgDurationMs,
            MaxDurationMs = maxDurationMs,
            TotalDurationMs = reader.GetDouble(reader.GetOrdinal("total_duration_ms")),
            AvgLogicalReads = reader.GetDouble(reader.GetOrdinal("avg_logical_reads")),
            TotalLogicalReads = reader.GetDouble(reader.GetOrdinal("total_logical_reads")),
            AvgLogicalWrites = reader.GetDouble(reader.GetOrdinal("avg_logical_writes")),
            TotalLogicalWrites = reader.GetDouble(reader.GetOrdinal("total_logical_writes")),
            FailureCount = reader.GetInt64(reader.GetOrdinal("failure_count")),
            AvgRowCount = reader.GetDouble(reader.GetOrdinal("avg_row_count")),
            FirstSeenAt = reader.IsDBNull(reader.GetOrdinal("first_seen_at"))
                ? null
                : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("first_seen_at")), TimeSpan.Zero),
            LastSeenAt = reader.IsDBNull(reader.GetOrdinal("last_seen_at"))
                ? null
                : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("last_seen_at")), TimeSpan.Zero),
            PlanCount = planCount,
            HasRegression = DetectRegression(planCount, avgDurationMs, maxDurationMs),
        };
    }

    internal static bool DetectRegression(int planCount, double avgDurationMs, double maxDurationMs)
    {
        return planCount > 1 && avgDurationMs > 0 && maxDurationMs > avgDurationMs * 3.0;
    }

    internal static IReadOnlyList<QueryPerformanceEntry> SortAndLimit(
        IReadOnlyList<QueryPerformanceEntry> entries,
        GetQueryPerformanceRequest request)
    {
        IEnumerable<QueryPerformanceEntry> sorted = request.SortBy switch
        {
            QueryPerformanceSortField.TotalDuration => entries.OrderByDescending(e => e.TotalDurationMs),
            QueryPerformanceSortField.ExecutionCount => entries.OrderByDescending(e => e.ExecutionCount),
            QueryPerformanceSortField.TotalLogicalReads => entries.OrderByDescending(e => e.TotalLogicalReads),
            QueryPerformanceSortField.FailureCount => entries.OrderByDescending(e => e.FailureCount),
            QueryPerformanceSortField.LastSeenAt => entries.OrderByDescending(e => e.LastSeenAt),
            _ => entries.OrderByDescending(e => e.AvgDurationMs),
        };

        if (request.Limit > 0)
        {
            sorted = sorted.Take(request.Limit);
        }

        return sorted.ToList();
    }

    private static string? ResolveConnectionString(ConnectionMetadata metadata)
    {
        if (metadata.Properties.TryGetValue(ConnectionStringKey, out var direct)
            && !string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (metadata.Properties.TryGetValue(ConnectionStringEnvVarKey, out var envVarName)
            && !string.IsNullOrWhiteSpace(envVarName))
        {
            return Environment.GetEnvironmentVariable(envVarName);
        }

        return null;
    }
}
