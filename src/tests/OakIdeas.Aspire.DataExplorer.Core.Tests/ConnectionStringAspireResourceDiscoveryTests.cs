using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Configuration;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class ConnectionStringAspireResourceDiscoveryTests
{
    [Theory]
    [InlineData("Server=localhost;Database=app;User Id=sa;Password=P@ss1;")]
    [InlineData("Server=127.0.0.1;Database=app;")]
    [InlineData("Server=.;Database=app;")]
    [InlineData("Server=tcp:localhost,1433;Database=app;")]
    [InlineData("Server=localhost\\SQLEXPRESS;Database=app;")]
    [InlineData("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=app;")]
    [InlineData("Server=[::1];Database=app;")]
    [InlineData("Host=localhost;Database=app;Username=postgres;")]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnections_KeepsLocalServers(string connectionString)
    {
        var discovery = CreateDiscovery(requireLocalConnections: true, ("sql-app", connectionString));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Should().ContainSingle().Which.ResourceId.Should().Be("sql-app");
    }

    [Theory]
    [InlineData("Server=prod-sql.contoso.com;Database=app;User Id=sa;Password=P@ss1;")]
    [InlineData("Server=10.0.0.5,1433;Database=app;")]
    [InlineData("Server=tcp:myserver.database.windows.net,1433;Database=app;")]
    [InlineData("Host=db.internal.example;Database=app;Username=postgres;")]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnections_FiltersRemoteServers(string connectionString)
    {
        var discovery = CreateDiscovery(requireLocalConnections: true, ("remote-app", connectionString));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnectionsDisabled_KeepsRemoteServers()
    {
        var discovery = CreateDiscovery(
            requireLocalConnections: false,
            ("remote-app", "Server=prod-sql.contoso.com;Database=app;"));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Should().ContainSingle().Which.ResourceId.Should().Be("remote-app");
    }

    [Fact]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnections_TreatsMachineNameAsLocal()
    {
        var discovery = CreateDiscovery(
            requireLocalConnections: true,
            ("local-app", $"Server={Environment.MachineName};Database=app;"));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Should().ContainSingle().Which.ResourceId.Should().Be("local-app");
    }

    [Fact]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnections_KeepsOnlyLocalResourcesFromMixedSet()
    {
        var discovery = CreateDiscovery(
            requireLocalConnections: true,
            ("local-app", "Server=localhost;Database=localapp;"),
            ("remote-app", "Server=prod-sql.contoso.com;Database=remoteapp;"));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Select(r => r.ResourceId).Should().BeEquivalentTo("local-app");
    }

    [Theory]
    [InlineData("Server=sampledb,1433;Database=app;User Id=sa;******;")]
    [InlineData("Server=my-sql-container;Database=app;User Id=sa;******;")]
    [InlineData("Server=aspire-db,1433;Database=mydb;")]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnectionsDisabled_KeepsContainerHostnames(string connectionString)
    {
        var discovery = CreateDiscovery(requireLocalConnections: false, ("container-app", connectionString));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Should().ContainSingle().Which.ResourceId.Should().Be("container-app");
    }

    [Theory]
    [InlineData("Server=sampledb,1433;Database=app;User Id=sa;******;")]
    [InlineData("Server=my-sql-container;Database=app;User Id=sa;******;")]
    public async Task DiscoverResourcesAsync_WhenRequireLocalConnections_FiltersContainerHostnames(string connectionString)
    {
        var discovery = CreateDiscovery(requireLocalConnections: true, ("container-app", connectionString));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        response.Resources.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverResourcesAsync_StoresConnectionStringDirectlyInMetadata()
    {
        const string connectionString = "Server=localhost;Database=app;User Id=sa;******;";
        var discovery = CreateDiscovery(requireLocalConnections: false, ("local-app", connectionString));

        var response = await discovery.DiscoverResourcesAsync(new DiscoverResourcesRequest(), CancellationToken.None);

        var resource = response.Resources.Should().ContainSingle().Subject;
        resource.ConnectionMetadata.Properties.Should().ContainKey("connectionString")
            .WhoseValue.Should().Be(connectionString);
    }

    private static ConnectionStringAspireResourceDiscovery CreateDiscovery(
        bool requireLocalConnections,
        params (string Key, string Value)[] connectionStrings)
        => new(
            BuildConfiguration(connectionStrings),
            new StubHostEnvironment(),
            Options.Create(new DataExplorerOptions { RequireLocalConnections = requireLocalConnections }),
            new DiscoveredDatabaseResourceProjector(),
            new ErrorHandler(NullLogger<ErrorHandler>.Instance, []),
            NullLogger<ConnectionStringAspireResourceDiscovery>.Instance);

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] connectionStrings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(connectionStrings.Select(cs =>
                new KeyValuePair<string, string?>($"ConnectionStrings:{cs.Key}", cs.Value)))
            .Build();

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "OakIdeas.Aspire.DataExplorer.Tests";
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
