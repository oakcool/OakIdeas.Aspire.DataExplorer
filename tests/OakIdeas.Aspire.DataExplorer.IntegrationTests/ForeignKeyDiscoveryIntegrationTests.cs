using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class ForeignKeyDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeForeignKeyConstraints_SimpleForeignKey_IsProjected()
    {
        SqlServerDatabaseProvider.ForeignKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 11,
                ConstraintName: "FK_OrderItems_Orders",
                ParentSchemaName: "sales",
                ParentTableName: "OrderItems",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "Orders",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "Id",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 0,
                UpdateReferentialAction: 0,
                IsDisabled: false),
        ];

        var result = SqlServerDatabaseProvider.NormalizeForeignKeyConstraints(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("FK_OrderItems_Orders");
        result[0].ParentTableName.Should().Be("sales.OrderItems");
        result[0].ReferencedTableName.Should().Be("sales.Orders");
        result[0].KeyColumns.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ForeignKeyColumnMapping("OrderId", "Id"));
    }

    [Fact]
    public void NormalizeForeignKeyConstraints_CompositeForeignKey_PreservesColumnOrder()
    {
        SqlServerDatabaseProvider.ForeignKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 21,
                ConstraintName: "FK_LineAudit_OrderLines",
                ParentSchemaName: "audit",
                ParentTableName: "LineAudit",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "OrderLines",
                ParentColumnName: "LineNumber",
                ReferencedColumnName: "LineNumber",
                ConstraintColumnId: 2,
                DeleteReferentialAction: 0,
                UpdateReferentialAction: 0,
                IsDisabled: false),
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 21,
                ConstraintName: "FK_LineAudit_OrderLines",
                ParentSchemaName: "audit",
                ParentTableName: "LineAudit",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "OrderLines",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "OrderId",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 0,
                UpdateReferentialAction: 0,
                IsDisabled: false),
        ];

        var result = SqlServerDatabaseProvider.NormalizeForeignKeyConstraints(rows);

        result.Should().ContainSingle();
        result[0].KeyColumns.Select(column => column.ParentColumnName)
            .Should()
            .Equal("OrderId", "LineNumber");
    }

    [Fact]
    public void NormalizeForeignKeyConstraints_CascadeAndDisabledFlags_AreMapped()
    {
        SqlServerDatabaseProvider.ForeignKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 31,
                ConstraintName: "FK_Payments_Orders",
                ParentSchemaName: "sales",
                ParentTableName: "Payments",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "Orders",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "Id",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 1,
                UpdateReferentialAction: 2,
                IsDisabled: true),
        ];

        var result = SqlServerDatabaseProvider.NormalizeForeignKeyConstraints(rows);

        result.Should().ContainSingle();
        result[0].OnDeleteBehavior.Should().Be(ReferentialActionBehavior.Cascade);
        result[0].OnUpdateBehavior.Should().Be(ReferentialActionBehavior.SetNull);
        result[0].IsDisabled.Should().BeTrue();
    }
}
