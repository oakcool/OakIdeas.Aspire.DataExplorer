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
}
