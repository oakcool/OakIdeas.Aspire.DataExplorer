namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

/// <summary>
/// Request to discover all navigable relationships for a given table.
/// Used by the Relationship-Aware Data Navigator.
/// </summary>
public sealed record DiscoverTableRelationshipsRequest(
    string SchemaName,
    string TableName);
