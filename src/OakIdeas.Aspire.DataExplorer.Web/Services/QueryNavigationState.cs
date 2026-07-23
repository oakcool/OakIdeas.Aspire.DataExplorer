namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Circuit-scoped state that carries navigation intent from the Object Explorer to the
/// Query page without exposing the state via URL query parameters.
/// </summary>
/// <remarks>
/// Keeping execution flags and pre-populated SQL off the URL prevents drive-by execution:
/// a crafted external link cannot trigger query execution without explicit user interaction.
/// Only navigation that originates within the same Blazor circuit (for example, a context-menu
/// action in the Object Explorer) can set these flags.
/// </remarks>
public sealed class QueryNavigationState
{
    private bool _pendingAutoExecute;
    private string? _pendingSql;

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

    /// <summary>
    /// Stores SQL to be pre-populated in the Query page on the next navigation.
    /// Called by the Object Explorer context menu handler before navigating.
    /// </summary>
    public void SetPendingSql(string sql) => _pendingSql = sql;

    /// <summary>
    /// Reads and clears the pending SQL.
    /// Returns the SQL string once, then resets to <see langword="null"/>.
    /// </summary>
    public string? ConsumePendingSql()
    {
        var value = _pendingSql;
        _pendingSql = null;
        return value;
    }
}
