# Architecture

Scope: slice 1 from `BRIEF.md` (the public contract, OpenRouter facts, and the selected model's facts
under the Model field) on **v18 only**. The sortable table picker (slice 2) is sketched at the end so
slice 1 doesn't paint it into a corner, but it isn't designed in detail here.

## Extension points

### Server: a new ordered collection of fact providers (Umbraco.AI.Core)

Packages plug in through a new **ordered collection builder**, the same pattern as
`AIFileProcessingHandlerCollectionBuilder` (`Umbraco.AI.Core/FileProcessing/`):

```csharp
// Umbraco.AI.Core/ModelFacts/
public class AIModelFactProviderCollectionBuilder
    : OrderedCollectionBuilderBase<AIModelFactProviderCollectionBuilder, AIModelFactProviderCollection, IAIModelFactProvider>
{ protected override AIModelFactProviderCollectionBuilder This => this; }

// Umbraco.AI.Extensions, UmbracoBuilderExtensions.ModelFacts.cs
public static AIModelFactProviderCollectionBuilder AIModelFactProviders(this IUmbracoBuilder builder)
    => builder.WithCollectionBuilder<AIModelFactProviderCollectionBuilder>();
```

- Ordered, not type-scanned: registration order is display order, so a package decides with
  `.Append<T>()` / `.InsertBefore<…>()`. Core appends its built-in provider in `AddUmbracoAICore`.
- Rejected: `LazyCollectionBuilderBase` + attribute discovery (like `AIProviderCollectionBuilder`).
  Discovery gives no ordering control, and order matters for display.

### Server: one Management API endpoint, scoped to a connection

`GET /umbraco/ai/management/api/v1/connections/{connectionIdOrAlias}/model-facts?capability=…&modelId=…`,
a new `ModelFactsConnectionController` beside `ModelsConnectionController`. Details in `SPEC.md`.

### Backoffice: a core-rendered element inside the existing Profile Settings view

A new internal `<uai-model-facts>` element rendered in the Model field's `slot="editor"` in
`profile-details-workspace-view.element.ts`, under the existing `uui-select`. Not an extension slot.
Packages contribute data only.

## Data model & persistence

None. Facts are computed on request and held in the runtime cache. Nothing is stored in the
database, so no migrations, no SQL Server/SQLite work.

### New public types (Umbraco.AI.Core, namespace `Umbraco.AI.Core.ModelFacts`)

```csharp
public interface IAIModelFactProvider
{
    /// How long core may cache this provider's facts for one model. TimeSpan.Zero = don't cache.
    TimeSpan CacheDuration { get; }

    /// Facts keyed by model id. Models with no facts are left out.
    Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context,
        IReadOnlyList<AIModelDescriptor> models,
        CancellationToken cancellationToken);
}

public sealed class AIModelFactContext
{
    public required Guid ConnectionId { get; init; }
    public required string ProviderId { get; init; }
    public required AICapability Capability { get; init; }
}

public sealed class AIModelFact
{
    public required string Key { get; init; }        // "openrouter.contextWindow"; unique per fact, later the column id
    public required string Label { get; init; }      // "#uaiModelFacts_contextWindow" or plain text
    public string? ShortLabel { get; init; }         // column title in slice 2; falls back to Label
    public required string Value { get; init; }      // display text, already formatted
    public double? SortValue { get; init; }          // slice 2 sorting; null = not sortable
    public string? Detail { get; init; }             // tooltip text, may be a #key
    public AIModelFactTone Tone { get; init; } = AIModelFactTone.Neutral;
    public string? Url { get; init; }                // optional "learn more", http/https only
}

public enum AIModelFactTone { Neutral, Positive, Warning }

// Core-internal aggregate service, used by the controller.
public interface IAIModelFactService
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context, IReadOnlyList<AIModelDescriptor> models, CancellationToken cancellationToken);
}
```

### New Metadata keys (provider-owned, machine-readable)

Added to `AIModelMetadataKeys`, each with a reader in `AIModelDescriptorExtensions` (the file's own
rule: no key without a reader):

| Key | Value | Reader |
|-----|-------|--------|
| `model.contextWindow` | integer tokens, invariant culture | `GetContextWindow()` → `int?` |
| `pricing.inputPerMillionTokens` | decimal, invariant culture | `GetPricing()` → `AIModelPricing?` |
| `pricing.outputPerMillionTokens` | decimal, invariant culture | (same) |
| `pricing.currency` | ISO 4217, e.g. `USD` | (same) |

`AIModelPricing` is a small public record `(decimal InputPerMillionTokens, decimal OutputPerMillionTokens, string Currency)`.
A writer helper (`AIModelMetadata.ForContextWindow(...)` / `.ForPricing(...)` or a small builder)
keeps the formatting in one place, the way `AIModelSettingsSupport.ToMetadata()` does for the
settings keys.

> ASSUMPTION: OpenRouter's `GET /models` returns `context_length` (number) and `pricing.prompt` /
> `pricing.completion` (per-token USD as strings). Verify against a live response in the first
> build task; multiply by 1,000,000 for per-million. A model with pricing "0" or missing is shown
> as no price fact, not "$0" (free and unknown look the same otherwise).

## Connected systems

