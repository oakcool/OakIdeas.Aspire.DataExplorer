using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Atoms;
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
    public void DatabaseNode_StoresNameAndHierarchyNodes()
    {
        var node = new ObjectExplorer.DatabaseNode("appdb", []);

        node.Name.Should().Be("appdb");
        node.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void ExplorerNodeModel_StoresTreeAndSelectionFields()
    {
        var selection = new ObjectExplorer.ObjectSelection(
            "sample",
            "applicationdb",
            "dbo",
            "dbo.Users",
            "Users",
            ObjectExplorer.ObjectKind.Table);

        var node = new ObjectExplorer.ExplorerNodeModel(
            "tables/dbo.Users",
            "dbo.Users",
            HeroIconKind.TableCells,
            [],
            selection);

        node.NodeKey.Should().Be("tables/dbo.Users");
        node.Label.Should().Be("dbo.Users");
        node.Icon.Should().Be(HeroIconKind.TableCells);
        node.Selection.Should().Be(selection);
        node.HasChildren.Should().BeFalse();
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
        var tableLeaf = new ObjectExplorer.ExplorerNodeModel(
            "tables/dbo.Users",
            "dbo.Users",
            HeroIconKind.TableCells,
            [],
            new ObjectExplorer.ObjectSelection("my-connection", "applicationdb", "dbo", "dbo.Users", "Users", ObjectExplorer.ObjectKind.Table));

        var tables = new ObjectExplorer.ExplorerNodeModel(
            "tables",
            "Tables",
            HeroIconKind.TableCells,
            [tableLeaf]);

        var db = new ObjectExplorer.DatabaseNode("applicationdb", [tables]);
        var conn = new ObjectExplorer.ConnectionNode("my-connection", [db]);

        conn.Databases.Should().HaveCount(1);
        conn.Databases[0].Nodes.Should().ContainSingle();
        conn.Databases[0].Nodes[0].Children.Should().ContainSingle();
    }

    [Fact]
    public void DatabaseNode_HasAnyObjectsReflectsHierarchyNodes()
    {
        var empty = new ObjectExplorer.DatabaseNode("applicationdb", []);
        var populated = new ObjectExplorer.DatabaseNode("applicationdb",
        [
            new ObjectExplorer.ExplorerNodeModel(
                "functions",
                "Functions",
                HeroIconKind.CodeBracket,
                [
                    new ObjectExplorer.ExplorerNodeModel(
                        "functions/dbo.FormatName",
                        "dbo.FormatName",
                        HeroIconKind.CodeBracket,
                        [],
                        new ObjectExplorer.ObjectSelection("sample", "applicationdb", "dbo", "dbo.FormatName", "FormatName", ObjectExplorer.ObjectKind.Function))
                ])
        ]);

        empty.HasAnyObjects.Should().BeFalse();
        populated.HasAnyObjects.Should().BeTrue();
    }
}
