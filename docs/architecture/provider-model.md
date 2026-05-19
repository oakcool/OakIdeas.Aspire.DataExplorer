# Provider Model

Providers implement shared interfaces in `Core` and are resolved at runtime through `MetadataProviderFactoryOptions` + `MetadataProviderFactory`.

```mermaid
flowchart LR
    Shared[Core abstractions\nIMetadataProvider + discovery interfaces]
    Options[MetadataProviderFactoryOptions]
    Factory[MetadataProviderFactory\n(IProviderFactory)]
    Provider[Provider project\n(e.g., SqlServerDatabaseProvider)]
    Services[Core/Web services]

    Options --> Factory
    Shared --> Provider
    Factory --> Provider
    Services --> Factory
```

## Shared abstractions

- `IMetadataProvider` defines provider identity (`ProviderType`), capabilities, base metadata/query operations.
- Specialized discovery interfaces (`ITableDiscoveryProvider`, `IColumnDiscoveryProvider`, etc.) are implemented only when supported.
- Service operations use request/response contracts from `Contracts/Models`.
- Query execution stays provider-owned (`IMetadataProvider.ExecuteQueryAsync`) so SQL text execution, command configuration, and provider error surfaces remain isolated in provider projects.

## Registration and composition-root rule

Register provider mappings via `MetadataProviderFactoryOptions` in composition roots (`Web`, provider host projects):

```csharp
builder.Services.AddOptions<MetadataProviderFactoryOptions>()
    .Configure(options => options.Register(
        DatabaseProviderType.SqlServer,
        typeof(SqlServerDatabaseProvider)));
```

Rules:

- Keep provider-specific implementation and SQL in provider projects.
- Do not register concrete providers from shared contracts/core model projects.
- Keep shared layers provider-agnostic.

## Extensibility checklist

1. Implement `IMetadataProvider` in a provider project.
2. Implement relevant discovery interfaces for supported metadata types.
3. Add `IProviderErrorMapper` mapping for provider-specific exceptions.
4. Register provider type -> implementation mapping in composition root.
5. Add provider-focused tests (projection/normalization + error mapping).

For a full walkthrough, see [Provider implementation guide](../providers/implementation-guide.md).
