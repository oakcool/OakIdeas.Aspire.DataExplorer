using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class SelectedDatabaseServiceIntegrationTests
{
    [Fact]
    public async Task AddSelectedDatabaseService_ResolvesWithoutAspireDiscoveryRegistration()
    {
        var services = new ServiceCollection();
        services.AddSelectedDatabaseService();

        await using var serviceProvider = services.BuildServiceProvider().CreateAsyncScope();
        var service = serviceProvider.ServiceProvider.GetRequiredService<ISelectedDatabaseService>();

        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        selected.Should().BeNull();
    }

    [Fact]
    public async Task SelectDatabaseAsync_UsesAspireResourceDiscoveryFlow()
    {
        var services = new ServiceCollection();
        services.AddSelectedDatabaseService();
        services.AddScoped<IAspireResourceDiscovery, StubAspireResourceDiscovery>();

        await using var serviceProvider = services.BuildServiceProvider().CreateAsyncScope();
        var service = serviceProvider.ServiceProvider.GetRequiredService<ISelectedDatabaseService>();

        var response = await service.SelectDatabaseAsync("sql-main", CancellationToken.None);
        var selected = await service.GetSelectedDatabaseAsync(CancellationToken.None);

        response.Succeeded.Should().BeTrue();
        selected.Should().NotBeNull();
        selected!.Resource.ResourceId.Should().Be("sql-main");
        selected.Resource.DatabaseName.Should().Be("applicationdb");
    }

    private sealed class StubAspireResourceDiscovery : IAspireResourceDiscovery
    {
        public Task<DiscoverResourcesResponse> DiscoverResourcesAsync(
            DiscoverResourcesRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resources = new[]
            {
                new DiscoveredDatabaseResource(
                    ResourceId: "sql-main",
                    ResourceName: "sql-main",
                    DatabaseName: "applicationdb",
                    ProviderType: DatabaseProviderType.SqlServer,
                    ConnectionMetadata: new ConnectionMetadata(new Dictionary<string, string?>()),
                    IsAvailable: true,
                    DiscoveredAt: DateTimeOffset.UtcNow),
            };

            return Task.FromResult(new DiscoverResourcesResponse(resources));
        }
    }
}
