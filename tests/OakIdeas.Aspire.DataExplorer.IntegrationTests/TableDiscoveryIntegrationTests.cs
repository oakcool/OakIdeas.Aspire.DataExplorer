using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class TableDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeTables_SingleUserTable_IsProjected()
    {
        SqlServerDatabaseProvider.TableDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 101,
                SchemaName: "dbo",
                TableName: "Products",
                RowCount: 250L),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTables(rows);

        result.Should().ContainSingle();
        result[0].SchemaName.Should().Be("dbo");
        result[0].ObjectName.Should().Be("Products");
        result[0].FullyQualifiedName.Should().Be("dbo.Products");
        result[0].ObjectType.Should().Be(DatabaseObjectType.Table);
        result[0].ProviderMetadata["objectId"].Should().Be(101);
        result[0].ProviderMetadata["rowCount"].Should().Be(250L);
    }

    [Fact]
    public void NormalizeTables_MultipleTablesAcrossSchemas_AllProjected()
    {
        SqlServerDatabaseProvider.TableDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 201,
                SchemaName: "dbo",
                TableName: "Customers",
                RowCount: 5000L),
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 202,
                SchemaName: "sales",
                TableName: "Orders",
                RowCount: 12000L),
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 203,
                SchemaName: "sales",
                TableName: "OrderLines",
                RowCount: 48000L),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTables(rows);

        result.Should().HaveCount(3);
        result.Select(t => t.SchemaName).Should().Equal("dbo", "sales", "sales");
        result.Select(t => t.ObjectName).Should().Equal("Customers", "Orders", "OrderLines");
        result.Select(t => t.ProviderMetadata["rowCount"]).Should().Equal(5000L, 12000L, 48000L);
    }

    [Fact]
    public void NormalizeTables_TableWithZeroRows_IsProjected()
    {
        SqlServerDatabaseProvider.TableDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 301,
                SchemaName: "staging",
                TableName: "ImportQueue",
                RowCount: 0L),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTables(rows);

        result.Should().ContainSingle();
        result[0].ObjectName.Should().Be("ImportQueue");
        result[0].ProviderMetadata["rowCount"].Should().Be(0L);
    }

    [Fact]
    public void NormalizeTables_SystemTableIncluded_IsProjectedWithCorrectMetadata()
    {
        SqlServerDatabaseProvider.TableDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 401,
                SchemaName: "sys",
                TableName: "sysrowsets",
                RowCount: 100L),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTables(rows);

        result.Should().ContainSingle();
        result[0].SchemaName.Should().Be("sys");
        result[0].ObjectName.Should().Be("sysrowsets");
        result[0].ObjectType.Should().Be(DatabaseObjectType.Table);
        result[0].ProviderMetadata["objectId"].Should().Be(401);
    }

    [Fact]
    public void NormalizeTables_EmptyRows_ReturnsEmptyList()
    {
        SqlServerDatabaseProvider.TableDiscoveryRow[] rows = [];

        var result = SqlServerDatabaseProvider.NormalizeTables(rows);

        result.Should().BeEmpty();
    }
}
