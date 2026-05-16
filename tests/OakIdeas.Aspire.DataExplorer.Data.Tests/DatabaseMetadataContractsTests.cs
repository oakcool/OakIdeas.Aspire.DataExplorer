using System.Text.Json;
using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Data.Tests;

public sealed class DatabaseMetadataContractsTests
{
    [Fact]
    public void TableObject_WhenCreated_FormatsFullyQualifiedName()
    {
        var table = new TableObject(
            objectId: "12345",
            schemaName: "dbo",
            objectName: "Users");

        table.ObjectType.Should().Be(DatabaseObjectType.Table);
        table.ObjectName.Should().Be("Users");
        table.SchemaName.Should().Be("dbo");
        table.FullyQualifiedName.Should().Be("dbo.Users");
    }

    [Fact]
    public void DatabaseMetadataRoot_WhenCreated_StoresObjectsByTypeAndName()
    {
        var users = new TableObject("table.users", "dbo", "Users");
        var schema = new SchemaObject("schema.dbo", "dbo");

        var root = new DatabaseMetadataRoot(
            databaseName: "appdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-app",
            metadataCollectionTime: new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
            objects: new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>
            {
                [DatabaseObjectType.Schema] = new Dictionary<string, DatabaseObject>
                {
                    [schema.ObjectName] = schema,
                },
                [DatabaseObjectType.Table] = new Dictionary<string, DatabaseObject>
                {
                    [users.ObjectName] = users,
                },
            });

        root.DatabaseName.Should().Be("appdb");
        root.Objects.Should().ContainKey(DatabaseObjectType.Schema);
        root.Objects.Should().ContainKey(DatabaseObjectType.Table);
        root.Objects[DatabaseObjectType.Table]["Users"].Should().BeSameAs(users);
    }

    [Fact]
    public void TableObject_WhenRequiredValuesMissing_ThrowsArgumentException()
    {
        var action = () => new TableObject(
            objectId: "table.users",
            schemaName: "dbo",
            objectName: " ");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("objectName");
    }

    [Fact]
    public void DatabaseMetadataRoot_WhenSerialized_RoundTripsPolymorphicObjectsAndMetadata()
    {
        var table = new TableObject(
            objectId: "table.users",
            schemaName: "dbo",
            objectName: "Users",
            providerMetadata: new Dictionary<string, object?>
            {
                ["sqlServerObjectId"] = 42,
                ["isTemporal"] = true,
            },
            relationships:
            [
                new DatabaseObjectRelationship("FK_UserRoles_Users", "ForeignKey", "table.userroles"),
            ]);

        var root = new DatabaseMetadataRoot(
            databaseName: "appdb",
            providerType: DatabaseProviderType.SqlServer,
            resourceId: "sql-app",
            metadataCollectionTime: new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
            objects: new Dictionary<DatabaseObjectType, IReadOnlyDictionary<string, DatabaseObject>>
            {
                [DatabaseObjectType.Table] = new Dictionary<string, DatabaseObject> { ["Users"] = table },
            });

        var json = JsonSerializer.Serialize(root);
        var deserialized = JsonSerializer.Deserialize<DatabaseMetadataRoot>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Objects[DatabaseObjectType.Table]["Users"].Should().BeOfType<TableObject>();
        deserialized.Objects[DatabaseObjectType.Table]["Users"].ProviderMetadata.Should().ContainKey("sqlServerObjectId");
        deserialized.Objects[DatabaseObjectType.Table]["Users"].Relationships.Should().ContainSingle();
    }

    [Fact]
    public void DiscoverSchemasContracts_DefaultsAndPayload_ArePreserved()
    {
        var request = new DiscoverSchemasRequest();
        var response = new DiscoverSchemasResponse(
            [
                new SchemaObject(
                    objectId: "schema.sales",
                    objectName: "sales",
                    providerMetadata: new Dictionary<string, object?> { ["schemaId"] = 4 }),
            ]);

        request.IncludeSystemSchemas.Should().BeFalse();
        response.Schemas.Should().ContainSingle();
        response.Schemas[0].ObjectName.Should().Be("sales");
        response.Schemas[0].ProviderMetadata["schemaId"].Should().Be(4);
    }

    [Fact]
    public void DiscoverForeignKeysContracts_DefaultsAndPayload_ArePreserved()
    {
        var request = new DiscoverForeignKeysRequest();
        var response = new DiscoverForeignKeysResponse(
            [
                new ForeignKeyConstraint(
                    ConstraintName: "FK_OrderItems_Orders",
                    ParentTableName: "sales.OrderItems",
                    ParentSchemaName: "sales",
                    ReferencedTableName: "sales.Orders",
                    ReferencedSchemaName: "sales",
                    KeyColumns:
                    [
                        new ForeignKeyColumnMapping("OrderId", "Id"),
                    ],
                    OnDeleteBehavior: ReferentialActionBehavior.Cascade,
                    OnUpdateBehavior: ReferentialActionBehavior.NoAction,
                    IsDisabled: false,
                    ObjectId: "123"),
            ]);

        request.ParentSchemaName.Should().BeNull();
        request.ParentTableName.Should().BeNull();
        response.ForeignKeys.Should().ContainSingle();
        response.ForeignKeys[0].ConstraintName.Should().Be("FK_OrderItems_Orders");
        response.ForeignKeys[0].OnDeleteBehavior.Should().Be(ReferentialActionBehavior.Cascade);
        response.ForeignKeys[0].KeyColumns.Should().ContainSingle();
    }

