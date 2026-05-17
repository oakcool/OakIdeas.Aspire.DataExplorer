using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class ViewDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeViews_SingleUserView_IsProjected()
    {
        SqlServerDatabaseProvider.ViewDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 101,
                SchemaName: "sales",
                ViewName: "ActiveOrders",
                HasDefinition: true),
        ];

        var result = SqlServerDatabaseProvider.NormalizeViews(rows);

        result.Should().ContainSingle();
        result[0].SchemaName.Should().Be("sales");
        result[0].ObjectName.Should().Be("ActiveOrders");
        result[0].FullyQualifiedName.Should().Be("sales.ActiveOrders");
        result[0].ObjectType.Should().Be(DatabaseObjectType.View);
        result[0].HasDefinitionAvailable.Should().BeTrue();
        result[0].ProviderMetadata["objectId"].Should().Be(101);
    }

    [Fact]
    public void NormalizeViews_MultipleViewsAcrossSchemas_AllProjected()
    {
        SqlServerDatabaseProvider.ViewDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 201,
                SchemaName: "sales",
                ViewName: "OrderSummary",
                HasDefinition: true),
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 202,
                SchemaName: "analytics",
                ViewName: "MonthlyRevenue",
                HasDefinition: true),
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 203,
                SchemaName: "analytics",
                ViewName: "YearlySummary",
                HasDefinition: false),
        ];

        var result = SqlServerDatabaseProvider.NormalizeViews(rows);

        result.Should().HaveCount(3);
        result.Select(view => view.SchemaName).Should().Equal("sales", "analytics", "analytics");
        result.Select(view => view.ObjectName).Should().Equal("OrderSummary", "MonthlyRevenue", "YearlySummary");
    }

    [Fact]
    public void NormalizeViews_WhenDefinitionUnavailable_SetsHasDefinitionAvailableToFalse()
    {
        SqlServerDatabaseProvider.ViewDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 301,
                SchemaName: "dbo",
                ViewName: "RestrictedView",
                HasDefinition: false),
        ];

        var result = SqlServerDatabaseProvider.NormalizeViews(rows);

        result.Should().ContainSingle();
        result[0].HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void NormalizeViews_SystemViewIncluded_IsProjectedWithCorrectMetadata()
    {
        SqlServerDatabaseProvider.ViewDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 401,
                SchemaName: "sys",
                ViewName: "objects",
                HasDefinition: false),
        ];

        var result = SqlServerDatabaseProvider.NormalizeViews(rows);

        result.Should().ContainSingle();
        result[0].SchemaName.Should().Be("sys");
        result[0].ObjectName.Should().Be("objects");
        result[0].HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void NormalizeViews_EmptyRows_ReturnsEmptyList()
    {
        SqlServerDatabaseProvider.ViewDiscoveryRow[] rows = [];

        var result = SqlServerDatabaseProvider.NormalizeViews(rows);

        result.Should().BeEmpty();
    }
}
