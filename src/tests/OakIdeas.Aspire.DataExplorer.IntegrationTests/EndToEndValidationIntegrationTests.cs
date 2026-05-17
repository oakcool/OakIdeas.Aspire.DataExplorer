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
using ContractColumnMetadata = OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class EndToEndValidationIntegrationTests
{
    [Fact]
    public async Task MetadataDiscoveryWorkflow_LoadsAllMetadataObjectTypes()
    {
        await using var scope = CreateScope();
        var explorerService = scope.ServiceProvider.GetRequiredService<IExplorerService>();

        var available = await explorerService.GetAvailableDatabasesAsync(CancellationToken.None);
        var select = await explorerService.SelectDatabaseAsync("sql-main", CancellationToken.None);
        var metadata = await explorerService.GetDatabaseMetadataAsync(CancellationToken.None);

        available.Resources.Should().ContainSingle(resource => resource.ResourceId == "sql-main");
        select.Succeeded.Should().BeTrue();
        metadata.CollectionStatus.Should().Be(MetadataCollectionStatus.Success);
        metadata.AggregatedMetadata.Should().NotBeNull();
        metadata.Metadata.Should().NotBeNull();

        metadata.AggregatedMetadata!.Schemas.Should().ContainSingle(schema => schema.ObjectName == "sales");
        metadata.AggregatedMetadata.Tables.Should().ContainSingle(table => table.FullyQualifiedName == "sales.Orders");
        metadata.AggregatedMetadata.Views.Should().ContainSingle(view => view.FullyQualifiedName == "sales.vw_OrderSummary");
        metadata.AggregatedMetadata.ProceduresBySchema["sales"].Should().ContainSingle(procedure => procedure.ProcedureName == "usp_GetOrders");
        metadata.AggregatedMetadata.FunctionsBySchema["sales"][FunctionType.Scalar].Should().ContainSingle(function => function.FunctionName == "ufn_OrderCount");
        metadata.AggregatedMetadata.Triggers.Should().ContainSingle(trigger => trigger.TriggerName == "trg_Orders_Audit");
        metadata.AggregatedMetadata.PrimaryKeysByTable["sales.Orders"].Should().ContainSingle(key => key.ConstraintName == "PK_Orders");
        metadata.AggregatedMetadata.ForeignKeysByTable["sales.Orders"].Should().ContainSingle(key => key.ConstraintName == "FK_Orders_Customers");
        metadata.AggregatedMetadata.IndexesByTable["sales.Orders"].Should().ContainSingle(index => index.IndexName == "IX_Orders_Customer_Created");
        metadata.AggregatedMetadata.Constraints.Should().Contain(constraint => constraint.ConstraintName == "CK_Orders_Total");
        metadata.AggregatedMetadata.ColumnsByObject["sales.Orders"].Should().HaveCount(3);

        metadata.Metadata!.Objects.Should().ContainKeys(
            DatabaseObjectType.Schema,
            DatabaseObjectType.Table,
            DatabaseObjectType.View,
            DatabaseObjectType.Procedure,
            DatabaseObjectType.Function,
            DatabaseObjectType.Trigger);
    }

    [Fact]
    public async Task RefreshWorkflow_RefreshInvalidatesCacheAndLoadsUpdatedMetadata()
    {
        await using var scope = CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<MutableMetadataProvider>();
        var explorerService = scope.ServiceProvider.GetRequiredService<IExplorerService>();

        await explorerService.SelectDatabaseAsync("sql-main", CancellationToken.None);

        var initial = await explorerService.GetDatabaseMetadataAsync(CancellationToken.None);
        provider.MetadataVersion = 2;
        var cached = await explorerService.GetDatabaseMetadataAsync(CancellationToken.None);
        var refreshed = await explorerService.RefreshDatabaseMetadataAsync(CancellationToken.None);
        var afterRefresh = await explorerService.GetDatabaseMetadataAsync(CancellationToken.None);

        initial.Metadata!.Objects[DatabaseObjectType.Table].Should().ContainKey("sales.Orders");
        cached.Metadata!.Objects[DatabaseObjectType.Table].Should().ContainKey("sales.Orders");
        refreshed.Status.Should().Be(RefreshStatus.Completed);
        refreshed.Metadata!.Objects[DatabaseObjectType.Table].Should().ContainKey("sales.OrderHistory");
        afterRefresh.Metadata!.Objects[DatabaseObjectType.Table].Should().ContainKey("sales.OrderHistory");
    }

    [Fact]
    public async Task DefinitionWorkflow_DefinitionRetrievalReturnsSqlDefinition()
    {
        await using var scope = CreateScope();
        var explorerService = scope.ServiceProvider.GetRequiredService<IExplorerService>();

        await explorerService.SelectDatabaseAsync("sql-main", CancellationToken.None);
        var definition = await explorerService.GetObjectDefinitionAsync("table.orders", DatabaseObjectType.Table, CancellationToken.None);

        definition.IsAvailable.Should().BeTrue();
        definition.Definition.Should().Contain("CREATE TABLE [sales].[Orders]");
    }

    [Fact]
    public async Task ErrorRecoveryWorkflow_RecoversFromDiscoveryFailureAfterRefresh()
    {
        await using var scope = CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<MutableMetadataProvider>();
        var explorerService = scope.ServiceProvider.GetRequiredService<IExplorerService>();

        await explorerService.SelectDatabaseAsync("sql-main", CancellationToken.None);
        provider.FailSchemaDiscovery = true;
        var failed = await explorerService.GetDatabaseMetadataAsync(CancellationToken.None);

        provider.FailSchemaDiscovery = false;
        var recovered = await explorerService.RefreshDatabaseMetadataAsync(CancellationToken.None);

        failed.CollectionStatus.Should().Be(MetadataCollectionStatus.Failed);
        failed.Errors.Should().NotBeEmpty();
        recovered.Status.Should().Be(RefreshStatus.Completed);
        recovered.Metadata!.Objects[DatabaseObjectType.Table].Should().ContainKey("sales.Orders");
    }

    private static AsyncServiceScope CreateScope()
    {
        var services = new ServiceCollection();
        services.AddSingleton<StubResourceDiscovery>();
        services.AddSingleton<IAspireResourceDiscovery>(provider => provider.GetRequiredService<StubResourceDiscovery>());
        services.AddSingleton<MutableMetadataProvider>();
        services.AddSingleton<IProviderFactory, MetadataProviderFactory>();
        services.AddOptions<MetadataProviderFactoryOptions>()
            .Configure(options => options.Register(DatabaseProviderType.SqlServer, typeof(MutableMetadataProvider)));
        services.AddSelectedDatabaseService();
        services.AddMetadataRefreshService();
        services.AddScoped<IExplorerService, ExplorerService>();
        return services.BuildServiceProvider().CreateAsyncScope();
    }

    private sealed class StubResourceDiscovery : IAspireResourceDiscovery
    {
        public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(DiscoverResourcesRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverResourcesResponse(
            [
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
            ]));
    }

    private sealed class MutableMetadataProvider : IDatabaseProvider,
        ISchemaDiscoveryProvider,
        ITableDiscoveryProvider,
        IViewDiscoveryProvider,
        IColumnDiscoveryProvider,
        IPrimaryKeyDiscoveryProvider,
        IForeignKeyDiscoveryProvider,
        IIndexDiscoveryProvider,
        IConstraintDiscoveryProvider,
        IStoredProcedureDiscoveryProvider,
        IFunctionDiscoveryProvider,
        ITriggerDiscoveryProvider,
        IObjectDefinitionProvider
    {
        public string ProviderName => "stub-sql";
        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;
        public int MetadataVersion { get; set; } = 1;
        public bool FailSchemaDiscovery { get; set; }
        public ProviderCapabilities Capabilities => new()
        {
            SupportsSchemas = true,
            SupportsTables = true,
            SupportsViews = true,
            SupportsKeys = true,
            SupportsIndexes = true,
            SupportsConstraints = true,
            SupportsStoredProcedures = true,
            SupportsFunctions = true,
            SupportsTriggers = true,
            SupportsDefinitionRetrieval = true,
        };

        public bool CanHandle(DatabaseResource resource) => true;

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new QueryResult([], [], 0, TimeSpan.Zero));

        public Task<DiscoverSchemasResponse> DiscoverSchemasAsync(DatabaseResource resource, DiscoverSchemasRequest request, CancellationToken cancellationToken)
            => FailSchemaDiscovery
                ? throw new InvalidOperationException("Schema discovery failed.")
                : Task.FromResult(new DiscoverSchemasResponse([new SchemaObject("schema.sales", "sales")]));

        public Task<DiscoverTablesResponse> DiscoverTablesAsync(DatabaseResource resource, DiscoverTablesRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTablesResponse(
                MetadataVersion == 1
                    ? [new TableObject("table.orders", "sales", "Orders")]
                    : [new TableObject("table.orderhistory", "sales", "OrderHistory")]));

        public Task<DiscoverViewsResponse> DiscoverViewsAsync(DatabaseResource resource, DiscoverViewsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverViewsResponse([new ViewObject("view.ordersummary", "sales", "vw_OrderSummary", true)]));

        public Task<DiscoverColumnsResponse> DiscoverColumnsAsync(DatabaseResource resource, DiscoverColumnsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverColumnsResponse(
            [
                new ContractColumnMetadata("OrderId", 1, "int", null, 10, 0, false, true, false, null, null, new Dictionary<string, object?>()),
                new ContractColumnMetadata("CustomerId", 2, "int", null, 10, 0, false, false, false, null, null, new Dictionary<string, object?>()),
                new ContractColumnMetadata("TotalAmount", 3, "decimal", null, 18, 2, false, false, false, "(0)", null, new Dictionary<string, object?>()),
            ]));

        public Task<DiscoverPrimaryKeysResponse> DiscoverPrimaryKeysAsync(DatabaseResource resource, DiscoverPrimaryKeysRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverPrimaryKeysResponse(
            [
                new PrimaryKeyConstraint("PK_Orders", "Orders", "sales", ["OrderId"], true, "pk.orders"),
            ]));

        public Task<DiscoverForeignKeysResponse> DiscoverForeignKeysAsync(DatabaseResource resource, DiscoverForeignKeysRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverForeignKeysResponse(
            [
                new ForeignKeyConstraint(
                    "FK_Orders_Customers",
                    "Orders",
                    "sales",
                    "Customers",
                    "sales",
                    [new ForeignKeyColumnMapping("CustomerId", "CustomerId")],
                    ReferentialActionBehavior.NoAction,
                    ReferentialActionBehavior.NoAction,
                    false,
                    "fk.orders.customers"),
            ]));

        public Task<DiscoverIndexesResponse> DiscoverIndexesAsync(DatabaseResource resource, DiscoverIndexesRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverIndexesResponse(
            [
                new IndexMetadata("IX_Orders_Customer_Created", "Orders", "sales", false, false, false, ["CustomerId"], ["TotalAmount"], null, "idx.orders.customer"),
            ]));

        public Task<DiscoverConstraintsResponse> DiscoverConstraintsAsync(DatabaseResource resource, DiscoverConstraintsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverConstraintsResponse(
            [
                new ConstraintMetadata("CK_Orders_Total", ConstraintType.Check, "Orders", "sales", "TotalAmount", "[TotalAmount] >= (0)", false, "ck.orders.total"),
            ]));

        public Task<DiscoverStoredProceduresResponse> DiscoverStoredProceduresAsync(DatabaseResource resource, DiscoverStoredProceduresRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverStoredProceduresResponse(
                new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sales"] =
                    [
                        new StoredProcedureMetadata("sales", "usp_GetOrders", "proc.getorders", true, [new StoredProcedureParameterMetadata("@CustomerId", "int")], DateTimeOffset.UtcNow),
                    ],
                }));

        public Task<DiscoverFunctionsResponse> DiscoverFunctionsAsync(DatabaseResource resource, DiscoverFunctionsRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverFunctionsResponse(
                new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sales"] = new Dictionary<FunctionType, IReadOnlyList<FunctionMetadata>>
                    {
                        [FunctionType.Scalar] =
                        [
                            new FunctionMetadata("sales", "ufn_OrderCount", FunctionType.Scalar, "fn.ordercount", "int", true, DateTimeOffset.UtcNow),
                        ],
                    },
                }));

        public Task<DiscoverTriggersResponse> DiscoverTriggersAsync(DatabaseResource resource, DiscoverTriggersRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTriggersResponse(
            [
                new TriggerMetadata("trg_Orders_Audit", "sales", "Orders", TriggerParentObjectType.Table, TriggerType.Insert | TriggerType.After, true, true, "trg.orders.audit", DateTimeOffset.UtcNow),
            ]));

        public Task<ObjectDefinitionResponse> GetDefinitionAsync(DatabaseResource resource, ObjectDefinitionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ObjectDefinitionResponse("CREATE TABLE [sales].[Orders] ([OrderId] int NOT NULL PRIMARY KEY);", true));
    }
}
