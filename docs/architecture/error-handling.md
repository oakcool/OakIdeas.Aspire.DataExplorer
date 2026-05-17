# Error Handling and Diagnostics

OakIdeas.Aspire.DataExplorer uses a shared `DataExplorerError` contract to move safe, developer-focused diagnostics from service layers to the UI.

## Error categories

- `ResourceNotFound`
- `ConnectionFailed`
- `QueryTimeout`
- `PermissionDenied`
- `ProviderError`
- `UnknownError`

## Design

- `IErrorHandler` in `Core` creates sanitized `DataExplorerError` payloads and structured log entries.
- `DataExplorerOperationException` carries sanitized error details across service boundaries when lower layers need to rethrow.
- Provider-specific exception mapping stays in provider projects through `IProviderErrorMapper`; SQL Server uses `SqlServerErrorMapper`.
- `ExplorerService` converts service failures into response models instead of letting UI-facing calls crash.
- Blazor UI surfaces errors through `ErrorAlert`, `ErrorRecovery`, and `DiagnosticInfo` components.

## Safety rules

- Do not log or render connection strings, credentials, usernames, IP addresses, or local system paths.
- Prefer operation names, resource names, categories, and diagnostic codes in logs.
- Keep recovery guidance actionable: retry, refresh metadata, or select a different database.
