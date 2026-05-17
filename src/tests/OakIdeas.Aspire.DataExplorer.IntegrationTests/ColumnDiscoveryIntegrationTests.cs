using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class ColumnDiscoveryProjectionTests
{
    [Fact]
    public void NormalizeColumns_MultipleDataTypes_AreProjectedInOrdinalOrder()
    {
        SqlServerDatabaseProvider.ColumnDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 77,
                ColumnId: 3,
                Name: "CreatedOn",
                DataType: "datetime2",
                MaxLength: 8,
                Precision: 27,
                Scale: 7,
                IsNullable: false,
                IsIdentity: false,
                IsComputed: false,
                DefaultValue: "(sysutcdatetime())",
                Description: null),
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 77,
                ColumnId: 1,
                Name: "Id",
                DataType: "int",
                MaxLength: 4,
                Precision: 10,
                Scale: 0,
                IsNullable: false,
                IsIdentity: true,
                IsComputed: false,
                DefaultValue: null,
                Description: "Primary identifier"),
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 77,
                ColumnId: 2,
                Name: "Name",
                DataType: "nvarchar",
                MaxLength: 400,
                Precision: null,
                Scale: null,
                IsNullable: false,
                IsIdentity: false,
                IsComputed: false,
                DefaultValue: "('')",
                Description: null),
        ];

        var result = SqlServerDatabaseProvider.NormalizeColumns(rows);

        result.Select(column => column.Name).Should().Equal("Id", "Name", "CreatedOn");
        result[0].DataType.Should().Be("int");
        result[1].DataType.Should().Be("nvarchar");
        result[2].DataType.Should().Be("datetime2");
    }

    [Fact]
    public void NormalizeColumns_IdentityComputedAndNullableFlags_AreMapped()
    {
        SqlServerDatabaseProvider.ColumnDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 99,
                ColumnId: 1,
                Name: "LineId",
                DataType: "bigint",
                MaxLength: 8,
                Precision: 19,
                Scale: 0,
                IsNullable: false,
                IsIdentity: true,
                IsComputed: false,
                DefaultValue: null,
                Description: null),
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 99,
                ColumnId: 2,
                Name: "LineTotal",
                DataType: "decimal",
                MaxLength: 9,
                Precision: 18,
                Scale: 2,
                IsNullable: true,
                IsIdentity: false,
                IsComputed: true,
                DefaultValue: null,
                Description: null),
        ];

        var result = SqlServerDatabaseProvider.NormalizeColumns(rows);

        result[0].IsIdentity.Should().BeTrue();
        result[0].IsComputed.Should().BeFalse();
        result[1].IsNullable.Should().BeTrue();
        result[1].IsComputed.Should().BeTrue();
    }

    [Fact]
    public void NormalizeColumns_DefaultValues_ArePreserved()
    {
        SqlServerDatabaseProvider.ColumnDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 121,
                ColumnId: 1,
                Name: "IsActive",
                DataType: "bit",
                MaxLength: 1,
                Precision: null,
                Scale: null,
                IsNullable: false,
                IsIdentity: false,
                IsComputed: false,
                DefaultValue: "((1))",
                Description: null),
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 121,
                ColumnId: 2,
                Name: "TrackingId",
                DataType: "uniqueidentifier",
                MaxLength: 16,
                Precision: null,
                Scale: null,
                IsNullable: false,
                IsIdentity: false,
                IsComputed: false,
                DefaultValue: "(newid())",
                Description: null),
        ];

        var result = SqlServerDatabaseProvider.NormalizeColumns(rows);

        result[0].DefaultValue.Should().Be("((1))");
        result[1].DefaultValue.Should().Be("(newid())");
    }
}
