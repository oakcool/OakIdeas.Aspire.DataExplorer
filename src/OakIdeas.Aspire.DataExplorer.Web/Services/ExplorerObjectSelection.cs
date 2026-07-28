namespace OakIdeas.Aspire.DataExplorer.Web.Services;

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
