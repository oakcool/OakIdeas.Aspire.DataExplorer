using FluentAssertions;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Services;

namespace OakIdeas.Aspire.DataExplorer.Core.Tests;

public sealed class DataChangeTimelineServiceTests
{
    private static DataChangeEvent MakeEvent(
        string? sessionId = null,
        string? eventId = null,
        DataChangeOperation operation = DataChangeOperation.Insert,
        string databaseName = "TestDb",
        string schemaName = "dbo",
        string tableName = "Users",
        string? traceId = null,
        string? transactionId = null,
        DateTimeOffset? timestamp = null) => new(
        EventId: eventId ?? Guid.NewGuid().ToString("N"),
        SessionId: sessionId ?? "test-session",
        Timestamp: timestamp ?? DateTimeOffset.UtcNow,
        Operation: operation,
        DatabaseName: databaseName,
        SchemaName: schemaName,
        TableName: tableName,
        PrimaryKeyColumns: ["Id"],
        PrimaryKeyValues: new Dictionary<string, string?> { ["Id"] = "1" },
        Changes: new Dictionary<string, ColumnChange> { ["Name"] = new("Alice", "Bob") },
        TraceId: traceId,
        TransactionId: transactionId);

    // ── StartSession ──────────────────────────────────────────────────────────

    [Fact]
    public void StartSession_CreatesActiveSession()
    {
        var sut = new InMemoryChangeTimelineService();

        var session = sut.StartSession("TestDb");

        session.State.Should().Be(CaptureSessionState.Active);
        session.DatabaseName.Should().Be("TestDb");
        session.EventCount.Should().Be(0);
    }

    [Fact]
    public void StartSession_UsesSuppliedLabel()
    {
        var sut = new InMemoryChangeTimelineService();

        var session = sut.StartSession("TestDb", "My Label");

        session.Label.Should().Be("My Label");
    }

    [Fact]
    public void StartSession_GeneratesLabelWhenNotProvided()
    {
        var sut = new InMemoryChangeTimelineService();

        var session = sut.StartSession("TestDb");

        session.Label.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void StartSession_StopsPreviousActiveSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var first = sut.StartSession("Db1");

        sut.StartSession("Db2");

        var sessions = sut.Sessions;
        sessions.Should().HaveCount(2);
        sessions.First(s => s.SessionId == first.SessionId).State.Should().Be(CaptureSessionState.Stopped);
    }

    [Fact]
    public void StartSession_ThrowsOnNullOrWhiteSpaceDatabase()
    {
        var sut = new InMemoryChangeTimelineService();

        var act = () => sut.StartSession(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    // ── PauseSession ──────────────────────────────────────────────────────────

    [Fact]
    public void PauseSession_SetsStateToPaused()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");

        sut.PauseSession(session.SessionId);

        sut.Sessions.First().State.Should().Be(CaptureSessionState.Paused);
    }

    [Fact]
    public void PauseSession_ThrowsWhenAlreadyPaused()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.PauseSession(session.SessionId);

        var act = () => sut.PauseSession(session.SessionId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PauseSession_ThrowsForUnknownSession()
    {
        var sut = new InMemoryChangeTimelineService();

        var act = () => sut.PauseSession("nonexistent");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── ResumeSession ─────────────────────────────────────────────────────────

    [Fact]
    public void ResumeSession_SetsStateToActive()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.PauseSession(session.SessionId);

        sut.ResumeSession(session.SessionId);

        sut.Sessions.First().State.Should().Be(CaptureSessionState.Active);
    }

    [Fact]
    public void ResumeSession_ThrowsWhenNotPaused()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");

        var act = () => sut.ResumeSession(session.SessionId);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── StopSession ───────────────────────────────────────────────────────────

    [Fact]
    public void StopSession_SetsStateToStopped()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");

        sut.StopSession(session.SessionId);

        sut.Sessions.First().State.Should().Be(CaptureSessionState.Stopped);
    }

    [Fact]
    public void StopSession_ThrowsWhenAlreadyStopped()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.StopSession(session.SessionId);

        var act = () => sut.StopSession(session.SessionId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StopSession_CanStopPausedSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.PauseSession(session.SessionId);

        sut.StopSession(session.SessionId);

        sut.Sessions.First().State.Should().Be(CaptureSessionState.Stopped);
    }

    // ── DeleteSession ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteSession_RemovesSessionAndEvents()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.DeleteSession(session.SessionId);

        sut.Sessions.Should().BeEmpty();
        sut.TotalEventCount.Should().Be(0);
    }

    [Fact]
    public void DeleteSession_ThrowsForUnknownSession()
    {
        var sut = new InMemoryChangeTimelineService();

        var act = () => sut.DeleteSession("nonexistent");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── RecordEvent ───────────────────────────────────────────────────────────

    [Fact]
    public void RecordEvent_StoresSingleEvent()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");

        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.TotalEventCount.Should().Be(1);
    }

    [Fact]
    public void RecordEvent_ThrowsOnNull()
    {
        var sut = new InMemoryChangeTimelineService();

        var act = () => sut.RecordEvent(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordEvent_DropsEventsForPausedSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.PauseSession(session.SessionId);

        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.TotalEventCount.Should().Be(0);
    }

    [Fact]
    public void RecordEvent_DropsEventsForStoppedSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.StopSession(session.SessionId);

        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.TotalEventCount.Should().Be(0);
    }

    [Fact]
    public void RecordEvent_DropsEventsForUnknownSession()
    {
        var sut = new InMemoryChangeTimelineService();

        sut.RecordEvent(MakeEvent(sessionId: "unknown"));

        sut.TotalEventCount.Should().Be(0);
    }

