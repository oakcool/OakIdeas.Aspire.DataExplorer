using System.Reflection;
using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Contracts.Models.Explorer;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;
using OakIdeas.Aspire.DataExplorer.Web.Components.Pages;

namespace OakIdeas.Aspire.DataExplorer.Web.Tests;

public sealed class DiagramPageTests
{
    [Fact]
    public void BuildDiagramModel_UsesAlreadyQualifiedForeignKeyTableNamesWithoutSchemaDuplication()
    {
        var metadata = CreateMetadata(new ForeignKeyConstraint(
            ConstraintName: "FK_Orders_Customers",
            ParentTableName: "sales.Orders",
            ParentSchemaName: "sales",
            ReferencedTableName: "sales.Customers",
            ReferencedSchemaName: "sales",
            KeyColumns: [new ForeignKeyColumnMapping("CustomerId", "Id")],
            OnDeleteBehavior: ReferentialActionBehavior.NoAction,
            OnUpdateBehavior: ReferentialActionBehavior.NoAction,
            IsDisabled: false,
            ObjectId: "fk-1"));

        var model = InvokeBuildDiagramModel(metadata);

        model.Relationships.Should().ContainSingle();
        model.Relationships[0].ParentEntityId.Should().Be("sales.Orders");
        model.Relationships[0].ReferencedEntityId.Should().Be("sales.Customers");
    }

    [Fact]
    public void BuildDiagramModel_ResolvesUnqualifiedForeignKeyTableNamesToEntityIds()
    {
        var metadata = CreateMetadata(new ForeignKeyConstraint(
            ConstraintName: "FK_Orders_Customers",
            ParentTableName: "Orders",
            ParentSchemaName: "sales",
            ReferencedTableName: "Customers",
            ReferencedSchemaName: "sales",
            KeyColumns: [new ForeignKeyColumnMapping("CustomerId", "Id")],
            OnDeleteBehavior: ReferentialActionBehavior.NoAction,
            OnUpdateBehavior: ReferentialActionBehavior.NoAction,
            IsDisabled: false,
            ObjectId: "fk-1"));

        var model = InvokeBuildDiagramModel(metadata);

        model.Relationships.Should().ContainSingle();
        model.Relationships[0].ParentEntityId.Should().Be("sales.Orders");
        model.Relationships[0].ReferencedEntityId.Should().Be("sales.Customers");
    }

    private static DiagramModel InvokeBuildDiagramModel(DatabaseMetadata metadata)
    {
        var method = typeof(DiagramPage).GetMethod("BuildDiagramModel", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var result = method!.Invoke(null, [metadata]);
        result.Should().BeOfType<DiagramModel>();

        return (DiagramModel)result!;
    }

    private static DatabaseMetadata CreateMetadata(ForeignKeyConstraint foreignKey)
    {
        return new DatabaseMetadata(
            DatabaseName: "appdb",
            ProviderType: DatabaseProviderType.SqlServer,
            ResourceId: "sql-main",
            Schemas: [new SchemaObject("sales", "sales")],
            Tables:
            [
                new TableObject("sales.Orders", "sales", "Orders"),
                new TableObject("sales.Customers", "sales", "Customers"),
            ],
            Views: [],
            ProceduresBySchema: new Dictionary<string, IReadOnlyList<StoredProcedureMetadata>>(StringComparer.OrdinalIgnoreCase),
            FunctionsBySchema: new Dictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>>(StringComparer.OrdinalIgnoreCase),
            Triggers: [],
            Constraints: [],
            ColumnsByObject: new Dictionary<string, IReadOnlyList<ColumnMetadata>>(StringComparer.OrdinalIgnoreCase)
            {
                ["sales.Orders"] =
                [
                    new ColumnMetadata("Id", 1, "int", null, null, null, false, true, false, null, null, new Dictionary<string, object?>()),
                    new ColumnMetadata("CustomerId", 2, "int", null, null, null, false, false, false, null, null, new Dictionary<string, object?>()),
                ],
                ["sales.Customers"] =
                [
                    new ColumnMetadata("Id", 1, "int", null, null, null, false, true, false, null, null, new Dictionary<string, object?>()),
                ],
            },
            PrimaryKeysByTable: new Dictionary<string, IReadOnlyList<PrimaryKeyConstraint>>(StringComparer.OrdinalIgnoreCase)
            {
                ["sales.Orders"] = [new PrimaryKeyConstraint("PK_Orders", "Orders", "sales", ["Id"], true, "pk-orders")],
                ["sales.Customers"] = [new PrimaryKeyConstraint("PK_Customers", "Customers", "sales", ["Id"], true, "pk-customers")],
            },
            ForeignKeysByTable: new Dictionary<string, IReadOnlyList<ForeignKeyConstraint>>(StringComparer.OrdinalIgnoreCase)
            {
                ["sales.Orders"] = [foreignKey],
            },
            IndexesByTable: new Dictionary<string, IReadOnlyList<IndexMetadata>>(StringComparer.OrdinalIgnoreCase),
            MetadataCollectionTime: DateTimeOffset.UtcNow,
            CollectionStatus: MetadataCollectionStatus.Success,
            FailureDetails: []);
    }
}
