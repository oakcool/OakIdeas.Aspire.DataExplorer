namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// The lifecycle state of a data change capture session.
/// </summary>
public enum CaptureSessionState
{
    /// <summary>The session is actively capturing change events.</summary>
    Active,

    /// <summary>The session has been paused; events are not captured while paused.</summary>
    Paused,

    /// <summary>The session has been stopped and is no longer capturing events.</summary>
    Stopped,
}
