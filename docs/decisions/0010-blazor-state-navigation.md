# ADR 0010: Blazor State Navigation — Remove Operational State from URL Parameters

## Status

Accepted

## Context

The application was passing operational state between pages using URL query parameters.
For example, navigating to the Explorer page after selecting a database object from the
sidebar produced URLs like:

```
/explorer?objectId=dbo.Users&objectType=table&objectName=Users&schemaName=dbo
         &connectionName=sql-main&databaseName=applicationdb
```

And navigating to the Query page from the context menu produced:

```
/query?sql=SELECT+TOP+1000+*+FROM+%5Bdbo%5D.%5BUsers%5D
```

This approach exposed internal schema names, object identifiers, connection names, database names, and SQL statement fragments in:

- Browser history and the address bar
- Server access logs
- HTTP `Referer` headers
- Bookmarks and link-sharing

The security review (finding F-01) had already identified URL-driven execution as a risk.
Phase 2 of the same review extended this concern to broader URL-based state exposure.

## Decision

Use Blazor Server's circuit-scoped DI services as the primary channel for passing
page-to-page navigation state within the application.

Two services carry this state:

### `QueryNavigationState`

Carries the SQL text and the auto-execute flag from the Object Explorer sidebar to
the Query page.  The service is consumed once (read-and-clear semantics) so the state
is discarded after the page reads it.

The `?sql=` URL parameter is **retained** for backwards-compatible deep linking from
external tools.  External links can populate the SQL editor but cannot trigger execution
(the auto-execute flag is only settable via the circuit-scoped service).

### `ExplorerNavigationState`

Carries the selected database object's identity (`objectId`, `objectType`, `objectName`,
`schemaName`, `connectionName`, `databaseName`) from the Object Explorer sidebar to the
Explorer page. The service is consumed once (read-and-clear semantics).

`ExplorerPage` subscribes to `NavigationManager.LocationChanged` so that it also receives
state updates when the user selects a different object while the Explorer page is already
active (same URL → no route re-render, but `LocationChanged` still fires).

### What changed

| Before | After |
|--------|-------|
| `MainLayout.HandleObjectSelect` builds a 6-param query string and navigates to `/explorer?...` | Sets `ExplorerNavigationState` and navigates to `/explorer` (no params) |
| `MainLayout.HandleContextAction` navigates to `/query?sql=...` | Sets `QueryNavigationState.SetPendingSql()` and navigates to `/query` (no params) |
| `ExplorerPage` reads 6 `[SupplyParameterFromQuery]` parameters | Reads from `ExplorerNavigationState` in `OnInitializedAsync` + `LocationChanged` handler |
| `QueryPage` reads only the `?sql=` URL parameter | Reads `QueryNavigationState.ConsumePendingSql()` first; falls back to `?sql=` URL param |

## Considered Alternatives

### Cascading Values

`MainLayout` could cascade `_selectedObject` as a cascading parameter that `ExplorerPage`
reads.  This is a valid Blazor pattern but creates tight coupling between a specific layout
and a specific page — `ExplorerPage` would break if rendered under a different layout.

### URL Route Segment for Object Identity

Using a route segment (e.g., `/explorer/{objectId}`) keeps identity addressable but still
exposes it.  It also complicates the URL for composite identifiers.

### Persistent Component State (browser storage)

`ProtectedBrowserStorage` persists across browser refreshes but requires async access
and is not appropriate for transient, one-shot navigation intent.

## Consequences

- **Security**: Schema names, connection names, object identifiers, and SQL fragments no
  longer appear in URLs, browser history, or server logs for in-process navigation.
- **Deep linking**: The `?sql=` parameter is preserved for the Query page to support
  external tool integration.  No deep linking to specific Explorer objects is supported;
  navigating directly to `/explorer` shows an empty selection state (intentional).
- **Refresh behaviour**: Refreshing the browser while on the Explorer page returns an
  empty selection (the state is gone).  Refreshing the Query page clears the pre-populated
  SQL unless it was provided via the `?sql=` deep link.
- **Testing**: Tests inject the state service and call `SetSelection` / `SetPendingSql`
  directly instead of navigating with query strings.
- **Extensibility**: The state services are the designated extension point for future
  per-user preferences (e.g., default connection, preferred schema); they can be evolved
  into richer state containers without changing the navigation contract.
