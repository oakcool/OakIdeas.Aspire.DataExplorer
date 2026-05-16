using FluentAssertions;
using Microsoft.Data.SqlClient;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.SqlServer.Tests;

public sealed class SqlServerDatabaseProviderTests
{
    [Fact]
    public void ProviderMetadata_UsesSqlServerTypeAndCapabilities()
    {
        var sut = new SqlServerDatabaseProvider();

        sut.ProviderType.Should().Be(DatabaseProviderType.SqlServer);
        sut.Capabilities.Should().BeEquivalentTo(new
        {
            SupportsSchemas = true,
            SupportsTables = true,
            SupportsViews = true,
            SupportsStoredProcedures = true,
            SupportsFunctions = true,
            SupportsTriggers = true,
            SupportsIndexes = true,
            SupportsConstraints = true,
            SupportsKeys = true,
            SupportsDefinitionRetrieval = true,
            SupportsLiveStats = false,
        });
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("mssql")]
    [InlineData("Microsoft.Data.SqlClient")]
    public void CanHandle_ForSqlServerProviders_ReturnsTrue(string providerName)
    {
        var sut = new SqlServerDatabaseProvider();
        var resource = CreateResource(providerName);

        sut.CanHandle(resource).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ForNonSqlServerProvider_ReturnsFalse()
    {
        var sut = new SqlServerDatabaseProvider();
        var resource = CreateResource("postgresql");

        sut.CanHandle(resource).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteQueryAsync_ReturnsEmptyResult()
    {
        var sut = new SqlServerDatabaseProvider();
        var resource = CreateResource("sqlserver");
        var request = new ExecuteQueryRequest("db", "select 1", 10);

        QueryResult result = await sut.ExecuteQueryAsync(resource, request, CancellationToken.None);

        result.Columns.Should().BeEmpty();
        result.Rows.Should().BeEmpty();
        result.RowCount.Should().Be(0);
    }

    [Fact]
    public void CreateSchemaObject_IncludesSchemaIdMetadata()
    {
        var schema = SqlServerDatabaseProvider.CreateSchemaObject(schemaId: 7, schemaName: "sales");

        schema.ObjectId.Should().Be("schema.sales");
        schema.ObjectName.Should().Be("sales");
        schema.ProviderMetadata.Should().ContainKey("schemaId");
        schema.ProviderMetadata["schemaId"].Should().Be(7);
    }

    [Fact]
    public void CreateDiscoverSchemasCommand_UsesSchemaCatalogQueryAndParameter()
    {
        using var connection = new SqlConnection();

        using var command = SqlServerDatabaseProvider.CreateDiscoverSchemasCommand(
            connection,
            includeSystemSchemas: false);

        command.CommandText.Should().Contain("FROM sys.schemas");
        command.CommandText.Should().Contain("ORDER BY name");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .ContainSingle(parameter => parameter.ParameterName == "@IncludeSystemSchemas");
        command.Parameters["@IncludeSystemSchemas"].Value.Should().Be(false);
    }

    [Fact]
    public void CreateDiscoverSchemasCommand_WhenIncludingSystemSchemas_SetsParameterToTrue()
    {
        using var connection = new SqlConnection();

        using var command = SqlServerDatabaseProvider.CreateDiscoverSchemasCommand(
            connection,
            includeSystemSchemas: true);

        command.Parameters["@IncludeSystemSchemas"].Value.Should().Be(true);
    }

    [Fact]
    public void CreateDiscoverForeignKeysCommand_UsesForeignKeyCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverForeignKeysRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverForeignKeysCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.foreign_keys AS fk");
        command.CommandText.Should().Contain("JOIN sys.foreign_key_columns AS fkc");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(parameter => parameter.ParameterName == "@ParentSchemaName")
            .And.Contain(parameter => parameter.ParameterName == "@ParentTableName");
        command.Parameters["@ParentSchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@ParentTableName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverForeignKeysCommand_WhenTableFilterProvided_TrimsAndSetsParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverForeignKeysRequest(ParentSchemaName: " sales ", ParentTableName: " orders ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverForeignKeysCommand(connection, request);

        command.Parameters["@ParentSchemaName"].Value.Should().Be("sales");
        command.Parameters["@ParentTableName"].Value.Should().Be("orders");
    }

    [Fact]
    public void CreateDiscoverColumnsCommand_UsesColumnCatalogQueryAndObjectId()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverColumnsRequest(ObjectId: "42", ObjectType: DatabaseObjectType.Table);

        using var command = SqlServerDatabaseProvider.CreateDiscoverColumnsCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.columns AS c");
        command.CommandText.Should().Contain("LEFT JOIN sys.identity_columns AS ic");
        command.CommandText.Should().Contain("LEFT JOIN sys.computed_columns AS cc");
        command.Parameters["@ObjectId"].Value.Should().Be(42);
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@ObjectName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@ObjectType"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverColumnsCommand_WhenUsingFullyQualifiedName_SetsSchemaNameObjectNameAndType()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverColumnsRequest(
            FullyQualifiedName: "analytics.MonthlyRevenue",
            ObjectType: DatabaseObjectType.View);

        using var command = SqlServerDatabaseProvider.CreateDiscoverColumnsCommand(connection, request);

        command.Parameters["@ObjectId"].Value.Should().Be(DBNull.Value);
        command.Parameters["@SchemaName"].Value.Should().Be("analytics");
        command.Parameters["@ObjectName"].Value.Should().Be("MonthlyRevenue");
        command.Parameters["@ObjectType"].Value.Should().Be("V");
    }

