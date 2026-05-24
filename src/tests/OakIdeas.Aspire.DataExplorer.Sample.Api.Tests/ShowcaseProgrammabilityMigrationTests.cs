using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using OakIdeas.Aspire.DataExplorer.Sample.Api.Migrations;

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Tests;

public sealed class ShowcaseProgrammabilityMigrationTests
{
    [Fact]
    public void Up_CreatesShowcaseSchemaObjects()
    {
        var operations = new TestableShowcaseMigration().GetUpOperations();

        operations.OfType<CreateTableOperation>()
            .Where(x => string.Equals(x.Schema, "showcase", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Should()
            .BeEquivalentTo(["TodoListsReplica", "TodoItemsReplica"]);

        var sqlOperations = operations.OfType<SqlOperation>().Select(x => x.Sql).ToArray();
        sqlOperations.Should().Contain(sql => sql.Contains("CREATE VIEW [showcase].[vwTodoReplicaOverview]", StringComparison.OrdinalIgnoreCase));
        sqlOperations.Should().Contain(sql => sql.Contains("CREATE FUNCTION [showcase].[ufn_OpenReplicaTodoCount]", StringComparison.OrdinalIgnoreCase));
        sqlOperations.Should().Contain(sql => sql.Contains("CREATE PROCEDURE [showcase].[usp_ListReplicaTodosByStatus]", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Down_DropsShowcaseProgrammabilityObjects()
    {
        var operations = new TestableShowcaseMigration().GetDownOperations();

        var sqlOperations = operations.OfType<SqlOperation>().Select(x => x.Sql).ToArray();
        sqlOperations.Should().Contain(sql => sql.Contains("DROP PROCEDURE IF EXISTS [showcase].[usp_ListReplicaTodosByStatus]", StringComparison.OrdinalIgnoreCase));
        sqlOperations.Should().Contain(sql => sql.Contains("DROP FUNCTION IF EXISTS [showcase].[ufn_OpenReplicaTodoCount]", StringComparison.OrdinalIgnoreCase));
        sqlOperations.Should().Contain(sql => sql.Contains("DROP VIEW IF EXISTS [showcase].[vwTodoReplicaOverview]", StringComparison.OrdinalIgnoreCase));

        operations.OfType<DropTableOperation>()
            .Where(x => string.Equals(x.Schema, "showcase", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Should()
            .BeEquivalentTo(["TodoItemsReplica", "TodoListsReplica"]);
    }

    private sealed class TestableShowcaseMigration : ShowcaseProgrammabilityObjects
    {
        public IReadOnlyList<MigrationOperation> GetUpOperations()
        {
            var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
            Up(migrationBuilder);
            return migrationBuilder.Operations;
        }

        public IReadOnlyList<MigrationOperation> GetDownOperations()
        {
            var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
            Down(migrationBuilder);
            return migrationBuilder.Operations;
        }
    }
}
