# Stories

Slice 1 of Model Facts, v18 only. Derived from `SPEC.md`. Slice 2 (the sortable picker) is a
placeholder at the end.

## Definition of Ready / Definition of Done

**Ready**: role, capability and value stated and not hollow; Given/When/Then criteria cover the
happy path; out of scope is explicit; passes INVEST.

**Done**: every criterion passes as an executable spec (`bdd-specs`), sad paths included;
`dotnet build` + `dotnet test` green for Umbraco.AI and Umbraco.AI.OpenRouter; `npm run build:core`
green; wire tasks verified through a real request on the demo site; no change to
`@umbraco-ai/core` public frontend exports; no v17 backport (v18 only, per the brief).

> ASSUMPTION: These are the same Ready/Done shape as `decision-capability`. Correct here if the
> team wants more (e.g. docs PR as part of Done).

---

## MF-1: Package developers can supply facts about models

As a package developer with per-model knowledge (e.g. Carbon's CO2e estimates),
I want to register a fact provider that returns short facts for a list of models,
so that my data appears where admins choose a model, without touching the profile editor.

### Acceptance criteria

**AC1: Registered providers are resolved in registration order**
```
Given  two IAIModelFactProvider types appended via builder.AIModelFactProviders()
When   AIModelFactProviderCollection is resolved
Then   it contains both providers in the order they were appended
```

**AC2: Core's built-in provider is registered first**
```
Given  a default AddUmbracoAI() setup
When   AIModelFactProviderCollection is resolved
Then   its first item is AIMetadataModelFactProvider
```

**AC3: Facts from all providers are aggregated per model**
```
Given  provider A returns fact a1 for model m1 and provider B returns fact b1 for model m1
When   IAIModelFactService.GetModelFactsAsync is called for [m1]
Then   m1's facts are [a1, b1]
```

**AC4: Warning facts come first**
```
Given  provider A returns a Neutral fact and provider B returns a Warning fact for m1
When   IAIModelFactService.GetModelFactsAsync is called for [m1]
Then   m1's first fact is B's Warning fact
```

**AC5: Models with no facts are omitted**
```
Given  providers return facts for m1 only
When   IAIModelFactService.GetModelFactsAsync is called for [m1, m2]
Then   the result has no entry for m2
```

**AC6: Each provider is called once per request with only uncached models**
```
Given  provider A has CacheDuration 1 hour and m1's facts were fetched in an earlier call
When   IAIModelFactService.GetModelFactsAsync is called for [m1, m2]
Then   provider A is called once, with models [m2] only
```

**AC7: A cached empty result is not re-requested**
```
Given  provider A returned no facts for m1 in an earlier call, with CacheDuration 1 hour
When   IAIModelFactService.GetModelFactsAsync is called for [m1]
Then   provider A is not called
```

**AC8: Zero cache duration means no caching**
```
Given  provider A has CacheDuration TimeSpan.Zero
When   IAIModelFactService.GetModelFactsAsync is called twice for [m1]
Then   provider A is called twice
```

**AC9: The context carries connection, provider and capability**
```
Given  a request for connection c1 (provider "openrouter"), capability Chat
When   provider A is called
Then   its AIModelFactContext has ConnectionId c1, ProviderId "openrouter" and Capability Chat
```

### Sad path

**AC10: A throwing provider is skipped**
```
Given  provider A throws and provider B returns fact b1 for m1
When   IAIModelFactService.GetModelFactsAsync is called for [m1]
Then   m1's facts are [b1]
```

**AC11: A throwing provider is logged**
```
Given  provider A throws
When   IAIModelFactService.GetModelFactsAsync is called
Then   a Warning is logged naming provider A's type
```

**AC12: A slow provider is cut off at the timeout**
```
Given  ProviderTimeout is 50 ms and provider A takes 5 s
When   IAIModelFactService.GetModelFactsAsync is called for [m1]
Then   the call completes in well under 5 s, without provider A's facts
```

**AC13: Facts for models that weren't requested are ignored**
```
Given  provider A returns facts for m1 and m9
When   IAIModelFactService.GetModelFactsAsync is called for [m1]
Then   the result has no entry for m9
```

**AC14: Unsafe URLs are dropped**
```
Given  provider A returns a fact with Url "javascript:alert(1)"
When   IAIModelFactService.GetModelFactsAsync is called
Then   that fact's Url is null
```

---

## MF-2: Providers can declare a model's context window and price

As a provider package author,
I want standard Metadata keys and readers for context window and price,
so that the numbers my provider API already returns become usable by core without each package
inventing its own keys.

### Acceptance criteria

**AC1: Context window round-trips**
```
Given  a descriptor whose Metadata was written with a context window of 200000
When   GetContextWindow() is called
Then   it returns 200000
```

**AC2: Pricing round-trips**
```
Given  a descriptor whose Metadata was written with pricing 3.00 in / 15.00 out, USD
When   GetPricing() is called
Then   it returns AIModelPricing(3.00, 15.00, "USD")
```

**AC3: Values are written in invariant culture**
```
Given  the current culture is da-DK
When   pricing 3.5 in / 15 out is written to Metadata
Then   the pricing.inputPerMillionTokens value is "3.5"
```

### Sad path

**AC4: Missing context window reads as null**
```
Given  a descriptor with no model.contextWindow key
When   GetContextWindow() is called
Then   it returns null
```

**AC5: Malformed context window reads as null**
```
Given  model.contextWindow is "lots"
When   GetContextWindow() is called
Then   it returns null
```

**AC6: Partial pricing reads as null**
```
Given  Metadata has pricing.inputPerMillionTokens but no pricing.outputPerMillionTokens
When   GetPricing() is called
Then   it returns null
```

---

## MF-3: Core turns standard Metadata into facts

As a backoffice admin configuring a profile,
I want a model's context window and price shown as readable facts,
so that I can compare models without leaving the profile editor.

### Acceptance criteria

**AC1: Context window becomes a fact**
```
Given  a descriptor with model.contextWindow 200000
When   AIMetadataModelFactProvider is asked for its facts
Then   the model has a fact with key core.contextWindow, value "200,000" and SortValue 200000
```

**AC2: Context window fact uses a localization key label**
```
Given  a descriptor with model.contextWindow 200000
When   AIMetadataModelFactProvider is asked for its facts
Then   the core.contextWindow fact's Label is "#uaiModelFacts_contextWindow"
```

**AC3: USD price becomes a fact**
```
Given  a descriptor with pricing 3.00 in / 15.00 out, USD
When   AIMetadataModelFactProvider is asked for its facts
Then   the model has a fact with key core.price and value "$3.00 / $15.00"
```

**AC4: Price fact sorts by input price**
```
Given  a descriptor with pricing 3.00 in / 15.00 out, USD
When   AIMetadataModelFactProvider is asked for its facts
Then   the core.price fact's SortValue is 3.0
```

**AC5: Non-USD price uses the ISO code**
```
Given  a descriptor with pricing 2.50 in / 10.00 out, EUR
When   AIMetadataModelFactProvider is asked for its facts
Then   the core.price fact's value is "EUR 2.50 / EUR 10.00"
```

**AC6: Price fact carries the "may be out of date" detail**
```
Given  a descriptor with pricing
When   AIMetadataModelFactProvider is asked for its facts
Then   the core.price fact's Detail is "#uaiModelFacts_priceDetail"
```

**AC7: Built-in provider doesn't cache**
```
Given  AIMetadataModelFactProvider
When   CacheDuration is read
Then   it is TimeSpan.Zero
```

### Sad path

**AC8: No standard Metadata means no facts**
```
Given  a descriptor with only settings-support Metadata
When   AIMetadataModelFactProvider is asked for its facts
Then   the result has no entry for that model
```

**AC9: Malformed Metadata never throws**
```
Given  a descriptor with model.contextWindow "lots" and partial pricing
When   AIMetadataModelFactProvider is asked for its facts
Then   it returns without throwing and with no entry for that model
```

---

## MF-4: OpenRouter reports context window and price

As a backoffice admin using an OpenRouter connection,
I want each OpenRouter model's context window and price available,
so that I can choose between 300+ models on more than their name.

### Acceptance criteria

**AC1: Context length is written to Metadata**
```
Given  an OpenRouter listing entry with context_length 200000
When   the chat capability's model list is built
Then   that model's GetContextWindow() returns 200000
```

**AC2: Per-token pricing is written per million tokens in USD**
```
Given  an OpenRouter listing entry with pricing.prompt "0.000003" and pricing.completion "0.000015"
When   the chat capability's model list is built
Then   that model's GetPricing() returns (3.00, 15.00, "USD")
```

**AC3: Settings-support Metadata is still present**
```
Given  an OpenRouter listing entry with supported_parameters lacking "temperature"
When   the chat capability's model list is built
Then   that model's IsProfileSettingSupported("temperature") is false
```

### Sad path

**AC4: Missing context length writes no key**
```
Given  a listing entry with no context_length
When   the chat capability's model list is built
Then   that model's GetContextWindow() returns null
```

**AC5: Zero pricing writes no price**
```
Given  a listing entry with pricing.prompt "0" and pricing.completion "0"
When   the chat capability's model list is built
Then   that model's GetPricing() returns null
```

**AC6: Unparseable pricing writes no price**
```
Given  a listing entry with pricing.prompt "-1" or "n/a"
When   the chat capability's model list is built
Then   that model's GetPricing() returns null
```

---

## MF-5: The backoffice can fetch a connection's model facts

As the profile editor (on behalf of a backoffice admin),
I want one endpoint that returns facts for a connection's models,
so that the UI can show them without knowing which fact providers exist.

### Acceptance criteria

**AC1: Facts for one model**
```
Given  connection c1 lists models m1 and m2, and m1 has facts
When   GET connections/c1/model-facts?capability=Chat&modelId=m1 is requested
Then   the response is 200 with one item, for m1
```

**AC2: Facts for every model**
```
Given  connection c1 lists models m1 and m2, both with facts
When   GET connections/c1/model-facts?capability=Chat is requested
Then   the response is 200 with items for m1 and m2
```

**AC3: Fact fields are mapped**
```
Given  m1 has a fact with every field set
When   GET connections/c1/model-facts?capability=Chat&modelId=m1 is requested
Then   the item's fact has key, label, shortLabel, value, sortValue, detail, tone and url
```

**AC4: Works by alias**
```
Given  connection c1 has alias "my-openrouter"
When   GET connections/my-openrouter/model-facts?capability=Chat is requested
Then   the response is 200
```

### Sad path

**AC5: Unknown connection**
```
Given  no connection with id or alias "nope"
When   GET connections/nope/model-facts?capability=Chat is requested
Then   the response is 404
```

**AC6: Missing capability**
```
Given  connection c1
When   GET connections/c1/model-facts is requested with no capability
Then   the response is 400
```

**AC7: Unparseable capability**
```
Given  connection c1
When   GET connections/c1/model-facts?capability=Banana is requested
Then   the response is 400
```

**AC8: Unsupported capability**
```
Given  connection c1 has no Embedding capability
When   GET connections/c1/model-facts?capability=Embedding is requested
Then   the response is 200 with no items
```

**AC9: Model not in the list**
```
Given  connection c1 doesn't list model "ghost"
When   GET connections/c1/model-facts?capability=Chat&modelId=ghost is requested
Then   the response is 200 with no items
```

**AC10: Model listing fails**
```
Given  connection c1's capability throws when listing models
When   GET connections/c1/model-facts?capability=Chat is requested
Then   the response is 200 with no items
```

**AC11: Model listing failure is logged**
```
Given  connection c1's capability throws when listing models
When   GET connections/c1/model-facts?capability=Chat is requested
Then   a Warning is logged
```

**AC12: Requires AI section access**
```
Given  the controller
When   its authorization attributes are inspected
Then   it requires AIAuthorizationPolicies.SectionAccessAI
```

---

## MF-6: Admins see the selected model's facts under the Model field

As a backoffice admin configuring a profile,
I want the chosen model's facts shown under the Model field,
so that I understand its cost, size and caveats at the moment I choose it.

### Acceptance criteria

**AC1: Facts appear for the selected model**
```
Given  a profile with an OpenRouter connection and a model that has a context window
When   the Settings view is shown
Then   a "Context window (tokens)" row appears under the Model field
```

**AC2: Facts follow the selection**
```
Given  model m1's facts are shown
When   the admin selects model m2
Then   m2's facts replace m1's
```

**AC3: Warnings are listed first with a warning style**
```
Given  the selected model has a Neutral fact and a Warning fact
When   the facts render
Then   the Warning fact is first and styled as a warning
```

**AC4: Detail shows as a tooltip**
```
Given  a fact with Detail "#uaiModelFacts_priceDetail"
When   the admin hovers or focuses that fact
Then   the localized detail text is shown
```

**AC5: Safe links only**
```
Given  a fact with Url "https://example.com/model"
When   the facts render
Then   a "Learn more" link opens it in a new tab with rel="noopener noreferrer"
```

**AC6: At most six facts**
```
Given  the selected model has 8 facts
When   the facts render
Then   6 rows are shown
```

**AC7: Loading indicator while facts load**
```
Given  the admin has just selected a model
When   the facts request is in flight
Then   a loader shows in the facts area only
```

### Sad path

**AC8: Nothing renders without a model**
```
Given  a connection is chosen but no model
When   the Settings view is shown
Then   no facts area is rendered
```

**AC9: Nothing renders when there are no facts**
```
Given  the selected model has no facts
When   the facts load
Then   no facts area is rendered
```

**AC10: A failed request shows no error**
```
Given  the facts request fails
When   the Settings view is shown
Then   no facts and no error message appear under the Model field
```

**AC11: A stale response is ignored**
```
Given  the admin selects m1, then m2 before m1's facts arrive
When   m1's response arrives after m2's
Then   m2's facts are shown
```

**AC12: Changing connection clears facts**
```
Given  facts are shown for the selected model
When   the admin changes the connection
Then   no facts are shown
```

---

## Slice 2 (placeholder, not decomposed)

- **MF-7: Admins choose a model from a sortable picker.** Replace the Model `uui-select` with a
  picker modal: table of models, one column per fact key, sortable by `SortValue`, warning tags,
  search. Needs a table-in-modal precedent from CMS core first.
