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
    public async Task ExecuteQueryAsync_WhenSelectQuery_ReturnsRowsAndMetrics()
    {
        var selected = CreateSelectedContext("sql-main", "applicationdb");
        var queryProvider = new QueryProvider(new QueryResult(
            Columns: ["Id", "Name"],
            Rows:
            [
                new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "First" },
                new Dictionary<string, object?> { ["Id"] = 2, ["Name"] = "Second" },
            ],
            RowCount: 2,
            Duration: TimeSpan.FromMilliseconds(25),
            AffectedRowCount: null,
            IsTruncated: false));
        var service = CreateService(
            selectedDatabaseService: new StubSelectedDatabaseService(selected),
            providerFactory: new StubProviderFactory(queryProvider));

        var response = await service.ExecuteQueryAsync(
            new ExecuteQueryRequest("ignored", "select * from dbo.Users", 100),
            CancellationToken.None);

        response.Errors.Should().BeEmpty();
        response.Columns.Should().ContainInOrder("Id", "Name");
        response.Rows.Should().HaveCount(2);
        response.Rows[0][0].Should().Be("1");
        response.Rows[0][1].Should().Be("First");
        response.RowCount.Should().Be(2);
        response.Duration.Should().Be(TimeSpan.FromMilliseconds(25));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenDestructiveWithoutConfirmation_ReturnsValidationError()
    {
        var service = CreateService();

        var response = await service.ExecuteQueryAsync(
            new ExecuteQueryRequest("ignored", "DELETE FROM dbo.Users", 100),
            CancellationToken.None);

        response.Rows.Should().BeEmpty();
        response.Errors.Should().ContainSingle().Which.Should().Contain("requires explicit confirmation");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenWriteOperationsDisabled_BlocksDestructiveQuery()
    {
        var service = CreateService(dataExplorerOptions: new DataExplorerOptions
        {
            EnableWriteOperations = false,
        });

        var response = await service.ExecuteQueryAsync(
            new ExecuteQueryRequest("ignored", "UPDATE dbo.Users SET Name = 'x'", 100, ConfirmDestructiveExecution: true),
            CancellationToken.None);

        response.Errors.Should().ContainSingle().Which.Should().Contain("disabled");
        response.Error.Should().NotBeNull();
        response.Error!.Category.Should().Be(ErrorCategory.PermissionDenied);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenProviderThrows_ReturnsSanitizedError()
    {
        var service = CreateService(
            providerFactory: new StubProviderFactory(new ThrowingQueryProvider(new TimeoutException("Server=secret"))));

        var response = await service.ExecuteQueryAsync(
            new ExecuteQueryRequest("ignored", "select 1", 100),
            CancellationToken.None);

        response.Error.Should().NotBeNull();
        response.Error!.Category.Should().Be(ErrorCategory.QueryTimeout);
        response.Errors.Should().ContainSingle().Which.Should().NotContain("Server=secret");
    }

    private static ExplorerService CreateService(
        IAspireResourceDiscovery? resourceDiscovery = null,
        ISelectedDatabaseService? selectedDatabaseService = null,
        IMetadataAggregationService? metadataAggregationService = null,
        IMetadataRefreshService? metadataRefreshService = null,
        IProviderFactory? providerFactory = null,
        DataExplorerOptions? dataExplorerOptions = null)
        => new(
            resourceDiscovery ?? new StubResourceDiscovery([]),
            selectedDatabaseService ?? new StubSelectedDatabaseService(CreateSelectedContext("sql-main", "applicationdb")),
            metadataAggregationService ?? new StubMetadataAggregationService(),
            metadataRefreshService ?? new StubMetadataRefreshService(),
            providerFactory ?? new StubProviderFactory(new DefinitionProvider("SELECT 1;")),
            new ErrorHandler(NullLogger<ErrorHandler>.Instance, []),
            Options.Create(dataExplorerOptions ?? new DataExplorerOptions()));

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

    private sealed class QueryProvider(QueryResult result) : IMetadataProvider
    {
        private readonly QueryResult _result = result;

        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

        public ProviderCapabilities Capabilities => new();

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => Task.FromResult(_result);
    }

    private sealed class ThrowingQueryProvider(Exception exception) : IMetadataProvider
    {
        private readonly Exception _exception = exception;

        public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

        public ProviderCapabilities Capabilities => new();

        public Task<IReadOnlyList<SchemaMetadata>> GetSchemasAsync(DatabaseResource resource, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SchemaMetadata>>([]);

        public Task<QueryResult> ExecuteQueryAsync(DatabaseResource resource, ExecuteQueryRequest request, CancellationToken cancellationToken)
            => throw _exception;
    }
}
