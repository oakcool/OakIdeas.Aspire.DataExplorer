using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record ForeignKeyConstraint(
    string ConstraintName,
    string ParentTableName,
    string ParentSchemaName,
    string ReferencedTableName,
    string ReferencedSchemaName,
    IReadOnlyList<ForeignKeyColumnMapping> KeyColumns,
    ReferentialActionBehavior OnDeleteBehavior,
    ReferentialActionBehavior OnUpdateBehavior,
    bool IsDisabled,
    string ObjectId);
