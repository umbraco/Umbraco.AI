# OpenTelemetry Integration Plan for Umbraco.AI

## Overview

This plan adds standard .NET observability (`System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics`) to Umbraco.AI, bridging the existing local-first governance system with the .NET OpenTelemetry ecosystem. No new NuGet dependencies are required — everything uses APIs built into .NET.

The approach is additive: the existing audit log and usage analytics systems remain untouched. OpenTelemetry support layers on top, so APM tools (Application Insights, Jaeger, Datadog, Grafana) can observe AI operations while the backoffice governance UI continues to work independently.

## Current State

### What Exists

1. **Audit Log System** — `AIAuditingChatClient` / `AIAuditingEmbeddingGenerator` middleware captures per-request governance data (user, profile, provider, model, tokens, errors, duration, parent-child nesting). Persisted to SQL Server/SQLite via EF Core.
2. **Usage Analytics** — `AIUsageRecordingChatClient` / `AIUsageRecordingEmbeddingGenerator` for lightweight token/duration tracking with hourly/daily aggregation.
3. **Response Tracking** — `AITrackingChatClient` captures `UsageDetails` and response messages.
4. **TraceIdentifier** — API model accepting either local GUID or OpenTelemetry TraceId string.

### What's Missing

1. **No `ActivitySource` or `Activity` usage** — External APM tools see nothing from Umbraco.AI.
2. **No `Meter` / metrics** — No Prometheus/OTLP-compatible counters, histograms, or gauges.
3. **No correlation between audit logs and distributed traces** — `AIAuditLog` doesn't capture `Activity.Current?.TraceId`.
4. **M.E.AI `UseOpenTelemetry()` not wired up** — Standard `gen_ai.*` spans not emitted.

---

## Implementation Steps

### Step 1: Create `AIActivitySource` — Central Tracing Infrastructure

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Telemetry/AIActivitySource.cs`

Create a static class holding the shared `ActivitySource` for all Umbraco.AI tracing:

```csharp
using System.Diagnostics;

namespace Umbraco.AI.Core.Telemetry;

public static class AIActivitySource
{
    public const string Name = "Umbraco.AI";
    internal static readonly ActivitySource Source = new(Name);
}
```

Follows .NET convention (single source per library). `Source` is `internal` so only Umbraco.AI creates activities; `Name` is `public` for consumer `AddSource()` calls.

---

### Step 2: Create `AIMetrics` — Central Metrics Infrastructure

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Telemetry/AIMetrics.cs`

DI-registered class holding all metrics instruments:

- `Counter<long>` `umbraco.ai.operations` — operation count, tagged by capability/provider/model
- `Counter<long>` `umbraco.ai.tokens` — token count, tagged by capability/provider/model/direction (input/output)
- `Histogram<double>` `umbraco.ai.operation.duration` — latency in ms, tagged by capability/provider/model/success
- `Counter<long>` `umbraco.ai.errors` — error count, tagged by capability/provider/model/error category

Uses `IMeterFactory` (already available via ASP.NET Core). Internal `Record*` methods encapsulate tagging. `MeterName` constant is `public` for consumer `AddMeter()` calls.

---

### Step 3: Add Tracing Spans to `AIAuditingChatClient`

**File:** `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingChatClient.cs`

Modify both `GetResponseAsync` and `GetStreamingResponseAsync`:

1. Start: `AIActivitySource.Source.StartActivity("umbraco.ai.chat", ActivityKind.Client)`
2. Tags from `AIAuditContext`: `ai.capability`, `ai.provider`, `ai.model`, `ai.profile.alias`, `ai.entity.type`, `ai.feature.type`, `ai.audit.id`
3. Success: `SetStatus(Ok)`, add `ai.tokens.input/output/total` tags
4. Failure: `SetStatus(Error, message)`, add exception event
5. `using` pattern for automatic disposal

Activity starts even when audit logging is disabled — tracing is independent.

---

### Step 4: Add Tracing Spans to `AIAuditingEmbeddingGenerator`

**File:** `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs`

Same pattern as Step 3:
- Activity name: `"umbraco.ai.embedding"`
- Tags: same set with `ai.capability` = `"embedding"`

---

### Step 5: Bridge Audit Logs to OpenTelemetry TraceId

