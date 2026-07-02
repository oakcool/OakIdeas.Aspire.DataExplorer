# Roadmap

This document outlines planned and potential work for OakIdeas.Aspire.DataExplorer. It reflects current intentions and may change as the project evolves.

## Current focus

The project is stabilizing its first public release on NuGet. The immediate priorities are:

- Polishing the SQL Server provider coverage (views, procedures, functions, triggers, indexes, object definitions)
- Ensuring the Query Window guardrails are robust and well-tested
- Completing public repository health files (code of conduct, security policy, issue templates)
- Validating the NuGet packaging workflow end to end

## Near-term

- **Additional metadata coverage** — Foreign key relationships, extended properties, and index statistics for SQL Server
- **Diagram improvements** — Richer relationship metadata, layout improvements, and export support
- **Query Window** — Query history, results export (CSV/JSON), and saved-query support
- **Object definition viewer** — In-UI rendering of view/procedure/function/trigger definitions

## Medium-term

- **Provider expansion** — PostgreSQL provider as the second supported database
- **Integration test coverage** — Containerized SQL Server integration tests for the provider layer
- **Accessibility and localization** — Screen reader improvements and initial localization groundwork

## Not planned

The following are explicitly out of scope for this project:

- Production deployment support (this is a development-time-only tool)
- Mutations — Data Explorer is a read-oriented tool; write operations are not planned beyond the guarded Query Window
- Multi-host or remote Aspire environments

## Contributing

If you have ideas or want to discuss the roadmap, open a GitHub Discussion or file an issue. See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidance.
