# OpenTelemetry Integration Plan for Umbraco.AI

## Overview

This plan adds OpenTelemetry support to Umbraco.AI by delegating to M.E.AI's built-in `OpenTelemetryChatClient` / `OpenTelemetryEmbeddingGenerator` for standard `gen_ai.*` tracing and metrics, then enriching with Umbraco-specific context (profile, entity, feature, user, audit log ID) in the existing auditing middleware.

No custom `ActivitySource` or `Meter`. No new NuGet dependencies. M.E.AI handles the heavy lifting; we just decorate.

**Key design principle:** When no OpenTelemetry SDK is configured by the user, `UseOpenTelemetry()` has zero overhead — `HasListeners()` returns false and all recording short-circuits to boolean checks. The middleware sits dormant until the user explicitly configures OpenTelemetry in their app.

## Current State

### What Exists

1. **Audit Log System** — `AIAuditingChatClient` / `AIAuditingEmbeddingGenerator` captures per-request governance data (user, profile, provider, model, tokens, errors, duration, parent-child nesting). Persisted to SQL Server/SQLite via EF Core.
2. **Usage Analytics** — `AIUsageRecordingChatClient` / `AIUsageRecordingEmbeddingGenerator` for lightweight token/duration tracking with hourly/daily aggregation.
3. **Response Tracking** — `AITrackingChatClient` captures `UsageDetails` and response messages.
4. **TraceIdentifier** — API model accepting either local GUID or OpenTelemetry TraceId string.

### What's Missing

1. **No `Activity` spans** — External APM tools see nothing from Umbraco.AI operations.
2. **No `gen_ai.*` metrics** — No Prometheus/OTLP-compatible operation duration, token usage, or streaming latency metrics.
3. **No correlation between audit logs and distributed traces** — `AIAuditLog` doesn't capture `Activity.Current?.TraceId`.

---

## Implementation Steps

### Step 1: Add M.E.AI OpenTelemetry Chat Middleware

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Chat/Middleware/AIOpenTelemetryChatMiddleware.cs`

```csharp
public sealed class AIOpenTelemetryChatMiddleware : IAIChatMiddleware
{
    private readonly ILoggerFactory _loggerFactory;

