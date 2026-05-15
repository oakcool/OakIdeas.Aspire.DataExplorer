using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerDatabaseProviderTests
{
    [Fact]
    public void ProviderMetadata_UsesSqlServerTypeAndCapabilities()
    {
        var sut = new SqlServerDatabaseProvider();

        sut.ProviderType.Should().Be(DatabaseProviderType.SqlServer);
        sut.Capabilities.Should().BeEquivalentTo(new
        {
            SupportsSchemas = true,
            SupportsTables = true,
            SupportsViews = true,
            SupportsStoredProcedures = true,
            SupportsFunctions = true,
            SupportsTriggers = true,
            SupportsIndexes = true,
            SupportsConstraints = true,
            SupportsKeys = true,
            SupportsDefinitionRetrieval = true,
            SupportsLiveStats = false,
        });
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("mssql")]
    [InlineData("Microsoft.Data.SqlClient")]
    public void CanHandle_ForSqlServerProviders_ReturnsTrue(string providerName)
    {
        var sut = new SqlServerDatabaseProvider();
        var resource = CreateResource(providerName);

        sut.CanHandle(resource).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ForNonSqlServerProvider_ReturnsFalse()
    {
        var sut = new SqlServerDatabaseProvider();
        var resource = CreateResource("postgresql");

        sut.CanHandle(resource).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteQueryAsync_ReturnsEmptyResult()
    {
        var sut = new SqlServerDatabaseProvider();
        var resource = CreateResource("sqlserver");
        var request = new ExecuteQueryRequest("db", "select 1", 10);

        QueryResult result = await sut.ExecuteQueryAsync(resource, request, CancellationToken.None);

        result.Columns.Should().BeEmpty();
        result.Rows.Should().BeEmpty();
        result.RowCount.Should().Be(0);
    }

    private static DatabaseResource CreateResource(string providerName)
        => new("db", providerName, "Server=localhost;Database=db;", IsLocal: true, IsWritable: false);
}
