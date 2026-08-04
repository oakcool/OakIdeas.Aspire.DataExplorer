using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class InMemoryScenarioBuilderServiceTests
{
    // ── CreateScenario ────────────────────────────────────────────────────────

    [Fact]
    public void CreateScenario_WithValidName_ReturnsSuccess()
    {
        var svc = new InMemoryScenarioBuilderService();
        var req = new CreateScenarioRequest("My Scenario", null, null, []);

        var result = svc.CreateScenario(req);

        result.Success.Should().BeTrue();
        result.Scenario.Should().NotBeNull();
        result.Scenario!.Name.Should().Be("My Scenario");
        result.Scenario.ScenarioId.Should().NotBeNullOrWhiteSpace();
        result.Scenario.Version.Should().Be(1);
        result.Scenario.Tables.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CreateScenario_WithEmptyName_ReturnsFailure()
    {
        var svc = new InMemoryScenarioBuilderService();
        var req = new CreateScenarioRequest("", null, null, []);

        var result = svc.CreateScenario(req);

        result.Success.Should().BeFalse();
        result.Scenario.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateScenario_WithWhitespaceName_ReturnsFailure()
    {
        var svc = new InMemoryScenarioBuilderService();
        var req = new CreateScenarioRequest("   ", null, null, []);

        var result = svc.CreateScenario(req);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void CreateScenario_AppearsInScenariosList()
    {
        var svc = new InMemoryScenarioBuilderService();

        svc.CreateScenario(new CreateScenarioRequest("First", null, null, []));
        svc.CreateScenario(new CreateScenarioRequest("Second", null, null, []));

        svc.Scenarios.Should().HaveCount(2);
    }

    [Fact]
    public void CreateScenario_StoresSeedAndDescription()
    {
        var svc = new InMemoryScenarioBuilderService();
        var req = new CreateScenarioRequest("Seeded", "A description", 42, []);

        var result = svc.CreateScenario(req);

        result.Scenario!.Seed.Should().Be(42);
        result.Scenario.Description.Should().Be("A description");
    }

    [Fact]
    public void CreateScenario_WithTables_StoresOperations()
    {
        var svc = new InMemoryScenarioBuilderService();
        var tables = new List<ScenarioTableOperation>
        {
            new("dbo", "Customers", "customer", [
                new ScenarioColumnValue("Name", ScenarioValueKind.Fixed, FixedValue: "Test"),
            ]),
        };

        var result = svc.CreateScenario(new CreateScenarioRequest("With Tables", null, null, tables));

        result.Scenario!.Tables.Should().HaveCount(1);
        result.Scenario.Tables[0].TableName.Should().Be("Customers");
        result.Scenario.Tables[0].Alias.Should().Be("customer");
        result.Scenario.Tables[0].Columns.Should().HaveCount(1);
    }

    // ── GetScenario ───────────────────────────────────────────────────────────

    [Fact]
    public void GetScenario_ExistingId_ReturnsScenario()
    {
        var svc = new InMemoryScenarioBuilderService();
        var created = svc.CreateScenario(new CreateScenarioRequest("Find Me", null, null, [])).Scenario!;

        var found = svc.GetScenario(created.ScenarioId);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Find Me");
    }

    [Fact]
    public void GetScenario_UnknownId_ReturnsNull()
    {
        var svc = new InMemoryScenarioBuilderService();

        var result = svc.GetScenario("does-not-exist");

        result.Should().BeNull();
    }

    // ── UpdateScenario ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateScenario_ExistingId_UpdatesFields()
    {
        var svc = new InMemoryScenarioBuilderService();
        var created = svc.CreateScenario(new CreateScenarioRequest("Original", null, null, [])).Scenario!;

        var updated = svc.UpdateScenario(created.ScenarioId,
            new CreateScenarioRequest("Renamed", "new desc", 7, []));

        updated.Success.Should().BeTrue();
        updated.Scenario!.Name.Should().Be("Renamed");
        updated.Scenario.Description.Should().Be("new desc");
        updated.Scenario.Seed.Should().Be(7);
        updated.Scenario.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateScenario_UnknownId_ReturnsFailure()
    {
        var svc = new InMemoryScenarioBuilderService();

        var result = svc.UpdateScenario("no-such-id", new CreateScenarioRequest("X", null, null, []));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no-such-id");
    }

    [Fact]
    public void UpdateScenario_EmptyName_ReturnsFailure()
    {
        var svc = new InMemoryScenarioBuilderService();
        var created = svc.CreateScenario(new CreateScenarioRequest("Original", null, null, [])).Scenario!;

        var result = svc.UpdateScenario(created.ScenarioId, new CreateScenarioRequest("", null, null, []));

        result.Success.Should().BeFalse();
    }

    // ── DeleteScenario ────────────────────────────────────────────────────────

    [Fact]
    public void DeleteScenario_ExistingId_RemovesFromList()
    {
        var svc = new InMemoryScenarioBuilderService();
        var created = svc.CreateScenario(new CreateScenarioRequest("Delete Me", null, null, [])).Scenario!;

        svc.DeleteScenario(new DeleteScenarioRequest(created.ScenarioId));

        svc.Scenarios.Should().BeEmpty();
        svc.GetScenario(created.ScenarioId).Should().BeNull();
    }

    [Fact]
    public void DeleteScenario_UnknownId_IsNoOp()
    {
        var svc = new InMemoryScenarioBuilderService();
        svc.CreateScenario(new CreateScenarioRequest("Keep Me", null, null, []));

        svc.DeleteScenario(new DeleteScenarioRequest("ghost-id"));

        svc.Scenarios.Should().HaveCount(1);
    }

    // ── ExecuteScenarioAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteScenario_UnknownId_ReturnsFailure()
    {
        var svc = new InMemoryScenarioBuilderService();

        var result = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest("ghost"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ghost");
    }

    [Fact]
    public async Task ExecuteScenario_WithFixedValues_RecordsInsertedRow()
    {
        var svc = new InMemoryScenarioBuilderService();
        var tables = new List<ScenarioTableOperation>
        {
            new("dbo", "Users", null, [
                new ScenarioColumnValue("Name", ScenarioValueKind.Fixed, FixedValue: "Alice"),
            ]),
        };

        var scenario = svc.CreateScenario(new CreateScenarioRequest("Insert User", null, null, tables)).Scenario!;

        var result = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.InsertedRows.Should().ContainKey("dbo.Users");
        result.InsertedRows["dbo.Users"].Should().Be(1);
    }

    [Fact]
    public async Task ExecuteScenario_WithGeneratedGuid_CapturesKeyUnderAlias()
    {
        var svc = new InMemoryScenarioBuilderService();
        var tables = new List<ScenarioTableOperation>
        {
            new("dbo", "Customers", "cust1", [
                new ScenarioColumnValue("Id", ScenarioValueKind.Generated, GeneratorName: "guid"),
            ]),
        };

        var scenario = svc.CreateScenario(new CreateScenarioRequest("Gen Guid", null, null, tables)).Scenario!;

        var result = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.GeneratedKeys.Should().ContainKey("cust1");
        result.GeneratedKeys["cust1"].Should().MatchRegex(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
    }

    [Fact]
    public async Task ExecuteScenario_WithReferenceColumn_ResolvesFromPriorAlias()
    {
        var svc = new InMemoryScenarioBuilderService();
        var tables = new List<ScenarioTableOperation>
        {
            new("dbo", "Customers", "cust1", [
                new ScenarioColumnValue("Id", ScenarioValueKind.Generated, GeneratorName: "guid"),
            ]),
            new("dbo", "Orders", null, [
                new ScenarioColumnValue("CustomerId", ScenarioValueKind.Reference, ReferenceAlias: "cust1"),
            ]),
        };

        var scenario = svc.CreateScenario(
            new CreateScenarioRequest("Customer + Order", null, null, tables)).Scenario!;

        var result = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.GeneratedKeys.Should().ContainKey("cust1");
        result.InsertedRows.Should().ContainKey("dbo.Customers");
        result.InsertedRows.Should().ContainKey("dbo.Orders");
    }

    [Fact]
    public async Task ExecuteScenario_UpdatesLastExecutedAtOnScenario()
    {
        var svc = new InMemoryScenarioBuilderService();
        var scenario = svc.CreateScenario(new CreateScenarioRequest("Timestamped", null, null, [])).Scenario!;
        scenario.LastExecutedAt.Should().BeNull();

        await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId), CancellationToken.None);

        var refreshed = svc.GetScenario(scenario.ScenarioId)!;
        refreshed.LastExecutedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteScenario_WithDeterministicSeed_IsRepeatable()
    {
        var svc = new InMemoryScenarioBuilderService();
        var tables = new List<ScenarioTableOperation>
        {
            new("dbo", "Items", "item1", [
                new ScenarioColumnValue("Tag", ScenarioValueKind.Generated, GeneratorName: "randomstring(8)"),
            ]),
        };

        var scenario = svc.CreateScenario(
            new CreateScenarioRequest("Seeded", null, 42, tables)).Scenario!;

        var r1 = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId, SeedOverride: 42), CancellationToken.None);

        var r2 = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId, SeedOverride: 42), CancellationToken.None);

        r1.GeneratedKeys["item1"].Should().Be(r2.GeneratedKeys["item1"]);
    }

    [Fact]
    public async Task ExecuteScenario_MultipleTablesSameSchema_AccumulatesInsertCounts()
    {
        var svc = new InMemoryScenarioBuilderService();
        var tables = new List<ScenarioTableOperation>
        {
            new("dbo", "Orders", null, [new ScenarioColumnValue("Num", ScenarioValueKind.Fixed, FixedValue: "1")]),
            new("dbo", "Orders", null, [new ScenarioColumnValue("Num", ScenarioValueKind.Fixed, FixedValue: "2")]),
        };

        var scenario = svc.CreateScenario(
            new CreateScenarioRequest("Two Orders", null, null, tables)).Scenario!;

        var result = await svc.ExecuteScenarioAsync(
            new ExecuteScenarioRequest(scenario.ScenarioId), CancellationToken.None);

        result.InsertedRows["dbo.Orders"].Should().Be(2);
    }

    // ── Null-guard ────────────────────────────────────────────────────────────

    [Fact]
    public void CreateScenario_NullRequest_Throws()
    {
        var svc = new InMemoryScenarioBuilderService();
        var act = () => svc.CreateScenario(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetScenario_NullOrEmpty_Throws()
    {
        var svc = new InMemoryScenarioBuilderService();
        var act = () => svc.GetScenario(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeleteScenario_NullRequest_Throws()
    {
        var svc = new InMemoryScenarioBuilderService();
        var act = () => svc.DeleteScenario(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
