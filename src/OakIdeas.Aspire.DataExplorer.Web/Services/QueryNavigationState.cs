namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Circuit-scoped state that carries an auto-execute intent from the Object Explorer to the
/// Query page without exposing the flag via a URL query parameter.
/// </summary>
/// <remarks>
/// Keeping auto-execute off the URL prevents drive-by execution: a crafted external link can
/// populate the SQL editor but cannot trigger execution without explicit user interaction.
/// Only navigation that originates within the same Blazor circuit (for example, a context-menu
/// action in the Object Explorer) can set the flag.
/// </remarks>
public sealed class QueryNavigationState
{
    private bool _pendingAutoExecute;

    /// <summary>
    /// Signals that the next query-page navigation should auto-execute its SQL.
    /// Called by the Object Explorer context menu handler before navigating.
    /// </summary>
    public void RequestAutoExecute() => _pendingAutoExecute = true;

    /// <summary>
    /// Reads and clears the pending auto-execute flag.
    /// Returns <see langword="true"/> once, then resets to <see langword="false"/>.
    /// </summary>
    public bool ConsumeAutoExecute()
    {
        var value = _pendingAutoExecute;
        _pendingAutoExecute = false;
        return value;
    }
}
