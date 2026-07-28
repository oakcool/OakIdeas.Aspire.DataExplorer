using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ForeignKeyColumnMapping(
    string ParentColumnName,
    string ReferencedColumnName);

