# ADR 0006: Sanitized error handling with provider-specific mapping

## Status
Accepted

## Context
Service and provider failures need consistent user-safe diagnostics, while still keeping enough context for development troubleshooting.

## Decision
Adopt a centralized error handling strategy:

- Use `IErrorHandler` to create/shape `DataExplorerError` responses.
- Use shared `ErrorCategory` values for consistent UI behavior and recovery guidance.
- Keep provider-specific exception mapping behind `IProviderErrorMapper` in provider projects.
- Avoid exposing secrets, connection strings, machine paths, and credentials in UI/log payloads.

## Consequences

- Error behavior is consistent across Web, Core, and provider layers.
- Provider projects can evolve exception interpretation without changing shared contracts.
- Diagnostics remain safe by default for development-time UI exposure.