| System | Applies? | Why |
|--------|----------|-----|
| Database / migrations | No | Nothing persisted. |
| Deploy connectors | No | No new entity or stored setting. |
| Cache refreshers (load-balanced) | No | Per-server runtime cache of derived data. Every server computes the same thing; nothing to invalidate across servers. A data change in a package ships with a package upgrade, which restarts the app. |
| Notifications | No | Read-only data; no lifecycle to announce. |
| Audit log / version history | No | Nothing changes. |
| Authorization | Yes | Same policy as `GET connections/{id}/models` (`SectionAccessAI`). Facts are only shown inside the AI section's profile editor. |
| OpenAPI client | Yes | Regenerate the core client (`npm run generate-client`) for the new endpoint. |
| Frontend localization (`en.ts`) | Yes | Core's built-in labels use `#uaiModelFacts_*` keys. |
| Provider package version ranges | Yes | `Umbraco.AI.OpenRouter` uses the new Metadata keys, so its `Umbraco.AI.Core` floor rises to the release that adds them. Co-release both. |
| Public docs (Umbraco.Docs) | Yes, later | New public extension point; a docs PR once the contract is final. |
| `docs/ideas/model-facts.md` | Yes | Mark as promoted to this plan once PR #518 merges. |

## Key decisions

1. **Metadata and facts are two layers.** Providers write raw, machine-readable values to
   `AIModelDescriptor.Metadata` (new governed keys above). Facts are the display layer. Core ships
   one built-in `AIMetadataModelFactProvider` that turns well-known keys into facts. Packages add
   their own fact providers and never write Metadata. *Rejected:* facts stored in Metadata
   (packages can't write it, strings can't carry label/tone/detail/sort); facts replacing Metadata
   (Metadata drives settings pruning and needs machine values).

2. **Endpoint is scoped to a connection, not a free-floating `POST /model-facts`.** The idea proposed
   `POST /v1/model-facts { capability, models[] }`. That can't serve the built-in Metadata facts:
   it only has model refs, not descriptors, so core would have nothing to read. Scoping to the
   connection lets the server load descriptors itself (provider listings are already cached for an
   hour in each provider) and hand fact providers the **full descriptors**, so a package can read
   Metadata too. It also gives `ConnectionId` to the context for free, which answers the brief's
   "do facts need the connection?" risk without widening the request. *Rejected:* the idea's POST
   (can't do Metadata facts; client must send the list back); adding facts into the existing
   `GET …/models` response (a slow fact provider would delay the model list, breaking "facts never
   block the list").

3. **GET with an optional `modelId`, not POST.** Slice 1 asks for one model (the selected one).
   Slice 2's picker asks for all (no `modelId`). Neither needs a long query string, so the POST
   reason in the idea goes away.

4. **Context and fact are classes with `required`/`init` properties, not positional records.**
   Adding a positional parameter later breaks the constructor for every package compiled against
   it. Adding an optional `init` property doesn't. Same reason `AIModelFactContext` is a class:
   new context (culture, user) can be added later without a breaking change.

5. **Facts keyed by model id string, not `AIModelRef`.** One connection means one provider, so the
   model id is unique. `AIModelRef` doesn't implement `IEquatable<>`, so it's a poor dictionary key.

6. **Core caches per model, and the provider sets the duration.** Cache key:
   `factProviderType + connectionId + capability + modelId`. On a request, core passes only the
   uncached models to each provider. Uses `AppCaches.RuntimeCache` (the `IAppPolicyCache`
   `AISettingsService` already uses). Built-in Metadata provider: `TimeSpan.Zero`, because the
   descriptors are already cached by the provider and the projection is trivial.

7. **Each fact provider runs with a time limit, in parallel, and fails closed.** A per-provider
   timeout (linked `CancellationTokenSource`), all providers run concurrently, and any exception or
   timeout is logged at Warning with the provider type and skipped. The endpoint still returns 200
   with the facts that did arrive.

   > ASSUMPTION: 2 seconds per provider, set in `AIModelFactOptions.ProviderTimeout`
   > (`Umbraco:AI:ModelFacts:ProviderTimeout`), not hard-coded.

8. **Display rules live in core, not in providers.** Order: `Warning` tone first, then
   provider registration order, then the provider's own order. At most **6 facts** in the
   under-field view. Nothing renders when there are no facts. (A per-provider cap would need the
   response to say which provider each fact came from; not worth it for one model's facts.)

9. **Localization follows the existing `localize.string()` convention.** `Label`, `ShortLabel` and
   `Detail` may be a `#key` (the frontend already does this for server-supplied field labels in
   `model-editor.element.ts`). `Value` is display text formatted by the provider. Core's built-in
   values stay short and mostly numeric ("200,000", "$3.00 / $15.00") with the unit in the
   localized label ("Context window (tokens)", "Price per 1M tokens, in / out").

   > ASSUMPTION: Server-side number formatting uses invariant-style grouping. No server-side
   > culture plumbing in slice 1.

10. **Security.** All fact text is rendered with Lit text bindings, never `unsafeHTML`. `Url` is shown
    only if it parses as absolute `http`/`https`, opened with `target="_blank" rel="noopener noreferrer"`.
    Fact providers get the connection id and provider id, never connection settings or secrets.

11. **Component convention: keep the `uui-select` for slice 1.** Umbraco's convention for a long
    reference list is a picker modal. The bare select stays for now as a deliberate, scoped choice:
    slice 1 only adds facts for the selected model under the field. Slice 2 replaces it with the
    picker. There's no table-in-modal precedent in this repo (`UAI_ITEM_PICKER_MODAL` is a
    `uui-ref-list`), so slice 2 must first find one in CMS core to mirror.

12. **`@umbraco-ai/core` frontend exports unchanged.** `<uai-model-facts>`, its repository and the
    fact types stay internal. Packages contribute on the server only.

### Slice 2 sketch (not designed here)

The picker calls the same endpoint without `modelId`. Columns = distinct fact `Key`s, titled by
`ShortLabel ?? Label`, sortable where any row has a `SortValue`. Warning facts show as tags on the
row. Open: the table-in-modal pattern to mirror, how many columns fit, and whether column choice is
remembered.
