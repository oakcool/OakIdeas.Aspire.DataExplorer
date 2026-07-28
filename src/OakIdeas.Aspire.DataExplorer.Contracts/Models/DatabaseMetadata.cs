using System.Text.Json.Serialization;

namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record DatabaseMetadata(
    string DatabaseName,
    DatabaseProviderType ProviderType,
    string ResourceId,
    IReadOnlyList<SchemaObject> Schemas,
    IReadOnlyList<TableObject> Tables,
    IReadOnlyList<ViewObject> Views,
    IReadOnlyDictionary<string, IReadOnlyList<StoredProcedureMetadata>> ProceduresBySchema,
    IReadOnlyDictionary<string, IReadOnlyDictionary<FunctionType, IReadOnlyList<FunctionMetadata>>> FunctionsBySchema,
    IReadOnlyList<TriggerMetadata> Triggers,
    IReadOnlyList<ConstraintMetadata> Constraints,
    IReadOnlyDictionary<string, IReadOnlyList<ColumnMetadata>> ColumnsByObject,
    IReadOnlyDictionary<string, IReadOnlyList<PrimaryKeyConstraint>> PrimaryKeysByTable,
    IReadOnlyDictionary<string, IReadOnlyList<ForeignKeyConstraint>> ForeignKeysByTable,
    IReadOnlyDictionary<string, IReadOnlyList<IndexMetadata>> IndexesByTable,
    DateTimeOffset MetadataCollectionTime,
    MetadataCollectionStatus CollectionStatus,
    IReadOnlyList<MetadataCollectionFailure> FailureDetails);

