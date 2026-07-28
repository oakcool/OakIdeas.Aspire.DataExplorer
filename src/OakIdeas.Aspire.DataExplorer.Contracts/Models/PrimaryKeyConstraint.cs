using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record PrimaryKeyConstraint(
    string ConstraintName,
    string TableName,
    string SchemaName,
    IReadOnlyList<string> KeyColumns,
    bool IsClustered,
    string ObjectId);
