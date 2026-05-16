# SQL Server Provider (MVP)

The SQL Server provider project contains SQL Server-specific discovery and query behavior.

`SqlServerDatabaseProvider` implements `IMetadataProvider` (via `IDatabaseProvider`) and reports:

- `ProviderType = DatabaseProviderType.SqlServer`
- SQL Server capability flags through `ProviderCapabilities`

## Schema discovery

`SqlServerDatabaseProvider` also implements `ISchemaDiscoveryProvider` and returns `DiscoverSchemasResponse` with `SchemaObject` entries.

- Source catalog view: `sys.schemas`
- Default behavior: excludes system schemas (`dbo`, `guest`, `INFORMATION_SCHEMA`, `sys`)
- Optional behavior: include system schemas via `DiscoverSchemasRequest.IncludeSystemSchemas = true`
- Provider metadata: each schema includes `schemaId` from `sys.schemas.schema_id`

SQL used for discovery:

```sql
SELECT schema_id, name
FROM sys.schemas
WHERE schema_id > 0
  AND (
      @IncludeSystemSchemas = 1
      OR name NOT IN (N'dbo', N'guest', N'INFORMATION_SCHEMA', N'sys')
  )
ORDER BY name;
```

## Foreign key discovery

`SqlServerDatabaseProvider` also implements `IForeignKeyDiscoveryProvider` and returns `DiscoverForeignKeysResponse` with normalized `ForeignKeyConstraint` entries.

- Source catalog views: `sys.foreign_keys`, `sys.foreign_key_columns`, `sys.tables`, `sys.schemas`, and `sys.columns`
- Supports full-database discovery or optional parent table filtering via `DiscoverForeignKeysRequest.ParentSchemaName` and `ParentTableName`
- Preserves composite key column order using `sys.foreign_key_columns.constraint_column_id`
- Includes disabled state from `sys.foreign_keys.is_disabled`

SQL used for discovery:

```sql
SELECT
    fk.object_id,
    fk.name AS constraint_name,
    ps.name AS parent_schema,
    pt.name AS parent_table,
    rs.name AS referenced_schema,
    rt.name AS referenced_table,
    pc.name AS parent_column,
    rc.name AS referenced_column,
    fkc.constraint_column_id,
    fk.delete_referential_action,
    fk.update_referential_action,
    fk.is_disabled
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS pt ON fk.parent_object_id = pt.object_id
INNER JOIN sys.schemas AS ps ON pt.schema_id = ps.schema_id
INNER JOIN sys.tables AS rt ON fk.referenced_object_id = rt.object_id
INNER JOIN sys.schemas AS rs ON rt.schema_id = rs.schema_id
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns AS pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
INNER JOIN sys.columns AS rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
WHERE (@ParentSchemaName IS NULL OR ps.name = @ParentSchemaName)
  AND (@ParentTableName IS NULL OR pt.name = @ParentTableName)
ORDER BY fk.object_id, fkc.constraint_column_id;
```

Referential action mapping:

| SQL Server action code | `ReferentialActionBehavior` |
|---|---|
| `0` | `NoAction` |
| `1` | `Cascade` |
| `2` | `SetNull` |
| `3` | `SetDefault` |
| `4` | `NoAction` |

## Table discovery

`SqlServerDatabaseProvider` also implements `ITableDiscoveryProvider` and returns `DiscoverTablesResponse` with `TableObject` entries.

- Source catalog views: `sys.tables`, `sys.dm_db_partition_stats`
- Default behavior: excludes system tables (`is_ms_shipped = 1`)
- Optional behavior: include system tables via `DiscoverTablesRequest.IncludeSystemTables = true`
- Optional behavior: filter to a single schema via `DiscoverTablesRequest.SchemaName`
- Row counts are approximate estimates from `sys.dm_db_partition_stats` (heap or clustered index pages only, `index_id IN (0, 1)`)
- Provider metadata: each table includes `objectId` from `sys.tables.object_id` and `rowCount` (approximate)

SQL used for discovery:

