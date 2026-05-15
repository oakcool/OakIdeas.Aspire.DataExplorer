using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class DiscoveredDatabaseResourceProjectorTests
{
    private static readonly DateTimeOffset DiscoveredAt = new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Project_WhenSingleResourceProvided_ReturnsMappedResource()
    {
        var projector = new DiscoveredDatabaseResourceProjector();

        var response = projector.Project(
            [
                new DiscoveredDatabaseResourceDescriptor(
                    "sql-app",
                    "sql-app",
                    "applicationdb",
                    "sqlserver",
                    new Dictionary<string, string?> { ["serverResourceName"] = "sql" },
                    true),
            ],
            DiscoveredAt,
            includeUnavailableResources: true);

        response.Resources.Should().ContainSingle();
        response.Resources[0].Should().BeEquivalentTo(new DiscoveredDatabaseResource(
            "sql-app",
            "sql-app",
            "applicationdb",
            DatabaseProviderType.SqlServer,
            new ConnectionMetadata(new Dictionary<string, string?> { ["serverResourceName"] = "sql" }),
            true,
            DiscoveredAt));
    }

    [Fact]
    public void Project_WhenMixedProvidersProvided_SortsByProviderThenResourceName()
    {
        var projector = new DiscoveredDatabaseResourceProjector();

        var response = projector.Project(
            [
                new DiscoveredDatabaseResourceDescriptor("pg-b", "pg-b", "inventory", "postgres", null, true),
                new DiscoveredDatabaseResourceDescriptor("sql-c", "sql-c", "sales", "sqlserver", null, true),
                new DiscoveredDatabaseResourceDescriptor("mysql-a", "mysql-a", "catalog", "mysql", null, true),
                new DiscoveredDatabaseResourceDescriptor("sql-a", "sql-a", "orders", "sqlserver", null, true),
            ],
            DiscoveredAt,
            includeUnavailableResources: true);

        response.Resources.Select(resource => resource.ResourceName).Should().Equal(
            "sql-a",
            "sql-c",
            "pg-b",
            "mysql-a");
    }

    [Fact]
    public void Project_WhenNoResourcesProvided_ReturnsEmptyResponse()
    {
        var projector = new DiscoveredDatabaseResourceProjector();

        var response = projector.Project([], DiscoveredAt, includeUnavailableResources: true);

        response.Resources.Should().BeEmpty();
    }

    [Fact]
    public void Project_WhenMetadataIsInvalid_HandlesItGracefully()
    {
        var projector = new DiscoveredDatabaseResourceProjector();

        var response = projector.Project(
            [
                new DiscoveredDatabaseResourceDescriptor(null, "valid-resource", null, "unknown-provider", null, true),
                new DiscoveredDatabaseResourceDescriptor("ignored", "   ", "db", "sqlserver", null, true),
                new DiscoveredDatabaseResourceDescriptor(null, "   ", null, "sqlserver", null, true),
            ],
            DiscoveredAt,
            includeUnavailableResources: true);

        response.Resources.Should().HaveCount(2);
        response.Resources[0].ResourceId.Should().Be("valid-resource");
        response.Resources[0].DatabaseName.Should().Be("valid-resource");
        response.Resources[0].ProviderType.Should().Be(DatabaseProviderType.Unknown);
        response.Resources[1].ResourceId.Should().Be("ignored");
        response.Resources[1].ResourceName.Should().Be("ignored");
        response.Resources[1].DatabaseName.Should().Be("db");
        response.Resources[1].ProviderType.Should().Be(DatabaseProviderType.SqlServer);
    }
}
