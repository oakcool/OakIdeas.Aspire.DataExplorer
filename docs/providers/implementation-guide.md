# Provider Implementation Guide

This guide describes how to add a new database provider while preserving provider isolation and shared service contracts.

## Design constraints

- Keep provider-specific logic in provider projects.
- Keep shared layers (`Contracts`, `Core`) provider-agnostic.
- Use request/response contracts for discovery and service operations.
- Keep generated SQL parameterized.

## Step-by-step

1. Create/extend a provider project under `src/` (for example, `OakIdeas.Aspire.DataExplorer.SqlServer`).
2. Implement `IMetadataProvider` and supported discovery interfaces.
3. Implement `IProviderErrorMapper` for provider-specific exception mapping.
4. Register provider implementation + mapping in composition root.
5. Add tests for projection/normalization and error mapping behavior.

## Minimal provider skeleton

```csharp
public sealed class ExampleDatabaseProvider : IMetadataProvider, ISchemaDiscoveryProvider, ITableDiscoveryProvider
{
    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    public ProviderCapabilities Capabilities { get; } = new();

    public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(
        DatabaseResource resource,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

    public Task<QueryResult> ExecuteQueryAsync(
        DatabaseResource resource,
        ExecuteQueryRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult([], [], 0, TimeSpan.Zero));

    public Task<DiscoverSchemasResponse> DiscoverSchemasAsync(
        DatabaseResource resource,
        DiscoverSchemasRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(new DiscoverSchemasResponse([]));

    public Task<DiscoverTablesResponse> DiscoverTablesAsync(
        DatabaseResource resource,
        DiscoverTablesRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(new DiscoverTablesResponse([]));
}
```

## Composition root registration example

```csharp
builder.Services.AddSingleton<SqlServerDatabaseProvider>();
builder.Services.AddOptions<MetadataProviderFactoryOptions>()
    .Configure(options => options.Register(DatabaseProviderType.SqlServer, typeof(SqlServerDatabaseProvider)));
```

## Testing requirements

- Add focused unit tests for SQL/query generation and mapping logic.
- Add provider integration tests for normalization/projection behavior.
- Keep solution-wide suites in `07 - Tests`; provider-specific tests stay in the provider section `Tests` folder.

See also:

- [Provider model](../architecture/provider-model.md)
- [SQL Server provider reference](./sqlserver.md)
- [Error handling architecture](../architecture/error-handling.md)
