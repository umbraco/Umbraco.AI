[Plan folder](docs/plans/model-facts/) | Source idea: #518

## Why the change

Editors pick a model from a bare dropdown with no idea what it costs or how much it can hold, so this adds a small set of "model facts" (context window and price to start) shown under the profile's Model field, with a public extension point so packages can add their own facts (CO2e, retirement dates, and so on).

## Special things to note

- **Needs a decision:** `Umbraco.AI.OpenRouter` now writes the new Metadata keys, so its `Umbraco.AI.Core` floor (`[18.5.2, …)`) must be raised to the release that ships this, and the two co-released. Not changed in this PR, because the version isn't known yet.
- **Needs a decision:** price wording. The price note says "Per 1M tokens, input / output. From the provider; may be out of date." It should get a wording review (like Carbon's) before it's in public docs, so it can't be read as a quote.
- **Needs a decision:** `docs/ideas/model-facts.md` lives in the still-open #518. Either merge #518 and mark the idea as promoted, or close it and let this plan folder replace it.
- The facts endpoint is `GET connections/{id}/model-facts?capability=&modelId=`, not the idea's `POST /model-facts`. The server needs each model's full descriptor (Metadata) for the built-in facts, which a POST of model refs can't give it. Approved during design.
- Numeric capability values (`?capability=1`) get a 400. That's stricter than the sibling `GET …/models` endpoint; the backoffice always sends names.
- Fact providers resolve as singletons, so they can't inject scoped services. Packages must register with `[ComposeAfter(typeof(UmbracoAIComposer))]` so the built-in provider stays first. Both go in the public docs PR (follow-up).
- A provider that writes a zero price shows "$0.00" (an honest "free"); only OpenRouter's writer drops all-zero prices, because there zero also means "router, price unknown". Prices under half a cent per 1M tokens also round to $0.00; revisit with the slice 2 picker.
- New public contract (`IAIModelFactProvider`, `AIModelFact`, `AIModelFactContext`, `AIModelFactTone`, collection builder, `AIModelPricing`, Metadata keys and readers). `AIModelFact`/`AIModelFactContext` are classes with `init` properties so new members can be added later as optional properties without breaking packages.
- OpenRouter gets its first unit test project; it's added to the root `Umbraco.AI.slnx` so CI runs it.
- The last commit (compact, borderless info button) is styling only and was checked live but didn't go through the per-task reviewer.
- v18 only. A v17 backport waits until the contract has settled here. No migrations; nothing is stored.

## Change outline

The new public contract in Umbraco.AI.Core. Packages implement a provider and append it to an ordered collection; order is display order.

```csharp
// Umbraco.AI.Core/ModelFacts/
public interface IAIModelFactProvider
{
    TimeSpan CacheDuration { get; }   // TimeSpan.Zero = don't cache
    Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context, IReadOnlyList<AIModelDescriptor> models, CancellationToken ct);
}

public sealed class AIModelFact
{
    required string Key; required string Label; string? ShortLabel;
    required string Value; double? SortValue; string? Detail;
    AIModelFactTone Tone = Neutral; string? Url;   // http(s) only
}

builder.AIModelFactProviders().Append<MyFactProvider>();
```

Providers write raw values to `AIModelDescriptor.Metadata`; a built-in fact provider turns them into facts.

```diff
 AIModelMetadataKeys
+  model.contextWindow              -> GetContextWindow() : int?
+  pricing.inputPerMillionTokens    -> GetPricing() : AIModelPricing?
+  pricing.outputPerMillionTokens
+  pricing.currency                 (ISO 4217)
+AIModelMetadata.ForContextWindow(int) / ForPricing(AIModelPricing)   // invariant-culture writers
```

How a request flows, from the profile editor to the providers and back.

```
<uai-model-facts connectionId capability modelId>          (profile Model field)
└─ UaiConnectionModelFactsRepository.requestModelFacts     (no toast on failure, console.warn)
   └─ GET connections/{idOrAlias}/model-facts?capability=Chat&modelId=…
      ModelFactsConnectionController                         400 bad capability, 404 unknown connection
      ├─ list the connection's models for the capability    (failure logged, 200 + empty)
      └─ IAIModelFactService.GetModelFactsAsync
         ├─ per provider: cache by provider + connection + capability + model
         ├─ uncached models only, all providers in parallel, 2 s timeout each
         │  ├─ AIMetadataModelFactProvider (core, first)  -> core.contextWindow, core.price
         │  └─ …package providers
         ├─ failures/timeouts logged and skipped (not cached)
         └─ Warning facts first, non-http(s) urls dropped
```

OpenRouter now reads `context_length` and `pricing.prompt`/`completion` from its model list (leniently, so one odd value never breaks the list) and writes the new keys.

```diff
 OpenRouterModelInfo
+  context_length            -> model.contextWindow
+  pricing.prompt/completion -> pricing.* (per token × 1,000,000, USD; all-zero or invalid = no price)
```

Under the Model field the editor sees up to six facts, with a small info button for any fact that has a detail note.

```
Model  [ anthropic/claude-sonnet-4        ▾ ]
       Context window (tokens)            200,000
       Price per 1M tokens (in / out)     $3.00 / $15.00  ⓘ
```

## Verified

- Unit: Core 1556/1556, integration 30/30, OpenRouter 13/13, frontend 62/62; `build:core` green.
- Live on the demo site against OpenRouter's real model list (469 models): Claude Sonnet 4 shows both facts, free models show context window only, an OpenAI connection shows nothing, switching connection clears the facts, no console errors.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
