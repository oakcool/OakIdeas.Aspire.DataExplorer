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