    public AIOpenTelemetryChatMiddleware(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IChatClient Apply(IChatClient client)
    {
        return client.AsBuilder()
            .UseOpenTelemetry(_loggerFactory, sourceName: "Umbraco.AI")
            .Build();
    }
}
```

This wraps M.E.AI's `OpenTelemetryChatClient` which provides:
- **Tracing:** `gen_ai.chat {model}` spans with `ActivityKind.Client` and full `gen_ai.*` semantic convention tags (model, provider, tokens, temperature, finish reasons, tool definitions, etc.)
- **Metrics:** `gen_ai.client.token.usage`, `gen_ai.client.operation.duration`, `gen_ai.client.time_to_first_chunk`, `gen_ai.client.time_per_output_chunk`

All of this is zero-cost when no listener is configured.

**Source name `"Umbraco.AI"`** — consumers add `AddSource("Umbraco.AI")` and `AddMeter("Umbraco.AI")` to their OpenTelemetry configuration to opt in.

---

### Step 2: Add M.E.AI OpenTelemetry Embedding Middleware

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Embeddings/Middleware/AIOpenTelemetryEmbeddingMiddleware.cs`

```csharp
public sealed class AIOpenTelemetryEmbeddingMiddleware : IAIEmbeddingMiddleware
{
    private readonly ILoggerFactory _loggerFactory;

    public AIOpenTelemetryEmbeddingMiddleware(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IEmbeddingGenerator<string, Embedding<float>> Apply(
        IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return generator.AsBuilder()
            .UseOpenTelemetry(_loggerFactory, sourceName: "Umbraco.AI")
            .Build();
    }
}
```

Same pattern — M.E.AI's `OpenTelemetryEmbeddingGenerator` provides `gen_ai.embeddings` spans and token usage metrics.

---

### Step 3: Register Middleware in Pipeline

**File:** `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`

Insert the OpenTelemetry middleware as the **innermost** layer (closest to the provider), before everything else. This ensures the `gen_ai.*` span wraps the actual provider call, and all other middleware (tracking, usage recording, auditing) executes within that span's context.

```csharp
// Current:
builder.AIChatMiddleware()
    .Append<AIRuntimeContextInjectingChatMiddleware>()
    .Append<AIFunctionInvokingChatMiddleware>()
    .Append<AITrackingChatMiddleware>()
    .Append<AIUsageRecordingChatMiddleware>()
    .Append<AIAuditingChatMiddleware>()
    .Append<AIContextInjectingChatMiddleware>();

// New:
builder.AIChatMiddleware()
    .Append<AIOpenTelemetryChatMiddleware>()              // OpenTelemetry (innermost - wraps provider call)
    .Append<AIRuntimeContextInjectingChatMiddleware>()
    .Append<AIFunctionInvokingChatMiddleware>()
    .Append<AITrackingChatMiddleware>()
    .Append<AIUsageRecordingChatMiddleware>()
    .Append<AIAuditingChatMiddleware>()
    .Append<AIContextInjectingChatMiddleware>();

// Same for embeddings:
builder.AIEmbeddingMiddleware()
    .Append<AIOpenTelemetryEmbeddingMiddleware>()         // OpenTelemetry (innermost)
    .Append<AITrackingEmbeddingMiddleware>()
    .Append<AIUsageRecordingEmbeddingMiddleware>()
    .Append<AIAuditingEmbeddingMiddleware>();
```

**Why innermost:** The M.E.AI OpenTelemetry client creates the `Activity` span around the provider call. By placing it innermost, `Activity.Current` is available to all outer middleware — the auditing middleware can read it to capture `TraceId` and enrich with UAI-specific tags.

---

### Step 4: Enrich Activity with Umbraco-Specific Tags in Auditing Middleware

**Files:**
- `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingChatClient.cs`
- `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs`

In both `GetResponseAsync` / `GetStreamingResponseAsync` / `GenerateAsync`, after extracting the `AIAuditContext`, enrich the ambient activity:

```csharp
// After extracting auditLogContext and creating auditLog:
var activity = Activity.Current;
if (activity is not null)
{
    activity.SetTag("umbraco.ai.profile.id", auditLogContext.ProfileId?.ToString());
    activity.SetTag("umbraco.ai.profile.alias", auditLogContext.ProfileAlias);
    activity.SetTag("umbraco.ai.entity.id", auditLogContext.EntityId);
    activity.SetTag("umbraco.ai.entity.type", auditLogContext.EntityType);
    activity.SetTag("umbraco.ai.feature.type", auditLogContext.FeatureType);
    activity.SetTag("umbraco.ai.feature.id", auditLogContext.FeatureId?.ToString());
    activity.SetTag("umbraco.ai.audit.id", auditLog?.Id.ToString());
    activity.SetTag("umbraco.ai.user.id", auditLog?.UserId);
}
```

This adds Umbraco context to M.E.AI's `gen_ai.*` span without creating a separate span. APM tools show one span per AI operation with both standard GenAI attributes and Umbraco-specific context.

**When audit logging is disabled:** The tag enrichment should still run (it's independent of audit persistence). Extract the context-reading into a small helper that runs regardless of `_auditLogOptions.CurrentValue.Enabled`.

---

### Step 5: Bridge Audit Logs to OpenTelemetry TraceId

**Files:**
- `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/AIAuditLog.cs` — Add `string? TraceId` property
- `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingChatClient.cs` — Set `auditLog.TraceId = Activity.Current?.TraceId.ToString()`
- `Umbraco.AI/src/Umbraco.AI.Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs` — Same
- `Umbraco.AI/src/Umbraco.AI.Persistence/AuditLog/AIAuditLogEntity.cs` — Add `string? TraceId` column
- EF Core entity mapping — Map the new property
- **New migration:** `UmbracoAI_AddTraceIdToAuditLog` (both SqlServer and Sqlite) — nullable `nvarchar(32)` / `TEXT` column

Every audit log entry carries the TraceId (when an Activity is present), enabling:
- Backoffice "View in APM" links
- APM-to-governance correlation (search by TraceId to find the audit entry)

---

### Step 6: Expose Source Name Constant

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Telemetry/AITelemetry.cs`

```csharp
namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Constants for configuring OpenTelemetry to capture Umbraco.AI telemetry.
/// </summary>
/// <example>
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(AITelemetry.SourceName))
///     .WithMetrics(m => m.AddMeter(AITelemetry.SourceName));
/// </code>
/// </example>
public static class AITelemetry
{
    /// <summary>
    /// The source name for all Umbraco.AI tracing and metrics.
    /// Use with <c>AddSource()</c> and <c>AddMeter()</c> in your OpenTelemetry configuration.
    /// </summary>
    public const string SourceName = "Umbraco.AI";
}
```

Single constant, single name for both tracing and metrics — keeps consumer configuration simple.

---

### Step 7: Unit Tests

**New files:**

1. `Tests.Unit/Telemetry/AIOpenTelemetryChatMiddlewareTests.cs`
   - Register `ActivityListener` for source name `"Umbraco.AI"`
   - Invoke chat through the middleware pipeline with a mock inner client
   - Assert: `gen_ai.chat` activity created with M.E.AI standard tags
   - Assert: `umbraco.ai.*` tags present from auditing middleware enrichment
   - Assert: on failure, activity status is Error

2. `Tests.Unit/Telemetry/AuditLogTraceIdTests.cs`
   - With `ActivityListener`: audit log gets `TraceId` = `Activity.Current.TraceId`
   - Without listener: `TraceId` is null (no activity created)

3. `Tests.Unit/Telemetry/AIOpenTelemetryEmbeddingMiddlewareTests.cs`
   - Same pattern for embedding pipeline

---

### Step 8: Update Documentation

- **Modified:** `docs/public/extending/middleware/README.md` — Update to reflect that OpenTelemetry is wired up by default
- **New:** `docs/public/concepts/observability.md` — Observability guide covering:
  - What telemetry Umbraco.AI emits (M.E.AI `gen_ai.*` spans and metrics + `umbraco.ai.*` tags)
  - How to opt in: `AddSource("Umbraco.AI")` + `AddMeter("Umbraco.AI")`
  - Tag reference table (both standard `gen_ai.*` and custom `umbraco.ai.*`)
  - Example: Application Insights integration
  - Example: Prometheus + Grafana
  - Note: zero overhead when not configured

---

## Files Summary

| File | Change |
|------|--------|
| `Core/Chat/Middleware/AIOpenTelemetryChatMiddleware.cs` | **New** — wraps M.E.AI `UseOpenTelemetry()` |
| `Core/Embeddings/Middleware/AIOpenTelemetryEmbeddingMiddleware.cs` | **New** — wraps M.E.AI `UseOpenTelemetry()` for embeddings |
| `Core/Telemetry/AITelemetry.cs` | **New** — public source name constant |
| `Core/AuditLog/AIAuditLog.cs` | **Modified** — add `TraceId` property |
| `Core/AuditLog/Middleware/AIAuditingChatClient.cs` | **Modified** — enrich `Activity.Current` with UAI tags + capture TraceId |
| `Core/AuditLog/Middleware/AIAuditingEmbeddingGenerator.cs` | **Modified** — same enrichment + TraceId |
| `Core/Configuration/UmbracoBuilderExtensions.cs` | **Modified** — insert OTel middleware as innermost |
| `Persistence/AuditLog/AIAuditLogEntity.cs` | **Modified** — add `TraceId` column |
| Persistence EF mapping | **Modified** — map `TraceId` |
| `Persistence.SqlServer/Migrations/UmbracoAI_AddTraceIdToAuditLog` | **New** |
| `Persistence.Sqlite/Migrations/UmbracoAI_AddTraceIdToAuditLog` | **New** |
| `Tests.Unit/Telemetry/AIOpenTelemetryChatMiddlewareTests.cs` | **New** |
| `Tests.Unit/Telemetry/AIOpenTelemetryEmbeddingMiddlewareTests.cs` | **New** |
| `Tests.Unit/Telemetry/AuditLogTraceIdTests.cs` | **New** |
| `docs/public/extending/middleware/README.md` | **Modified** |
| `docs/public/concepts/observability.md` | **New** |

---

## What M.E.AI Provides (We Don't Rebuild)

| Concern | M.E.AI Handles | Source |
|---------|---------------|--------|
| `gen_ai.chat` / `gen_ai.embeddings` spans | `OpenTelemetryChatClient` / `OpenTelemetryEmbeddingGenerator` | ActivityKind.Client |
| Model, provider, temperature, tokens, finish reasons tags | Standard `gen_ai.*` semantic conventions | Automatic |
| `gen_ai.client.operation.duration` histogram | Seconds, tagged by operation/model/provider | Automatic |
| `gen_ai.client.token.usage` histogram | Input/output tokens | Automatic |
| `gen_ai.client.time_to_first_chunk` histogram | Streaming latency | Automatic |
| `gen_ai.client.time_per_output_chunk` histogram | Per-chunk streaming latency | Automatic |
| Sensitive data opt-in (prompts/responses in spans) | `EnableSensitiveData` flag | Configurable |

## What Umbraco.AI Adds (The UAI-Specific Part)

| Tag | Value | Purpose |
|-----|-------|---------|
| `umbraco.ai.profile.id` | Profile GUID | Which AI profile was used |
| `umbraco.ai.profile.alias` | Profile alias string | Human-readable profile identifier |
| `umbraco.ai.entity.id` | Content/media ID | What CMS entity the operation targeted |
| `umbraco.ai.entity.type` | "content", "media", etc. | CMS entity type |
| `umbraco.ai.feature.type` | "prompt", "agent", etc. | Which UAI feature initiated the call |
| `umbraco.ai.feature.id` | Feature GUID | Specific prompt/agent ID |
| `umbraco.ai.audit.id` | Audit log GUID | Link to governance record |
| `umbraco.ai.user.id` | Umbraco user key | Who initiated the operation |

---

## Intentionally Deferred

- **Custom `ActivitySource` / `Meter`** — Not needed. M.E.AI provides standard tracing and metrics; we enrich, not duplicate.
- **Per-tool-call spans (Tier 2)** — M.E.AI's function invocation client may add these in future. Can be revisited.
- **CRUD operation tracing** — Out of scope for AI operation telemetry.
- **`EnableSensitiveData` configuration** — Could be exposed via `AIAuditLogOptions` later. For now, defaults to off.
