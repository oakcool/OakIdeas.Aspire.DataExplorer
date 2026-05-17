using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Web.Abstractions;
using OakIdeas.Aspire.DataExplorer.Web.Components.Layout;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class MainLayoutDatabasePickerTests : TestContext
{
    [Fact]
    public void DatabasePicker_ChangingSelection_UpdatesExplorerMetadata()
    {
        var service = new FakeExplorerService();
        Services.AddSingleton<IExplorerService>(service);

        var component = RenderComponent<MainLayout>();

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("applicationdb");
            component.Markup.Should().Contain("Users");
        });

        var picker = component.Find("#database-picker");
        picker.Change("sql-analytics");

        component.WaitForAssertion(() =>
        {
            component.Markup.Should().Contain("analyticsdb");
            component.Markup.Should().Contain("Events");
            service.CurrentResourceId.Should().Be("sql-analytics");
        });
    }

    private sealed class FakeExplorerService : IExplorerService
    {
        private readonly List<DiscoveredDatabaseResource> _resources =
        [
            CreateResource("sql-main", "applicationdb"),
            CreateResource("sql-analytics", "analyticsdb"),
        ];

        public string CurrentResourceId { get; private set; } = "sql-main";

        public Task<GetAvailableDatabasesResponse> GetAvailableDatabasesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new GetAvailableDatabasesResponse(_resources));
        }

        public Task<SelectDatabaseResponse> SelectDatabaseAsync(string resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resource = _resources.FirstOrDefault(candidate =>
                string.Equals(candidate.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase));

            if (resource is null)
            {
                return Task.FromResult(new SelectDatabaseResponse(
                    Succeeded: false,
                    Selection: null,
                    ValidationErrors: ["Resource was not found."]));
            }

            CurrentResourceId = resource.ResourceId;

            return Task.FromResult(new SelectDatabaseResponse(
                Succeeded: true,
                Selection: Map(resource),
                ValidationErrors: []));
        }

        public Task<GetSelectedDatabaseResponse> GetSelectedDatabaseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resource = _resources.First(candidate =>
                string.Equals(candidate.ResourceId, CurrentResourceId, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(new GetSelectedDatabaseResponse(Map(resource)));
        }

        public Task<GetDatabaseMetadataResponse> GetDatabaseMetadataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resource = _resources.First(candidate =>
                string.Equals(candidate.ResourceId, CurrentResourceId, StringComparison.OrdinalIgnoreCase));

            var objectName = string.Equals(resource.ResourceId, "sql-main", StringComparison.OrdinalIgnoreCase)
                ? "Users"
                : "Events";

            var metadata = new DatabaseMetadataRoot(
                databaseName: resource.DatabaseName,
                providerType: resource.ProviderType,
                resourceId: resource.ResourceId,
                metadataCollectionTime: DateTimeOffset.UtcNow,
                objects: new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>
                {
                    [DatabaseObjectType.Table] = new Dictionary<string, DatabaseObject>(StringComparer.OrdinalIgnoreCase)
                    {
                        [$"dbo.{objectName}"] = new TableObject(
                            objectId: $"dbo.{objectName}",
                            schemaName: "dbo",
                            objectName: objectName),
                    },
                });

            return Task.FromResult(new GetDatabaseMetadataResponse(
                Metadata: metadata,
                AggregatedMetadata: null,
                CollectionStatus: MetadataCollectionStatus.Success,
                FailureDetails: [],
                Errors: []));
        }

        public Task<RefreshMetadataResponse> RefreshDatabaseMetadataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = DateTimeOffset.UtcNow;

            return Task.FromResult(new RefreshMetadataResponse(
                Status: RefreshStatus.Completed,
                StartedAt: now,
                CompletedAt: now,
                Errors: [],
                IsPartialSuccess: false,
                Metadata: null));
        }

        public Task<GetObjectDefinitionResponse> GetObjectDefinitionAsync(
            string objectId,
            DatabaseObjectType objectType,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new GetObjectDefinitionResponse(
                ObjectId: objectId,
                ObjectType: objectType,
                Definition: null,
                IsAvailable: false,
                UnavailableReason: "Not used by this test.",
                Errors: []));
        }

        private static ExplorerDatabaseSelection Map(DiscoveredDatabaseResource resource)
            => new(
                ResourceId: resource.ResourceId,
                ResourceName: resource.ResourceName,
                DatabaseName: resource.DatabaseName,
                ProviderType: resource.ProviderType,
                IsAvailable: resource.IsAvailable,
                IsValid: true,
                ValidationMessage: null);

        private static DiscoveredDatabaseResource CreateResource(string resourceId, string databaseName)
            => new(
                ResourceId: resourceId,
                ResourceName: resourceId,
                DatabaseName: databaseName,
                ProviderType: DatabaseProviderType.SqlServer,
                ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>()),
                IsAvailable: true,
                DiscoveredAt: DateTimeOffset.UtcNow);
    }
}
