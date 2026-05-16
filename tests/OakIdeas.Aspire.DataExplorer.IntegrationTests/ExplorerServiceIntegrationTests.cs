using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class ExplorerServiceIntegrationTests
{
    [Fact]
    public async Task ExplorerWorkflow_DiscoverSelectMetadataRefreshAndDefinition_WorksEndToEnd()
    {
        var services = new ServiceCollection();
        services.AddSingleton<StubResourceDiscovery>();
        services.AddSingleton<IAspireResourceDiscovery>(provider => provider.GetRequiredService<StubResourceDiscovery>());
        services.AddSingleton<StubDatabaseProvider>();
        services.AddSingleton<IProviderFactory, MetadataProviderFactory>();
        services.AddOptions<MetadataProviderFactoryOptions>()
            .Configure(options => options.Register(DatabaseProviderType.SqlServer, typeof(StubDatabaseProvider)));
        services.AddSelectedDatabaseService();
        services.AddMetadataRefreshService();
        services.AddScoped<IExplorerService, ExplorerService>();

        await using var scope = services.BuildServiceProvider().CreateAsyncScope();
        var explorerService = scope.ServiceProvider.GetRequiredService<IExplorerService>();

        var available = await explorerService.GetAvailableDatabasesAsync(CancellationToken.None);
        var selectResponse = await explorerService.SelectDatabaseAsync("sql-main", CancellationToken.None);
        var selected = await explorerService.GetSelectedDatabaseAsync(CancellationToken.None);
        var metadata = await explorerService.GetDatabaseMetadataAsync(CancellationToken.None);
        var refresh = await explorerService.RefreshDatabaseMetadataAsync(CancellationToken.None);
        var definition = await explorerService.GetObjectDefinitionAsync("table.products", DatabaseObjectType.Table, CancellationToken.None);

        available.Resources.Should().ContainSingle(resource => resource.ResourceId == "sql-main");
        selectResponse.Succeeded.Should().BeTrue();
        selected.Selection.Should().NotBeNull();
        selected.Selection!.ResourceId.Should().Be("sql-main");
        metadata.Metadata.Should().NotBeNull();
        metadata.Metadata!.Objects[DatabaseObjectType.Table].Should().ContainKey("dbo.Products");
        refresh.Status.Should().Be(RefreshStatus.Completed);
        definition.IsAvailable.Should().BeTrue();
        definition.Definition.Should().Contain("CREATE TABLE");
    }

    private sealed class StubResourceDiscovery : IAspireResourceDiscovery
    {
        public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(DiscoverResourcesRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resources = new[]
            {
                new DiscoveredDatabaseResource(
                    ResourceId: "sql-main",
                    ResourceName: "sql-main",
                    DatabaseName: "applicationdb",
                    ProviderType: DatabaseProviderType.SqlServer,
                    ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>
                    {
                        ["connectionString"] = "Server=(localdb);Database=applicationdb;",
                    }),
                    IsAvailable: true,
                    DiscoveredAt: DateTimeOffset.UtcNow),
            };

            return Task.FromResult(new DiscoverResourcesResponse(resources));
        }
    }

    private sealed class StubDatabaseProvider : IDatabaseProvider,
        ISchemaDiscoveryProvider,
        ITableDiscoveryProvider,
        IViewDiscoveryProvider,
        IColumnDiscoveryProvider,
        IObjectDefinitionProvider
    {
        public string ProviderName => "stub-sql";

        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

        public ProviderCapabilities Capabilities => new()
        {
            SupportsSchemas = true,
            SupportsTables = true,
            SupportsViews = true,
            SupportsDefinitionRetrieval = true,
        };

        public bool CanHandle(DatabaseResource resource) => true;

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new QueryResult([], [], 0, TimeSpan.Zero));

        public Task<DiscoverSchemasResponse> DiscoverSchemasAsync(
            DatabaseResource resource,
            DiscoverSchemasRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverSchemasResponse(
                [new SchemaObject("schema.dbo", "dbo")]));

        public Task<DiscoverTablesResponse> DiscoverTablesAsync(
            DatabaseResource resource,
            DiscoverTablesRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTablesResponse(
                [new TableObject("table.products", "dbo", "Products")]));

        public Task<DiscoverViewsResponse> DiscoverViewsAsync(
            DatabaseResource resource,
            DiscoverViewsRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverViewsResponse([]));

        public Task<DiscoverColumnsResponse> DiscoverColumnsAsync(
            DatabaseResource resource,
            DiscoverColumnsRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverColumnsResponse(
                [
                    new OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata(
                        Name: "Id",
                        Ordinal: 1,
                        DataType: "int",
                        MaxLength: null,
                        Precision: 10,
                        Scale: 0,
                        IsNullable: false,
                        IsIdentity: true,
                        IsComputed: false,
                        DefaultValue: null,
                        Description: null,
                        ProviderMetadata: new Dictionary<string, object?>()),
                ]));

        public Task<ObjectDefinitionResponse> GetDefinitionAsync(
            DatabaseResource resource,
            ObjectDefinitionRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new ObjectDefinitionResponse(
                Definition: "CREATE TABLE dbo.Products (Id int NOT NULL PRIMARY KEY);",
                IsAvailable: true,
                UnavailableReason: null));
    }
}
