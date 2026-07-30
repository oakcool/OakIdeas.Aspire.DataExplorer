namespace OakIdeas.Aspire.DataExplorer.Contracts.Models;

public sealed record SchemaDriftItem(
    SchemaDriftSource Source,
    SchemaDriftSeverity Severity,
    string ObjectType,
    string ObjectName,
    string Summary);