```sql
SELECT
    t.object_id,
    SCHEMA_NAME(t.schema_id) AS schema_name,
    t.name AS table_name,
    ISNULL(SUM(ps.row_count), 0) AS row_count
FROM sys.tables AS t
LEFT JOIN sys.dm_db_partition_stats AS ps ON t.object_id = ps.object_id
    AND ps.index_id IN (0, 1)
WHERE (@IncludeSystemTables = 1 OR t.is_ms_shipped = 0)
  AND (@SchemaName IS NULL OR SCHEMA_NAME(t.schema_id) = @SchemaName)
GROUP BY t.object_id, SCHEMA_NAME(t.schema_id), t.name
ORDER BY schema_name, table_name;
```

Table metadata shape:

- `ObjectId` (string, SQL Server `object_id` as string)
- `SchemaName`, `ObjectName`, `FullyQualifiedName`
- `ProviderMetadata` (`objectId`, `rowCount`)

## View discovery

`SqlServerDatabaseProvider` also implements `IViewDiscoveryProvider` and returns `DiscoverViewsResponse` with `ViewObject` entries.

- Source catalog view: `sys.views`
- Default behavior: excludes system views (`is_ms_shipped = 1`)
- Optional behavior: include system views via `DiscoverViewsRequest.IncludeSystemViews = true`
- Optional behavior: filter to a single schema via `DiscoverViewsRequest.SchemaName`
- `ViewObject.HasDefinitionAvailable` is `true` when `OBJECT_DEFINITION()` returns a non-null result
- Provider metadata: each view includes `objectId` from `sys.views.object_id`

SQL used for discovery:

```sql
SELECT
    v.object_id,
    SCHEMA_NAME(v.schema_id) AS schema_name,
    v.name AS view_name,
    CASE WHEN OBJECT_DEFINITION(v.object_id) IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS has_definition
FROM sys.views AS v
WHERE (@IncludeSystemViews = 1 OR v.is_ms_shipped = 0)
  AND (@SchemaName IS NULL OR SCHEMA_NAME(v.schema_id) = @SchemaName)
ORDER BY schema_name, view_name;
```

View metadata shape:

- `ObjectId` (string, SQL Server `object_id` as string)
- `SchemaName`, `ObjectName`, `FullyQualifiedName`
- `HasDefinitionAvailable` — `true` when the view SQL definition can be retrieved
- `ProviderMetadata` (`objectId`)

## Column discovery

`SqlServerDatabaseProvider` also implements `IColumnDiscoveryProvider` and returns `DiscoverColumnsResponse` with normalized `ColumnMetadata` entries.

- Source catalog views: `sys.columns`, `sys.types`, `sys.identity_columns`, `sys.computed_columns`, `sys.default_constraints`, and `sys.extended_properties`
- Supports discovery by SQL Server `object_id` (`DiscoverColumnsRequest.ObjectId`) or fully-qualified name (`DiscoverColumnsRequest.FullyQualifiedName`) for `Table` or `View`
- Preserves ordinal ordering using `sys.columns.column_id`
- Captures nullability, identity/computed flags, defaults, and optional `MS_Description` text

SQL used for discovery:

```sql
SELECT
    c.object_id,
    c.column_id,
    c.name AS column_name,
    t.name AS data_type,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    CAST(ISNULL(ic.is_identity, 0) AS bit) AS is_identity,
    CAST(ISNULL(cc.is_computed, 0) AS bit) AS is_computed,
    dc.definition AS default_value,
    CAST(ep.value AS nvarchar(4000)) AS description
FROM sys.columns AS c
INNER JOIN sys.objects AS o ON c.object_id = o.object_id
INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id
INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
LEFT JOIN sys.identity_columns AS ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT JOIN sys.computed_columns AS cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
LEFT JOIN sys.default_constraints AS dc ON c.default_object_id = dc.object_id
LEFT JOIN sys.extended_properties AS ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.class = 1 AND ep.name = N'MS_Description'
WHERE (
        @ObjectId IS NOT NULL AND c.object_id = @ObjectId
    )
    OR (
        @ObjectId IS NULL
        AND s.name = @SchemaName
        AND o.name = @ObjectName
        AND o.type = @ObjectType
    )
ORDER BY c.column_id;
```

Common type mapping examples:

| SQL Server type | `ColumnMetadata.DataType` | Example metadata |
|---|---|---|
| `int` | `int` | `MaxLength=4`, `Precision=10`, `Scale=0` |
| `nvarchar(200)` | `nvarchar` | `MaxLength=400`, `Precision=null`, `Scale=null` |
| `decimal(18,2)` | `decimal` | `MaxLength=9`, `Precision=18`, `Scale=2` |
| `datetime2(7)` | `datetime2` | `MaxLength=8`, `Precision=27`, `Scale=7` |
| `uniqueidentifier` | `uniqueidentifier` | `MaxLength=16`, `DefaultValue=(newid())` |

Column metadata shape:

- `Name`, `Ordinal`, `DataType`
- `MaxLength`, `Precision`, `Scale`
- `IsNullable`, `IsIdentity`, `IsComputed`
- `DefaultValue`, `Description`
- `ProviderMetadata` (`objectId`, `columnId`)

## Index discovery

`SqlServerDatabaseProvider` also implements `IIndexDiscoveryProvider` and returns `DiscoverIndexesResponse` with normalized `IndexMetadata` entries.

- Source catalog views: `sys.indexes`, `sys.index_columns`, `sys.tables`, `sys.schemas`, and `sys.columns`
- Default behavior: excludes heaps by requiring `sys.indexes.index_id > 0`
- Excludes hypothetical indexes (`is_hypothetical = 0`)
- Supports full-database discovery or optional table filtering via `DiscoverIndexesRequest.SchemaName` and `TableName`
- Preserves key column order using `sys.index_columns.key_ordinal`
- Captures included columns separately using `sys.index_columns.is_included_column`
- Captures filtered index predicates from `sys.indexes.filter_definition`
- Uses a provider-specific composite identifier in `IndexMetadata.ObjectId` formatted as `{object_id}:{index_id}`

SQL used for discovery:

```sql
SELECT
    i.object_id,
    i.index_id,
    i.name AS index_name,
    s.name AS schema_name,
    t.name AS table_name,
    i.is_primary_key,
    i.is_unique,
    CAST(CASE WHEN i.type IN (1, 5) THEN 1 ELSE 0 END AS bit) AS is_clustered,
    c.name AS column_name,
    ic.is_included_column,
    ic.key_ordinal,
    ic.index_column_id,
    i.filter_definition
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON i.object_id = t.object_id
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns AS c ON t.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.index_id > 0
  AND i.is_hypothetical = 0
  AND (@SchemaName IS NULL OR s.name = @SchemaName)
  AND (@TableName IS NULL OR t.name = @TableName)
  AND (ic.key_ordinal > 0 OR ic.is_included_column = 1)
ORDER BY s.name, t.name, i.name, ic.is_included_column, CASE
    WHEN ic.is_included_column = 1 THEN ic.index_column_id
    ELSE ic.key_ordinal
END;
```

Index type notes:

| SQL Server metadata | Meaning in `IndexMetadata` |
|---|---|
| `is_primary_key = 1` | `IsPrimaryKey = true` |
| `is_unique = 1` | `IsUnique = true` |
| `type IN (1, 5)` | `IsClustered = true` |
| `is_included_column = 1` | Column appears in `IncludedColumns` instead of `Columns` |
| `filter_definition IS NOT NULL` | Filtered index predicate is exposed through `FilterDefinition` |

Index metadata shape:

- `IndexName`
- `TableName` (schema-qualified)
- `SchemaName`
- `IsPrimaryKey`, `IsUnique`, `IsClustered`
- `Columns` (key column names in ordinal order)
- `IncludedColumns`
- `FilterDefinition`
- `ObjectId` (`{object_id}:{index_id}`)

## Primary key discovery

`SqlServerDatabaseProvider` also implements `IPrimaryKeyDiscoveryProvider` and returns `DiscoverPrimaryKeysResponse` with normalized `PrimaryKeyConstraint` entries.

