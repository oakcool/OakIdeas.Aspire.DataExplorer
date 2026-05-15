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
