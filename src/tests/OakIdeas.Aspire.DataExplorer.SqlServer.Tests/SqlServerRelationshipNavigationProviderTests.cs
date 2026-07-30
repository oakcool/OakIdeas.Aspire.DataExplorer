using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerRelationshipNavigationProviderTests
{
    // ── NormalizeRelationships ────────────────────────────────────────────────

    [Fact]
    public void NormalizeRelationships_EmptyRows_ReturnsEmpty()
    {
        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships(
            "dbo", "Orders", []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeRelationships_CurrentTableIsChild_ReturnsParentRelationship()
    {
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                ConstraintName: "FK_Orders_Customers",
                ParentSchema: "dbo",
                ParentTable: "Orders",
                ReferencedSchema: "dbo",
                ReferencedTable: "Customers",
                ParentColumn: "CustomerId",
                ReferencedColumn: "Id",
                IsDisabled: false),
        };

        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "Orders", rows);

        result.Should().ContainSingle();
        var rel = result.Single();
        rel.Kind.Should().Be(RelationshipKind.Parent);
        rel.ConstraintName.Should().Be("FK_Orders_Customers");
        rel.RelatedSchemaName.Should().Be("dbo");
        rel.RelatedTableName.Should().Be("Customers");
        rel.ColumnMappings.Should().ContainSingle();
        rel.ColumnMappings[0].SourceColumnName.Should().Be("CustomerId");
        rel.ColumnMappings[0].RelatedColumnName.Should().Be("Id");
        rel.IsEnforced.Should().BeTrue();
    }

    [Fact]
    public void NormalizeRelationships_CurrentTableIsParent_ReturnsChildRelationship()
    {
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                ConstraintName: "FK_Orders_Customers",
                ParentSchema: "dbo",
                ParentTable: "Orders",
                ReferencedSchema: "dbo",
                ReferencedTable: "Customers",
                ParentColumn: "CustomerId",
                ReferencedColumn: "Id",
                IsDisabled: false),
        };

        // From Customers' perspective, Orders is the child.
        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "Customers", rows);

        result.Should().ContainSingle();
        var rel = result.Single();
        rel.Kind.Should().Be(RelationshipKind.Child);
        rel.RelatedSchemaName.Should().Be("dbo");
        rel.RelatedTableName.Should().Be("Orders");
        rel.ColumnMappings[0].SourceColumnName.Should().Be("Id");
        rel.ColumnMappings[0].RelatedColumnName.Should().Be("CustomerId");
    }

    [Fact]
    public void NormalizeRelationships_DisabledConstraint_SetsIsEnforcedFalse()
    {
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                ConstraintName: "FK_Orders_Customers",
                ParentSchema: "dbo",
                ParentTable: "Orders",
                ReferencedSchema: "dbo",
                ReferencedTable: "Customers",
                ParentColumn: "CustomerId",
                ReferencedColumn: "Id",
                IsDisabled: true),
        };

        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "Orders", rows);

        result.Should().ContainSingle();
        result.Single().IsEnforced.Should().BeFalse();
    }

    [Fact]
    public void NormalizeRelationships_CompositeKey_AllColumnMappingsPresent()
    {
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                ConstraintName: "FK_OrderItems_Composite",
                ParentSchema: "dbo",
                ParentTable: "OrderItems",
                ReferencedSchema: "dbo",
                ReferencedTable: "Products",
                ParentColumn: "ProductCategory",
                ReferencedColumn: "Category",
                IsDisabled: false),
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                ConstraintName: "FK_OrderItems_Composite",
                ParentSchema: "dbo",
                ParentTable: "OrderItems",
                ReferencedSchema: "dbo",
                ReferencedTable: "Products",
                ParentColumn: "ProductCode",
                ReferencedColumn: "Code",
                IsDisabled: false),
        };

        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "OrderItems", rows);

        result.Should().ContainSingle();
        var rel = result.Single();
        rel.ColumnMappings.Should().HaveCount(2);
        rel.ColumnMappings.Select(m => m.SourceColumnName)
            .Should().Contain(["ProductCategory", "ProductCode"]);
    }

    [Fact]
    public void NormalizeRelationships_MultipleConstraints_ReturnsOnePerConstraint()
    {
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                "FK_Orders_Customers", "dbo", "Orders", "dbo", "Customers", "CustomerId", "Id", false),
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                "FK_Orders_Addresses", "dbo", "Orders", "dbo", "Addresses", "ShipToAddressId", "Id", false),
        };

        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "Orders", rows);

        result.Should().HaveCount(2);
        result.Select(r => r.ConstraintName).Should().Contain(["FK_Orders_Customers", "FK_Orders_Addresses"]);
    }

    [Fact]
    public void NormalizeRelationships_RowNotInvolvedWithTable_Skipped()
    {
        // FK between two other tables — should be ignored
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                "FK_Unrelated", "other", "TableA", "other", "TableB", "ColA", "ColB", false),
        };

        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "Orders", rows);

        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeRelationships_BothSidesOfSelfReferencing_ReturnsBothRelationships()
    {
        // Self-referencing: Employee.ManagerId → Employee.Id
        var rows = new[]
        {
            new SqlServerRelationshipNavigationProvider.RelationshipRow(
                "FK_Employee_Manager", "dbo", "Employee", "dbo", "Employee", "ManagerId", "Id", false),
        };

        var result = SqlServerRelationshipNavigationProvider.NormalizeRelationships("dbo", "Employee", rows);

        // Should contain both parent and child perspectives for self-referencing
        result.Should().HaveCount(2);
        result.Select(r => r.Kind).Should()
            .Contain(RelationshipKind.Parent)
            .And.Contain(RelationshipKind.Child);
    }

    // ── ProviderType ────────────────────────────────────────────────────────────

    [Fact]
    public void ProviderType_IsSqlServer()
    {
        var sut = new SqlServerRelationshipNavigationProvider();
        sut.ProviderType.Should().Be(DatabaseProviderType.SqlServer);
    }
}
