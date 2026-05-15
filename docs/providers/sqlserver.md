# SQL Server Provider (MVP)

The SQL Server provider project contains SQL Server-specific discovery and query behavior.

`SqlServerDatabaseProvider` implements `IMetadataProvider` (via `IDatabaseProvider`) and reports:

- `ProviderType = DatabaseProviderType.SqlServer`
- SQL Server capability flags through `ProviderCapabilities`

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
