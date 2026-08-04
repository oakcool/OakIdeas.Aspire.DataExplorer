# Test Data Scenario Builder

The Test Data Scenario Builder is a development-time feature for creating, editing, and executing reusable, deterministic data scenarios. Each scenario inserts related records into a database in dependency order, supporting fixed values, generated values, and references to prior generated keys.

## Status

Phase 1 (feature flag + navigation) and Phase 2 (contracts, service abstraction, in-memory implementation) are complete. Feature is preview and disabled by default.

## Feature Flag

| Property | Value |
|----------|-------|
| Key | `ScenarioBuilder.TestDataScenarioBuilder` |
| Category | `Scenarios` |
| Default | `false` (disabled by default) |
| Lifecycle | `Preview` |

The feature is disabled by default so that existing installations retain their current behavior when upgrading. Enable it explicitly in configuration:

```json
{
  "OakIdeas": {
    "Aspire": {
      "DataExplorer": {
        "FeatureFlags": {
          "ScenarioBuilder.TestDataScenarioBuilder": true
        }
      }
    }
  }
}
```

The feature flag is enforced at three independent layers:

1. **Navigation** — The "Scenarios" nav link is only rendered when `FeatureFlagStateService.TestDataScenarioBuilderEnabled` returns `true`.
2. **Page guard** — `TestDataScenarioBuilderPage.razor` renders an "unavailable" banner and returns immediately when the flag is off; no service calls are made.
3. **Service registration** — `AddScenarioBuilderServices()` is called unconditionally in `Program.cs`, but the service is stateless when unused and safe to have registered at all times.

Direct URL navigation to `/scenario-builder` is handled by the page guard — the feature is inaccessible through any path when disabled.

## Architecture

### Scenario Model

A `TestDataScenario` is a versioned, named container of ordered `ScenarioTableOperation` entries. Each operation targets a single table and defines the column values for one row to insert.

```
TestDataScenario
  ├── ScenarioId        (unique identifier)
  ├── Name / Description
  ├── Version           (schema version, currently 1)
  ├── Seed              (optional deterministic seed)
  └── Tables: ScenarioTableOperation[]
        ├── SchemaName / TableName
        ├── Alias         (used by later reference columns)
        └── Columns: ScenarioColumnValue[]
              ├── ColumnName
              ├── ValueKind  (Fixed | Generated | Reference)
              ├── FixedValue
              ├── GeneratorName
              ├── ReferenceAlias / ReferenceColumn
```

### Value Kinds

| Kind | Description | Example |
|------|-------------|---------|
| `Fixed` | A literal value specified in the scenario definition | `"Alice"`, `"2024-01-01"` |
| `Generated` | A named generator evaluated at execution time | `guid`, `utcnow`, `randomstring(8)`, `randomint` |
| `Reference` | The generated key output from a prior aliased table operation | alias `customer1`, column `Id` |

### Execution Pipeline

1. Operations are iterated in definition order.
2. For each operation, all column values are resolved:
   - `Fixed` → literal value.
   - `Generated` → calls the named generator (optionally seeded).
   - `Reference` → looks up the aliased prior operation's captured key.
3. The first `Generated` column value in an operation is captured as the alias key output.
4. After all operations, `LastExecutedAt` is updated on the scenario.

### Provider-Neutral Contracts

All contracts live in the shared `Contracts` and `Core` layers:

| Contract / Type | Location | Purpose |
|---|---|---|
| `IScenarioBuilderService` | `Core/Abstractions` | Scenario CRUD and execution |
| `TestDataScenario` | `Contracts/Models` | Scenario definition and metadata |
| `ScenarioTableOperation` | `Contracts/Models` | Single table insert operation |
| `ScenarioColumnValue` | `Contracts/Models` | Column value specification |
| `ScenarioValueKind` | `Contracts/Models` | Fixed / Generated / Reference enum |
| `ScenarioState` | `Contracts/Models` | Draft / Executed / Failed lifecycle enum |
| `CreateScenarioRequest` | `Contracts/Models` | Create/update request |
| `CreateScenarioResponse` | `Contracts/Models` | Create/update result |
| `ExecuteScenarioRequest` | `Contracts/Models` | Execution request (with optional seed override) |
| `ExecuteScenarioResponse` | `Contracts/Models` | Execution result with inserted row counts and captured keys |
| `DeleteScenarioRequest` | `Contracts/Models` | Delete request |

### In-Memory Service

`InMemoryScenarioBuilderService` is the default implementation of `IScenarioBuilderService`. It is:

- **Thread-safe** using a `Lock` for all mutable state.
- **Stateless across restarts** — definitions and results are held only in memory.
- **Deterministic** when a seed is supplied — the execution pipeline uses `new Random(seed)` so generated values can be reproduced.

The service is registered as a singleton via `AddScenarioBuilderServices()`.

### Service Registration

```csharp
builder.Services.AddScenarioBuilderServices();
```

This is called unconditionally in `Program.cs`. The service itself is lightweight and safe to register at all times; the feature flag prevents any actual use when disabled.

## UI

The page at `/scenario-builder` provides:

- **Scenario list panel** — shows all defined scenarios with table count, creation time, and a "run" badge when executed.
- **Create / Edit form** — name, description, optional seed, and ordered table operations with per-column value kind selection.
- **Detail / action panel** — shows scenario metadata, execution actions (Run / Edit / Delete), the most recent execution result (inserted row counts and captured keys), and a table operations summary.

## Testing

- `InMemoryScenarioBuilderServiceTests` — unit tests covering CRUD operations, value resolution, seeded repeatability, multi-table execution, and null-guard behavior.
- `TestDataScenarioBuilderPageTests` — Bunit component tests covering the disabled flag guard, create form, run, delete, and runtime flag change scenarios.
- `ApplicationFeaturesCatalogTests` — validates the new flag key is present and defaults to disabled.
