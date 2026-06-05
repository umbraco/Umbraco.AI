# Observability Systems in Umbraco.AI

Umbraco.AI has four related-but-distinct systems that record information about AI operations.
They are easy to confuse — especially the two that share the word "telemetry" — so this doc
maps where each one lives, what it captures, and where the data goes.

| System | Namespace / key types | Data destination | Contains | Gated by |
| --- | --- | --- | --- | --- |
| **OpenTelemetry** | `Umbraco.AI.Core.Telemetry.AITelemetry`, `AIOpenTelemetry*Middleware` | The **host application's own** APM/tracing backend | Full operational detail: `gen_ai.*` spans, token metrics, profile/user/entity tags | Host opts in via `AddOpenTelemetry()`; zero-cost when unconfigured |
| **Audit log** | `Umbraco.AI.Core.AuditLog`, `IAIAuditLogService`, `AIAuditing*Middleware` | Local database (`AIAuditLogEntity`) | Per-call forensic record: who/what/when, prompt & response snapshots, errors, trace ID | `Umbraco:AI:AuditLog:Enabled` (default true) |
| **Usage analytics** | `Umbraco.AI.Core.Analytics.Usage`, `IAIUsageAnalyticsService`, hourly/daily rollup jobs | Local database (usage records + aggregated statistics) | Aggregate counts/tokens/durations broken down by provider, model, profile, user | `Umbraco:AI:Analytics:Enabled` (default true) |
| **Usage telemetry** | `Umbraco.AI.Core.Telemetry.AIUsageTelemetryProvider` (+ per-add-on providers) | **Umbraco HQ** via the CMS telemetry pipeline | Anonymous aggregate counts and configuration shape only — see whitelist below | CMS `TelemetryLevel.Detailed` **and** `Umbraco:AI:Telemetry:Enabled` (default true) |

The first three never leave the customer's environment. Only the last one reports externally,
and it is built **on top of** usage analytics (it reads the 30-day daily rollups rather than
collecting anything itself).

## Usage telemetry: how it works

Umbraco.AI does not have its own telemetry endpoint, site identifier, scheduler, or consent UI.
Each product registers an implementation of the CMS extension point
`Umbraco.Cms.Infrastructure.Telemetry.Interfaces.IDetailedTelemetryProvider`:

| Product | Provider | Registered in |
| --- | --- | --- |
| Umbraco.AI | `AIUsageTelemetryProvider` | `AddUmbracoAICore()` |
| Umbraco.AI.Prompt | `AIPromptUsageTelemetryProvider` | `AddUmbracoAIPromptCore()` |
| Umbraco.AI.Agent | `AIAgentUsageTelemetryProvider` | `AddUmbracoAIAgentCore()` |
| Umbraco.AI.Search | `AISearchUsageTelemetryProvider` | `UmbracoAISearchComposer` |

The CMS `ReportSiteJob` runs daily, gathers all registered providers, and POSTs a single
anonymized payload (random per-install GUID, no domain, no PII) to Umbraco's telemetry
service. This is the same mechanism Umbraco Commerce uses. Consent is inherited from the CMS:

- **Minimal** — site GUID only; no Umbraco.AI data.
- **Basic** — CMS version + installed package list (the `Umbraco.AI.*` packages appear here
  with their versions); still no detailed Umbraco.AI data.
- **Detailed** — detailed providers run, including ours.

On top of the CMS level, `Umbraco:AI:Telemetry:Enabled` (bound to `AIUsageTelemetryOptions`)
is an AI-specific kill switch checked by every provider. Setting it to `false` suppresses all
Umbraco.AI usage telemetry regardless of the CMS telemetry level.

## What is reported (whitelist)

The complete key list lives in code — one constants class per product, which unit tests
enforce as a whitelist (`Umbraco.AI.Tests.Unit/Telemetry/`):

- `AIUsageTelemetryConstants` (Core) — installed/connected provider IDs, connection count,
  profile counts (total + per capability), context/guardrail counts, guardrail evaluator IDs
  in use, test count, test run count, test feature and grader type IDs in use,
  default-profile configuration, audit/analytics enablement, 30-day request count and
  success rate. Plus extension registration counts via `AIExtensionUsageTelemetryProvider`:
  tool counts (total + custom), context resource type counts (total + custom), and custom
  middleware counts per pipeline.

### System vs custom extension IDs

Extension point IDs are developer-authored and can encode business information (a tool ID
like "send-to-acme-erp"), so `AIUsageTelemetryClassification` splits every reported ID set
by the implementing type's assembly: types in `Umbraco.*` assemblies are system and their
IDs may be reported verbatim; everything else (and any unregistered ID found in stored
config) is reported only as a distinct count (`*CustomCount` keys). Tool IDs are never
reported at all — only counts. Middleware pipelines and `Default{Capability}ProfileAlias`
options are discovered by reflection, so new capabilities flow into telemetry without code
changes here.
- `AIPromptUsageTelemetryConstants` — prompt counts (total/active/linkage) and display modes.
- `AIAgentUsageTelemetryConstants` — agent counts (total/active/per-type/linkage) and
  code-authored surface IDs.
- `AISearchUsageTelemetryConstants` — vector entry count.

## What is never reported

These rules are deliberate and enforced by tests; treat them as policy, not implementation
detail:

- **No token totals** — they are a proxy for customer spend.
- **No user-authored names or aliases** — profile/prompt/agent/connection aliases can encode
  business information. Counts only.
- **No model IDs** — model IDs can be user-authored (Azure AI Foundry deployment names,
  OpenAI fine-tune names, self-hosted models), so they are not reported at all. Provider
  IDs and per-capability profile counts carry the demand signal instead. (A hardcoded
  model-family normalization map was considered and rejected as unscalable; validating
  against provider catalogs was rejected because catalogs can contain user-authored
  deployment names and fetching them makes vendor API calls with customer credentials.
  If model-level data is ever needed, the right mechanism is a provider-owned hook so
  each provider package declares its own public-catalog knowledge.)
- **No content** — no prompt instructions, system messages, snapshots, context resources, or
  indexed documents.
- **No identities** — no user IDs/names, no entity IDs, no per-user breakdowns.
- **No connection settings** — providers never read `AIConnection.Settings`.
- **No error messages** — failure *counts* only (via success rate).

## Adding a new metric

1. Add the key to the product's `*UsageTelemetryConstants` class (this updates the test
   whitelist automatically for Core; add-on tests should assert their own constants).
2. Emit it from the product's provider. Wrap the collection in the existing best-effort
   pattern — providers must never throw into the CMS `ReportSiteJob`.
3. Check the value against the "never reported" list above. If it's a string that a
   backoffice user typed, it doesn't ship.
4. Coordinate with the team that owns telemetry ingestion at HQ so the new key is dashboarded.
