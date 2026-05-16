using FluentAssertions;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;
using ContractColumnMetadata = OakIdeas.Aspire.DataExplorer.Contracts.Models.ColumnMetadata;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class MetadataAggregationServiceTests
{
    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenAllDiscoveriesSucceed_ReturnsAggregatedMetadata()
    {
        var provider = new FakeMetadataProvider();
        var service = CreateService(provider);
        var context = CreateSelectedDatabaseContext();

        var response = await service.GetDatabaseMetadataAsync(context, CancellationToken.None);

        response.CollectionStatus.Should().Be(MetadataCollectionStatus.Success);
        response.AggregatedMetadata.Should().NotBeNull();
        response.AggregatedMetadata!.Schemas.Should().HaveCount(1);
        response.AggregatedMetadata.Tables.Should().HaveCount(1);
        response.AggregatedMetadata.Views.Should().HaveCount(1);
        response.AggregatedMetadata.ColumnsByObject.Should().ContainKey("dbo.Products");
        response.AggregatedMetadata.PrimaryKeysByTable.Should().ContainKey("dbo.Products");
        response.AggregatedMetadata.IndexesByTable.Should().ContainKey("dbo.Products");
        response.AggregatedMetadata.CollectionStatus.Should().Be(MetadataCollectionStatus.Success);
        response.FailureDetails.Should().BeEmpty();
        provider.CallSequence[0].Should().Be("schemas");
        provider.CallSequence.Should().Contain("tables");
        provider.CallSequence.Should().Contain("views");
    }

    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenCalledTwice_UsesCacheOnSecondCall()
    {
        var provider = new FakeMetadataProvider();
        var service = CreateService(provider);
        var context = CreateSelectedDatabaseContext();

        _ = await service.GetDatabaseMetadataAsync(context, CancellationToken.None);
        _ = await service.GetDatabaseMetadataAsync(context, CancellationToken.None);

        provider.SchemaCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenColumnsFailForTable_ReturnsPartialSuccess()
    {
        var provider = new FakeMetadataProvider
        {
            FailColumnsForObjectId = "101",
        };
        var service = CreateService(provider);
        var context = CreateSelectedDatabaseContext();

        var response = await service.GetDatabaseMetadataAsync(context, CancellationToken.None);

        response.CollectionStatus.Should().Be(MetadataCollectionStatus.PartialSuccess);
        response.AggregatedMetadata.Should().NotBeNull();
        response.FailureDetails.Should().Contain(failure =>
            failure.Operation == "columns"
            && failure.Target == "dbo.Products");
    }

    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenAggregationTimesOut_ReturnsFailedStatus()
    {
        var provider = new FakeMetadataProvider
        {
            DelayOnTableDiscovery = TimeSpan.FromSeconds(2),
        };
        var options = Options.Create(new MetadataAggregationOptions
        {
            AggregationTimeoutSeconds = 1,
        });
        var service = CreateService(provider, options);
        var context = CreateSelectedDatabaseContext();

        var response = await service.GetDatabaseMetadataAsync(context, CancellationToken.None);

        response.CollectionStatus.Should().Be(MetadataCollectionStatus.Failed);
        response.FailureDetails.Should().ContainSingle();
        response.FailureDetails![0].Message.Should().Contain("timed out");
    }

    private static MetadataAggregationService CreateService(
        FakeMetadataProvider provider,
        IOptions<MetadataAggregationOptions>? options = null)
    {
        var cache = new InMemoryMetadataCache(Options.Create(new MetadataAggregationOptions
        {
            CacheTtlMinutes = 5,
        }));

        return new MetadataAggregationService(
            new StubProviderFactory(provider),
            cache,
            options ?? Options.Create(new MetadataAggregationOptions()));
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

    private sealed class FakeMetadataProvider : IMetadataProvider, ISchemaDiscoveryProvider, ITableDiscoveryProvider,
        IViewDiscoveryProvider, IColumnDiscoveryProvider, IPrimaryKeyDiscoveryProvider, IForeignKeyDiscoveryProvider,
        IIndexDiscoveryProvider, IConstraintDiscoveryProvider, IStoredProcedureDiscoveryProvider, IFunctionDiscoveryProvider,
        ITriggerDiscoveryProvider, IObjectDefinitionProvider
    {
        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;
        public ProviderCapabilities Capabilities { get; } = new();
        public List<string> CallSequence { get; } = [];
        public int SchemaCalls { get; private set; }
        public string? FailColumnsForObjectId { get; init; }
        public TimeSpan? DelayOnTableDiscovery { get; init; }

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new QueryResult([], [], 0, TimeSpan.Zero));

        public Task<DiscoverSchemasResponse> DiscoverSchemasAsync(
            DatabaseResource resource,
            DiscoverSchemasRequest request,
            CancellationToken cancellationToken)
        {
            SchemaCalls++;
            CallSequence.Add("schemas");
            return Task.FromResult(new DiscoverSchemasResponse(
            [
                new SchemaObject("1", "dbo"),
            ]));
        }

        public async Task<DiscoverTablesResponse> DiscoverTablesAsync(
            DatabaseResource resource,
            DiscoverTablesRequest request,
            CancellationToken cancellationToken)
        {
            CallSequence.Add("tables");
            if (DelayOnTableDiscovery is not null)
            {
                await Task.Delay(DelayOnTableDiscovery.Value, cancellationToken);
            }

            return new DiscoverTablesResponse(
            [
                new TableObject("101", "dbo", "Products"),
            ]);
        }

        public Task<DiscoverViewsResponse> DiscoverViewsAsync(
            DatabaseResource resource,
            DiscoverViewsRequest request,
            CancellationToken cancellationToken)
        {
            CallSequence.Add("views");
            return Task.FromResult(new DiscoverViewsResponse(
            [
                new ViewObject("201", "dbo", "ActiveProducts", hasDefinitionAvailable: true),
            ]));
        }

        public Task<DiscoverColumnsResponse> DiscoverColumnsAsync(
            DatabaseResource resource,
            DiscoverColumnsRequest request,
            CancellationToken cancellationToken)
        {
            CallSequence.Add("columns");

            if (FailColumnsForObjectId is not null && request.ObjectId == FailColumnsForObjectId)
            {
                throw new InvalidOperationException("Column discovery failed.");
            }

            return Task.FromResult(new DiscoverColumnsResponse(
            [
                new ContractColumnMetadata(
                    Name: "Id",
                    Ordinal: 1,
                    DataType: "int",
                    MaxLength: null,
                    Precision: null,
                    Scale: null,
                    IsNullable: false,
                    IsIdentity: true,
                    IsComputed: false,
                    DefaultValue: null,
                    Description: null,
                    ProviderMetadata: new Dictionary<string, object?>()),
            ]));
        }

        public Task<DiscoverPrimaryKeysResponse> DiscoverPrimaryKeysAsync(
            DatabaseResource resource,
            DiscoverPrimaryKeysRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverPrimaryKeysResponse(
            [
                new PrimaryKeyConstraint(
                    ConstraintName: "PK_Products",
                    TableName: request.TableName ?? "Products",
                    SchemaName: request.SchemaName ?? "dbo",
                    KeyColumns: ["Id"],
                    IsClustered: true,
                    ObjectId: "301"),
            ]));

        public Task<DiscoverForeignKeysResponse> DiscoverForeignKeysAsync(
            DatabaseResource resource,
            DiscoverForeignKeysRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverForeignKeysResponse([]));

        public Task<DiscoverIndexesResponse> DiscoverIndexesAsync(
            DatabaseResource resource,
            DiscoverIndexesRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverIndexesResponse(
            [
                new IndexMetadata(
                    IndexName: "IX_Products_Id",
                    TableName: request.TableName ?? "Products",
                    SchemaName: request.SchemaName ?? "dbo",
                    IsPrimaryKey: false,
                    IsUnique: true,
                    IsClustered: false,
                    Columns: ["Id"],
                    IncludedColumns: [],
                    FilterDefinition: null,
                    ObjectId: "401"),
            ]));

        public Task<DiscoverConstraintsResponse> DiscoverConstraintsAsync(
            DatabaseResource resource,
            DiscoverConstraintsRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverConstraintsResponse(
            [
                new ConstraintMetadata(
                    ConstraintName: "CK_Products_Id",
                    ConstraintType: ConstraintType.Check,
                    TableName: request.TableName ?? "Products",
                    SchemaName: request.SchemaName ?? "dbo",
                    ColumnName: "Id",
                    Definition: "[Id] > 0",
                    IsDisabled: false,
                    ObjectId: "501"),
            ]));

        public Task<DiscoverStoredProceduresResponse> DiscoverStoredProceduresAsync(
            DatabaseResource resource,
            DiscoverStoredProceduresRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverStoredProceduresResponse(
                new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbo"] =
                    [
                        new StoredProcedureMetadata(
                            SchemaName: "dbo",
                            ProcedureName: "usp_GetProducts",
                            ObjectId: "601",
                            HasDefinitionAvailable: true,
                            Parameters: null,
                            CreatedAt: DateTimeOffset.UtcNow),
                    ],
                }));

        public Task<DiscoverFunctionsResponse> DiscoverFunctionsAsync(
            DatabaseResource resource,
            DiscoverFunctionsRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverFunctionsResponse(
                new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbo"] = new Dictionary<FunctionType, IReadOnlyList<FunctionMetadata>>
                    {
                        [FunctionType.Scalar] =
                        [
                            new FunctionMetadata(
                                SchemaName: "dbo",
                                FunctionName: "fn_TotalProducts",
                                FunctionType: FunctionType.Scalar,
                                ObjectId: "701",
                                ReturnType: "int",
                                HasDefinitionAvailable: true,
                                CreatedAt: DateTimeOffset.UtcNow),
                        ],
                    },
                }));

        public Task<DiscoverTriggersResponse> DiscoverTriggersAsync(
            DatabaseResource resource,
            DiscoverTriggersRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new DiscoverTriggersResponse(
            [
                new TriggerMetadata(
                    TriggerName: "trg_Products_Audit",
                    SchemaName: "dbo",
                    ParentObjectName: "Products",
                    ParentObjectType: TriggerParentObjectType.Table,
                    TriggerType: TriggerType.Insert | TriggerType.After,
                    IsEnabled: true,
                    HasDefinitionAvailable: true,
                    ObjectId: "801",
                    CreatedAt: DateTimeOffset.UtcNow),
            ]));

        public Task<ObjectDefinitionResponse> GetDefinitionAsync(
            DatabaseResource resource,
            ObjectDefinitionRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult(new ObjectDefinitionResponse("SELECT 1", true));
    }
}
