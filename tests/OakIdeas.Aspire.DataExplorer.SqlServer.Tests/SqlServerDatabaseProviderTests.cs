using FluentAssertions;
using Microsoft.Data.SqlClient;
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
    public void CreateDiscoverSchemasCommand_UsesSchemaCatalogQueryAndParameter()
    {
        using var connection = new SqlConnection();

        using var command = SqlServerDatabaseProvider.CreateDiscoverSchemasCommand(
            connection,
            includeSystemSchemas: false);

        command.CommandText.Should().Contain("FROM sys.schemas");
        command.CommandText.Should().Contain("ORDER BY name");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .ContainSingle(parameter => parameter.ParameterName == "@IncludeSystemSchemas");
        command.Parameters["@IncludeSystemSchemas"].Value.Should().Be(false);
    }

    [Fact]
    public void CreateDiscoverSchemasCommand_WhenIncludingSystemSchemas_SetsParameterToTrue()
    {
        using var connection = new SqlConnection();

        using var command = SqlServerDatabaseProvider.CreateDiscoverSchemasCommand(
            connection,
            includeSystemSchemas: true);

        command.Parameters["@IncludeSystemSchemas"].Value.Should().Be(true);
    }

    [Fact]
    public void CreateDiscoverForeignKeysCommand_UsesForeignKeyCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverForeignKeysRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverForeignKeysCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.foreign_keys AS fk");
        command.CommandText.Should().Contain("JOIN sys.foreign_key_columns AS fkc");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(parameter => parameter.ParameterName == "@ParentSchemaName")
            .And.Contain(parameter => parameter.ParameterName == "@ParentTableName");
        command.Parameters["@ParentSchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@ParentTableName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverForeignKeysCommand_WhenTableFilterProvided_TrimsAndSetsParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverForeignKeysRequest(ParentSchemaName: " sales ", ParentTableName: " orders ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverForeignKeysCommand(connection, request);

        command.Parameters["@ParentSchemaName"].Value.Should().Be("sales");
        command.Parameters["@ParentTableName"].Value.Should().Be("orders");
    }

    [Theory]
    [InlineData(0, ReferentialActionBehavior.NoAction)]
    [InlineData(1, ReferentialActionBehavior.Cascade)]
    [InlineData(2, ReferentialActionBehavior.SetNull)]
    [InlineData(3, ReferentialActionBehavior.SetDefault)]
    [InlineData(4, ReferentialActionBehavior.NoAction)]
    [InlineData(99, ReferentialActionBehavior.NoAction)]
    public void MapReferentialAction_MapsExpectedCodes(int actionCode, ReferentialActionBehavior expected)
    {
        SqlServerDatabaseProvider.MapReferentialAction(actionCode).Should().Be(expected);
    }

    [Fact]
    public void NormalizeForeignKeyConstraints_PreservesCompositeColumnOrderAndMetadata()
    {
        SqlServerDatabaseProvider.ForeignKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 101,
                ConstraintName: "FK_OrderLines_Orders",
                ParentSchemaName: "sales",
                ParentTableName: "OrderLines",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "Orders",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "Id",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 1,
                UpdateReferentialAction: 0,
                IsDisabled: false),
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 202,
                ConstraintName: "FK_OrderLineAudit_OrderLines",
                ParentSchemaName: "audit",
                ParentTableName: "OrderLineAudit",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "OrderLines",
                ParentColumnName: "LineNo",
                ReferencedColumnName: "LineNo",
                ConstraintColumnId: 2,
                DeleteReferentialAction: 3,
                UpdateReferentialAction: 2,
                IsDisabled: true),
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 202,
                ConstraintName: "FK_OrderLineAudit_OrderLines",
                ParentSchemaName: "audit",
                ParentTableName: "OrderLineAudit",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "OrderLines",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "OrderId",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 3,
                UpdateReferentialAction: 2,
                IsDisabled: true),
        ];

        var constraints = SqlServerDatabaseProvider.NormalizeForeignKeyConstraints(rows);

        constraints.Should().HaveCount(2);
        constraints[0].ConstraintName.Should().Be("FK_OrderLines_Orders");
        constraints[0].ParentTableName.Should().Be("sales.OrderLines");
        constraints[0].ReferencedTableName.Should().Be("sales.Orders");
        constraints[0].KeyColumns.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ForeignKeyColumnMapping("OrderId", "Id"));
        constraints[0].OnDeleteBehavior.Should().Be(ReferentialActionBehavior.Cascade);
        constraints[0].OnUpdateBehavior.Should().Be(ReferentialActionBehavior.NoAction);
        constraints[0].IsDisabled.Should().BeFalse();

        constraints[1].ConstraintName.Should().Be("FK_OrderLineAudit_OrderLines");
        constraints[1].ObjectId.Should().Be("202");
        constraints[1].KeyColumns.Should().HaveCount(2);
        constraints[1].KeyColumns[0].Should().BeEquivalentTo(new ForeignKeyColumnMapping("OrderId", "OrderId"));
        constraints[1].KeyColumns[1].Should().BeEquivalentTo(new ForeignKeyColumnMapping("LineNo", "LineNo"));
        constraints[1].OnDeleteBehavior.Should().Be(ReferentialActionBehavior.SetDefault);
        constraints[1].OnUpdateBehavior.Should().Be(ReferentialActionBehavior.SetNull);
        constraints[1].IsDisabled.Should().BeTrue();
    }

    private static DatabaseResource CreateResource(string providerName)
        => new("db", providerName, "Server=localhost;Database=db;", IsLocal: true, IsWritable: false);
}
