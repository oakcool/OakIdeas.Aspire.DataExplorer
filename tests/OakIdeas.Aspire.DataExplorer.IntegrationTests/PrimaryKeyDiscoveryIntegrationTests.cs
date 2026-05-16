using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class PrimaryKeyDiscoveryProjectionTests
{
    [Fact]
    public void NormalizePrimaryKeys_SingleColumnPrimaryKey_IsProjected()
    {
        SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 41,
                ConstraintName: "PK_Customers",
                SchemaName: "sales",
                TableName: "Customers",
                IsClustered: true,
                ColumnName: "CustomerId",
                KeyOrdinal: 1),
        ];

        var result = SqlServerDatabaseProvider.NormalizePrimaryKeys(rows);

        result.Should().ContainSingle();
        result[0].ConstraintName.Should().Be("PK_Customers");
        result[0].TableName.Should().Be("sales.Customers");
        result[0].SchemaName.Should().Be("sales");
        result[0].KeyColumns.Should().Equal("CustomerId");
        result[0].IsClustered.Should().BeTrue();
    }

    [Fact]
    public void NormalizePrimaryKeys_CompositePrimaryKey_PreservesColumnOrder()
    {
        SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 51,
                ConstraintName: "PK_OrderLines",
                SchemaName: "sales",
                TableName: "OrderLines",
                IsClustered: false,
                ColumnName: "LineNumber",
                KeyOrdinal: 2),
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 51,
                ConstraintName: "PK_OrderLines",
                SchemaName: "sales",
                TableName: "OrderLines",
                IsClustered: false,
                ColumnName: "OrderId",
                KeyOrdinal: 1),
        ];

        var result = SqlServerDatabaseProvider.NormalizePrimaryKeys(rows);

        result.Should().ContainSingle();
        result[0].KeyColumns.Should().Equal("OrderId", "LineNumber");
        result[0].IsClustered.Should().BeFalse();
    }

    [Fact]
    public void NormalizePrimaryKeys_MultipleTablesWithMixedKeys_AreProjected()
    {
        SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 61,
                ConstraintName: "PK_Users",
                SchemaName: "auth",
                TableName: "Users",
                IsClustered: true,
                ColumnName: "UserId",
                KeyOrdinal: 1),
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 71,
                ConstraintName: "PK_UserRoles",
                SchemaName: "auth",
                TableName: "UserRoles",
                IsClustered: false,
                ColumnName: "RoleId",
                KeyOrdinal: 2),
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 71,
                ConstraintName: "PK_UserRoles",
                SchemaName: "auth",
                TableName: "UserRoles",
                IsClustered: false,
                ColumnName: "UserId",
                KeyOrdinal: 1),
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 81,
                ConstraintName: "PK_AuditEntries",
                SchemaName: "audit",
                TableName: "AuditEntries",
                IsClustered: true,
                ColumnName: "AuditEntryId",
                KeyOrdinal: 1),
        ];

        var result = SqlServerDatabaseProvider.NormalizePrimaryKeys(rows);

        result.Should().HaveCount(3);

        result[0].Should().BeEquivalentTo(new PrimaryKeyConstraint(
            ConstraintName: "PK_Users",
            TableName: "auth.Users",
            SchemaName: "auth",
            KeyColumns: ["UserId"],
            IsClustered: true,
            ObjectId: "61"));

        result[1].Should().BeEquivalentTo(new PrimaryKeyConstraint(
            ConstraintName: "PK_UserRoles",
            TableName: "auth.UserRoles",
            SchemaName: "auth",
            KeyColumns: ["UserId", "RoleId"],
            IsClustered: false,
            ObjectId: "71"));

        result[2].Should().BeEquivalentTo(new PrimaryKeyConstraint(
            ConstraintName: "PK_AuditEntries",
            TableName: "audit.AuditEntries",
            SchemaName: "audit",
            KeyColumns: ["AuditEntryId"],
            IsClustered: true,
            ObjectId: "81"));
    }
}