    [Fact]
    public void DiscoverColumnsContracts_DefaultsAndPayload_ArePreserved()
    {
        var request = new DiscoverColumnsRequest(
            FullyQualifiedName: "sales.Orders",
            ObjectType: DatabaseObjectType.Table);
        var response = new DiscoverColumnsResponse(
            [
                new ColumnMetadata(
                    Name: "OrderId",
                    Ordinal: 1,
                    DataType: "int",
                    MaxLength: 4,
                    Precision: 10,
                    Scale: 0,
                    IsNullable: false,
                    IsIdentity: true,
                    IsComputed: false,
                    DefaultValue: null,
                    Description: "Primary key",
                    ProviderMetadata: new Dictionary<string, object?>
                    {
                        ["objectId"] = 1001,
                        ["columnId"] = 1,
                    }),
            ]);

        request.ObjectId.Should().BeNull();
        request.FullyQualifiedName.Should().Be("sales.Orders");
        request.ObjectType.Should().Be(DatabaseObjectType.Table);
        response.Columns.Should().ContainSingle();
        response.Columns[0].Name.Should().Be("OrderId");
        response.Columns[0].IsIdentity.Should().BeTrue();
        response.Columns[0].ProviderMetadata["objectId"].Should().Be(1001);
    }

    [Fact]
    public void DiscoverIndexesContracts_DefaultsAndPayload_ArePreserved()
    {
        var request = new DiscoverIndexesRequest();
        var response = new DiscoverIndexesResponse(
            [
                new IndexMetadata(
                    IndexName: "IX_Orders_CustomerId",
                    TableName: "sales.Orders",
                    SchemaName: "sales",
                    IsPrimaryKey: false,
                    IsUnique: false,
                    IsClustered: false,
                    Columns: ["CustomerId", "OrderDate"],
                    IncludedColumns: ["TotalAmount"],
                    FilterDefinition: "[IsActive]=(1)",
                    ObjectId: "1001:2"),
            ]);

        request.SchemaName.Should().BeNull();
        request.TableName.Should().BeNull();
        response.Indexes.Should().ContainSingle();
        response.Indexes[0].IndexName.Should().Be("IX_Orders_CustomerId");
        response.Indexes[0].TableName.Should().Be("sales.Orders");
        response.Indexes[0].Columns.Should().Equal("CustomerId", "OrderDate");
        response.Indexes[0].IncludedColumns.Should().ContainSingle().Which.Should().Be("TotalAmount");
        response.Indexes[0].FilterDefinition.Should().Be("[IsActive]=(1)");
        response.Indexes[0].ObjectId.Should().Be("1001:2");
    }

    [Fact]
    public void DiscoverTablesContracts_DefaultsAndPayload_ArePreserved()
    {
        var request = new DiscoverTablesRequest();
        var response = new DiscoverTablesResponse(
            [
                new TableObject(
                    objectId: "4001",
                    schemaName: "sales",
                    objectName: "Orders",
                    providerMetadata: new Dictionary<string, object?> { ["objectId"] = 4001, ["rowCount"] = 500L }),
            ]);

        request.SchemaName.Should().BeNull();
        request.IncludeSystemTables.Should().BeFalse();
        response.Tables.Should().ContainSingle();
        response.Tables[0].SchemaName.Should().Be("sales");
        response.Tables[0].ObjectName.Should().Be("Orders");
        response.Tables[0].FullyQualifiedName.Should().Be("sales.Orders");
        response.Tables[0].ProviderMetadata["objectId"].Should().Be(4001);
        response.Tables[0].ProviderMetadata["rowCount"].Should().Be(500L);
    }

    [Fact]
    public void DiscoverViewsContracts_DefaultsAndPayload_ArePreserved()
    {
        var request = new DiscoverViewsRequest();
        var response = new DiscoverViewsResponse(
            [
                new ViewObject(
                    objectId: "5001",
                    schemaName: "analytics",
                    objectName: "MonthlyRevenue",
                    hasDefinitionAvailable: true,
                    providerMetadata: new Dictionary<string, object?> { ["objectId"] = 5001 }),
            ]);

        request.SchemaName.Should().BeNull();
        request.IncludeSystemViews.Should().BeFalse();
        response.Views.Should().ContainSingle();
        response.Views[0].SchemaName.Should().Be("analytics");
        response.Views[0].ObjectName.Should().Be("MonthlyRevenue");
        response.Views[0].FullyQualifiedName.Should().Be("analytics.MonthlyRevenue");
        response.Views[0].HasDefinitionAvailable.Should().BeTrue();
        response.Views[0].ProviderMetadata["objectId"].Should().Be(5001);
    }
}
