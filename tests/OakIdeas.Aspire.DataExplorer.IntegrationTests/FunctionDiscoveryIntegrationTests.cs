using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class FunctionDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeFunctions_ScalarFunction_IsProjected()
    {
        SqlServerDatabaseProvider.FunctionDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.FunctionDiscoveryRow(
                ObjectId: 1001,
                SchemaName: "dbo",
                FunctionName: "fn_OrderCount",
                FunctionTypeCode: "FN",
                ReturnType: "int",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0)),
        ];

        var result = SqlServerDatabaseProvider.NormalizeFunctions(rows);

        result.Should().ContainSingle();
        result[0].FunctionType.Should().Be(FunctionType.Scalar);
        result[0].FunctionName.Should().Be("fn_OrderCount");
        result[0].ReturnType.Should().Be("int");
    }

    [Fact]
    public void NormalizeFunctions_TableValuedFunction_IsProjected()
    {
        SqlServerDatabaseProvider.FunctionDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.FunctionDiscoveryRow(
                ObjectId: 1002,
                SchemaName: "sales",
                FunctionName: "tvf_OrdersByCustomer",
                FunctionTypeCode: "TF",
                ReturnType: "table",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0)),
        ];

        var result = SqlServerDatabaseProvider.NormalizeFunctions(rows);

        result.Should().ContainSingle();
        result[0].FunctionType.Should().Be(FunctionType.TableValued);
        result[0].FunctionName.Should().Be("tvf_OrdersByCustomer");
    }

    [Fact]
    public void NormalizeFunctions_InlineTableValuedFunction_IsProjected()
    {
        SqlServerDatabaseProvider.FunctionDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.FunctionDiscoveryRow(
                ObjectId: 1003,
                SchemaName: "analytics",
                FunctionName: "itvf_MonthlyRevenue",
                FunctionTypeCode: "IF",
                ReturnType: "table",
                HasDefinitionAvailable: false,
                CreatedAt: null),
        ];

        var result = SqlServerDatabaseProvider.NormalizeFunctions(rows);

        result.Should().ContainSingle();
        result[0].FunctionType.Should().Be(FunctionType.InlineTableValued);
        result[0].HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void GroupFunctionsBySchemaAndType_MultipleSchemas_GroupsAsExpected()
    {
        SqlServerDatabaseProvider.FunctionDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.FunctionDiscoveryRow(
                ObjectId: 1004,
                SchemaName: "sales",
                FunctionName: "fn_OrderCount",
                FunctionTypeCode: "FN",
                ReturnType: "int",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0)),
            new SqlServerDatabaseProvider.FunctionDiscoveryRow(
                ObjectId: 1005,
                SchemaName: "sales",
                FunctionName: "tvf_OrderTotals",
                FunctionTypeCode: "TF",
                ReturnType: "table",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0)),
            new SqlServerDatabaseProvider.FunctionDiscoveryRow(
                ObjectId: 1006,
                SchemaName: "analytics",
                FunctionName: "itvf_MonthlyRevenue",
                FunctionTypeCode: "IF",
                ReturnType: "table",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0)),
        ];

        var grouped = SqlServerDatabaseProvider.GroupFunctionsBySchemaAndType(
            SqlServerDatabaseProvider.NormalizeFunctions(rows));

        grouped.Keys.Should().Equal("analytics", "sales");
        grouped["sales"].Should().ContainKeys(FunctionType.Scalar, FunctionType.TableValued);
        grouped["analytics"].Should().ContainKey(FunctionType.InlineTableValued);
    }
}
