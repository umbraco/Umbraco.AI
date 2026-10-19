# Brief

## Problem

### What

Choosing a model is where cost, quality and footprint get decided, but the profile editor's Model
field is a bare `uui-select` showing only model names
(`profile/workspace/profile/views/profile-details-workspace-view.element.ts:465-479`). An editor
picking between ~100 OpenAI models or 300+ OpenRouter models has no price, size, context window,
retirement or footprint information at the point of choice. They have to look it up elsewhere, or
guess.

Useful facts exist, but nothing can show them:

- **Packages.** Umbraco.Community.AI.Carbon already estimates CO2e per model (EcoLogits data,
  in-memory lookups). Its only option today is a separate `workspaceFooterApp` or tab, not next to
  the Model field. There is no extension slot, and the profile workspace context and model types
  aren't public exports.
- **Core / providers.** Some provider APIs already return facts that core throws away:
  - OpenRouter's models API returns `context_length` and `pricing`; the DTO
    (`OpenRouterModelsResponse.cs`) only reads `id`, `name`, `supported_parameters`.
  - Anthropic's models API returns max tokens (`AnthropicModelCapability`), used internally only.
- **Existing channel.** `AIModelDescriptor.Metadata` already rides on the model list, but it's a
  provider-owned, governed key set (`AIModelMetadataKeys`, settings-support and image keys) meant
  for machine use (pruning settings), not display. Other packages can't add to it.

### Who

- **Primary: backoffice users who configure profiles** (site admins / AI implementers). They choose
  the model and live with its cost and footprint.
- **Secondary: package developers** who have per-model knowledge (pricing, carbon, compliance,
  hosting region) and want it shown in the right place without hacking the profile editor.
- **Not the audience:** content editors using AI features (they pick profiles, not models); site
  visitors.

### Why now

The idea's own gate ("wait for a second real consumer beyond Carbon") is met by core itself.
Context window and pricing from OpenRouter (and max tokens from Anthropic) are a real, cheap second
consumer, already sitting in API responses we discard. Carbon is the first package consumer and is
already published (18.0.0-beta.1 / 17.0.0-beta.1).

### Success looks like

- In the profile editor, after choosing a connection, the user can see at least one fact per model
  for models that have one (core-supplied for OpenRouter; Carbon-supplied when installed), without
  leaving the editor.
- Installing a fact-supplying package adds its facts with zero changes to core UI or the profile
  editor.
- Opening the model choice for a 300-model connection is not noticeably slower than today: the
  model list appears first; facts never block it.
- A failing or slow fact source never breaks choosing a model.
- The public contract a package implements is small enough to keep stable across majors (the
  "never break a public API" rule applies from day one).

> ASSUMPTION: No hard numeric target. "Not noticeably slower" means facts arrive within ~1 s of the
> model list on a warm cache, and the list itself is unchanged.

### Constraints

- Public API added to Umbraco.AI.Core is a long-term commitment (obsolete-for-two-majors rule).
- Must follow the backoffice convention: mirror an existing picker/modal, not hand-rolled UI. The
  public `UAI_ITEM_PICKER_MODAL` exists but is a list, not a table; no table-in-modal precedent in
  this repo.
- Facts shown are estimates or third-party data (price, CO2e). Display must not read as a
  guarantee.
- **v18 only first.** The public contract settles on one line before any v17 backport, so contract
  changes during the first release don't have to be made twice. A v17 backport is a later,
  separate decision.

### Riskiest unknowns

1. **Model identity across sources.** A fact source must match core's `providerId/modelId` to its
   own data. Carbon already solves this with its own resolver chain; a pricing package would need
   the same. Unknown whether core should help (e.g. normalised ids) or leave it to each source.
2. **Overlap with `AIModelDescriptor.Metadata`.** Two channels for "data about a model" could
   confuse package authors. Whether facts reuse, sit beside, or absorb Metadata is the biggest
   design call.
3. **Picker scope.** Replacing the dropdown with a table picker is more core UI work than the facts
   themselves, with no table-in-modal precedent here. It could dominate the effort.
4. **Context the facts need.** Capability alone may not be enough. Region-dependent facts (pricing,
   electricity zone) may need the connection. Carbon today uses per-provider zones plus a site-wide
   override, so it doesn't need the connection; a pricing package might.
5. **Wording/legal.** Showing CO2e or prices in core UI, even when supplied by a package, may need
   a wording review (Carbon's public CO2 wording review is still open).

### Smallest version worth shipping

The public server contract for facts, core's own OpenRouter facts (context window, price), and
display of the **selected model's** facts under the Model field. The sortable table picker is a
second slice, built once facts exist to fill its columns.

### Replacing or extending

Extends the existing profile editor Model field and the model list pipeline. Doesn't replace
anything public. If the picker ships, it replaces the internal `uui-select` (not public API).

### What would kill it

- If fact sources can't reliably match models (identity problem too hard), facts would show for
  too few models to be worth the public contract.
- If the only real consumers stay Carbon + one provider's data, a small Carbon-side footer app is
  cheaper.

## Non-goals

- **Free-form UI slot** for packages under the Model field. Rejected in the idea: inconsistent
  layout, accessibility left to each package, facts can't be reused, contract shaped by one
  consumer.
- **Model recommendations or auto-selection** ("pick the cheapest"). Facts inform; they don't
  decide.
- **Live cost tracking or budgets.** That's usage/analytics, not model choice.
- **Side-by-side model comparison view** and **Copilot explaining model choices.** Later reuse of
  the same data, not part of this.
- **Facts on profile pickers** (agents, prompts, settings). Users pick profiles there, not models.
- **Carbon package changes.** Carbon adopting the contract is its own work in its own repo.
- **Populating facts for every provider.** Core adds facts only where the provider API already
  returns them cheaply.
