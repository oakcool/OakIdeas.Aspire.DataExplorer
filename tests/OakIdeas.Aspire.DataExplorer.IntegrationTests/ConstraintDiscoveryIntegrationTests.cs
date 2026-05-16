using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class ConstraintDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeConstraints_EmptyRows_ReturnsEmpty()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows = [];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeConstraints_DefaultConstraint_IsProjected()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 901,
                ConstraintName: "DF_Products_IsActive",
                SchemaName: "inventory",
                TableName: "Products",
                ColumnName: "IsActive",
                Definition: "((1))",
                IsDisabled: false,
                ConstraintTypeCode: "D"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("DF_Products_IsActive");
        result[0].ConstraintType.Should().Be(ConstraintType.Default);
        result[0].TableName.Should().Be("inventory.Products");
        result[0].SchemaName.Should().Be("inventory");
        result[0].ColumnName.Should().Be("IsActive");
        result[0].Definition.Should().Be("((1))");
        result[0].IsDisabled.Should().BeFalse();
        result[0].ObjectId.Should().Be("901");
    }

    [Fact]
    public void NormalizeConstraints_ColumnLevelCheckConstraint_IsProjected()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 902,
                ConstraintName: "CK_Products_Price",
                SchemaName: "inventory",
                TableName: "Products",
                ColumnName: "Price",
                Definition: "([Price]>(0))",
                IsDisabled: false,
                ConstraintTypeCode: "C"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("CK_Products_Price");
        result[0].ConstraintType.Should().Be(ConstraintType.Check);
        result[0].ColumnName.Should().Be("Price");
        result[0].Definition.Should().Be("([Price]>(0))");
        result[0].IsDisabled.Should().BeFalse();
    }

    [Fact]
    public void NormalizeConstraints_TableLevelCheckConstraint_HasNullColumnName()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 903,
                ConstraintName: "CK_Orders_DateRange",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: null,
                Definition: "([ShippedDate]>=[OrderDate])",
                IsDisabled: false,
                ConstraintTypeCode: "C"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("CK_Orders_DateRange");
        result[0].ConstraintType.Should().Be(ConstraintType.Check);
        result[0].ColumnName.Should().BeNull();
        result[0].Definition.Should().Be("([ShippedDate]>=[OrderDate])");
    }

    [Fact]
    public void NormalizeConstraints_DisabledCheckConstraint_IsProjectedAsDisabled()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 904,
                ConstraintName: "CK_Orders_Amount_Disabled",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: null,
                Definition: "([TotalAmount]>=(0))",
                IsDisabled: true,
                ConstraintTypeCode: "C"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("CK_Orders_Amount_Disabled");
        result[0].IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void NormalizeConstraints_UniqueConstraint_IsProjected()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 905,
                ConstraintName: "UQ_Customers_Email",
                SchemaName: "sales",
                TableName: "Customers",
                ColumnName: null,
                Definition: null,
                IsDisabled: false,
                ConstraintTypeCode: "U"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("UQ_Customers_Email");
        result[0].ConstraintType.Should().Be(ConstraintType.Unique);
        result[0].TableName.Should().Be("sales.Customers");
        result[0].SchemaName.Should().Be("sales");
        result[0].ColumnName.Should().BeNull();
        result[0].Definition.Should().BeNull();
        result[0].IsDisabled.Should().BeFalse();
        result[0].ObjectId.Should().Be("905");
    }

    [Fact]
    public void NormalizeConstraints_MultipleConstraintTypesOnSameTable_AreAllProjected()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 911,
                ConstraintName: "DF_Orders_CreatedAt",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: "CreatedAt",
                Definition: "(getutcdate())",
                IsDisabled: false,
                ConstraintTypeCode: "D"),
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 912,
                ConstraintName: "CK_Orders_TotalAmount",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: "TotalAmount",
                Definition: "([TotalAmount]>=(0))",
                IsDisabled: false,
                ConstraintTypeCode: "C"),
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 913,
                ConstraintName: "UQ_Orders_OrderNumber",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: null,
                Definition: null,
                IsDisabled: false,
                ConstraintTypeCode: "U"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().HaveCount(3);
        result.Select(c => c.ConstraintType).Should().Equal(
            ConstraintType.Default,
            ConstraintType.Check,
            ConstraintType.Unique);
        result.Select(c => c.TableName).Should().AllBe("sales.Orders");
    }
}
