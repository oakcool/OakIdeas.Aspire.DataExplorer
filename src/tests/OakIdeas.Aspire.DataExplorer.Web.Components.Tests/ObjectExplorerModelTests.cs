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
    public void DatabaseNode_StoresNameAndGroups()
    {
        var node = new ObjectExplorer.DatabaseNode("appdb", [], [], [], [], [], [], [], []);

        node.Name.Should().Be("appdb");
        node.Tables.Should().BeEmpty();
        node.Views.Should().BeEmpty();
        node.StoredProcedures.Should().BeEmpty();
        node.Functions.Should().BeEmpty();
        node.Triggers.Should().BeEmpty();
        node.Schemas.Should().BeEmpty();
    }

    [Fact]
    public void ObjectNodeModel_StoresDisplayAndSelectionFields()
    {
        var node = new ObjectExplorer.ObjectNodeModel(
            "dbo.Users",
            "dbo.Users",
            "sample",
            "applicationdb",
            "dbo",
            "Users",
            ObjectExplorer.ObjectKind.Table);

        node.Name.Should().Be("dbo.Users");
        node.ConnectionName.Should().Be("sample");
        node.DatabaseName.Should().Be("applicationdb");
        node.SchemaName.Should().Be("dbo");
        node.ObjectName.Should().Be("Users");
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
            new("dbo.Users", "dbo.Users", "my-connection", "applicationdb", "dbo", "Users", ObjectExplorer.ObjectKind.Table),
            new("dbo.Products", "dbo.Products", "my-connection", "applicationdb", "dbo", "Products", ObjectExplorer.ObjectKind.Table),
            new("dbo.Orders", "dbo.Orders", "my-connection", "applicationdb", "dbo", "Orders", ObjectExplorer.ObjectKind.Table),
        ];
        var db = new ObjectExplorer.DatabaseNode("applicationdb", tables, [], [], [], [], [], [], []);
        var conn = new ObjectExplorer.ConnectionNode("my-connection", [db]);

        conn.Databases.Should().HaveCount(1);
        conn.Databases[0].Tables.Should().HaveCount(3);
    }

    [Fact]
    public void DatabaseNode_HasAnyObjectsReflectsGroups()
    {
        var empty = new ObjectExplorer.DatabaseNode("applicationdb", [], [], [], [], [], [], [], []);
        var populated = new ObjectExplorer.DatabaseNode("applicationdb", [], [], [], [
            new ObjectExplorer.ObjectNodeModel("dbo.FormatName", "dbo.FormatName", "sample", "applicationdb", "dbo", "FormatName", ObjectExplorer.ObjectKind.Function)
        ], [], [], [], []);

        empty.HasAnyObjects.Should().BeFalse();
        populated.HasAnyObjects.Should().BeTrue();
    }
}
