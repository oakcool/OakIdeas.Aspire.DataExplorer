namespace OakIdeas.Aspire.DataExplorer.SqlServer.Connection;

public sealed record ConnectionValidationResult(bool IsValid, string? ErrorMessage);
