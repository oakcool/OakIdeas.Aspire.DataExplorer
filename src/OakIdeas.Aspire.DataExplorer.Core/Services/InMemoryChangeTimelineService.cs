using System.Collections.Concurrent;
using OakIdeas.Aspire.DataExplorer.Contracts.Models;
using OakIdeas.Aspire.DataExplorer.Core.Abstractions;

namespace OakIdeas.Aspire.DataExplorer.Core.Services;

/// <summary>
/// Thread-safe, bounded in-memory implementation of <see cref="IChangeTimelineService"/>.
/// State is not persisted across restarts. Intended for development-time use only.
/// </summary>
public sealed class InMemoryChangeTimelineService : IChangeTimelineService
{
    /// <summary>Default maximum number of events to retain per session.</summary>
    public const int DefaultMaxEventsPerSession = 5_000;

    private readonly int _maxEventsPerSession;
    private readonly Lock _lock = new();

    private readonly LinkedList<CaptureSession> _sessions = new();
    private readonly ConcurrentDictionary<string, LinkedList<DataChangeEvent>> _eventsBySession = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialises a new instance with the specified per-session event cap.
    /// </summary>
    public InMemoryChangeTimelineService(int maxEventsPerSession = DefaultMaxEventsPerSession)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxEventsPerSession, 1);
        _maxEventsPerSession = maxEventsPerSession;
    }

    /// <inheritdoc />
    public CaptureSession? ActiveSession
    {
        get
        {
            lock (_lock)
            {
                return _sessions.FirstOrDefault(s => s.State != CaptureSessionState.Stopped);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CaptureSession> Sessions
    {
        get
        {
            lock (_lock) { return [.. _sessions]; }
        }
    }

    /// <inheritdoc />
    public int TotalEventCount
    {
        get
        {
            var total = 0;
            foreach (var list in _eventsBySession.Values)
            {
                lock (_lock) { total += list.Count; }
            }

            return total;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTableNames(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_eventsBySession.TryGetValue(sessionId, out var events))
        {
            return [];
        }

        lock (_lock)
        {
            return events
                .Select(e => $"{e.SchemaName}.{e.TableName}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <inheritdoc />
    public CaptureSession StartSession(string databaseName, string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        lock (_lock)
        {
            // Stop any currently active session before starting a new one.
            var existing = _sessions.FirstOrDefault(s => s.State != CaptureSessionState.Stopped);
            if (existing is not null)
            {
                var stopped = existing with { State = CaptureSessionState.Stopped, StoppedAt = DateTimeOffset.UtcNow };
                ReplaceSession(existing, stopped);
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var now = DateTimeOffset.UtcNow;
            var effectiveLabel = string.IsNullOrWhiteSpace(label)
                ? now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : label;

            var session = new CaptureSession(
                SessionId: sessionId,
                Label: effectiveLabel,
                DatabaseName: databaseName,
                StartedAt: now,
                PausedAt: null,
                StoppedAt: null,
                State: CaptureSessionState.Active,
                EventCount: 0);

            _sessions.AddFirst(session);
            _eventsBySession[sessionId] = new LinkedList<DataChangeEvent>();
            return session;
        }
    }

    /// <inheritdoc />
    public void PauseSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            var session = FindSession(sessionId);
            if (session.State != CaptureSessionState.Active)
            {
                throw new InvalidOperationException(
                    $"Session '{sessionId}' cannot be paused; current state is '{session.State}'.");
            }

            var updated = session with { State = CaptureSessionState.Paused, PausedAt = DateTimeOffset.UtcNow };
            ReplaceSession(session, updated);
        }
    }

    /// <inheritdoc />
    public void ResumeSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            var session = FindSession(sessionId);
            if (session.State != CaptureSessionState.Paused)
            {
                throw new InvalidOperationException(
                    $"Session '{sessionId}' cannot be resumed; current state is '{session.State}'.");
            }

            var updated = session with { State = CaptureSessionState.Active, PausedAt = null };
            ReplaceSession(session, updated);
        }
    }

    /// <inheritdoc />
    public void StopSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            var session = FindSession(sessionId);
            if (session.State == CaptureSessionState.Stopped)
            {
                throw new InvalidOperationException(
                    $"Session '{sessionId}' is already stopped.");
            }

            var updated = session with { State = CaptureSessionState.Stopped, StoppedAt = DateTimeOffset.UtcNow };
            ReplaceSession(session, updated);
        }
    }

    /// <inheritdoc />
    public void DeleteSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            var node = FindNode(sessionId);
            _sessions.Remove(node);
        }

        _eventsBySession.TryRemove(sessionId, out _);
    }

    /// <inheritdoc />
    public void ClearEvents(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            if (_eventsBySession.TryGetValue(sessionId, out var events))
            {
                events.Clear();
            }

            // Update the event count on the session record.
            var session = _sessions.FirstOrDefault(s =>
                string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));
            if (session is not null)
            {
                var updated = session with { EventCount = 0 };
                ReplaceSession(session, updated);
            }
        }
    }

    /// <inheritdoc />
    public void RecordEvent(DataChangeEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s =>
                string.Equals(s.SessionId, evt.SessionId, StringComparison.OrdinalIgnoreCase));

            // Silently drop events for unknown, paused, or stopped sessions.
            if (session is null || session.State != CaptureSessionState.Active)
            {
                return;
            }

            if (!_eventsBySession.TryGetValue(evt.SessionId, out var events))
            {
                return;
            }

            events.AddLast(evt);

            // Evict oldest events when the per-session cap is reached.
            while (events.Count > _maxEventsPerSession)
            {
                events.RemoveFirst();
            }

            var updated = session with { EventCount = events.Count };
            ReplaceSession(session, updated);
        }
    }

    /// <inheritdoc />
    public DataChangeQueryResponse Query(string sessionId, DataChangeQueryRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        if (!_eventsBySession.TryGetValue(sessionId, out var eventList))
        {
            return new DataChangeQueryResponse([], 0, false, $"Session '{sessionId}' not found.");
        }

        DataChangeEvent[] snapshot;
        lock (_lock) { snapshot = [.. eventList]; }

        var filtered = (IEnumerable<DataChangeEvent>)snapshot;

        if (request.TableName is not null)
        {
            filtered = filtered.Where(e =>
                string.Equals(e.TableName, request.TableName, StringComparison.OrdinalIgnoreCase));
        }

        if (request.SchemaName is not null)
        {
            filtered = filtered.Where(e =>
                string.Equals(e.SchemaName, request.SchemaName, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Operation is not null)
        {
            filtered = filtered.Where(e => e.Operation == request.Operation.Value);
        }

        if (request.TraceId is not null)
        {
            filtered = filtered.Where(e =>
                string.Equals(e.TraceId, request.TraceId, StringComparison.OrdinalIgnoreCase));
        }

        if (request.TransactionId is not null)
        {
            filtered = filtered.Where(e =>
                string.Equals(e.TransactionId, request.TransactionId, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Since is not null)
        {
            filtered = filtered.Where(e => e.Timestamp >= request.Since.Value);
        }

        var all = filtered.OrderByDescending(e => e.Timestamp).ToArray();
        var totalCount = all.Length;

        var cap = request.MaxEvents ?? 500;
        var truncated = totalCount > cap;
        var page = truncated ? all.AsSpan(0, cap).ToArray() : all;

        return new DataChangeQueryResponse(page, totalCount, truncated);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Must be called while holding _lock.
    private CaptureSession FindSession(string sessionId)
    {
        var session = _sessions.FirstOrDefault(s =>
            string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));

        return session ?? throw new InvalidOperationException($"Session '{sessionId}' not found.");
    }

    // Must be called while holding _lock.
    private LinkedListNode<CaptureSession> FindNode(string sessionId)
    {
        var node = _sessions.First;
        while (node is not null)
        {
            if (string.Equals(node.Value.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            node = node.Next;
        }

        throw new InvalidOperationException($"Session '{sessionId}' not found.");
    }

    // Must be called while holding _lock.
    private void ReplaceSession(CaptureSession original, CaptureSession replacement)
    {
        var node = _sessions.First;
        while (node is not null)
        {
            if (ReferenceEquals(node.Value, original))
            {
                node.Value = replacement;
                return;
            }

            node = node.Next;
        }
    }
}