    [Fact]
    public void CreateDiscoverIndexesCommand_UsesIndexCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverIndexesRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverIndexesCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.indexes AS i");
        command.CommandText.Should().Contain("JOIN sys.index_columns AS ic");
        command.CommandText.Should().Contain("i.index_id > 0");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(parameter => parameter.ParameterName == "@SchemaName")
            .And.Contain(parameter => parameter.ParameterName == "@TableName");
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@TableName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverIndexesCommand_WhenFiltersProvided_TrimsAndSetsParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverIndexesRequest(SchemaName: " sales ", TableName: " Orders ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverIndexesCommand(connection, request);

        command.Parameters["@SchemaName"].Value.Should().Be("sales");
        command.Parameters["@TableName"].Value.Should().Be("Orders");
    }

    [Fact]
    public void CreateDiscoverPrimaryKeysCommand_UsesPrimaryKeyCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverPrimaryKeysRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverPrimaryKeysCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.key_constraints AS kc");
        command.CommandText.Should().Contain("JOIN sys.index_columns AS ic");
        command.CommandText.Should().Contain("kc.type = 'PK'");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(parameter => parameter.ParameterName == "@SchemaName")
            .And.Contain(parameter => parameter.ParameterName == "@TableName");
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@TableName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverPrimaryKeysCommand_WhenFiltersProvided_TrimsAndSetsParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverPrimaryKeysRequest(SchemaName: " sales ", TableName: " Orders ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverPrimaryKeysCommand(connection, request);

        command.Parameters["@SchemaName"].Value.Should().Be("sales");
        command.Parameters["@TableName"].Value.Should().Be("Orders");
    }

