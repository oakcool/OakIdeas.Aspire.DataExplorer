using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;
using OakIdeas.Aspire.DataExplorer.Core.Extensions;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class AspireResourceDiscoveryRegistrationTests
{
    [Fact]
    public async Task AddAspireResourceDiscovery_WhenConnectionStringExists_ReturnsDiscoveredResource()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:sql"] = "Server=localhost;Database=applicationdb;User Id=sa;Password=Pass@word1;",
            })
            .Build();

        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddLogging();
        services.AddSelectedDatabaseService();
        services.AddAspireResourceDiscovery(configuration);

        await using var scope = services.BuildServiceProvider().CreateAsyncScope();
        var discovery = scope.ServiceProvider.GetRequiredService<IAspireResourceDiscovery>();

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(IncludeUnavailableResources: true), CancellationToken.None);

        response.Resources.Should().ContainSingle();
        var resource = response.Resources[0];
        resource.ResourceId.Should().Be("sql");
        resource.DatabaseName.Should().Be("applicationdb");
    }

    [Fact]
    public async Task AddAspireResourceDiscovery_WhenMultipleConnectionStringsExist_ReturnsAllDiscoveredResources()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:sampledb"] = "Server=localhost;Database=sampledb;User Id=sa;Password=placeholder;",
                ["ConnectionStrings:warehousedb"] = "Server=localhost;Database=warehousedb;User Id=sa;Password=placeholder;",
            })
            .Build();

        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddLogging();
        services.AddSelectedDatabaseService();
        services.AddAspireResourceDiscovery(configuration);

        await using var scope = services.BuildServiceProvider().CreateAsyncScope();
        var discovery = scope.ServiceProvider.GetRequiredService<IAspireResourceDiscovery>();

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(IncludeUnavailableResources: true), CancellationToken.None);

        response.Resources.Select(resource => resource.ResourceId)
            .Should()
            .BeEquivalentTo(["sampledb", "warehousedb"]);
        response.Resources.Select(resource => resource.DatabaseName)
            .Should()
            .BeEquivalentTo(["sampledb", "warehousedb"]);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "OakIdeas.Aspire.DataExplorer.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
