using Bunit;
using OakIdeas.Aspire.DataExplorer.Web.Components.Components.Molecules;

namespace OakIdeas.Aspire.DataExplorer.Web.Components.Tests;

public sealed class ObjectDetailsRenderingTests : TestContext
{
    public ObjectDetailsRenderingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ── Empty / no-selection state ──────────────────────────────────────────

    [Fact]
    public void RendersEmptyPromptWhenSelectionIsNull()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, null));

        component.Markup.Should().Contain("Select an object in the explorer");
    }

    [Fact]
    public void RendersLoadingSpinnerWhenIsLoadingIsTrue()
    {
        var selection = MakeSelection(ObjectDetails.ObjectKind.Table);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, selection)
            .Add(p => p.IsLoading, true));

        component.Markup.Should().Contain("Loading object details");
    }

    // ── Header ──────────────────────────────────────────────────────────────

    [Fact]
    public void RendersObjectNameAndSchemaInHeader()
    {
        var selection = MakeSelection(ObjectDetails.ObjectKind.Table, schema: "dbo", name: "Orders");
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, selection));

        component.Markup.Should().Contain("dbo");
        component.Markup.Should().Contain("Orders");
    }

    [Fact]
    public void RendersKindBadgeForTable()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table)));

        component.Markup.Should().Contain("Table");
    }

    [Fact]
    public void RendersKindBadgeForView()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.View)));

        component.Markup.Should().Contain("View");
    }

    [Fact]
    public void RendersKindBadgeForProcedure()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Procedure)));

        component.Markup.Should().Contain("Procedure");
    }

    [Fact]
    public void RendersKindBadgeForFunction()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Function)));

        component.Markup.Should().Contain("Function");
    }

    [Fact]
    public void RendersKindBadgeForTrigger()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Trigger)));

        component.Markup.Should().Contain("Trigger");
    }

    // ── Table details ────────────────────────────────────────────────────────

    [Fact]
    public void TableDetails_RendersColumnsSection()
    {
        var columns = new[]
        {
            new ColumnDetails.ColumnModel("Id", 1, "int", false, true, false, null, IsPrimaryKey: true, IsForeignKey: true),
            new ColumnDetails.ColumnModel("Name", 2, "nvarchar(100)", true, false, false, null),
        };
        var metadata = new ObjectDetails.ObjectMetadata(Columns: columns);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Columns");
        component.Markup.Should().Contain("Id");
        component.Markup.Should().Contain("Name");
        component.Markup.Should().Contain("int");
        component.Markup.Should().Contain("nvarchar(100)");
        component.Markup.Should().Contain("PK, FK, identity, int, not null");
    }

    [Fact]
    public void TableDetails_RendersKeysSection()
    {
        var metadata = new ObjectDetails.ObjectMetadata(
            Keys:
            [
                new ObjectDetails.KeyModel("PK_TestObject", ["Id", "TenantId"]),
            ]);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Keys");
        component.Markup.Should().Contain("PK_TestObject");
        component.Markup.Should().Contain("Id, TenantId");
    }

    [Fact]
    public void TableDetails_RendersEmptyKeysWhenNone()
    {
        var metadata = new ObjectDetails.ObjectMetadata(Keys: null);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("No keys defined");
    }

    [Fact]
    public void TableDetails_RendersForeignKeysSection()
    {
        var fks = new[]
        {
            new RelationshipDetails.ForeignKeyModel(
                "FK_Orders_Customers",
                "Orders", "dbo",
                "Customers", "dbo",
                [new RelationshipDetails.ColumnMappingModel("CustomerId", "Id")]),
        };
        var metadata = new ObjectDetails.ObjectMetadata(ForeignKeys: fks);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Foreign Keys");
        component.Markup.Should().Contain("FK_Orders_Customers");
    }

    [Fact]
    public void TableDetails_RendersIndexesSection()
    {
        var indexes = new[]
        {
            new IndexDetails.IndexModel("PK_Orders", true, true, true, ["Id"]),
            new IndexDetails.IndexModel("IX_Orders_CustomerId", false, false, false, ["CustomerId"]),
        };
        var metadata = new ObjectDetails.ObjectMetadata(Indexes: indexes);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Indexes");
        component.Markup.Should().Contain("PK_Orders");
        component.Markup.Should().Contain("IX_Orders_CustomerId");
        component.Markup.Should().Contain("Clustered");
        component.Markup.Should().Contain("Non-clustered");
    }

    [Fact]
    public void TableDetails_RendersConstraintsSection()
    {
        var constraints = new[]
        {
            new ObjectDetails.ConstraintModel("CHK_Orders_Total", "Check", "Total", "Total > 0"),
        };
        var metadata = new ObjectDetails.ObjectMetadata(Constraints: constraints);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Constraints");
        component.Markup.Should().Contain("CHK_Orders_Total");
    }

    [Fact]
    public void TableDetails_RendersEmptyConstraintsWhenNone()
    {
        var metadata = new ObjectDetails.ObjectMetadata(Constraints: null);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("No additional constraints");
    }

    // ── View details ─────────────────────────────────────────────────────────

    [Fact]
    public void ViewDetails_RendersColumnsSection()
    {
        var columns = new[]
        {
            new ColumnDetails.ColumnModel("UserId", 1, "int", false, false, false, null),
        };
        var metadata = new ObjectDetails.ObjectMetadata(Columns: columns);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.View))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Columns");
        component.Markup.Should().Contain("UserId");
    }

    [Fact]
    public void ViewDetails_RendersDefinitionSection()
    {
        var definition = new ObjectDetails.DefinitionModel("CREATE VIEW dbo.ActiveUsers AS SELECT * FROM dbo.Users", true);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.View))
            .Add(p => p.Definition, definition));

        component.Markup.Should().Contain("Definition");
        component.Markup.Should().Contain("CREATE VIEW");
    }

    [Fact]
    public void ViewDetails_HandlesUnavailableDefinitionGracefully()
    {
        var definition = new ObjectDetails.DefinitionModel(null, false, "Definition not available for this view.");
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.View))
            .Add(p => p.Definition, definition));

        component.Markup.Should().Contain("Definition not available for this view.");
    }

    // ── Procedure details ────────────────────────────────────────────────────

    [Fact]
    public void ProcedureDetails_RendersDefinitionSection()
    {
        var definition = new ObjectDetails.DefinitionModel("CREATE PROCEDURE dbo.SyncUsers AS BEGIN SELECT 1 END", true);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Procedure))
            .Add(p => p.Definition, definition));

        component.Markup.Should().Contain("Definition");
        component.Markup.Should().Contain("CREATE PROCEDURE");
    }

    [Fact]
    public void ProcedureDetails_RendersParametersSection()
    {
        var metadata = new ObjectDetails.ObjectMetadata(
            Parameters:
            [
                new ObjectDetails.ParameterModel("@UserId", "int", "Input", false),
                new ObjectDetails.ParameterModel("@Name", "nvarchar(100)", "Output", true),
            ]);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Procedure))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Parameters");
        component.Markup.Should().Contain("@UserId");
        component.Markup.Should().Contain("@Name");
        component.Markup.Should().Contain("Input");
        component.Markup.Should().Contain("Output");
        component.Markup.Should().Contain("No default");
        component.Markup.Should().Contain("Has default");
    }

    // ── Function details ─────────────────────────────────────────────────────

    [Fact]
    public void FunctionDetails_RendersFunctionTypeParametersAndReturnType()
    {
        var metadata = new ObjectDetails.ObjectMetadata(
            FunctionType: "Scalar",
            ReturnType: "int",
            Parameters:
            [
                new ObjectDetails.ParameterModel("@UserId", "int"),
            ]);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Function))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Scalar");
        component.Markup.Should().Contain("Parameters");
        component.Markup.Should().Contain("@UserId");
        component.Markup.Should().Contain("Return Type");
        component.Markup.Should().Contain("int");
    }

    [Fact]
    public void TableDetails_RendersTriggersSection()
    {
        var metadata = new ObjectDetails.ObjectMetadata(
            Triggers:
            [
                new ObjectDetails.TriggerModel("trg_TestObject_Audit", "AFTER INSERT", true),
            ]);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Triggers");
        component.Markup.Should().Contain("trg_TestObject_Audit");
        component.Markup.Should().Contain("AFTER INSERT");
        component.Markup.Should().Contain("Enabled");
    }

    [Fact]
    public void FunctionDetails_RendersDefinition()
    {
        var definition = new ObjectDetails.DefinitionModel("CREATE FUNCTION dbo.FormatName() RETURNS int AS BEGIN RETURN 1 END", true);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Function))
            .Add(p => p.Definition, definition));

        component.Markup.Should().Contain("CREATE FUNCTION");
    }

    // ── Trigger details ──────────────────────────────────────────────────────

    [Fact]
    public void TriggerDetails_RendersTriggerTypeAndEnabledStatus()
    {
        var metadata = new ObjectDetails.ObjectMetadata(
            TriggerType: "AFTER INSERT, UPDATE",
            TriggerIsEnabled: true);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Trigger))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("AFTER INSERT, UPDATE");
        component.Markup.Should().Contain("Enabled");
    }

    [Fact]
    public void TriggerDetails_RendersDisabledStatus()
    {
        var metadata = new ObjectDetails.ObjectMetadata(TriggerIsEnabled: false);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Trigger))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Disabled");
    }

    // ── Definition loading state ─────────────────────────────────────────────

    [Fact]
    public void DefinitionViewer_ShowsLoadingSpinnerWhenIsDefinitionLoadingIsTrue()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.View))
            .Add(p => p.IsDefinitionLoading, true));

        component.Markup.Should().Contain("Loading definition");
    }

    // ── Component refresh on object change ───────────────────────────────────

    [Fact]
    public void RendersUpdatedObjectWhenSelectionChanges()
    {
        var selectionA = MakeSelection(ObjectDetails.ObjectKind.Table, name: "Orders");
        var selectionB = MakeSelection(ObjectDetails.ObjectKind.View, name: "ActiveUsers");

        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, selectionA));

        component.Markup.Should().Contain("Orders");

        component.SetParametersAndRender(parameters => parameters
            .Add(p => p.Selection, selectionB));

        component.Markup.Should().Contain("ActiveUsers");
        component.Markup.Should().NotContain("Orders");
    }

    // ── Many-row scenarios ───────────────────────────────────────────────────

    [Fact]
    public void TableDetails_HandlesManyColumns()
    {
        var columns = Enumerable.Range(1, 50)
            .Select(i => new ColumnDetails.ColumnModel($"Col{i}", i, "int", i % 2 == 0, false, false, null))
            .ToArray();
        var metadata = new ObjectDetails.ObjectMetadata(Columns: columns);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("Col1");
        component.Markup.Should().Contain("Col50");
    }

    [Fact]
    public void TableDetails_HandlesManyIndexes()
    {
        var indexes = Enumerable.Range(1, 20)
            .Select(i => new IndexDetails.IndexModel($"IX_Test_{i}", false, false, false, [$"Col{i}"]))
            .ToArray();
        var metadata = new ObjectDetails.ObjectMetadata(Indexes: indexes);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("IX_Test_1");
        component.Markup.Should().Contain("IX_Test_20");
    }

    [Fact]
    public void TableDetails_HandlesManyForeignKeys()
    {
        var fks = Enumerable.Range(1, 10)
            .Select(i => new RelationshipDetails.ForeignKeyModel(
                $"FK_Table_Ref{i}", "Table", "dbo", $"Ref{i}", "dbo",
                [new RelationshipDetails.ColumnMappingModel($"Ref{i}Id", "Id")]))
            .ToArray();
        var metadata = new ObjectDetails.ObjectMetadata(ForeignKeys: fks);
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, metadata));

        component.Markup.Should().Contain("FK_Table_Ref1");
        component.Markup.Should().Contain("FK_Table_Ref10");
    }

    // ── Null/unavailable metadata handled gracefully ─────────────────────────

    [Fact]
    public void TableDetails_HandlesNullMetadataGracefully()
    {
        var component = RenderComponent<ObjectDetails>(parameters => parameters
            .Add(p => p.Selection, MakeSelection(ObjectDetails.ObjectKind.Table))
            .Add(p => p.Metadata, null));

        // Should render sections without exceptions
        component.Markup.Should().Contain("Columns");
        component.Markup.Should().Contain("No columns available");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ObjectDetails.ObjectModel MakeSelection(
        ObjectDetails.ObjectKind kind,
        string schema = "dbo",
        string name = "TestObject")
        => new(
            ObjectId: $"{schema}.{name}",
            ObjectName: name,
            SchemaName: schema,
            ConnectionName: "sql-main",
            DatabaseName: "applicationdb",
            Kind: kind);
}
