using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using ContractColumnMetadata = OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class MetadataAggregationIntegrationTests
{
    [Fact]
    public async Task GetDatabaseMetadataAsync_WithSampleDatabaseShape_ReturnsCompleteMetadata()
    {
        var provider = new SampleAggregationProvider();
        await using var scope = CreateScope(provider);
        var service = scope.ServiceProvider.GetRequiredService<IMetadataAggregationService>();

        var response = await service.GetDatabaseMetadataAsync(CreateSelectedDatabaseContext(), CancellationToken.None);

        response.CollectionStatus.Should().Be(MetadataCollectionStatus.Success);
        response.AggregatedMetadata.Should().NotBeNull();
        response.AggregatedMetadata!.Schemas.Should().ContainSingle(schema => schema.ObjectName == "dbo");
        response.AggregatedMetadata.Tables.Should().ContainSingle(table => table.FullyQualifiedName == "dbo.Products");
        response.AggregatedMetadata.Views.Should().ContainSingle(view => view.FullyQualifiedName == "dbo.ActiveProducts");
        response.AggregatedMetadata.ProceduresBySchema.Should().ContainKey("dbo");
        response.AggregatedMetadata.FunctionsBySchema.Should().ContainKey("dbo");
        response.AggregatedMetadata.Triggers.Should().ContainSingle(trigger => trigger.TriggerName == "trg_Products_Audit");
    }

    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenSingleDiscoveryFails_ReturnsPartialSuccess()
    {
        var provider = new SampleAggregationProvider
        {
            FailConstraintDiscovery = true,
        };
        await using var scope = CreateScope(provider);
        var service = scope.ServiceProvider.GetRequiredService<IMetadataAggregationService>();

        var response = await service.GetDatabaseMetadataAsync(CreateSelectedDatabaseContext(), CancellationToken.None);

        response.CollectionStatus.Should().Be(MetadataCollectionStatus.PartialSuccess);
        response.FailureDetails.Should().Contain(failure => failure.Operation == "constraints");
    }

    private static AsyncServiceScope CreateScope(SampleAggregationProvider provider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMetadataCache, InMemoryMetadataCache>();
        services.AddSingleton<IMetadataAggregationService, MetadataAggregationService>();
        services.AddSingleton<IProviderFactory>(new StubProviderFactory(provider));
        services.AddSingleton<IProviderErrorMapper, NoOpProviderErrorMapper>();
        services.AddSingleton<IErrorHandler>(_ => new ErrorHandler(NullLogger<ErrorHandler>.Instance, []));
        services.AddOptions<MetadataAggregationOptions>()
            .Configure(options =>
            {
                options.CacheTtlMinutes = 5;
                options.AggregationTimeoutSeconds = 30;
            });

        return services.BuildServiceProvider().CreateAsyncScope();
    }

    private static SelectedDatabaseContext CreateSelectedDatabaseContext()
        => new(
            Resource: new DiscoveredDatabaseResource(
                ResourceId: "sql-main",
                ResourceName: "sql-main",
                DatabaseName: "applicationdb",
                ProviderType: DatabaseProviderType.SqlServer,
                ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>
                {
                    ["connectionString"] = "Server=localhost;Database=applicationdb;Trusted_Connection=True;",
                }),
                IsAvailable: true,
                DiscoveredAt: DateTimeOffset.UtcNow),
            IsValid: true,
            ValidationMessage: null);

    private sealed class StubProviderFactory(IMetadataProvider provider) : IProviderFactory
    {
        private readonly IMetadataProvider _provider = provider;

        public IMetadataProvider Create(DatabaseProviderType providerType) => _provider;

        public bool TryCreate(DatabaseProviderType providerType, out IMetadataProvider? provider)
        {
            provider = _provider;
            return true;
        }
    }

    private sealed class NoOpProviderErrorMapper : IProviderErrorMapper
    {
        public DatabaseProviderType ProviderType => DatabaseProviderType.Unknown;

        public bool TryMap(Exception exception, ErrorContext context, out DataExplorerError error)
        {
            error = null!;
            return false;
        }
    }

    private sealed class SampleAggregationProvider : IMetadataProvider, ISchemaDiscoveryProvider, ITableDiscoveryProvider,
        IViewDiscoveryProvider, IColumnDiscoveryProvider, IPrimaryKeyDiscoveryProvider, IForeignKeyDiscoveryProvider,
        IIndexDiscoveryProvider, IConstraintDiscoveryProvider, IStoredProcedureDiscoveryProvider, IFunctionDiscoveryProvider,
        ITriggerDiscoveryProvider
    {
        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;
        public ProviderCapabilities Capabilities { get; } = new();
        public bool FailConstraintDiscovery { get; init; }

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new QueryResult([], [], 0, TimeSpan.Zero));

        public Task<DiscoverSchemasResponse> DiscoverSchemasAsync(DatabaseResource resource, DiscoverSchemasRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverSchemasResponse([new SchemaObject("1", "dbo")]));

        public Task<DiscoverTablesResponse> DiscoverTablesAsync(DatabaseResource resource, DiscoverTablesRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTablesResponse([new TableObject("101", "dbo", "Products")]));

        public Task<DiscoverViewsResponse> DiscoverViewsAsync(DatabaseResource resource, DiscoverViewsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverViewsResponse([new ViewObject("201", "dbo", "ActiveProducts", hasDefinitionAvailable: true)]));

        public Task<DiscoverColumnsResponse> DiscoverColumnsAsync(DatabaseResource resource, DiscoverColumnsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverColumnsResponse(
            [
                new ContractColumnMetadata("Id", 1, "int", null, null, null, false, true, false, null, null, new Dictionary<string, object?>()),
            ]));

        public Task<DiscoverPrimaryKeysResponse> DiscoverPrimaryKeysAsync(DatabaseResource resource, DiscoverPrimaryKeysRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverPrimaryKeysResponse(
            [
                new PrimaryKeyConstraint("PK_Products", "Products", "dbo", ["Id"], true, "301"),
            ]));

        public Task<DiscoverForeignKeysResponse> DiscoverForeignKeysAsync(DatabaseResource resource, DiscoverForeignKeysRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverForeignKeysResponse([]));

        public Task<DiscoverIndexesResponse> DiscoverIndexesAsync(DatabaseResource resource, DiscoverIndexesRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverIndexesResponse(
            [
                new IndexMetadata("IX_Products_Id", "Products", "dbo", false, true, false, ["Id"], [], null, "401"),
            ]));

        public Task<DiscoverConstraintsResponse> DiscoverConstraintsAsync(DatabaseResource resource, DiscoverConstraintsRequest request, CancellationToken cancellationToken)
            => FailConstraintDiscovery
                ? throw new InvalidOperationException("Constraint discovery failed.")
                : Task.FromResult(new DiscoverConstraintsResponse(
                [
                    new ConstraintMetadata("CK_Products_Id", ConstraintType.Check, "Products", "dbo", "Id", "[Id] > 0", false, "501"),
                ]));

        public Task<DiscoverStoredProceduresResponse> DiscoverStoredProceduresAsync(DatabaseResource resource, DiscoverStoredProceduresRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverStoredProceduresResponse(
                new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbo"] =
                    [
                        new StoredProcedureMetadata("dbo", "usp_GetProducts", "601", true, null, DateTimeOffset.UtcNow),
                    ],
                }));

        public Task<DiscoverFunctionsResponse> DiscoverFunctionsAsync(DatabaseResource resource, DiscoverFunctionsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverFunctionsResponse(
                new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbo"] = new Dictionary<FunctionType, IReadOnlyList<FunctionMetadata>>
                    {
                        [FunctionType.Scalar] =
                        [
                            new FunctionMetadata("dbo", "fn_TotalProducts", FunctionType.Scalar, "701", "int", true, DateTimeOffset.UtcNow),
                        ],
                    },
                }));

        public Task<DiscoverTriggersResponse> DiscoverTriggersAsync(DatabaseResource resource, DiscoverTriggersRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTriggersResponse(
            [
                new TriggerMetadata("trg_Products_Audit", "dbo", "Products", TriggerParentObjectType.Table, TriggerType.Insert, true, true, "801", DateTimeOffset.UtcNow),
            ]));
    }
}
