using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class ObjectDefinitionIntegrationTests
{
    [Fact]
    public void BuildIndexDefinition_SingleKeyColumn_BuildsNonClusteredIndexDefinition()
    {
        SqlServerDatabaseProvider.IndexDefinitionRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "IX_Orders_CustomerId",
                SchemaName: "sales",
                TableName: "Orders",
                IsUnique: false,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "CustomerId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
        ];

        var result = SqlServerDatabaseProvider.BuildIndexDefinition(rows);

        result.Should().StartWith("INDEX [IX_Orders_CustomerId]");
        result.Should().Contain("NONCLUSTERED");
        result.Should().Contain("ON [sales].[Orders]");
        result.Should().Contain("([CustomerId])");
        result.Should().NotContain("INCLUDE");
        result.Should().NotContain("WHERE");
    }

    [Fact]
    public void BuildIndexDefinition_CompositeKeyColumns_OrderedByKeyOrdinal()
    {
        SqlServerDatabaseProvider.IndexDefinitionRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "IX_Orders_CustomerDate",
                SchemaName: "sales",
                TableName: "Orders",
                IsUnique: false,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "OrderDate",
                IsIncludedColumn: false,
                KeyOrdinal: 2,
                IndexColumnId: 2,
                FilterDefinition: null),
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "IX_Orders_CustomerDate",
                SchemaName: "sales",
                TableName: "Orders",
                IsUnique: false,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "CustomerId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
        ];

        var result = SqlServerDatabaseProvider.BuildIndexDefinition(rows);

        result.Should().Contain("([CustomerId], [OrderDate])");
    }

    [Fact]
    public void BuildIndexDefinition_IndexWithIncludedColumns_IncludesIncludeClause()
    {
        SqlServerDatabaseProvider.IndexDefinitionRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "IX_Orders_Status",
                SchemaName: "dbo",
                TableName: "Orders",
                IsUnique: false,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "Status",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "IX_Orders_Status",
                SchemaName: "dbo",
                TableName: "Orders",
                IsUnique: false,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "TotalAmount",
                IsIncludedColumn: true,
                KeyOrdinal: 0,
                IndexColumnId: 2,
                FilterDefinition: null),
        ];

        var result = SqlServerDatabaseProvider.BuildIndexDefinition(rows);

        result.Should().Contain("INCLUDE ([TotalAmount])");
    }

    [Fact]
    public void BuildIndexDefinition_FilteredIndex_IncludesWhereClause()
    {
        SqlServerDatabaseProvider.IndexDefinitionRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "IX_Orders_Active",
                SchemaName: "dbo",
                TableName: "Orders",
                IsUnique: false,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "CustomerId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: "([IsDeleted] = (0))"),
        ];

        var result = SqlServerDatabaseProvider.BuildIndexDefinition(rows);

        result.Should().Contain("WHERE ([IsDeleted] = (0))");
    }

    [Fact]
    public void BuildIndexDefinition_UniqueIndex_IncludesUniqueKeyword()
    {
        SqlServerDatabaseProvider.IndexDefinitionRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "UQ_Products_Sku",
                SchemaName: "inventory",
                TableName: "Products",
                IsUnique: true,
                IsClustered: false,
                IsPrimaryKey: false,
                ColumnName: "Sku",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
        ];

        var result = SqlServerDatabaseProvider.BuildIndexDefinition(rows);

        result.Should().StartWith("UNIQUE INDEX");
    }

    [Fact]
    public void BuildIndexDefinition_PrimaryKeyIndex_UsesPrimaryKeyKeyword()
    {
        SqlServerDatabaseProvider.IndexDefinitionRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDefinitionRow(
                IndexName: "PK_Orders",
                SchemaName: "sales",
                TableName: "Orders",
                IsUnique: true,
                IsClustered: true,
                IsPrimaryKey: true,
                ColumnName: "OrderId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
        ];

        var result = SqlServerDatabaseProvider.BuildIndexDefinition(rows);

        result.Should().StartWith("PRIMARY KEY CLUSTERED");
        result.Should().NotContain("UNIQUE");
        result.Should().NotContain("INDEX [");
    }

    [Theory]
    [InlineData("100:2", true, 100, 2)]
    [InlineData("0:0", true, 0, 0)]
    [InlineData("999:10", true, 999, 10)]
    [InlineData("100", false, 0, 0)]
    [InlineData("abc:def", false, 0, 0)]
    [InlineData(":2", false, 0, 0)]
    [InlineData("100:", false, 0, 0)]
    public void TryParseIndexObjectId_VariousFormats_ParsesCorrectly(
        string objectId,
        bool expectedSuccess,
        int expectedTableObjectId,
        int expectedIndexId)
    {
        var success = SqlServerDatabaseProvider.TryParseIndexObjectId(
            objectId, out var tableObjectId, out var indexId);

        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            tableObjectId.Should().Be(expectedTableObjectId);
            indexId.Should().Be(expectedIndexId);
        }
    }
}