**Files:**
- `Core/AuditLog/AIAuditLog.cs` — Add `string? TraceId` property
- `Core/AuditLog/Middleware/AIAuditingChatClient.cs` — Set `auditLog.TraceId = Activity.Current?.TraceId.ToString()`
- `Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs` — Same
- `Persistence/AuditLog/AIAuditLogEntity.cs` — Add `string? TraceId` column
- EF Core mapping + new migration `UmbracoAI_AddTraceIdToAuditLog` (SqlServer + Sqlite)

Critical bridge: every audit log entry carries the TraceId, enabling backoffice "View in APM" links and APM-to-governance correlation.

---

### Step 6: Add Metrics Recording to Auditing Middleware

**Files:**
- `Core/AuditLog/Middleware/AIAuditingChatClient.cs` — Accept `AIMetrics`, call `Record*` methods
- `Core/AuditLog/Middleware/AIAuditingChatMiddleware.cs` — Inject `AIMetrics`
- `Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs` — Same
- `Core/AuditLog/Middleware/AIAuditingEmbeddingMiddleware.cs` — Same

Auditing middleware already has all context (capability, provider, model, tokens, duration, error category). Metrics emit even when audit logging is disabled.

---

### Step 7: Register `AIMetrics` in DI

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`

```csharp
services.AddSingleton<AIMetrics>();
```

---

### Step 8: Consumer-Facing Constants

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Telemetry/OpenTelemetryExtensions.cs`

```csharp
public static class OpenTelemetryExtensions
{
    public static class SourceNames
    {
        public const string Tracing = AIActivitySource.Name;
        public const string Metrics = AIMetrics.MeterName;
    }
}
```

Exposes constants only — no OpenTelemetry SDK dependency. Consumers reference these in their `AddOpenTelemetry()` call.

---

### Step 9: Unit Tests

1. `Tests.Unit/Telemetry/AIActivitySourceTests.cs` — Verify activities created with correct tags, status, and exception events
2. `Tests.Unit/Telemetry/AIMetricsTests.cs` — Verify metric instruments populated with correct tags
3. `Tests.Unit/Telemetry/AuditLogTraceIdTests.cs` — Verify TraceId captured from ambient Activity

---

### Step 10: Documentation

- **Modified:** `docs/public/extending/middleware/README.md` — Update OpenTelemetry section
- **New:** `docs/public/concepts/observability.md` — Observability guide (wiring, tag reference, Application Insights + Grafana examples)

---

## Files Summary

| File | Change |
|------|--------|
| `Core/Telemetry/AIActivitySource.cs` | **New** |
| `Core/Telemetry/AIMetrics.cs` | **New** |
| `Core/Telemetry/OpenTelemetryExtensions.cs` | **New** |
| `Core/AuditLog/AIAuditLog.cs` | **Modified** — add `TraceId` |
| `Core/AuditLog/Middleware/AIAuditingChatClient.cs` | **Modified** — spans + metrics + TraceId |
| `Core/AuditLog/Middleware/AIAuditingChatMiddleware.cs` | **Modified** — inject `AIMetrics` |
| `Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs` | **Modified** — spans + metrics + TraceId |
| `Core/AuditLog/Middleware/AIAuditingEmbeddingMiddleware.cs` | **Modified** — inject `AIMetrics` |
| `Core/Configuration/UmbracoBuilderExtensions.cs` | **Modified** — register `AIMetrics` |
| `Persistence/AuditLog/AIAuditLogEntity.cs` | **Modified** — add `TraceId` |
| Persistence EF mapping | **Modified** |
| `Persistence.SqlServer/Migrations/UmbracoAI_AddTraceIdToAuditLog` | **New** |
| `Persistence.Sqlite/Migrations/UmbracoAI_AddTraceIdToAuditLog` | **New** |
| `Tests.Unit/Telemetry/AIActivitySourceTests.cs` | **New** |
| `Tests.Unit/Telemetry/AIMetricsTests.cs` | **New** |
| `Tests.Unit/Telemetry/AuditLogTraceIdTests.cs` | **New** |
| `docs/public/extending/middleware/README.md` | **Modified** |
| `docs/public/concepts/observability.md` | **New** |

---

## Intentionally Deferred

- **M.E.AI `UseOpenTelemetry()` in default pipeline** — Would duplicate spans. Consumers can add it themselves. Documented as an option.
- **Per-tool-call spans (Tier 2)** — Requires `AIFunctionInvokingChatMiddleware` changes. Can be added later as child spans.
- **CRUD operation tracing** — Out of scope for AI operation telemetry.
