using OakIdeas.Aspire.DataExplorer.Web.Components.ContextMenu;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ExplorerQueryTemplatesTests
{
    [Fact]
    public void SelectTop1000_ContainsSchemaAndTable()
    {
        var sql = ExplorerQueryTemplates.SelectTop1000("dbo", "Orders");

        sql.Should().Contain("SELECT TOP 1000");
        sql.Should().Contain("[dbo].[Orders]");
    }

    [Fact]
    public void InsertStatement_ContainsSchemaAndTable()
    {
        var sql = ExplorerQueryTemplates.InsertStatement("sales", "Invoices");

        sql.Should().Contain("INSERT INTO");
        sql.Should().Contain("[sales].[Invoices]");
    }

    [Fact]
    public void DeleteStatement_ContainsSafeWhereClause()
    {
        var sql = ExplorerQueryTemplates.DeleteStatement("dbo", "Customers");

        sql.Should().Contain("DELETE FROM");
        sql.Should().Contain("[dbo].[Customers]");
        sql.Should().Contain("WHERE 1 = 0");
    }

    [Fact]
    public void TruncateStatement_ContainsSchemaAndTable()
    {
        var sql = ExplorerQueryTemplates.TruncateStatement("dbo", "AuditLog");

        sql.Should().Contain("TRUNCATE TABLE");
        sql.Should().Contain("[dbo].[AuditLog]");
    }

    [Fact]
    public void ScriptDefinition_ContainsSpHelptextAndObject()
    {
        var sql = ExplorerQueryTemplates.ScriptDefinition("dbo", "vActiveUsers");

        sql.Should().Contain("sp_helptext");
        sql.Should().Contain("[dbo].[vActiveUsers]");
    }

    [Fact]
    public void ExecuteProcedure_ContainsExecAndObject()
    {
        var sql = ExplorerQueryTemplates.ExecuteProcedure("dbo", "spGetUsers");

        sql.Should().Contain("EXEC");
        sql.Should().Contain("[dbo].[spGetUsers]");
    }
}
