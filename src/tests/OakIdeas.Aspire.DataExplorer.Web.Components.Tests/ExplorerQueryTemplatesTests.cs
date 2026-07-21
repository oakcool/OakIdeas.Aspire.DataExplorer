using FluentAssertions;
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
        sql.Should().Contain("dbo.vActiveUsers");
    }

    [Fact]
    public void ExecuteProcedure_ContainsExecAndObject()
    {
        var sql = ExplorerQueryTemplates.ExecuteProcedure("dbo", "spGetUsers");

        sql.Should().Contain("EXEC");
        sql.Should().Contain("[dbo].[spGetUsers]");
    }

    // ── F-04 identifier escaping ──────────────────────────────────────────────

    [Fact]
    public void BracketQuote_EscapesClosingBracketInIdentifier()
    {
        // A closing bracket inside an identifier must be doubled to avoid breaking the quote
        var result = ExplorerQueryTemplates.BracketQuote("tricky]]name");

        result.Should().Be("[tricky]]]]name]");
    }

    [Fact]
    public void SelectTop1000_WithClosingBracketInName_ProducesSafeIdentifier()
    {
        var sql = ExplorerQueryTemplates.SelectTop1000("dbo", "Evil]Table");

        sql.Should().Contain("[Evil]]Table]");
        sql.Should().NotContain("[Evil]Table]");
    }

    [Fact]
    public void ScriptDefinition_WithSingleQuoteInName_EscapesQuote()
    {
        // A single quote in the object name must be doubled inside the string literal
        var sql = ExplorerQueryTemplates.ScriptDefinition("dbo", "O'Brien");

        sql.Should().Contain("O''Brien");
        // There must be no unescaped single quote that would break the string literal
        sql.Should().NotMatchRegex(@"'[^']*O'[^'B]");
    }

    [Fact]
    public void SingleQuoteEscape_DoublesEmbeddedSingleQuote()
    {
        var result = ExplorerQueryTemplates.SingleQuoteEscape("O'Reilly");

        result.Should().Be("O''Reilly");
    }

    [Fact]
    public void SingleQuoteEscape_NoQuote_ReturnsOriginal()
    {
        var result = ExplorerQueryTemplates.SingleQuoteEscape("plain");

        result.Should().Be("plain");
    }
}
