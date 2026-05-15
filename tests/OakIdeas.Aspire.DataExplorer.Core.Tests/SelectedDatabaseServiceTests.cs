using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class SelectedDatabaseServiceTests
{
    [Fact]
    public async Task SelectDatabaseAsync_WhenResourceExistsAndAvailable_SelectsDatabase()
    {
        var service = CreateService(
            [
                CreateResource("sql-main", isAvailable: true),
            ]);

        var response = await service.SelectDatabaseAsync("sql-main", CancellationToken.None);
        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        response.Succeeded.Should().BeTrue();
        response.ErrorMessage.Should().BeNull();
        selected.Should().NotBeNull();
        selected!.Resource.ResourceId.Should().Be("sql-main");
        selected.IsValid.Should().BeTrue();
        selected.ValidationMessage.Should().BeNull();
        (await service.IsSelectedAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task SelectDatabaseAsync_WhenResourceDoesNotExist_ReturnsClearErrorAndKeepsSelectionUnchanged()
    {
        var service = CreateService(
            [
                CreateResource("sql-main", isAvailable: true),
            ]);
        await service.SelectDatabaseAsync("sql-main", CancellationToken.None);

        var response = await service.SelectDatabaseAsync("missing-resource", CancellationToken.None);
        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        response.Succeeded.Should().BeFalse();
        response.ErrorMessage.Should().Contain("missing-resource").And.Contain("not found");
        selected.Should().NotBeNull();
        selected!.Resource.ResourceId.Should().Be("sql-main");
    }

    [Fact]
    public async Task SelectDatabaseAsync_WhenResourceUnavailable_ReturnsClearError()
    {
        var service = CreateService(
            [
                CreateResource("sql-offline", isAvailable: false),
            ]);

        var response = await service.SelectDatabaseAsync("sql-offline", CancellationToken.None);
        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        response.Succeeded.Should().BeFalse();
        response.ErrorMessage.Should().Contain("sql-offline").And.Contain("unavailable");
        selected.Should().BeNull();
        (await service.IsSelectedAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task SelectDatabaseAsync_WhenSelectingDifferentResource_SwitchesSelection()
    {
        var service = CreateService(
            [
                CreateResource("sql-main", isAvailable: true),
                CreateResource("sql-analytics", isAvailable: true),
            ]);

        await service.SelectDatabaseAsync("sql-main", CancellationToken.None);
        var response = await service.SelectDatabaseAsync("sql-analytics", CancellationToken.None);
        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        response.Succeeded.Should().BeTrue();
        selected.Should().NotBeNull();
        selected!.Resource.ResourceId.Should().Be("sql-analytics");
    }

    [Fact]
    public async Task ClearSelectionAsync_WhenSelectionExists_ClearsSelection()
    {
        var service = CreateService(
            [
                CreateResource("sql-main", isAvailable: true),
            ]);
        await service.SelectDatabaseAsync("sql-main", CancellationToken.None);

        await service.ClearSelectionAsync(CancellationToken.None);
        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        selected.Should().BeNull();
        (await service.IsSelectedAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task GetSelectedDatabaseAsync_WhenNoSelection_ReturnsNull()
    {
        var service = CreateService([]);

        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        selected.Should().BeNull();
    }

    private static ISelectedDatabaseService CreateService(IReadOnlyList<DiscoveredDatabaseResource> resources)
        => new SelectedDatabaseService(new StubAspireResourceDiscovery(resources));

    private static DiscoveredDatabaseResource CreateResource(
        string resourceId,
        bool isAvailable,
        DatabaseProviderType providerType = DatabaseProviderType.SqlServer)
        => new(
            resourceId,
            resourceId,
            $"{resourceId}-db",
            providerType,
            new ConnectionMetadata(new Dictionary<string, string?>()),
            isAvailable,
            DateTimeOffset.UtcNow);

    private sealed class StubAspireResourceDiscovery(IReadOnlyList<DiscoveredDatabaseResource> resources)
        : IAspireResourceDiscovery
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
}
