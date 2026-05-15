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

    [Theory]
    [InlineData("dbo", true)]
    [InlineData("guest", true)]
    [InlineData("INFORMATION_SCHEMA", true)]
    [InlineData("sys", true)]
    [InlineData("app", false)]
    public void IsSystemSchema_RecognizesExpectedSchemas(string schemaName, bool expected)
    {
        var result = SqlServerDatabaseProvider.IsSystemSchema(schemaName);

        result.Should().Be(expected);
    }

    [Fact]
    public void CreateSchemaObject_IncludesSchemaIdMetadata()
    {
        var schema = SqlServerDatabaseProvider.CreateSchemaObject(schemaId: 7, schemaName: "sales");

        schema.ObjectId.Should().Be("schema.sales");
        schema.ObjectName.Should().Be("sales");
        schema.ProviderMetadata.Should().ContainKey("schemaId");
        schema.ProviderMetadata["schemaId"].Should().Be(7);
    }

    [Fact]
    public void BuildDiscoverSchemasResponse_DefaultExcludesSystemSchemas_AndSortsAlphabetically()
    {
        var schemas = new[]
        {
            SqlServerDatabaseProvider.CreateSchemaObject(1, "dbo"),
            SqlServerDatabaseProvider.CreateSchemaObject(2, "sales"),
            SqlServerDatabaseProvider.CreateSchemaObject(3, "audit"),
        };

        var response = SqlServerDatabaseProvider.BuildDiscoverSchemasResponse(
            schemas,
            includeSystemSchemas: false);

        response.Schemas.Select(schema => schema.ObjectName)
            .Should()
            .Equal("audit", "sales");
    }

    [Fact]
    public void BuildDiscoverSchemasResponse_WhenIncludeSystemSchemasTrue_IncludesDboAndCustomSchemas()
    {
        var schemas = new[]
        {
            SqlServerDatabaseProvider.CreateSchemaObject(1, "dbo"),
            SqlServerDatabaseProvider.CreateSchemaObject(2, "sales"),
        };

        var response = SqlServerDatabaseProvider.BuildDiscoverSchemasResponse(
            schemas,
            includeSystemSchemas: true);

        response.Schemas.Select(schema => schema.ObjectName)
            .Should()
            .Equal("dbo", "sales");
    }

    [Fact]
    public void BuildDiscoverSchemasResponse_WhenNoSchemas_ReturnsEmptyList()
    {
        var response = SqlServerDatabaseProvider.BuildDiscoverSchemasResponse(
            Array.Empty<SchemaObject>(),
            includeSystemSchemas: false);

        response.Schemas.Should().BeEmpty();
    }

    private static DatabaseResource CreateResource(string providerName)
        => new("db", providerName, "Server=localhost;Database=db;", IsLocal: true, IsWritable: false);
}
