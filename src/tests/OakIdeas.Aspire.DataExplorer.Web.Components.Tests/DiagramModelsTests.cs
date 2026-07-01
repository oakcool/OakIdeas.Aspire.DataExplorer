using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class DiagramModelsTests
{
    [Fact]
    public void DiagramModel_WithEntitiesAndRelationships_StoresAllData()
    {
        var columns = new List<DiagramColumnItem>
        {
            new("Id", "int", IsPrimaryKey: true, IsForeignKey: false, IsNullable: false, IsIdentity: true),
            new("Name", "nvarchar(100)", IsPrimaryKey: false, IsForeignKey: false, IsNullable: false, IsIdentity: false),
        };

        var entity = new DiagramEntityNode(
            Id: "dbo.Orders",
            Name: "Orders",
            Schema: "dbo",
            EntityType: "Table",
            Columns: columns);

        var mapping = new DiagramColumnMapping("CustomerId", "Id");
        var edge = new DiagramRelationshipEdge(
            Id: "edge-0",
            ConstraintName: "FK_Orders_Customers",
            ParentEntityId: "dbo.Orders",
            ReferencedEntityId: "dbo.Customers",
            ColumnMappings: [mapping]);

        var model = new DiagramModel(Entities: [entity], Relationships: [edge]);

        model.Entities.Should().HaveCount(1);
        model.Relationships.Should().HaveCount(1);

        var e = model.Entities[0];
        e.Id.Should().Be("dbo.Orders");
        e.Name.Should().Be("Orders");
        e.Schema.Should().Be("dbo");
        e.EntityType.Should().Be("Table");
        e.Columns.Should().HaveCount(2);

        var pk = e.Columns[0];
        pk.Name.Should().Be("Id");
        pk.IsPrimaryKey.Should().BeTrue();
        pk.IsIdentity.Should().BeTrue();
        pk.IsForeignKey.Should().BeFalse();
        pk.IsNullable.Should().BeFalse();

        var rel = model.Relationships[0];
        rel.ConstraintName.Should().Be("FK_Orders_Customers");
        rel.ParentEntityId.Should().Be("dbo.Orders");
        rel.ReferencedEntityId.Should().Be("dbo.Customers");
        rel.ColumnMappings.Should().HaveCount(1);
        rel.ColumnMappings[0].ParentColumn.Should().Be("CustomerId");
        rel.ColumnMappings[0].ReferencedColumn.Should().Be("Id");
    }

    [Fact]
    public void DiagramEntityNode_ViewType_IsDistinguishedFromTable()
    {
        var view = new DiagramEntityNode(
            Id: "dbo.vw_Summary",
            Name: "vw_Summary",
            Schema: "dbo",
            EntityType: "View",
            Columns: []);

        view.EntityType.Should().Be("View");
        view.EntityType.Should().NotBe("Table");
    }

    [Fact]
    public void DiagramColumnItem_NullableColumn_HasCorrectFlags()
    {
        var col = new DiagramColumnItem(
            Name: "Description",
            DataType: "nvarchar(500)",
            IsPrimaryKey: false,
            IsForeignKey: false,
            IsNullable: true,
            IsIdentity: false);

        col.IsNullable.Should().BeTrue();
        col.IsPrimaryKey.Should().BeFalse();
        col.IsForeignKey.Should().BeFalse();
        col.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void DiagramModel_EmptyCollections_AreValid()
    {
        var model = new DiagramModel(Entities: [], Relationships: []);

        model.Entities.Should().BeEmpty();
        model.Relationships.Should().BeEmpty();
    }

    [Fact]
    public void DiagramRelationshipEdge_MultipleColumnMappings_StoresAll()
    {
        var mappings = new List<DiagramColumnMapping>
        {
            new("OrderId", "Id"),
            new("OrderDate", "Date"),
        };

        var edge = new DiagramRelationshipEdge(
            Id: "edge-1",
            ConstraintName: "FK_Test",
            ParentEntityId: "dbo.Child",
            ReferencedEntityId: "dbo.Parent",
            ColumnMappings: mappings);

        edge.ColumnMappings.Should().HaveCount(2);
        edge.ColumnMappings[0].ParentColumn.Should().Be("OrderId");
        edge.ColumnMappings[1].ParentColumn.Should().Be("OrderDate");
    }
}
