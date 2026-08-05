using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class InMemorySnapshotServiceTests
{
    // ── CreateSnapshotAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateSnapshotAsync_ReturnsSuccessfulResponse()
    {
        var sut = new InMemorySnapshotService();

        var response = await sut.CreateSnapshotAsync(new CreateSnapshotRequest
        {
            DatabaseName = "TestDb",
            Name = "Before feature work",
        });

        response.Success.Should().BeTrue();
        response.Snapshot.Should().NotBeNull();
        response.Snapshot!.Name.Should().Be("Before feature work");
        response.Snapshot.DatabaseName.Should().Be("TestDb");
        response.Snapshot.State.Should().Be(SnapshotState.Available);
        response.Snapshot.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateSnapshotAsync_AssignsUniqueId()
    {
        var sut = new InMemorySnapshotService();

        var r1 = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "S1" });
        var r2 = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "S2" });

        r1.Snapshot!.Id.Should().NotBe(r2.Snapshot!.Id);
    }

    [Fact]
    public async Task CreateSnapshotAsync_StoresOptionalNotes()
    {
        var sut = new InMemorySnapshotService();

        var response = await sut.CreateSnapshotAsync(new CreateSnapshotRequest
        {
            DatabaseName = "Db",
            Name = "Checkpoint",
            Notes = "Before migration run",
        });

        response.Snapshot!.Notes.Should().Be("Before migration run");
    }

    [Fact]
    public async Task CreateSnapshotAsync_ThrowsOnNullDatabaseName()
    {
        var sut = new InMemorySnapshotService();

        var act = () => sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = null!, Name = "N" });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetSnapshots ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSnapshots_ReturnsMostRecentFirst()
    {
        var sut = new InMemorySnapshotService();

        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "First" });
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "Second" });

        var snapshots = sut.GetSnapshots("Db");

        snapshots.Should().HaveCount(2);
        snapshots[0].Name.Should().Be("Second");
        snapshots[1].Name.Should().Be("First");
    }

    [Fact]
    public async Task GetSnapshots_ReturnsEmptyListForUnknownDatabase()
    {
        var sut = new InMemorySnapshotService();
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "OtherDb", Name = "S" });

        sut.GetSnapshots("UnknownDb").Should().BeEmpty();
    }

    [Fact]
    public async Task GetSnapshots_FiltersToRequestedDatabase()
    {
        var sut = new InMemorySnapshotService();
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "DbA", Name = "A1" });
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "DbB", Name = "B1" });

        sut.GetSnapshots("DbA").Should().HaveCount(1).And.Contain(s => s.Name == "A1");
    }

    // ── GetAllSnapshots ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSnapshots_ReturnsSnapshotsFromAllDatabases()
    {
        var sut = new InMemorySnapshotService();
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "DbA", Name = "A1" });
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "DbB", Name = "B1" });

        sut.GetAllSnapshots().Should().HaveCount(2);
    }

    // ── GetSnapshot ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSnapshot_ReturnsNullForUnknownId()
    {
        var sut = new InMemorySnapshotService();

        sut.GetSnapshot("nonexistent").Should().BeNull();
    }

    [Fact]
    public async Task GetSnapshot_ReturnsByIdCaseInsensitive()
    {
        var sut = new InMemorySnapshotService();
        var response = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "S" });
        var id = response.Snapshot!.Id;

        sut.GetSnapshot(id.ToUpperInvariant()).Should().NotBeNull();
    }

    // ── RenameSnapshot ────────────────────────────────────────────────────────

    [Fact]
    public async Task RenameSnapshot_UpdatesName()
    {
        var sut = new InMemorySnapshotService();
        var response = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "Old" });
        var id = response.Snapshot!.Id;

        sut.RenameSnapshot(new RenameSnapshotRequest { SnapshotId = id, NewName = "New Name" });

        sut.GetSnapshot(id)!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task RenameSnapshot_ThrowsForUnknownId()
    {
        var sut = new InMemorySnapshotService();

        var act = () => sut.RenameSnapshot(new RenameSnapshotRequest { SnapshotId = "missing", NewName = "X" });

        act.Should().Throw<InvalidOperationException>();
    }

    // ── DeleteSnapshot ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSnapshot_RemovesSnapshot()
    {
        var sut = new InMemorySnapshotService();
        var response = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "S" });
        var id = response.Snapshot!.Id;

        sut.DeleteSnapshot(new DeleteSnapshotRequest { SnapshotId = id });

        sut.GetSnapshot(id).Should().BeNull();
        sut.TotalSnapshotCount.Should().Be(0);
    }

    [Fact]
    public void DeleteSnapshot_SilentlySucceedsForUnknownId()
    {
        var sut = new InMemorySnapshotService();

        var act = () => sut.DeleteSnapshot(new DeleteSnapshotRequest { SnapshotId = "nonexistent" });

        act.Should().NotThrow();
    }

    // ── CompareSnapshotAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CompareSnapshotAsync_ReturnsSuccessForExistingSnapshot()
    {
        var sut = new InMemorySnapshotService();
        var created = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "S" });

        var result = await sut.CompareSnapshotAsync(new CompareSnapshotRequest
        {
            SnapshotId = created.Snapshot!.Id,
        });

        result.Success.Should().BeTrue();
        result.Snapshot.Should().NotBeNull();
        result.TableDiffs.Should().BeEmpty();
    }

    [Fact]
    public async Task CompareSnapshotAsync_ReturnsErrorForUnknownId()
    {
        var sut = new InMemorySnapshotService();

        var result = await sut.CompareSnapshotAsync(new CompareSnapshotRequest
        {
            SnapshotId = "missing",
        });

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be(ErrorCategory.ResourceNotFound);
    }

    // ── RestoreSnapshotAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RestoreSnapshotAsync_DryRun_ReturnsSuccessWithoutChangingData()
    {
        var sut = new InMemorySnapshotService();
        var created = await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "Db", Name = "S" });

        var result = await sut.RestoreSnapshotAsync(new RestoreSnapshotRequest
        {
            SnapshotId = created.Snapshot!.Id,
            DryRun = true,
        });

        result.Success.Should().BeTrue();
        result.WasDryRun.Should().BeTrue();
        result.Summary.Should().NotBeNullOrWhiteSpace();
        sut.TotalSnapshotCount.Should().Be(1, "dry run must not delete snapshots");
    }

    [Fact]
    public async Task RestoreSnapshotAsync_ReturnsErrorForUnknownId()
    {
        var sut = new InMemorySnapshotService();

        var result = await sut.RestoreSnapshotAsync(new RestoreSnapshotRequest
        {
            SnapshotId = "missing",
        });

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be(ErrorCategory.ResourceNotFound);
    }

    // ── TotalSnapshotCount ────────────────────────────────────────────────────

    [Fact]
    public async Task TotalSnapshotCount_ReflectsAllSnapshots()
    {
        var sut = new InMemorySnapshotService();

        sut.TotalSnapshotCount.Should().Be(0);

        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "A", Name = "1" });
        await sut.CreateSnapshotAsync(new CreateSnapshotRequest { DatabaseName = "B", Name = "2" });

        sut.TotalSnapshotCount.Should().Be(2);
    }
}
