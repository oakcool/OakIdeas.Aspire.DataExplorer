using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.SqlServer.Providers;

namespace OakIdeas.Aspire.DataExplorer.IntegrationTests;

public sealed class TriggerDiscoveryIntegrationTests
{
    [Fact]
    public void NormalizeTriggers_TableLevelInsertUpdateDeleteTrigger_IsProjected()
    {
        SqlServerDatabaseProvider.TriggerDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 701,
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
                ObjectId: 701,
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
                ObjectId: 701,
                TriggerName: "TRG_Orders_Audit",
                SchemaName: "sales",
                ParentObjectName: "Orders",
                ParentClass: 1,
                IsDisabled: false,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                TriggerEventType: "DELETE"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTriggers(rows);

        result.Should().ContainSingle();
        result[0].TriggerType.Should().Be(TriggerType.After | TriggerType.Insert | TriggerType.Update | TriggerType.Delete);
        result[0].ParentObjectType.Should().Be(TriggerParentObjectType.Table);
        result[0].IsEnabled.Should().BeTrue();
        result[0].HasDefinitionAvailable.Should().BeTrue();
    }

    [Fact]
    public void NormalizeTriggers_InsteadOfTrigger_IsProjected()
    {
        SqlServerDatabaseProvider.TriggerDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 702,
                TriggerName: "TRG_Products_InsteadOfDelete",
                SchemaName: "inventory",
                ParentObjectName: "Products",
                ParentClass: 1,
                IsDisabled: false,
                IsInsteadOfTrigger: true,
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                TriggerEventType: "DELETE"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTriggers(rows);

        result.Should().ContainSingle();
        result[0].TriggerType.Should().Be(TriggerType.InsteadOf | TriggerType.Delete);
        result[0].ParentObjectType.Should().Be(TriggerParentObjectType.Table);
    }

    [Fact]
    public void NormalizeTriggers_AfterTrigger_IsProjected()
    {
        SqlServerDatabaseProvider.TriggerDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 703,
                TriggerName: "TRG_Orders_AfterInsert",
                SchemaName: "sales",
                ParentObjectName: "Orders",
                ParentClass: 1,
                IsDisabled: false,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                TriggerEventType: "INSERT"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTriggers(rows);

        result.Should().ContainSingle();
        result[0].TriggerType.Should().Be(TriggerType.After | TriggerType.Insert);
    }

    [Fact]
    public void NormalizeTriggers_DisabledTrigger_IsProjectedAsDisabled()
    {
        SqlServerDatabaseProvider.TriggerDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 704,
                TriggerName: "TRG_Orders_Disabled",
                SchemaName: "sales",
                ParentObjectName: "Orders",
                ParentClass: 1,
                IsDisabled: true,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: false,
                CreatedAt: null,
                TriggerEventType: "UPDATE"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTriggers(rows);

        result.Should().ContainSingle();
        result[0].IsEnabled.Should().BeFalse();
        result[0].HasDefinitionAvailable.Should().BeFalse();
    }

    [Fact]
    public void NormalizeTriggers_DatabaseLevelTrigger_IsProjectedWithDatabaseParent()
    {
        SqlServerDatabaseProvider.TriggerDiscoveryRow[] rows =
        [
            new SqlServerDatabaseProvider.TriggerDiscoveryRow(
                ObjectId: 705,
                TriggerName: "TRG_Database_CreateTableAudit",
                SchemaName: "dbo",
                ParentObjectName: "AppDb",
                ParentClass: 0,
                IsDisabled: false,
                IsInsteadOfTrigger: false,
                HasDefinitionAvailable: true,
                CreatedAt: new DateTime(2026, 5, 16, 0, 0, 0),
                TriggerEventType: "CREATE_TABLE"),
        ];

        var result = SqlServerDatabaseProvider.NormalizeTriggers(rows);

        result.Should().ContainSingle();
        result[0].ParentObjectType.Should().Be(TriggerParentObjectType.Database);
        result[0].TriggerType.Should().Be(TriggerType.After);
    }
}