    [Fact]
    public void RecordEvent_EvictsOldestEventWhenCapReached()
    {
        var sut = new InMemoryChangeTimelineService(maxEventsPerSession: 2);
        var session = sut.StartSession("TestDb");
        var first = MakeEvent(sessionId: session.SessionId, eventId: "first");
        var second = MakeEvent(sessionId: session.SessionId, eventId: "second");
        var third = MakeEvent(sessionId: session.SessionId, eventId: "third");

        sut.RecordEvent(first);
        sut.RecordEvent(second);
        sut.RecordEvent(third);

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest());
        result.Events.Should().HaveCount(2);
        result.Events.Should().NotContain(e => e.EventId == "first");
    }

    [Fact]
    public void RecordEvent_UpdatesEventCountOnSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");

        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.Sessions.First().EventCount.Should().Be(2);
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Query_ReturnsAllEventsWhenNoFilters()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, tableName: "Orders"));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, tableName: "Users"));

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest());

        result.Events.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.IsTruncated.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Query_FiltersByTableName()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, tableName: "Orders"));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, tableName: "Users"));

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest(TableName: "Orders"));

        result.Events.Should().HaveCount(1);
        result.Events[0].TableName.Should().Be("Orders");
    }

    [Fact]
    public void Query_FiltersByOperation()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, operation: DataChangeOperation.Insert));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, operation: DataChangeOperation.Delete));

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest(Operation: DataChangeOperation.Insert));

        result.Events.Should().HaveCount(1);
        result.Events[0].Operation.Should().Be(DataChangeOperation.Insert);
    }

    [Fact]
    public void Query_FiltersByTraceId()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, traceId: "trace-aaa"));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, traceId: "trace-bbb"));

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest(TraceId: "trace-aaa"));

        result.Events.Should().HaveCount(1);
        result.Events[0].TraceId.Should().Be("trace-aaa");
    }

    [Fact]
    public void Query_FiltersBySince()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        var cutoff = DateTimeOffset.UtcNow;
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, timestamp: cutoff.AddSeconds(-10)));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, timestamp: cutoff.AddSeconds(5)));

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest(Since: cutoff));

        result.Events.Should().HaveCount(1);
    }

    [Fact]
    public void Query_ReturnsErrorForUnknownSession()
    {
        var sut = new InMemoryChangeTimelineService();

        var result = sut.Query("nonexistent", new DataChangeQueryRequest());

        result.Error.Should().NotBeNullOrWhiteSpace();
        result.Events.Should().BeEmpty();
    }

    [Fact]
    public void Query_ReturnsMostRecentEventFirst()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        var now = DateTimeOffset.UtcNow;
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, eventId: "older", timestamp: now.AddSeconds(-5)));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, eventId: "newer", timestamp: now));

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest());

        result.Events[0].EventId.Should().Be("newer");
        result.Events[1].EventId.Should().Be("older");
    }

    [Fact]
    public void Query_TruncatesResultsWhenMaxEventsExceeded()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");

        for (var i = 0; i < 5; i++)
        {
            sut.RecordEvent(MakeEvent(sessionId: session.SessionId));
        }

        var result = sut.Query(session.SessionId, new DataChangeQueryRequest(MaxEvents: 3));

        result.Events.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.IsTruncated.Should().BeTrue();
    }

    // ── ClearEvents ───────────────────────────────────────────────────────────

    [Fact]
    public void ClearEvents_RemovesAllEventsFromSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.ClearEvents(session.SessionId);

        sut.TotalEventCount.Should().Be(0);
        sut.Sessions.First().EventCount.Should().Be(0);
    }

    [Fact]
    public void ClearEvents_DoesNotDeleteSession()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId));

        sut.ClearEvents(session.SessionId);

        sut.Sessions.Should().HaveCount(1);
    }

    // ── GetTableNames ─────────────────────────────────────────────────────────

    [Fact]
    public void GetTableNames_ReturnsDistinctSortedSchemaTableNames()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, schemaName: "dbo", tableName: "Orders"));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, schemaName: "dbo", tableName: "Users"));
        sut.RecordEvent(MakeEvent(sessionId: session.SessionId, schemaName: "dbo", tableName: "Orders"));

        var names = sut.GetTableNames(session.SessionId);

        names.Should().BeEquivalentTo(["dbo.Orders", "dbo.Users"]);
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetTableNames_ReturnsEmptyForUnknownSession()
    {
        var sut = new InMemoryChangeTimelineService();

        var names = sut.GetTableNames("nonexistent");

        names.Should().BeEmpty();
    }

    // ── ActiveSession ─────────────────────────────────────────────────────────

    [Fact]
    public void ActiveSession_ReturnsNullWhenNoSessionStarted()
    {
        var sut = new InMemoryChangeTimelineService();

        sut.ActiveSession.Should().BeNull();
    }

    [Fact]
    public void ActiveSession_ReturnsNullAfterAllSessionsStopped()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.StopSession(session.SessionId);

        sut.ActiveSession.Should().BeNull();
    }

    [Fact]
    public void ActiveSession_ReturnsPausedSessionAsActive()
    {
        var sut = new InMemoryChangeTimelineService();
        var session = sut.StartSession("TestDb");
        sut.PauseSession(session.SessionId);

        sut.ActiveSession.Should().NotBeNull();
        sut.ActiveSession!.State.Should().Be(CaptureSessionState.Paused);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsWhenMaxEventsLessThanOne()
    {
        var act = () => new InMemoryChangeTimelineService(maxEventsPerSession: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