    [Theory]
    [InlineData("int", 4, 10, 0, false, true, false, null)]
    [InlineData("bigint", 8, 19, 0, false, false, false, null)]
    [InlineData("varchar", 100, null, null, true, false, false, "('value')")]
    [InlineData("char", 8, null, null, false, false, false, null)]
    [InlineData("nvarchar", 200, null, null, true, false, false, null)]
    [InlineData("datetime", 8, null, null, false, false, false, "(getdate())")]
    [InlineData("datetime2", 8, 27, 7, false, false, false, null)]
    [InlineData("date", 3, null, null, true, false, false, null)]
    [InlineData("decimal", 17, 19, 4, false, false, false, null)]
    [InlineData("money", 8, 19, 4, false, false, false, null)]
    [InlineData("float", 8, 53, null, true, false, false, null)]
    [InlineData("bit", 1, null, null, false, false, false, "((0))")]
    [InlineData("uniqueidentifier", 16, null, null, false, false, false, "(newid())")]
    public void NormalizeColumns_CoversCommonSqlServerDataTypesAndColumnFlags(
        string dataType,
        int? maxLength,
        int? precision,
        int? scale,
        bool isNullable,
        bool isIdentity,
        bool isComputed,
        string? defaultValue)
    {
        SqlServerDatabaseProvider.ColumnDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 900,
                ColumnId: 2,
                Name: "C",
                DataType: dataType,
                MaxLength: maxLength.HasValue ? (short)maxLength.Value : null,
                Precision: precision.HasValue ? (byte)precision.Value : null,
                Scale: scale.HasValue ? (byte)scale.Value : null,
                IsNullable: isNullable,
                IsIdentity: isIdentity,
                IsComputed: isComputed,
                DefaultValue: defaultValue,
                Description: "column"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeColumns(rows);

        result.Should().ContainSingle();
        var column = result[0];
        column.Ordinal.Should().Be(2);
        column.DataType.Should().Be(dataType);
        column.MaxLength.Should().Be(maxLength);
        column.Precision.Should().Be(precision);
        column.Scale.Should().Be(scale);
        column.IsNullable.Should().Be(isNullable);
        column.IsIdentity.Should().Be(isIdentity);
        column.IsComputed.Should().Be(isComputed);
        column.DefaultValue.Should().Be(defaultValue);
        column.ProviderMetadata["objectId"].Should().Be(900);
        column.ProviderMetadata["columnId"].Should().Be(2);
    }

    [Theory]
    [InlineData(0, ReferentialActionBehavior.NoAction)]
    [InlineData(1, ReferentialActionBehavior.Cascade)]
    [InlineData(2, ReferentialActionBehavior.SetNull)]
    [InlineData(3, ReferentialActionBehavior.SetDefault)]
    [InlineData(4, ReferentialActionBehavior.NoAction)]
    [InlineData(99, ReferentialActionBehavior.NoAction)]
    public void MapReferentialAction_MapsExpectedCodes(int actionCode, ReferentialActionBehavior expected)
    {
        SqlServerDatabaseProvider.MapReferentialAction(actionCode).Should().Be(expected);
    }

