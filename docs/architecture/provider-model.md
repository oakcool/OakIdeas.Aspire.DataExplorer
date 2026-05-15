# Provider Model

Providers implement shared interfaces from `Core` and expose provider type, capability flags, and metadata operations.

## Shared abstractions

- `IMetadataProvider` defines metadata operations and exposes:
  - `ProviderType` (`DatabaseProviderType`)
  - `Capabilities` (`ProviderCapabilities`)
  - `GetSchemasAsync(...)`
  - `ExecuteQueryAsync(...)`
- `ProviderCapabilities` is a provider feature map used by runtime selection/UI logic.
- `ProviderRegistration` captures startup registration entries (`ProviderType` + implementation `Type`).
- `IProviderFactory` creates providers by `DatabaseProviderType`.

## Factory registration (Options pattern)

Provider registration is configured through `MetadataProviderFactoryOptions`:

```csharp
builder.Services.AddOptions<MetadataProviderFactoryOptions>()
    .Configure(options => options.Register(
        DatabaseProviderType.SqlServer,
        typeof(SqlServerDatabaseProvider)));
```

`MetadataProviderFactory` uses these options at runtime to resolve the configured provider implementation from DI.

## SQL Server MVP

SQL Server is the first registered provider (`DatabaseProviderType.SqlServer`) and reports capabilities through `ProviderCapabilities`.
