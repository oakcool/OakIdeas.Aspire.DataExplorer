namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Represents a data change capture session.
/// Sessions are in-memory and scoped to the current development process; they are not persisted.
/// </summary>
/// <param name="SessionId">Unique identifier for this capture session.</param>
/// <param name="Label">Developer-supplied label for this session, e.g. "Create user flow".</param>
/// <param name="DatabaseName">The database being monitored during this session.</param>
/// <param name="StartedAt">UTC timestamp when the session was started.</param>
/// <param name="PausedAt">UTC timestamp when the session was last paused, or <see langword="null"/> if never paused.</param>
/// <param name="StoppedAt">UTC timestamp when the session was stopped, or <see langword="null"/> if still active or paused.</param>
/// <param name="State">The current lifecycle state of the session.</param>
/// <param name="EventCount">The number of change events captured so far in this session.</param>
public sealed record CaptureSession(
    string SessionId,
    string Label,
    string DatabaseName,
    DateTimeOffset StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? StoppedAt,
    CaptureSessionState State,
    int EventCount);
