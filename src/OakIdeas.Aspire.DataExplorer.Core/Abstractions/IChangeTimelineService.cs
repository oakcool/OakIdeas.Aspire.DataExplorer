using OakIdeas.Aspire.DataExplorer.Contracts.Models;

namespace OakIdeas.Aspire.DataExplorer.Core.Abstractions;

/// <summary>
/// Manages data change capture sessions and the event store for the Data Change Timeline feature.
/// Implementations must be thread-safe; the service is registered as a singleton.
/// </summary>
public interface IChangeTimelineService
{
    /// <summary>
    /// Returns the currently active (non-stopped) session, or <see langword="null"/> when no session is active.
    /// </summary>
    CaptureSession? ActiveSession { get; }

    /// <summary>
    /// Returns all capture sessions in reverse start order (most recent first).
    /// </summary>
    IReadOnlyList<CaptureSession> Sessions { get; }

    /// <summary>
    /// Returns the distinct table names (in <c>schema.table</c> form) across all events in the given session,
    /// sorted ascending. Returns an empty list when the session does not exist.
    /// </summary>
    IReadOnlyList<string> GetTableNames(string sessionId);

    /// <summary>
    /// Starts a new capture session for the specified database.
    /// Only one session may be active at a time; calling this method while a session is already active
    /// stops the previous session before starting the new one.
    /// </summary>
    /// <param name="databaseName">The database to monitor.</param>
    /// <param name="label">Optional developer-supplied label. Defaults to a timestamp when <see langword="null"/>.</param>
    /// <returns>The newly started <see cref="CaptureSession"/>.</returns>
    CaptureSession StartSession(string databaseName, string? label = null);

    /// <summary>
    /// Pauses the specified session. Change events are not recorded while a session is paused.
    /// </summary>
    /// <param name="sessionId">The session to pause.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session does not exist or is not active.</exception>
    void PauseSession(string sessionId);

    /// <summary>
    /// Resumes a previously paused session.
    /// </summary>
    /// <param name="sessionId">The session to resume.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session does not exist or is not paused.</exception>
    void ResumeSession(string sessionId);

    /// <summary>
    /// Stops the specified session. Captured events are retained for review but no new events are recorded.
    /// </summary>
    /// <param name="sessionId">The session to stop.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session does not exist or is already stopped.</exception>
    void StopSession(string sessionId);

    /// <summary>
    /// Removes the session and all its captured events from the store.
    /// </summary>
    /// <param name="sessionId">The session to delete.</param>
    void DeleteSession(string sessionId);

    /// <summary>
    /// Removes all captured events from the specified session without deleting the session itself.
    /// The session reverts to zero events but retains its state.
    /// </summary>
    /// <param name="sessionId">The session whose events should be cleared.</param>
    void ClearEvents(string sessionId);

    /// <summary>
    /// Records a data change event in the specified session.
    /// Events are silently dropped when the session is paused or stopped.
    /// </summary>
    /// <param name="evt">The event to record. Must not be <see langword="null"/>.</param>
    void RecordEvent(DataChangeEvent evt);

    /// <summary>
    /// Queries the captured events for the specified session using the provided filters.
    /// Returns events in descending timestamp order.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <param name="request">Query filters. Pass a default instance to return all events.</param>
    DataChangeQueryResponse Query(string sessionId, DataChangeQueryRequest request);

    /// <summary>
    /// Returns the total number of captured events across all sessions.
    /// </summary>
    int TotalEventCount { get; }
}
