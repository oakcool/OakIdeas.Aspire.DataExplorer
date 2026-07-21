namespace OakIdeas.Aspire.DataExplorer.Web.Services;

/// <summary>
/// Carries the identity of the database object that the Object Explorer has selected for
/// display in <c>ExplorerPage</c>, without placing that information in URL query parameters.
/// </summary>
/// <remarks>
/// Storing selected-object details in the URL exposed internal schema names, object identifiers,
/// connection names, and database names to browser history, referrer headers, and server logs.
/// Using this circuit-scoped service keeps the selection private to the current Blazor Server
/// circuit while remaining accessible to any page rendered in the same layout session.
/// </remarks>
public sealed class ExplorerNavigationState
{
    private ExplorerObjectSelection? _pendingSelection;

    /// <summary>
    /// Stores the object selection that the next <c>ExplorerPage</c> navigation should display.
    /// Called by the <c>MainLayout</c> object-select handler before navigating.
    /// </summary>
    public void SetSelection(ExplorerObjectSelection selection) => _pendingSelection = selection;

    /// <summary>
    /// Reads and clears the pending object selection.
    /// Returns the selection once, then resets to <see langword="null"/>.
    /// </summary>
    public ExplorerObjectSelection? ConsumeSelection()
    {
        var value = _pendingSelection;
        _pendingSelection = null;
        return value;
    }
}

/// <summary>
/// Identifies a database object selected in the Object Explorer sidebar.
/// </summary>
public sealed record ExplorerObjectSelection(
    string ObjectId,
    string ObjectType,
    string ObjectName,
    string SchemaName,
    string ConnectionName,
    string DatabaseName);
