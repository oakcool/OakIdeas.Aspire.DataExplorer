using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Web.Services;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class ExplorerServiceTests
{
    [Fact]
    public async Task GetAvailableDatabasesAsync_ReturnsDiscoveredResources()
    {
        var resource = CreateResource("sql-main");
        var service = CreateService(resourceDiscovery: new StubResourceDiscovery([resource]));

        var response = await service.GetAvailableDatabasesAsync(CancellationToken.None);

        response.Resources.Should().ContainSingle();
        response.Resources[0].ResourceId.Should().Be("sql-main");
    }

    [Fact]
    public async Task SelectDatabaseAsync_WhenResourceIdMissing_ReturnsValidationError()
    {
        var service = CreateService();

        var response = await service.SelectDatabaseAsync(" ", CancellationToken.None);

        response.Succeeded.Should().BeFalse();
        response.ValidationErrors.Should().ContainSingle().Which.Should().Contain("required");
    }

    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenNoSelectedDatabase_ReturnsValidationError()
    {
        var service = CreateService(
            selectedDatabaseService: new StubSelectedDatabaseService(selectedContext: null));

        var response = await service.GetDatabaseMetadataAsync(CancellationToken.None);

        response.Metadata.Should().BeNull();
        response.Errors.Should().ContainSingle().Which.Should().Contain("Select an available database");
    }

    [Fact]
    public async Task RefreshDatabaseMetadataAsync_WhenNoSelectedDatabase_ReturnsFailedStatus()
    {
        var service = CreateService(
            selectedDatabaseService: new StubSelectedDatabaseService(selectedContext: null));

        var response = await service.RefreshDatabaseMetadataAsync(CancellationToken.None);

        response.Status.Should().Be(RefreshStatus.Failed);
        response.Errors.Should().ContainSingle().Which.Should().Contain("Select an available database");
    }

    [Fact]
    public async Task GetObjectDefinitionAsync_WhenProviderSupportsDefinitions_ReturnsDefinition()
    {
        var selected = CreateSelectedContext("sql-main", "applicationdb");
        var service = CreateService(
            selectedDatabaseService: new StubSelectedDatabaseService(selected),
            providerFactory: new StubProviderFactory(new DefinitionProvider("CREATE TABLE dbo.Products (Id int);")));

        var response = await service.GetObjectDefinitionAsync(
            "dbo.Products",
            DatabaseObjectType.Table,
            CancellationToken.None);

        response.IsAvailable.Should().BeTrue();
        response.Definition.Should().Contain("CREATE TABLE");
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObjectDefinitionAsync_WhenObjectIdMissing_ReturnsValidationError()
    {
        var service = CreateService();

        var response = await service.GetObjectDefinitionAsync(
            " ",
            DatabaseObjectType.Table,
            CancellationToken.None);

        response.IsAvailable.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Should().Contain("Object ID is required");
    }

    private static ExplorerService CreateService(
        IAspireResourceDiscovery? resourceDiscovery = null,
        ISelectedDatabaseService? selectedDatabaseService = null,
        IMetadataAggregationService? metadataAggregationService = null,
        IMetadataRefreshService? metadataRefreshService = null,
        IProviderFactory? providerFactory = null)
        => new(
            resourceDiscovery ?? new StubResourceDiscovery([]),
            selectedDatabaseService ?? new StubSelectedDatabaseService(CreateSelectedContext("sql-main", "applicationdb")),
            metadataAggregationService ?? new StubMetadataAggregationService(),
            metadataRefreshService ?? new StubMetadataRefreshService(),
            providerFactory ?? new StubProviderFactory(new DefinitionProvider("SELECT 1;")));

    private static DiscoveredDatabaseResource CreateResource(string resourceId)
        => new(
            ResourceId: resourceId,
            ResourceName: resourceId,
            DatabaseName: "applicationdb",
            ProviderType: DatabaseProviderType.SqlServer,
            ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>()),
            IsAvailable: true,
            DiscoveredAt: DateTimeOffset.UtcNow);

    private static SelectedDatabaseContext CreateSelectedContext(string resourceId, string databaseName)
        => new(
            Resource: new DiscoveredDatabaseResource(
                ResourceId: resourceId,
                ResourceName: resourceId,
                DatabaseName: databaseName,
                ProviderType: DatabaseProviderType.SqlServer,
                ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>()),
                IsAvailable: true,
                DiscoveredAt: DateTimeOffset.UtcNow),
            IsValid: true,
            ValidationMessage: null);

    private sealed class StubResourceDiscovery(IReadOnlyList<DiscoveredDatabaseResource> resources) : IAspireResourceDiscovery
    {
        private readonly IReadOnlyList<DiscoveredDatabaseResource> _resources = resources;

        public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
            DiscoverResourcesRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new DiscoverResourcesResponse(_resources));
        }
    }

    private sealed class StubSelectedDatabaseService(SelectedDatabaseContext? selectedContext) : ISelectedDatabaseService
    {
        private SelectedDatabaseContext? _selectedContext = selectedContext;

        public event EventHandler<SelectedDatabaseContext?>? SelectionChanged;

        public Task<OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return Task.FromResult(new OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse(false, _selectedContext, "Resource ID is required."));
            }

            return Task.FromResult(new OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse(true, _selectedContext, null));
        }

        public Task<SelectedDatabaseContext?> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_selectedContext);
        }

        public Task ClearSelectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _selectedContext = null;
            SelectionChanged?.Invoke(this, null);
            return Task.CompletedTask;
        }

        public Task<bool> IsSelectedAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_selectedContext is not null);
        }
    }

    private sealed class StubMetadataAggregationService : IMetadataAggregationService
    {
        public Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new DiscoverDatabaseMetadataResponse(
                new DatabaseMetadataRoot(
                    databaseName: selectedDbContext.Resource.DatabaseName,
                    providerType: selectedDbContext.Resource.ProviderType,
                    resourceId: selectedDbContext.Resource.ResourceId,
                    metadataCollectionTime: DateTimeOffset.UtcNow)));
        }
    }

    private sealed class StubMetadataRefreshService : IMetadataRefreshService
    {
        public Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new RefreshMetadataResponse(
                Status: RefreshStatus.Completed,
                StartedAt: now,
                CompletedAt: now,
                Errors: [],
                IsPartialSuccess: false,
                Metadata: new DatabaseMetadataRoot(
                    databaseName: selectedDbContext.Resource.DatabaseName,
                    providerType: selectedDbContext.Resource.ProviderType,
                    resourceId: selectedDbContext.Resource.ResourceId,
                    metadataCollectionTime: now)));
        }

        public Task<RefreshMetadataResponse?> GetRefreshStatusAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<RefreshMetadataResponse?>(null);
        }
    }

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

    private sealed class DefinitionProvider(string definition) : IMetadataProvider, IObjectDefinitionProvider
    {
        private readonly string _definition = definition;

        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

        public ProviderCapabilities Capabilities => new()
        {
            SupportsDefinitionRetrieval = true,
        };

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new QueryResult([], [], 0, TimeSpan.Zero));

        public Task<ObjectDefinitionResponse> GetDefinitionAsync(DatabaseResource resource, ObjectDefinitionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ObjectDefinitionResponse(
                Definition: _definition,
                IsAvailable: true,
                UnavailableReason: null));
    }
}
