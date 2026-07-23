# Arcane Security Review — Audit Findings (July 2026)

This document records the verification and remediation status of every finding from the
[Arcane Security Review (2026-07)](arcane-security-review-2026-07.md).

---

## Findings Summary

| ID   | Title                              | Severity | Status        |
|------|------------------------------------|----------|---------------|
| F-01 | URL Driven Query Auto Execution    | Medium   | ✅ Resolved    |
| F-02 | RequireLocalConnections Not Enforced | Low    | ✅ Already Fixed |
| F-03 | Write Detection — First SQL Token  | Low      | ✅ Already Fixed |
| F-04 | Identifier Escaping                | Low      | ✅ Resolved    |
| F-05 | Mermaid Loaded From CDN            | Low      | ✅ Resolved    |
| F-06 | Operational State Exposed in URLs  | Low      | ✅ Resolved    |

---

## F-01 — URL Driven Query Auto Execution

**Severity:** Medium | **Status:** Resolved

### Verification

`QueryPage.razor` accepted an `?autoexec=true` URL query parameter.
When `AutoExecute = true`, `OnParametersSetAsync` called `RunQuery()` immediately, meaning any
link crafted with `?sql=...&autoexec=true` could trigger SQL execution upon navigation.

```csharp
// Before — vulnerable
[SupplyParameterFromQuery(Name = "autoexec")]
public bool AutoExecute { get; set; }
...
if (AutoExecute)
    await RunQuery(_sql);
```

### Remediation

- Removed the `[SupplyParameterFromQuery(Name = "autoexec")]` parameter from `QueryPage`.
- Introduced `QueryNavigationState` — a circuit-scoped (Blazor Server) service that carries the
  auto-execute intent from the Object Explorer to the Query page **without a URL parameter**.
- `MainLayout` calls `QueryNavigationState.RequestAutoExecute()` before navigating, and
  `QueryPage` reads it with `ConsumeAutoExecute()`, which resets the flag after one use.
- External URLs can populate the SQL editor via `?sql=` but can no longer trigger execution.

### Tests Added

- `QueryPageTests.AutoExecute_ViaNavigationState_ExecutesSql` — verifies that the state service
  path still triggers execution.
- `QueryPageTests.AutoExecute_ViaUrlParameter_DoesNotExecute` — regression test proving that
  `?autoexec=true` in the URL no longer triggers execution.

### Documentation

- CHANGELOG updated with security note.
- This findings document created.

---

## F-02 — RequireLocalConnections Is Not Enforced

**Severity:** Low | **Status:** Already Fixed

### Verification

`DataExplorerOptions.RequireLocalConnections` (default `true`) **is** enforced in
`ConnectionStringAspireResourceDiscovery.IsLocalConnection()`.

The implementation checks the `Server`/`Data Source`/`Host` key against a list of known local
addresses including `localhost`, `127.0.0.1`, `.`, `::1`, `(localdb)`, and the machine name.
Remote addresses are excluded from the discovered resource list.

Comprehensive tests exist in `ConnectionStringAspireResourceDiscoveryTests`.

### Conclusion

Finding is **not applicable** to the current codebase. No changes required.

---

## F-03 — Write Detection Relies on First SQL Token

**Severity:** Low | **Status:** Already Fixed

### Verification

`SqlServerDatabaseProvider.ExecuteQueryAsync` runs every query inside an always-rolled-back
`SqlTransaction` whenever `request.ReadOnly == true`:

```csharp
await using var transaction = request.ReadOnly
    ? (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken)
    : null;
```

`ExplorerService` passes `ReadOnly: !_options.EnableWriteOperations` to the provider.
This means that even if a write statement bypasses the first-token keyword check (e.g., via a
leading comment or CTE), the transaction rollback prevents any data from persisting.

The first-token keyword check in `ExplorerService` and `QueryPage` remains as a UX convenience
(early user-facing feedback and confirmation prompt), not as the security gate.

### Conclusion

The finding is addressed. The transaction-level rollback is the actual security enforcement.
No additional changes required.

---

## F-04 — Identifier Escaping

**Severity:** Low | **Status:** Resolved

### Verification

`ExplorerQueryTemplates` generated SQL using string interpolation with unescaped identifiers:

```csharp
// Before — unescaped
$"SELECT TOP 1000 *\nFROM [{schemaName}].[{tableName}]"
```

A database object named `Evil]Table` would produce `FROM [Evil]Table]`, which is malformed.
Similarly, `ScriptDefinition` passed the object name as a string literal to `sp_helptext`
without escaping single quotes.

### Remediation

