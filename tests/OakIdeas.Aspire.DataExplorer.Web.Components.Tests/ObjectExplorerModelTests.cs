using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ObjectExplorerModelTests
{
    [Fact]
    public void ConnectionNode_StoresNameAndDatabases()
    {
        var node = new ObjectExplorer.ConnectionNode("sample", []);

        node.Name.Should().Be("sample");
        node.Databases.Should().BeEmpty();
    }

    [Fact]
    public void DatabaseNode_StoresNameAndSchemas()
    {
        var node = new ObjectExplorer.DatabaseNode("appdb", []);

        node.Name.Should().Be("appdb");
        node.Schemas.Should().BeEmpty();
    }

    [Fact]
    public void SchemaNode_StoresNameAndTables()
    {
        var node = new ObjectExplorer.SchemaNode("dbo", ["Users", "Orders"]);

        node.Name.Should().Be("dbo");
        node.Tables.Should().Equal("Users", "Orders");
    }

    [Fact]
    public void TableSelection_StoresAllFields()
    {
        var sel = new ObjectExplorer.TableSelection("sample", "dbo", "Users");

        sel.ConnectionName.Should().Be("sample");
        sel.SchemaName.Should().Be("dbo");
        sel.TableName.Should().Be("Users");
    }

    [Fact]
    public void ConnectionNode_CanBeNestedDeep()
    {
        var tables = (IReadOnlyList<string>)["Users", "Products", "Orders"];
        var schema = new ObjectExplorer.SchemaNode("dbo", tables);
        var db = new ObjectExplorer.DatabaseNode("applicationdb", [schema]);
        var conn = new ObjectExplorer.ConnectionNode("my-connection", [db]);

        conn.Databases.Should().HaveCount(1);
        conn.Databases[0].Schemas.Should().HaveCount(1);
        conn.Databases[0].Schemas[0].Tables.Should().HaveCount(3);
    }

    [Fact]
    public void SchemaNode_TablesAreOrdered_AsProvided()
    {
        var tables = (IReadOnlyList<string>)["Zebra", "Alpha", "Middle"];
        var node = new ObjectExplorer.SchemaNode("dbo", tables);

        node.Tables.Should().Equal("Zebra", "Alpha", "Middle");
    }
}
