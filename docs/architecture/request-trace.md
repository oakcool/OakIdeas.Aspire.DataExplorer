# Request-to-Database Trace

The Request-to-Database Trace feature correlates Aspire application traces with the SQL statements, transactions, errors, and database activity produced by each request.

## Status

Phase 1 complete (telemetry analysis and feature flag infrastructure). Subsequent phases are planned.

## Feature Flag

| Property | Value |
|----------|-------|
| Key | `Telemetry.RequestTrace` |
| Category | `Telemetry` |
| Default | `false` (disabled by default) |
| Lifecycle | `Preview` |

The feature is disabled by default to preserve existing behavior for all current installations. It must be explicitly enabled in configuration before any trace functionality is accessible.

```json
{
  "OakIdeas": {
    "Aspire": {
      "DataExplorer": {
        "FeatureFlags": {
          "Telemetry.RequestTrace": true
        }
      }
    }
  }
}
```

## Goals

- Help developers understand exactly what a request did to the database.
- Correlate database activity with Aspire traces and services.
- Expose repeated queries, slow calls, failures, and transaction boundaries.
- Mask sensitive parameter values by default.

## Implementation Phases

### Phase 1: Telemetry Analysis (Complete)

- Analyzed the Aspire dashboard integration, OpenTelemetry span model, and existing instrumentation in the tool.
- Established the feature flag (`Telemetry.RequestTrace`, default `false`) and category (`Telemetry`).
- Introduced the navigation link and placeholder page, both guarded by the feature flag.
- Defined correlation identifiers: trace ID, span ID, and service name from OpenTelemetry spans.
- Documented provider extension points for telemetry enrichment.

**Correlation identifiers**

| Field | Source | Notes |
|-------|--------|-------|
| `TraceId` | OpenTelemetry `Activity.TraceId` | 128-bit identifier for the request root |
| `SpanId` | OpenTelemetry `Activity.SpanId` | 64-bit identifier for the database span |
| `ServiceName` | `service.name` resource attribute | Aspire resource name |
| `DbSystem` | `db.system` span attribute | e.g., `mssql`, `postgresql` |
| `DbStatement` | `db.statement` span attribute | SQL text (may be sanitized by SDK) |
| `DbName` | `db.name` span attribute | Target database name |
| `PeerAddress` | `server.address` + `server.port` | Connection endpoint |

**Partial telemetry behavior**

When only partial telemetry is available:
- Missing `TraceId`: the span cannot be correlated to a request; display with a warning.
- Missing `DbStatement`: show operation type and duration only; indicate statement is unavailable.
- Missing service name: group under "Unknown Service".
- Missing database name: group under "Unknown Database".

### Phase 2: Trace Ingestion and Correlation (Planned)

- Collect or consume database-related OpenTelemetry spans from the Aspire dashboard OTLP endpoint or a local exporter.
- Correlate spans to requests, services, and configured Aspire data sources by `TraceId` and resource attributes.
- Define `ITraceCorrelationProvider` for provider-specific enrichment.
- Add request/response contracts: `TraceQueryRequest`, `TraceQueryResponse`, `CorrelatedSpan`.
- Add unit and integration tests using representative OTLP trace fixtures.

### Phase 3: Trace Visualization (Planned)

- Create a timeline/tree view of database operations grouped by request and service.
- Add filters for service, database, duration, status, and operation type.
- Add links to query details, execution plans, and affected records where supported by the provider.
- Sanitize parameter values before display.

### Phase 4: Diagnostics (Planned)

- Identify repeated queries, long-running calls, and likely N+1 patterns.
- Add clear warnings without presenting guesses as facts.
- Document instrumentation requirements and known limitations per provider.

## Architecture

### Feature Flag Enforcement

The feature flag must be enforced independently at each layer:

| Layer | Enforcement |
|-------|-------------|
| Navigation | `MainLayout.razor` checks `FeatureFlags.RequestTraceEnabled` |
| Route / Page | `RequestTracePage.razor` checks `FeatureFlags.RequestTraceEnabled` and returns early |
| Backend services | Future: service methods must check the flag before proceeding |
| API endpoints | Future: endpoints must return 404 or 403 when the flag is disabled |

Hiding the navigation link alone is not sufficient. Direct URL access, stale client state, and saved links must all be blocked by the page-level guard.

### Provider Extension Points

Provider-specific telemetry enrichment is planned through a `ITraceEnrichmentProvider` interface (Phase 2). This follows the existing pattern of provider-specific discovery interfaces (`IColumnDiscoveryProvider`, `ITableDiscoveryProvider`, etc.) and keeps provider-specific logic inside provider projects.

### Security and Safety

- All database content from traces is treated as potentially sensitive.
- SQL statement text is displayed with parameter values masked by default.
- Connection strings and credentials must never appear in trace views, logs, or exports.
- The feature is read-only; no state-changing operations are performed.

## Configuration

```json
{
  "OakIdeas": {
    "Aspire": {
      "DataExplorer": {
        "FeatureFlags": {
          "Telemetry.RequestTrace": true
        }
      }
    }
  }
}
```

Via environment variable:
```
OakIdeas__Aspire__DataExplorer__FeatureFlags__Telemetry.RequestTrace=true
```

## Rollout Strategy

1. Deploy with `Telemetry.RequestTrace: false` (default).
2. Enable on development or staging environments via configuration to validate each phase.
3. Promote the feature flag default to `true` after Phase 4 is stable and tested.
4. After a defined stabilization period (minimum one release cycle), retire the flag and remove the guard code.

## Retirement Criteria

The feature flag may be retired when:
- All four implementation phases are complete and stable.
- All tests (unit, integration, UI, end-to-end) pass consistently.
- The feature has been enabled by default for at least one full release cycle without regressions.
- Documentation, samples, and website content are fully updated.