- Added `ExplorerQueryTemplates.BracketQuote(string identifier)` — wraps an identifier in
  `[...]` and escapes `]` as `]]` per T-SQL conventions.
- Added `ExplorerQueryTemplates.SingleQuoteEscape(string value)` — doubles embedded `'`
  characters for use inside T-SQL single-quoted string literals.
- All template methods updated to use these helpers.

### Tests Added

- `ExplorerQueryTemplatesTests.BracketQuote_EscapesClosingBracketInIdentifier`
- `ExplorerQueryTemplatesTests.SelectTop1000_WithClosingBracketInName_ProducesSafeIdentifier`
- `ExplorerQueryTemplatesTests.ScriptDefinition_WithSingleQuoteInName_EscapesQuote`
- `ExplorerQueryTemplatesTests.SingleQuoteEscape_DoublesEmbeddedSingleQuote`
- `ExplorerQueryTemplatesTests.SingleQuoteEscape_NoQuote_ReturnsOriginal`

### Documentation

- CHANGELOG updated.

---

## F-05 — Mermaid Loaded From CDN

**Severity:** Low | **Status:** Resolved

### Verification

`App.razor` loaded Mermaid with a version range and no SRI:

```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
```

Using `@11` (a range) means the resolved file can change without notice.
The absence of a `crossorigin` attribute and `integrity` hash means a CDN compromise
could serve malicious JavaScript to users.

### Remediation

- Pinned to an exact version: `mermaid@11.16.0`.
- Added SHA-384 Subresource Integrity hash computed from the npm package.
- Added `crossorigin="anonymous"` as required by the SRI specification.

```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@11.16.0/dist/mermaid.min.js"
        integrity="sha384-T/0lMUdJpd2S1ZHtRiofG3htU3xPCrFVeAQ1UUE2TJwlEJSV5NUwn30kP28n238E"
        crossorigin="anonymous"></script>
```

### Documentation

- CHANGELOG updated.

> **Upgrade note:** When Mermaid is upgraded in future, recompute the SRI hash with
> `openssl dgst -sha384 -binary mermaid.min.js | base64` and update both the version pin
> and the `integrity` attribute in `App.razor`.

---

## F-06 — Operational State Exposed in URLs

**Severity:** Low | **Status:** Resolved

### Verification

Multiple pages passed operational state through URL query parameters:

- `ExplorerPage` accepted six query parameters: `objectId`, `objectType`, `objectName`,
  `schemaName`, `connectionName`, and `databaseName`.
- `MainLayout.HandleContextAction` built a `?sql=...` query string for Query page navigation.
- Internal schema names, database names, connection identifiers, and SQL fragments appeared
  in browser history, server access logs, and HTTP `Referer` headers on every navigation.

### Remediation

- Introduced `ExplorerNavigationState` — a circuit-scoped service that carries the selected
  database object's identity from the Object Explorer sidebar to `ExplorerPage` without any
  URL parameters.  `ExplorerPage` subscribes to `NavigationManager.LocationChanged` so that
  re-selection while the page is already active is also handled without a URL change.
- Extended `QueryNavigationState` with `SetPendingSql` / `ConsumePendingSql` — carries the
  context-menu SQL text to `QueryPage` without a `?sql=` URL parameter for in-process
  navigation.
- `MainLayout.HandleObjectSelect` now calls `ExplorerNavigationState.SetSelection` and
  navigates to `/explorer` (no query string).
- `MainLayout.HandleContextAction` now calls `QueryNavigationState.SetPendingSql` and
  navigates to `/query` (no query string).
- `ExplorerPage` no longer declares `[SupplyParameterFromQuery]` parameters.
- `QueryPage` consumes `ConsumePendingSql()` first; the `?sql=` URL parameter is retained
  as a backwards-compatible deep-link entry point for external tools — but it cannot
  trigger execution (unchanged from F-01 remediation).

### Tests Added

- `ExplorerPageTests` updated to use `ExplorerNavigationState` instead of URL parameters.
- `ExplorerPageTests.DirectNavigation_WithoutState_ShowsEmptyExplorer` — regression test
  proving that direct navigation to `/explorer` renders gracefully with no object selected.
- `QueryPageTests.PendingSql_ViaNavigationState_PopulatesEditor` — verifies that SQL set
  via the state service populates the editor when navigating to `/query` with no URL params.
- `QueryPageTests.PendingSql_WithAutoExecute_ViaNavigationState_ExecutesSql` — verifies that
  the state-service SQL + auto-execute flag triggers execution without any URL parameters.

### Documentation

- ADR 0010 created: `docs/decisions/0010-blazor-state-navigation.md`.
- CHANGELOG updated.

