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
    public void SchemaNode_StoresTypeGroups()
    {
        var table = new ObjectExplorer.ObjectNodeModel("dbo.Users", "Users", ObjectExplorer.ObjectKind.Table);
        var view = new ObjectExplorer.ObjectNodeModel("dbo.ActiveUsers", "ActiveUsers", ObjectExplorer.ObjectKind.View);
        var node = new ObjectExplorer.SchemaNode("dbo", [table], [view], [], [], []);

        node.Name.Should().Be("dbo");
        node.Tables.Should().ContainSingle().Which.Name.Should().Be("Users");
        node.Views.Should().ContainSingle().Which.Name.Should().Be("ActiveUsers");
    }

    [Fact]
    public void ObjectSelection_StoresAllFields()
    {
        var selection = new ObjectExplorer.ObjectSelection(
            "sample",
            "applicationdb",
            "dbo",
            "dbo.Users",
            "Users",
            ObjectExplorer.ObjectKind.Table);

        selection.ConnectionName.Should().Be("sample");
        selection.DatabaseName.Should().Be("applicationdb");
        selection.SchemaName.Should().Be("dbo");
        selection.ObjectId.Should().Be("dbo.Users");
        selection.ObjectName.Should().Be("Users");
        selection.ObjectKind.Should().Be(ObjectExplorer.ObjectKind.Table);
    }

    [Fact]
    public void ConnectionNode_CanBeNestedDeep()
    {
        var tables = (IReadOnlyList<ObjectExplorer.ObjectNodeModel>)
        [
            new("dbo.Users", "Users", ObjectExplorer.ObjectKind.Table),
            new("dbo.Products", "Products", ObjectExplorer.ObjectKind.Table),
            new("dbo.Orders", "Orders", ObjectExplorer.ObjectKind.Table),
        ];
        var schema = new ObjectExplorer.SchemaNode("dbo", tables, [], [], [], []);
        var db = new ObjectExplorer.DatabaseNode("applicationdb", [schema]);
        var conn = new ObjectExplorer.ConnectionNode("my-connection", [db]);

        conn.Databases.Should().HaveCount(1);
        conn.Databases[0].Schemas.Should().HaveCount(1);
        conn.Databases[0].Schemas[0].Tables.Should().HaveCount(3);
    }

    [Fact]
    public void SchemaNode_HasAnyObjectsReflectsGroups()
    {
        var empty = new ObjectExplorer.SchemaNode("dbo", [], [], [], [], []);
        var populated = new ObjectExplorer.SchemaNode("dbo", [], [], [], [
            new ObjectExplorer.ObjectNodeModel("dbo.FormatName", "FormatName", ObjectExplorer.ObjectKind.Function)
        ], []);

        empty.HasAnyObjects.Should().BeFalse();
        populated.HasAnyObjects.Should().BeTrue();
    }
}