- Source catalog views: `sys.key_constraints`, `sys.indexes`, `sys.index_columns`, `sys.tables`, `sys.schemas`, and `sys.columns`
- Supports full-database discovery or optional table filtering via `DiscoverPrimaryKeysRequest.SchemaName` and `TableName`
- Preserves primary key column order using `sys.index_columns.key_ordinal`
- Captures whether the backing primary key index is clustered
- Uses the SQL Server constraint `object_id` as the provider-specific `PrimaryKeyConstraint.ObjectId`

SQL used for discovery:

```sql
SELECT
    kc.object_id,
    kc.name AS constraint_name,
    s.name AS schema_name,
    t.name AS table_name,
    CAST(CASE WHEN i.type IN (1, 5) THEN 1 ELSE 0 END AS bit) AS is_clustered,
    c.name AS column_name,
    ic.key_ordinal
FROM sys.key_constraints AS kc
INNER JOIN sys.tables AS t ON kc.parent_object_id = t.object_id
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
INNER JOIN sys.indexes AS i ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns AS c ON t.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.type = 'PK'
  AND (@SchemaName IS NULL OR s.name = @SchemaName)
  AND (@TableName IS NULL OR t.name = @TableName)
  AND ic.key_ordinal > 0
ORDER BY s.name, t.name, kc.name, ic.key_ordinal;
```

Primary key metadata shape:

- `ConstraintName`
- `TableName` (schema-qualified)
- `SchemaName`
- `KeyColumns` (ordinal order)
- `IsClustered`
- `ObjectId`

Registration uses `MetadataProviderFactoryOptions` with DI:

```csharp
builder.Services.AddSingleton<SqlServerDatabaseProvider>();
builder.Services.AddOptions<MetadataProviderFactoryOptions>()
    .Configure(options => options.Register(
        DatabaseProviderType.SqlServer,
        typeof(SqlServerDatabaseProvider)));
```

## Connection Provider

`SqlServerConnectionProvider` implements `ISqlServerConnectionFactory` and manages SQL Server connections for development-time use.

### Registration

```csharp
builder.Services.AddSingleton<ISqlServerConnectionFactory, SqlServerConnectionProvider>();
builder.Services.AddOptions<SqlServerConnectionOptions>()
    .Configure(options =>
    {
        options.ConnectionTimeoutSeconds = 30;
        options.ValidationTimeoutSeconds = 10;
    });
```

### `ISqlServerConnectionFactory` methods

- `CreateConnectionAsync(connectionString, cancellationToken)` — creates and opens a `SqlConnection` for the given connection string.
- `ValidateConnectionAsync(connectionString, cancellationToken)` — tests connectivity and returns a `ConnectionValidationResult` with `IsValid` and an optional `ErrorMessage`.
- `GetConnectionAsync(SelectedDatabaseContext, cancellationToken)` — resolves the connection string from the selected database context metadata and returns an open connection.

### Connection string resolution

`GetConnectionAsync` resolves the connection string in priority order:

1. `connectionString` key in `ConnectionMetadata.Properties` (direct value).
2. `connectionStringEnvironmentVariable` key — reads the named environment variable.

### Development-time guard

`SqlServerConnectionProvider` enforces development-only access via `DevelopmentEnvironmentGuard`. An `InvalidOperationException` is thrown at construction time when the environment is not `Development`.

### `SqlServerConnectionOptions`

| Property | Default | Description |
|---|---|---|
| `ConnectionTimeoutSeconds` | `30` | Timeout for opening a connection. |
| `ValidationTimeoutSeconds` | `10` | Timeout used by `ValidateConnectionAsync`. |

Configuration section: `OakIdeas:Aspire:DataExplorer:SqlServer`.

## Troubleshooting

| Symptom | Likely cause | Resolution |
|---|---|---|
| `InvalidOperationException: development-time-only` | Provider instantiated outside Development | Ensure `ASPNETCORE_ENVIRONMENT=Development`. |
| `InvalidOperationException: No connection string found` | Missing metadata keys | Verify `connectionStringEnvironmentVariable` or `connectionString` is populated in `ConnectionMetadata`. |
| `ConnectionValidationResult.IsValid = false` + timeout message | Server unreachable within timeout | Check SQL Server is running; increase `ValidationTimeoutSeconds` if needed. |
| `SqlException` during validation | Wrong credentials or database name | Check connection string values. |
