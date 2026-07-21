# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) conventions.
This project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `CODE_OF_CONDUCT.md` — community participation standards
- `SECURITY.md` — vulnerability reporting policy and development-only boundary guidance
- `.editorconfig` — consistent editor formatting rules for C#, Razor, JSON, TypeScript, CSS, and Markdown
- `ROADMAP.md` — current priorities, near-term, and medium-term goals
- `.github/PULL_REQUEST_TEMPLATE.md` — standard pull request template
- `.github/ISSUE_TEMPLATE/bug-report.yml` — structured bug report template
- `.github/ISSUE_TEMPLATE/feature-request.yml` — structured feature request template
- `ObjectDefinitionRequest` / `ObjectDefinitionResponse` contracts and `IObjectDefinitionProvider` interface for unified object definition retrieval
- `SqlServerDatabaseProvider` implementation of `IObjectDefinitionProvider` covering views, procedures, functions, triggers, and indexes
- `Index` (`7`) added to `DatabaseObjectType` enum
- `QueryNavigationState` — circuit-scoped service that carries the auto-execute intent from the Object Explorer to the Query page without exposing the flag in the URL (addresses F-01 from the Arcane security review)
- `QueryNavigationState.SetPendingSql` / `ConsumePendingSql` — extends the service to carry SQL text for in-process context-menu navigation, eliminating the `?sql=` URL parameter for internal navigation flows
- `ExplorerNavigationState` — circuit-scoped service that carries the selected database object identity from the Object Explorer to the Explorer page, replacing six URL query parameters (`objectId`, `objectType`, `objectName`, `schemaName`, `connectionName`, `databaseName`)
- `ExplorerQueryTemplates.BracketQuote` and `ExplorerQueryTemplates.SingleQuoteEscape` — public helpers for safe T-SQL identifier and string escaping (addresses F-04)
- ADR 0010 — documents the Blazor state navigation architectural decision, alternatives considered, and consequences

### Changed
- Consolidated public-facing documentation and removed temporary development artifacts ahead of public release.
- `ExplorerQueryTemplates` now escapes `]` as `]]` in bracket-quoted identifiers and doubles single quotes in `sp_helptext` string arguments (F-04).
- **ExplorerPage** no longer reads object identity from URL query parameters; selection state is now passed via `ExplorerNavigationState`.  Direct navigation to `/explorer` shows an empty selection state (backwards-compatible graceful behaviour).
- **QueryPage** context-menu navigation now passes SQL via `QueryNavigationState.SetPendingSql`; the `?sql=` URL parameter is retained for external deep-link support only.
- **MainLayout** `HandleObjectSelect` and `HandleContextAction` updated to use state services instead of constructing query strings.

### Security
- The Data Explorer resource is no longer published as an external endpoint by default; it binds for local development only. Consumers that deliberately need remote access can add `.WithExternalHttpEndpoints()` themselves. (**Breaking** for anyone relying on external reachability.)
- `DataExplorerOptions.EnableWriteOperations` now defaults to `false` (secure by default). Enable it explicitly to allow write/DDL statements from the Query Window.
- Read-only query execution is now enforced at the transaction level (statements run inside an always-rolled-back transaction) instead of first-token keyword matching, closing multi-statement/comment/CTE bypasses.
- `DataExplorerOptions.RequireLocalConnections` (default `true`) is now enforced: discovered database resources whose server is not on the local machine are excluded.
- `AllowedHosts` for the web app is scoped to loopback hosts instead of `*`.
    - **F-01 (URL-driven auto execution):** The `autoexec` URL query parameter has been removed from the Query page. External URLs can populate the SQL editor but can no longer trigger execution; only in-process Object Explorer navigation via `QueryNavigationState` may auto-execute, closing the drive-by execution surface.
    - **F-04 (Identifier escaping):** `ExplorerQueryTemplates` now correctly escapes `]` as `]]` in bracket-quoted identifiers and doubles single quotes in `sp_helptext` string arguments, preventing SQL injection via crafted database object names.
    - **F-05 (Mermaid CDN SRI):** Mermaid is now loaded from a pinned version (`11.16.0`) with a Subresource Integrity (`sha384`) hash, preventing supply-chain substitution attacks.
    - **URL state exposure:** Internal schema names, connection names, object identifiers, and SQL fragments no longer appear in URLs, browser history, or server logs for in-process navigation.  The `?sql=` deep-link parameter is retained on the Query page for legitimate external tool integration.
