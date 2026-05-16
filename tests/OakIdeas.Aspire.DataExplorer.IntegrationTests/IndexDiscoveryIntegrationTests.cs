using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class IndexDiscoveryProjectionTests
{
    [Fact]
    public void NormalizeIndexes_HeapTableWithoutRows_ReturnsNoIndexes()
    {
        SqlServerDatabaseProvider.IndexDiscoveryRow[] rows = [];

        var result = SqlServerDatabaseProvider.NormalizeIndexes(rows);

        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeIndexes_ClusteredPrimaryKey_IsProjected()
    {
        SqlServerDatabaseProvider.IndexDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 410,
                IndexId: 1,
                IndexName: "PK_Customers",
                SchemaName: "sales",
                TableName: "Customers",
                IsPrimaryKey: true,
                IsUnique: true,
                IsClustered: true,
                ColumnName: "CustomerId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
        ];

        var result = SqlServerDatabaseProvider.NormalizeIndexes(rows);

        result.Should().ContainSingle();
        result[0].IndexName.Should().Be("PK_Customers");
        result[0].IsPrimaryKey.Should().BeTrue();
        result[0].IsUnique.Should().BeTrue();
        result[0].IsClustered.Should().BeTrue();
        result[0].Columns.Should().Equal("CustomerId");
    }

    [Fact]
    public void NormalizeIndexes_NonClusteredIndexes_UniqueCompositeIncludedAndFiltered_AreProjected()
    {
        SqlServerDatabaseProvider.IndexDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 510,
                IndexId: 2,
                IndexName: "UX_Orders_OrderNumber",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: true,
                IsClustered: false,
                ColumnName: "OrderNumber",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 510,
                IndexId: 3,
                IndexName: "IX_Orders_CustomerId_OrderDate",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "OrderDate",
                IsIncludedColumn: false,
                KeyOrdinal: 2,
                IndexColumnId: 2,
                FilterDefinition: null),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 510,
                IndexId: 3,
                IndexName: "IX_Orders_CustomerId_OrderDate",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "CustomerId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 510,
                IndexId: 4,
                IndexName: "IX_Orders_Status_Filtered",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "Status",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: "[IsDeleted]=(0)"),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 510,
                IndexId: 4,
                IndexName: "IX_Orders_Status_Filtered",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "TotalAmount",
                IsIncludedColumn: true,
                KeyOrdinal: 0,
                IndexColumnId: 2,
                FilterDefinition: "[IsDeleted]=(0)"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeIndexes(rows);

        result.Should().HaveCount(3);

        result[0].IndexName.Should().Be("UX_Orders_OrderNumber");
        result[0].IsUnique.Should().BeTrue();
        result[0].Columns.Should().Equal("OrderNumber");

        result[1].IndexName.Should().Be("IX_Orders_CustomerId_OrderDate");
        result[1].Columns.Should().Equal("CustomerId", "OrderDate");
        result[1].IncludedColumns.Should().BeEmpty();

        result[2].IndexName.Should().Be("IX_Orders_Status_Filtered");
        result[2].Columns.Should().Equal("Status");
        result[2].IncludedColumns.Should().Equal("TotalAmount");
        result[2].FilterDefinition.Should().Be("[IsDeleted]=(0)");
    }
}
