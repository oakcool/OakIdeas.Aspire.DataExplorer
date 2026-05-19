using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;
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
    public async Task GetAvailableDatabasesAsync_WhenDiscoveryFails_ReturnsSanitizedError()
    {
        var service = CreateService(resourceDiscovery: new ThrowingResourceDiscovery(new InvalidOperationException("Server=secret;Database=app")));

        var response = await service.GetAvailableDatabasesAsync(CancellationToken.None);

        response.Resources.Should().BeEmpty();
        response.Error.Should().NotBeNull();
        response.Error!.Message.Should().NotContain("Server=secret");
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
    public async Task SelectDatabaseAsync_WhenResourceExists_ReturnsSelectedDatabase()
    {
        var resources = new[]
        {
            CreateResource("sql-main", "applicationdb"),
            CreateResource("sql-analytics", "analyticsdb"),
        };

        var selectedService = new StubSelectedDatabaseService(
            selectedContext: null,
            availableResources: resources);

        var service = CreateService(selectedDatabaseService: selectedService);

        var response = await service.SelectDatabaseAsync("sql-analytics", CancellationToken.None);

        response.Succeeded.Should().BeTrue();
        response.Selection.Should().NotBeNull();
        response.Selection!.ResourceId.Should().Be("sql-analytics");
        response.Selection.DatabaseName.Should().Be("analyticsdb");
    }

    [Fact]
    public async Task GetDatabaseMetadataAsync_WhenDatabaseChanges_ReturnsMetadataForSelectedDatabase()
    {
        var resources = new[]
        {
            CreateResource("sql-main", "applicationdb"),
            CreateResource("sql-analytics", "analyticsdb"),
        };

        var selectedService = new StubSelectedDatabaseService(
            selectedContext: CreateSelectedContext("sql-main", "applicationdb"),
            availableResources: resources);

        var service = CreateService(selectedDatabaseService: selectedService);

        var firstResponse = await service.GetDatabaseMetadataAsync(CancellationToken.None);
        await service.SelectDatabaseAsync("sql-analytics", CancellationToken.None);
        var secondResponse = await service.GetDatabaseMetadataAsync(CancellationToken.None);

        firstResponse.Metadata.Should().NotBeNull();
        firstResponse.Metadata!.DatabaseName.Should().Be("applicationdb");
        secondResponse.Metadata.Should().NotBeNull();
        secondResponse.Metadata!.DatabaseName.Should().Be("analyticsdb");
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
    public async Task GetDatabaseMetadataAsync_WhenAggregationThrows_ReturnsMappedError()
    {
        var service = CreateService(
            metadataAggregationService: new ThrowingMetadataAggregationService(new TimeoutException("Server=secret")));

        var response = await service.GetDatabaseMetadataAsync(CancellationToken.None);

        response.CollectionStatus.Should().Be(MetadataCollectionStatus.Failed);
        response.Error.Should().NotBeNull();
        response.Error!.Category.Should().Be(ErrorCategory.QueryTimeout);
        response.Errors.Should().ContainSingle().Which.Should().NotContain("Server=secret");
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

    [Fact]
    public async Task ExecuteQueryAsync_WhenNoSelectedDatabase_ReturnsValidationError()
    {
        var service = CreateService(selectedDatabaseService: new StubSelectedDatabaseService(selectedContext: null));

        var response = await service.ExecuteQueryAsync("SELECT 1", CancellationToken.None);

        response.Error.Should().NotBeNull();
        response.Error!.Category.Should().Be(ErrorCategory.ResourceNotFound);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ReturnsProviderResult()
    {
        var provider = new DefinitionProvider(
            "SELECT 1",
            new QueryResult(
                Columns: ["id"],
                Rows: [new Dictionary<string, object?> { ["id"] = 1 }],
                RowCount: 1,
                Duration: TimeSpan.FromMilliseconds(8),
                AffectedRowCount: null,
                IsTruncated: false));

        var service = CreateService(providerFactory: new StubProviderFactory(provider));

        var response = await service.ExecuteQueryAsync("SELECT id FROM dbo.Users", CancellationToken.None);

        response.Error.Should().BeNull();
        response.Columns.Should().ContainSingle().Which.Should().Be("id");
        response.Rows.Should().ContainSingle();
        response.RowCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenProviderThrows_ReturnsSanitizedError()
    {
        var provider = new DefinitionProvider("SELECT 1", null, new InvalidOperationException("Server=secret;Database=app"));
        var service = CreateService(providerFactory: new StubProviderFactory(provider));

        var response = await service.ExecuteQueryAsync("SELECT 1", CancellationToken.None);

        response.Error.Should().NotBeNull();
        response.Error!.Message.Should().NotContain("Server=secret");
    }

    private static ExplorerService CreateService(
        IAspireResourceDiscovery? resourceDiscovery = null,
        ISelectedDatabaseService? selectedDatabaseService = null,
        IMetadataAggregationService? metadataAggregationService = null,
        IMetadataRefreshService? metadataRefreshService = null,
        IProviderFactory? providerFactory = null,
        DataExplorerOptions? options = null)
        => new(
            resourceDiscovery ?? new StubResourceDiscovery([]),
            selectedDatabaseService ?? new StubSelectedDatabaseService(CreateSelectedContext("sql-main", "applicationdb")),
            metadataAggregationService ?? new StubMetadataAggregationService(),
            metadataRefreshService ?? new StubMetadataRefreshService(),
            providerFactory ?? new StubProviderFactory(new DefinitionProvider("SELECT 1;")),
            new ErrorHandler(NullLogger<ErrorHandler>.Instance, []),
            Options.Create(options ?? new DataExplorerOptions()));

    private static DiscoveredDatabaseResource CreateResource(string resourceId, string databaseName = "applicationdb")
        => new(
            ResourceId: resourceId,
            ResourceName: resourceId,
            DatabaseName: databaseName,
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

    private sealed class ThrowingResourceDiscovery(Exception exception) : IAspireResourceDiscovery
    {
        private readonly Exception _exception = exception;

        public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
            DiscoverResourcesRequest request,
            CancellationToken cancellationToken)
            => throw _exception;
    }

    private sealed class StubSelectedDatabaseService(
        SelectedDatabaseContext? selectedContext,
        IReadOnlyList<DiscoveredDatabaseResource>? availableResources = null) : ISelectedDatabaseService
    {
        private SelectedDatabaseContext? _selectedContext = selectedContext;
        private readonly IReadOnlyDictionary<string, DiscoveredDatabaseResource> _availableResources =
            (availableResources ?? [])
            .ToDictionary(resource => resource.ResourceId, StringComparer.OrdinalIgnoreCase);

        public event EventHandler<SelectedDatabaseContext?>? SelectionChanged;

        public Task<OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return Task.FromResult(new OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse(false, _selectedContext, "Resource ID is required."));
            }

            if (_availableResources.TryGetValue(resourceId.Trim(), out var resource))
            {
                _selectedContext = new SelectedDatabaseContext(resource, IsValid: true, ValidationMessage: null);
                SelectionChanged?.Invoke(this, _selectedContext);
                return Task.FromResult(new OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse(true, _selectedContext, null));
            }

            return Task.FromResult(new OakIdeas.Aspire.DataExplorer.Core.Models.SelectDatabaseResponse(false, _selectedContext, "Resource was not found."));
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

    private sealed class ThrowingMetadataAggregationService(Exception exception) : IMetadataAggregationService
    {
        private readonly Exception _exception = exception;

        public Task<DiscoverDatabaseMetadataResponse> GetDatabaseMetadataAsync(
            SelectedDatabaseContext selectedDbContext,
            CancellationToken cancellationToken)
            => throw _exception;
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

    private sealed class DefinitionProvider(
        string definition,
        QueryResult? queryResult = null,
        Exception? queryException = null) : IMetadataProvider, IObjectDefinitionProvider
    {
        private readonly string _definition = definition;
        private readonly QueryResult _queryResult = queryResult ?? new QueryResult([], [], 0, TimeSpan.Zero);
        private readonly Exception? _queryException = queryException;

        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

        public ProviderCapabilities Capabilities => new()
        {
            SupportsDefinitionRetrieval = true,
        };

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => _queryException is null
                ? Task.FromResult(_queryResult)
                : Task.FromException<QueryResult>(_queryException);

        public Task<ObjectDefinitionResponse> GetDefinitionAsync(DatabaseResource resource, ObjectDefinitionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ObjectDefinitionResponse(
                Definition: _definition,
                IsAvailable: true,
                UnavailableReason: null));
    }
}
