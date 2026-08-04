using System.Text;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

/// <summary>
/// SQL Server implementation of <see cref="IRelationshipNavigationProvider"/>.
/// Discovers parent, child, and many-to-many relationships using the SQL Server system catalogs
/// and generates safe parameterized T-SQL queries for relationship navigation.
/// </summary>
public sealed class SqlServerRelationshipNavigationProvider : IRelationshipNavigationProvider
{
    /// <inheritdoc />
    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    private const string DiscoverRelationshipsSql = """
        SELECT
            fk.name                          AS constraint_name,
            ps.name                          AS parent_schema,
            pt.name                          AS parent_table,
            rs.name                          AS referenced_schema,
            rt.name                          AS referenced_table,
            pc.name                          AS parent_column,
            rc.name                          AS referenced_column,
            fkc.constraint_column_id,
            fk.is_disabled
        FROM sys.foreign_keys AS fk
        INNER JOIN sys.tables   AS pt  ON fk.parent_object_id     = pt.object_id
        INNER JOIN sys.schemas  AS ps  ON pt.schema_id            = ps.schema_id
        INNER JOIN sys.tables   AS rt  ON fk.referenced_object_id = rt.object_id
        INNER JOIN sys.schemas  AS rs  ON rt.schema_id            = rs.schema_id
        INNER JOIN sys.foreign_key_columns AS fkc
            ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.columns AS pc
            ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
        INNER JOIN sys.columns AS rc
            ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
        WHERE (ps.name = @SchemaName AND pt.name = @TableName)
           OR (rs.name = @SchemaName AND rt.name = @TableName)
        ORDER BY fk.name, fkc.constraint_column_id;
        """;

    private const string CountRelatedRecordsSql = """
        SELECT COUNT(1) FROM [{0}].[{1}] WHERE {2};
        """;

    private const int DefaultCountLimit = 10_000;
    private const int DefaultPageSize = 100;

    /// <inheritdoc />
    public async Task<DiscoverTableRelationshipsResponse> DiscoverTableRelationshipsAsync(
        DatabaseResource resource,
        DiscoverTableRelationshipsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = new SqlConnection(resource.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var rows = new List<RelationshipRow>();

        await using var command = new SqlCommand(DiscoverRelationshipsSql, connection);
        command.Parameters.AddWithValue("@SchemaName", request.SchemaName);
        command.Parameters.AddWithValue("@TableName", request.TableName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new RelationshipRow(
                ConstraintName: reader.GetString(reader.GetOrdinal("constraint_name")),
                ParentSchema: reader.GetString(reader.GetOrdinal("parent_schema")),
                ParentTable: reader.GetString(reader.GetOrdinal("parent_table")),
                ReferencedSchema: reader.GetString(reader.GetOrdinal("referenced_schema")),
                ReferencedTable: reader.GetString(reader.GetOrdinal("referenced_table")),
                ParentColumn: reader.GetString(reader.GetOrdinal("parent_column")),
                ReferencedColumn: reader.GetString(reader.GetOrdinal("referenced_column")),
                IsDisabled: reader.GetBoolean(reader.GetOrdinal("is_disabled"))));
        }

        var relationships = NormalizeRelationships(request.SchemaName, request.TableName, rows);
        return new DiscoverTableRelationshipsResponse(relationships);
    }

    /// <inheritdoc />
    public async Task<GetRelatedRecordCountResponse> GetRelatedRecordCountAsync(
        DatabaseResource resource,
        GetRelatedRecordCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        if (request.KeyValues.Count == 0)
        {
            return new GetRelatedRecordCountResponse(0);
        }

        var (whereSql, _) = BuildWhereClause(request.KeyValues, "@p");

        var sql = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            CountRelatedRecordsSql,
            EscapeBracket(request.SchemaName),
            EscapeBracket(request.TableName),
            whereSql);

        await using var connection = new SqlConnection(resource.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        for (var i = 0; i < request.KeyValues.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", (object?)request.KeyValues[i].ColumnValue ?? DBNull.Value);
        }

        var countObj = await command.ExecuteScalarAsync(cancellationToken);
        var count = Convert.ToInt32(countObj, System.Globalization.CultureInfo.InvariantCulture);
        var isTruncated = count > DefaultCountLimit;

        return new GetRelatedRecordCountResponse(Math.Min(count, DefaultCountLimit), isTruncated);
    }

    /// <inheritdoc />
    public async Task<NavigateRelatedRecordsResponse> NavigateRelatedRecordsAsync(
        DatabaseResource resource,
        NavigateRelatedRecordsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);

        if (request.KeyValues.Count == 0)
        {
            return new NavigateRelatedRecordsResponse([], 0, false, string.Empty);
        }

