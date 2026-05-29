using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class StoredProcedureDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeStoredProcedures_SingleProcedureWithParameters_IsProjected()
    {
        SqlServerDatabaseProvider.StoredProcedureDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.StoredProcedureDiscoveryRow(
                ObjectId: 801,
                SchemaName: "dbo",
                ProcedureName: "usp_CreateOrder",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                ParameterId: 1,
                ParameterName: "@CustomerId",
                ParameterDataType: "int",
                Definition: "CREATE PROCEDURE dbo.usp_CreateOrder @CustomerId int, @OrderDate datetime2(7) = NULL OUTPUT AS SELECT 1;"),
            new SqlServerDatabaseProvider.StoredProcedureDiscoveryRow(
                ObjectId: 801,
                SchemaName: "dbo",
                ProcedureName: "usp_CreateOrder",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                ParameterId: 2,
                ParameterName: "@OrderDate",
                ParameterDataType: "datetime2",
                ParameterScale: 7,
                ParameterIsOutput: true,
                Definition: "CREATE PROCEDURE dbo.usp_CreateOrder @CustomerId int, @OrderDate datetime2(7) = NULL OUTPUT AS SELECT 1;"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeStoredProcedures(rows);

        result.Should().ContainSingle();
        result[0].SchemaName.Should().Be("dbo");
        result[0].ProcedureName.Should().Be("usp_CreateOrder");
        result[0].HasDefinitionAvailable.Should().BeTrue();
        result[0].Parameters.Should().HaveCount(2);
        result[0].Parameters![1].DataType.Should().Be("datetime2(7)");
        result[0].Parameters![1].Direction.Should().Be(RoutineParameterDirection.Output);
        result[0].Parameters![1].HasDefault.Should().BeTrue();
    }

    [Fact]
    public void NormalizeStoredProcedures_MultipleSchemas_AreGroupedBySchema()
    {
        SqlServerDatabaseProvider.StoredProcedureDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.StoredProcedureDiscoveryRow(
                ObjectId: 901,
                SchemaName: "sales",
                ProcedureName: "usp_GetOrders",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                ParameterId: null,
                ParameterName: null,
                ParameterDataType: null),
            new SqlServerDatabaseProvider.StoredProcedureDiscoveryRow(
                ObjectId: 902,
                SchemaName: "analytics",
                ProcedureName: "usp_GetRevenue",
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                ParameterId: null,
                ParameterName: null,
                ParameterDataType: null),
        ];

        var grouped = SqlServerDatabaseProvider.GroupStoredProceduresBySchema(
            SqlServerDatabaseProvider.NormalizeStoredProcedures(rows));

        grouped.Keys.Should().Equal("analytics", "sales");
        grouped["sales"].Should().ContainSingle().Which.ProcedureName.Should().Be("usp_GetOrders");
        grouped["analytics"].Should().ContainSingle().Which.ProcedureName.Should().Be("usp_GetRevenue");
    }

    [Fact]
    public void NormalizeStoredProcedures_WhenDefinitionUnavailable_SetsHasDefinitionAvailableToFalse()
    {
        SqlServerDatabaseProvider.StoredProcedureDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.StoredProcedureDiscoveryRow(
                ObjectId: 1001,
                SchemaName: "restricted",
                ProcedureName: "usp_RestrictedProc",
                HasDefinitionAvailable: false,
                CreatedAt: null,
                ParameterId: 1,
                ParameterName: "@Input",
                ParameterDataType: "nvarchar"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeStoredProcedures(rows);

        result.Should().ContainSingle();
        result[0].HasDefinitionAvailable.Should().BeFalse();
        result[0].Parameters.Should().ContainSingle().Which.Name.Should().Be("@Input");
    }
}