    [Fact]
    public void NormalizeForeignKeyConstraints_PreservesCompositeColumnOrderAndMetadata()
    {
        SqlServerDatabaseProvider.ForeignKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 101,
                ConstraintName: "FK_OrderLines_Orders",
                ParentSchemaName: "sales",
                ParentTableName: "OrderLines",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "Orders",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "Id",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 1,
                UpdateReferentialAction: 0,
                IsDisabled: false),
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 202,
                ConstraintName: "FK_OrderLineAudit_OrderLines",
                ParentSchemaName: "audit",
                ParentTableName: "OrderLineAudit",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "OrderLines",
                ParentColumnName: "LineNo",
                ReferencedColumnName: "LineNo",
                ConstraintColumnId: 2,
                DeleteReferentialAction: 3,
                UpdateReferentialAction: 2,
                IsDisabled: true),
            new SqlServerDatabaseProvider.ForeignKeyDiscoveryRow(
                ObjectId: 202,
                ConstraintName: "FK_OrderLineAudit_OrderLines",
                ParentSchemaName: "audit",
                ParentTableName: "OrderLineAudit",
                ReferencedSchemaName: "sales",
                ReferencedTableName: "OrderLines",
                ParentColumnName: "OrderId",
                ReferencedColumnName: "OrderId",
                ConstraintColumnId: 1,
                DeleteReferentialAction: 3,
                UpdateReferentialAction: 2,
                IsDisabled: true),
        ];

        var constraints = SqlServerDatabaseProvider.NormalizeForeignKeyConstraints(rows);

        constraints.Should().HaveCount(2);
        constraints[0].ConstraintName.Should().Be("FK_OrderLines_Orders");
        constraints[0].ParentTableName.Should().Be("sales.OrderLines");
        constraints[0].ReferencedTableName.Should().Be("sales.Orders");
        constraints[0].KeyColumns.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ForeignKeyColumnMapping("OrderId", "Id"));
        constraints[0].OnDeleteBehavior.Should().Be(ReferentialActionBehavior.Cascade);
        constraints[0].OnUpdateBehavior.Should().Be(ReferentialActionBehavior.NoAction);
        constraints[0].IsDisabled.Should().BeFalse();

        constraints[1].ConstraintName.Should().Be("FK_OrderLineAudit_OrderLines");
        constraints[1].ObjectId.Should().Be("202");
        constraints[1].KeyColumns.Should().HaveCount(2);
        constraints[1].KeyColumns[0].Should().BeEquivalentTo(new ForeignKeyColumnMapping("OrderId", "OrderId"));
        constraints[1].KeyColumns[1].Should().BeEquivalentTo(new ForeignKeyColumnMapping("LineNo", "LineNo"));
        constraints[1].OnDeleteBehavior.Should().Be(ReferentialActionBehavior.SetDefault);
        constraints[1].OnUpdateBehavior.Should().Be(ReferentialActionBehavior.SetNull);
        constraints[1].IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void NormalizeColumns_PreservesOrdinalOrder()
    {
        SqlServerDatabaseProvider.ColumnDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 51,
                ColumnId: 3,
                Name: "TotalAmount",
                DataType: "decimal",
                MaxLength: 9,
                Precision: 18,
                Scale: 2,
                IsNullable: false,
                IsIdentity: false,
                IsComputed: true,
                DefaultValue: null,
                Description: null),
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 51,
                ColumnId: 1,
                Name: "OrderId",
                DataType: "int",
                MaxLength: 4,
                Precision: 10,
                Scale: 0,
                IsNullable: false,
                IsIdentity: true,
                IsComputed: false,
                DefaultValue: null,
                Description: null),
            new SqlServerDatabaseProvider.ColumnDiscoveryRow(
                ObjectId: 51,
                ColumnId: 2,
                Name: "CreatedAtUtc",
                DataType: "datetime2",
                MaxLength: 8,
                Precision: 27,
                Scale: 7,
                IsNullable: false,
                IsIdentity: false,
                IsComputed: false,
                DefaultValue: "(sysutcdatetime())",
                Description: null),
        ];

        var result = SqlServerDatabaseProvider.NormalizeColumns(rows);

        result.Select(column => column.Name).Should().Equal("OrderId", "CreatedAtUtc", "TotalAmount");
    }

    [Fact]
    public void NormalizeIndexes_ProjectsKeyIncludedAndFilteredMetadata()
    {
        SqlServerDatabaseProvider.IndexDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 301,
                IndexId: 1,
                IndexName: "PK_Orders",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: true,
                IsUnique: true,
                IsClustered: true,
                ColumnName: "OrderId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: null),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 301,
                IndexId: 3,
                IndexName: "IX_Orders_CustomerId_Status",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "Status",
                IsIncludedColumn: false,
                KeyOrdinal: 2,
                IndexColumnId: 2,
                FilterDefinition: "[IsDeleted]=(0)"),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 301,
                IndexId: 3,
                IndexName: "IX_Orders_CustomerId_Status",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "CustomerId",
                IsIncludedColumn: false,
                KeyOrdinal: 1,
                IndexColumnId: 1,
                FilterDefinition: "[IsDeleted]=(0)"),
            new SqlServerDatabaseProvider.IndexDiscoveryRow(
                ObjectId: 301,
                IndexId: 3,
                IndexName: "IX_Orders_CustomerId_Status",
                SchemaName: "sales",
                TableName: "Orders",
                IsPrimaryKey: false,
                IsUnique: false,
                IsClustered: false,
                ColumnName: "OrderDate",
                IsIncludedColumn: true,
                KeyOrdinal: 0,
                IndexColumnId: 3,
                FilterDefinition: "[IsDeleted]=(0)"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeIndexes(rows);

        result.Should().HaveCount(2);
        result[0].IndexName.Should().Be("PK_Orders");
        result[0].TableName.Should().Be("sales.Orders");
        result[0].SchemaName.Should().Be("sales");
        result[0].IsPrimaryKey.Should().BeTrue();
        result[0].IsUnique.Should().BeTrue();
        result[0].IsClustered.Should().BeTrue();
        result[0].Columns.Should().Equal("OrderId");
        result[0].IncludedColumns.Should().BeEmpty();
        result[0].ObjectId.Should().Be("301:1");

        result[1].IndexName.Should().Be("IX_Orders_CustomerId_Status");
        result[1].Columns.Should().Equal("CustomerId", "Status");
        result[1].IncludedColumns.Should().Equal("OrderDate");
        result[1].FilterDefinition.Should().Be("[IsDeleted]=(0)");
        result[1].ObjectId.Should().Be("301:3");
    }

    [Fact]
    public void NormalizePrimaryKeys_PreservesCompositeColumnOrderAndClusteredState()
    {
        SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 601,
                ConstraintName: "PK_Customers",
                SchemaName: "sales",
                TableName: "Customers",
                IsClustered: true,
                ColumnName: "CustomerId",
                KeyOrdinal: 1),
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 701,
                ConstraintName: "PK_OrderLines",
                SchemaName: "sales",
                TableName: "OrderLines",
                IsClustered: false,
                ColumnName: "LineNumber",
                KeyOrdinal: 2),
            new SqlServerDatabaseProvider.PrimaryKeyDiscoveryRow(
                ObjectId: 701,
                ConstraintName: "PK_OrderLines",
                SchemaName: "sales",
                TableName: "OrderLines",
                IsClustered: false,
                ColumnName: "OrderId",
                KeyOrdinal: 1),
        ];

        var result = SqlServerDatabaseProvider.NormalizePrimaryKeys(rows);

        result.Should().HaveCount(2);
        result[0].ConstraintName.Should().Be("PK_Customers");
        result[0].TableName.Should().Be("sales.Customers");
        result[0].SchemaName.Should().Be("sales");
        result[0].KeyColumns.Should().Equal("CustomerId");
        result[0].IsClustered.Should().BeTrue();
        result[0].ObjectId.Should().Be("601");

        result[1].ConstraintName.Should().Be("PK_OrderLines");
        result[1].KeyColumns.Should().Equal("OrderId", "LineNumber");
        result[1].IsClustered.Should().BeFalse();
    }

    [Fact]
    public void CreateViewObject_IncludesObjectIdAndDefinitionAvailability()
    {
        var view = SqlServerDatabaseProvider.CreateViewObject(
            objectId: 3001,
            schemaName: "reporting",
            viewName: "SalesSummary",
            hasDefinition: true);

        view.ObjectId.Should().Be("3001");
        view.SchemaName.Should().Be("reporting");
        view.ObjectName.Should().Be("SalesSummary");
        view.FullyQualifiedName.Should().Be("reporting.SalesSummary");
        view.ObjectType.Should().Be(DatabaseObjectType.View);
        view.HasDefinitionAvailable.Should().BeTrue();
        view.ProviderMetadata["objectId"].Should().Be(3001);
    }

    [Fact]
    public void CreateViewObject_WhenDefinitionUnavailable_SetsFlagToFalse()
    {
        var view = SqlServerDatabaseProvider.CreateViewObject(
            objectId: 3002,
            schemaName: "sys",
            viewName: "SomeSystemView",
            hasDefinition: false);

        view.HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void CreateDiscoverViewsCommand_UsesViewCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverViewsRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverViewsCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.views AS v");
        command.CommandText.Should().Contain("OBJECT_DEFINITION");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(p => p.ParameterName == "@IncludeSystemViews")
            .And.Contain(p => p.ParameterName == "@SchemaName");
        command.Parameters["@IncludeSystemViews"].Value.Should().Be(false);
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverViewsCommand_WhenIncludingSystemViews_SetsParameterToTrue()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverViewsRequest(IncludeSystemViews: true);

        using var command = SqlServerDatabaseProvider.CreateDiscoverViewsCommand(connection, request);

        command.Parameters["@IncludeSystemViews"].Value.Should().Be(true);
    }

    [Fact]
    public void CreateDiscoverViewsCommand_WhenSchemaFilterProvided_TrimsAndSetsParameter()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverViewsRequest(SchemaName: " analytics ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverViewsCommand(connection, request);

        command.Parameters["@SchemaName"].Value.Should().Be("analytics");
    }

    [Fact]
    public void NormalizeViews_ProjectsViewObjectsWithCorrectMetadata()
    {
        SqlServerDatabaseProvider.ViewDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 401,
                SchemaName: "sales",
                ViewName: "OrderSummary",
                HasDefinition: true),
            new SqlServerDatabaseProvider.ViewDiscoveryRow(
                ObjectId: 402,
                SchemaName: "analytics",
                ViewName: "MonthlyRevenue",
                HasDefinition: false),
        ];

        var result = SqlServerDatabaseProvider.NormalizeViews(rows);

        result.Should().HaveCount(2);
        result[0].ObjectId.Should().Be("401");
        result[0].SchemaName.Should().Be("sales");
        result[0].ObjectName.Should().Be("OrderSummary");
        result[0].HasDefinitionAvailable.Should().BeTrue();
        result[0].ProviderMetadata["objectId"].Should().Be(401);

        result[1].ObjectId.Should().Be("402");
        result[1].SchemaName.Should().Be("analytics");
        result[1].ObjectName.Should().Be("MonthlyRevenue");
        result[1].HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void CreateDiscoverTriggersCommand_UsesTriggerCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverTriggersRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverTriggersCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.triggers AS t");
        command.CommandText.Should().Contain("LEFT JOIN sys.trigger_events AS te");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(parameter => parameter.ParameterName == "@SchemaName")
            .And.Contain(parameter => parameter.ParameterName == "@ParentObjectName");
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@ParentObjectName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverTriggersCommand_WhenFiltersProvided_TrimsAndSetsParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverTriggersRequest(SchemaName: " sales ", ParentObjectName: " Orders ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverTriggersCommand(connection, request);

        command.Parameters["@SchemaName"].Value.Should().Be("sales");
        command.Parameters["@ParentObjectName"].Value.Should().Be("Orders");
    }

    [Fact]
    public void NormalizeTriggers_ProjectsDmlAndDatabaseTriggersWithFlags()
    {
        SqlServerDatabaseProvider.TriggerDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 610,
                TriggerName: "TRG_Orders_Audit",
                SchemaName: "sales",
                ParentObjectName: "Orders",
                ParentClass: 1,
                IsDisabled: false,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                TriggerEventType: "UPDATE"),
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 610,
                TriggerName: "TRG_Orders_Audit",
                SchemaName: "sales",
                ParentObjectName: "Orders",
                ParentClass: 1,
                IsDisabled: false,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                TriggerEventType: "INSERT"),
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 611,
                TriggerName: "TRG_Database_Audit",
                SchemaName: "dbo",
                ParentObjectName: "db",
                ParentClass: 0,
                IsDisabled: true,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: false,
                CreatedAt: null,
                TriggerEventType: "CREATE_TABLE"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTriggers(rows);

        result.Should().HaveCount(2);

        result[0].TriggerName.Should().Be("TRG_Orders_Audit");
        result[0].ParentObjectType.Should().Be(TriggerParentObjectType.Table);
        result[0].TriggerType.Should().Be(TriggerType.After | TriggerType.Insert | TriggerType.Update);
        result[0].IsEnabled.Should().BeTrue();
        result[0].HasDefinitionAvailable.Should().BeTrue();
        result[0].ObjectId.Should().Be("610");

        result[1].TriggerName.Should().Be("TRG_Database_Audit");
        result[1].ParentObjectType.Should().Be(TriggerParentObjectType.Database);
        result[1].TriggerType.Should().Be(TriggerType.After);
        result[1].IsEnabled.Should().BeFalse();
        result[1].HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void CreateTableObject_IncludesObjectIdAndRowCountMetadata()
    {
        var table = SqlServerDatabaseProvider.CreateTableObject(
            objectId: 2001,
            schemaName: "sales",
            tableName: "Orders",
            rowCount: 1500L);

        table.ObjectId.Should().Be("2001");
        table.SchemaName.Should().Be("sales");
        table.ObjectName.Should().Be("Orders");
        table.FullyQualifiedName.Should().Be("sales.Orders");
        table.ObjectType.Should().Be(DatabaseObjectType.Table);
        table.ProviderMetadata["objectId"].Should().Be(2001);
        table.ProviderMetadata["rowCount"].Should().Be(1500L);
    }

    [Fact]
    public void CreateDiscoverTablesCommand_UsesTableCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverTablesRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverTablesCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.tables AS t");
        command.CommandText.Should().Contain("sys.dm_db_partition_stats AS ps");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(p => p.ParameterName == "@IncludeSystemTables")
            .And.Contain(p => p.ParameterName == "@SchemaName");
        command.Parameters["@IncludeSystemTables"].Value.Should().Be(false);
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverTablesCommand_WhenIncludingSystemTables_SetsParameterToTrue()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverTablesRequest(IncludeSystemTables: true);

        using var command = SqlServerDatabaseProvider.CreateDiscoverTablesCommand(connection, request);

        command.Parameters["@IncludeSystemTables"].Value.Should().Be(true);
    }

    [Fact]
    public void CreateDiscoverTablesCommand_WhenSchemaFilterProvided_TrimsAndSetsParameter()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverTablesRequest(SchemaName: " sales ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverTablesCommand(connection, request);

        command.Parameters["@SchemaName"].Value.Should().Be("sales");
    }

    [Fact]
    public void NormalizeTables_ProjectsTableObjectsWithCorrectMetadata()
    {
        SqlServerDatabaseProvider.TableDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 501,
                SchemaName: "sales",
                TableName: "Orders",
                RowCount: 1000L),
            new SqlServerDatabaseProvider.TableDiscoveryRow(
                ObjectId: 502,
                SchemaName: "analytics",
                TableName: "Events",
                RowCount: 0L),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTables(rows);

        result.Should().HaveCount(2);
        result[0].ObjectId.Should().Be("501");
        result[0].SchemaName.Should().Be("sales");
        result[0].ObjectName.Should().Be("Orders");
        result[0].ProviderMetadata["objectId"].Should().Be(501);
        result[0].ProviderMetadata["rowCount"].Should().Be(1000L);

        result[1].ObjectId.Should().Be("502");
        result[1].SchemaName.Should().Be("analytics");
        result[1].ObjectName.Should().Be("Events");
        result[1].ProviderMetadata["rowCount"].Should().Be(0L);
    }

    private static DatabaseResource CreateResource(string providerName)
        => new("db", providerName, "Server=localhost;Database=db;", IsLocal: true, IsWritable: false);

    [Fact]
    public void CreateDiscoverConstraintsCommand_UsesConstraintCatalogQueryAndParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverConstraintsRequest();

        using var command = SqlServerDatabaseProvider.CreateDiscoverConstraintsCommand(connection, request);

        command.CommandText.Should().Contain("FROM sys.default_constraints AS dc");
        command.CommandText.Should().Contain("FROM sys.check_constraints AS cc");
        command.CommandText.Should().Contain("FROM sys.key_constraints AS kc");
        command.CommandText.Should().Contain("kc.type = N'UQ'");
        command.Parameters.Cast<SqlParameter>()
            .Should()
            .Contain(parameter => parameter.ParameterName == "@SchemaName")
            .And.Contain(parameter => parameter.ParameterName == "@TableName");
        command.Parameters["@SchemaName"].Value.Should().Be(DBNull.Value);
        command.Parameters["@TableName"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void CreateDiscoverConstraintsCommand_WhenFiltersProvided_TrimsAndSetsParameters()
    {
        using var connection = new SqlConnection();
        var request = new DiscoverConstraintsRequest(SchemaName: " sales ", TableName: " Orders ");

        using var command = SqlServerDatabaseProvider.CreateDiscoverConstraintsCommand(connection, request);

        command.Parameters["@SchemaName"].Value.Should().Be("sales");
        command.Parameters["@TableName"].Value.Should().Be("Orders");
    }

    [Theory]
    [InlineData("D", ConstraintType.Default)]
    [InlineData("C", ConstraintType.Check)]
    [InlineData("U", ConstraintType.Unique)]
    public void MapConstraintType_MapsExpectedTypeCodes(string typeCode, ConstraintType expected)
    {
        SqlServerDatabaseProvider.MapConstraintType(typeCode).Should().Be(expected);
    }

    [Fact]
    public void MapConstraintType_WhenUnknownCode_ThrowsArgumentException()
    {
        var action = () => SqlServerDatabaseProvider.MapConstraintType("X");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("typeCode");
    }

    [Fact]
    public void NormalizeConstraints_ProjectsAllConstraintTypesWithCorrectMetadata()
    {
        SqlServerDatabaseProvider.ConstraintDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 801,
                ConstraintName: "DF_Orders_Status",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: "Status",
                Definition: "('Pending')",
                IsDisabled: false,
                ConstraintTypeCode: "D"),
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 802,
                ConstraintName: "CK_Orders_Amount",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: "TotalAmount",
                Definition: "(TotalAmount > 0)",
                IsDisabled: false,
                ConstraintTypeCode: "C"),
            new SqlServerDatabaseProvider.ConstraintDiscoveryRow(
                ObjectId: 803,
                ConstraintName: "UQ_Orders_OrderNumber",
                SchemaName: "sales",
                TableName: "Orders",
                ColumnName: null,
                Definition: null,
                IsDisabled: false,
                ConstraintTypeCode: "U"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeConstraints(rows);

        result.Should().HaveCount(3);

        result[0].ConstraintName.Should().Be("DF_Orders_Status");
        result[0].ConstraintType.Should().Be(ConstraintType.Default);
        result[0].TableName.Should().Be("sales.Orders");
        result[0].SchemaName.Should().Be("sales");
        result[0].ColumnName.Should().Be("Status");
        result[0].Definition.Should().Be("('Pending')");
        result[0].IsDisabled.Should().BeFalse();
        result[0].ObjectId.Should().Be("801");

        result[1].ConstraintName.Should().Be("CK_Orders_Amount");
        result[1].ConstraintType.Should().Be(ConstraintType.Check);
        result[1].ColumnName.Should().Be("TotalAmount");
        result[1].Definition.Should().Be("(TotalAmount > 0)");

        result[2].ConstraintName.Should().Be("UQ_Orders_OrderNumber");
        result[2].ConstraintType.Should().Be(ConstraintType.Unique);
        result[2].ColumnName.Should().BeNull();
        result[2].Definition.Should().BeNull();
        result[2].ObjectId.Should().Be("803");
    }
}