        var pageSize = Math.Clamp(request.PageSize, 1, DefaultPageSize);
        var pageNumber = Math.Max(1, request.PageNumber);
        var offset = (pageNumber - 1) * pageSize;

        var (whereSql, paramNames) = BuildWhereClause(request.KeyValues, "@p");

        var generatedSql = BuildNavigationSql(
            request.SchemaName,
            request.TableName,
            whereSql,
            pageSize,
            offset);

        await using var connection = new SqlConnection(resource.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(generatedSql, connection);
        for (var i = 0; i < request.KeyValues.Count; i++)
        {
            command.Parameters.AddWithValue(paramNames[i], (object?)request.KeyValues[i].ColumnValue ?? DBNull.Value);
        }

        var rows = new List<Dictionary<string, object?>>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[name] = value;
            }

            rows.Add(row);
        }

        var hasMore = rows.Count == pageSize;

        return new NavigateRelatedRecordsResponse(
            Rows: rows,
            TotalCount: rows.Count + offset,
            HasMore: hasMore,
            GeneratedSql: generatedSql);
    }

    internal static IReadOnlyList<TableRelationship> NormalizeRelationships(
        string schemaName,
        string tableName,
        IReadOnlyList<RelationshipRow> rows)
    {
        var byConstraint = rows
            .GroupBy(r => r.ConstraintName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var relationships = new List<TableRelationship>(byConstraint.Count * 2);

        foreach (var group in byConstraint)
        {
            var first = group.First();

            var isCurrentTableParent =
                string.Equals(first.ReferencedSchema, schemaName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(first.ReferencedTable, tableName, StringComparison.OrdinalIgnoreCase);

            var isCurrentTableChild =
                string.Equals(first.ParentSchema, schemaName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(first.ParentTable, tableName, StringComparison.OrdinalIgnoreCase);

            if (!isCurrentTableParent && !isCurrentTableChild)
            {
                continue;
            }

            // For self-referencing relationships the table is both parent and child.
            // Add both perspectives so the developer can navigate in either direction.
            if (isCurrentTableChild)
            {
                var parentMappings = group
                    .Select(r => new RelationshipColumnMapping(r.ParentColumn, r.ReferencedColumn))
                    .ToArray();

                relationships.Add(new TableRelationship
                {
                    ConstraintName = first.ConstraintName,
                    Kind = RelationshipKind.Parent,
                    RelatedSchemaName = first.ReferencedSchema,
                    RelatedTableName = first.ReferencedTable,
                    ColumnMappings = parentMappings,
                    IsEnforced = !first.IsDisabled,
                });
            }

            if (isCurrentTableParent)
            {
                var childMappings = group
                    .Select(r => new RelationshipColumnMapping(r.ReferencedColumn, r.ParentColumn))
                    .ToArray();

                relationships.Add(new TableRelationship
                {
                    ConstraintName = first.ConstraintName,
                    Kind = RelationshipKind.Child,
                    RelatedSchemaName = first.ParentSchema,
                    RelatedTableName = first.ParentTable,
                    ColumnMappings = childMappings,
                    IsEnforced = !first.IsDisabled,
                });
            }
        }

        return relationships;
    }

    private static (string whereSql, IReadOnlyList<string> paramNames) BuildWhereClause(
        IReadOnlyList<RelationshipKeyValue> keyValues,
        string paramPrefix)
    {
        var sb = new StringBuilder();
        var paramNames = new List<string>(keyValues.Count);

        for (var i = 0; i < keyValues.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(" AND ");
            }

            var paramName = $"{paramPrefix}{i}";
            paramNames.Add(paramName);

            if (keyValues[i].ColumnValue is null)
            {
                sb.Append($"[{EscapeBracket(keyValues[i].ColumnName)}] IS NULL");
            }
            else
            {
                sb.Append($"[{EscapeBracket(keyValues[i].ColumnName)}] = {paramName}");
            }
        }

        return (sb.ToString(), paramNames);
    }

    private static string BuildNavigationSql(
        string schemaName,
        string tableName,
        string whereSql,
        int pageSize,
        int offset)
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT * FROM [{EscapeBracket(schemaName)}].[{EscapeBracket(tableName)}]");

        if (!string.IsNullOrWhiteSpace(whereSql))
        {
            sb.Append($" WHERE {whereSql}");
        }

        sb.Append(
            System.Globalization.CultureInfo.InvariantCulture,
            $" ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;");

        return sb.ToString();
    }

    private static string EscapeBracket(string identifier)
        => identifier.Replace("]", "]]", StringComparison.Ordinal);

    internal readonly record struct RelationshipRow(
        string ConstraintName,
        string ParentSchema,
        string ParentTable,
        string ReferencedSchema,
        string ReferencedTable,
        string ParentColumn,
        string ReferencedColumn,
        bool IsDisabled);
}
