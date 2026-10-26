# Brief

Source idea: `extraction-capability`. This feature builds the core capability, a Cloudflare
provider (Clairic only), the Automate "Extract Values" action, and (if cheap) a chat fallback. The idea's
Copilot and agent-tool uses are left for later.

## Problem

Umbraco AI can answer typed questions about content (Decision: yes/no, pick one, score). It can't
pull typed **values** out of content: an order number, a date, an amount, a list of names.

Today a developer who wants that has two options, both poor:

- Prompt a chat model to return JSON. It works, but it's slow, pays for output tokens, and can make
  up values that aren't in the text.
- Write their own parsing code for each kind of input.

In Umbraco Automate it's worse. An automation can react to a webhook, a media upload or a content
save, and it can branch with Decision. But it can't turn messy incoming text into the clean values
that later steps (Create Content, Update Content Property, Send Email) need. There's no step for it,
so these automations can't be built without code.

### Who it's for

- **Automate users** building automations in the backoffice. This is the headline audience and the
  reason for the feature: Extraction is pitched as an Automate upsell, alongside Decision.
- **Developers** building on Umbraco AI who want typed values from C# or backoffice TypeScript.
- **Backoffice users** only for setup: a Cloudflare connection, an Extraction profile, a default
  Extraction profile.

Not for: content editors directly (no editor-facing UI beyond Automate), or site visitors.

### Why now

- Cloudflare released Clairic (preview, 2026), a dedicated extraction model: typed values or
  nothing, input tokens only, usually under a second. Source:
  [Cloudflare blog](https://blog.cloudflare.com/clairic-extraction-model/).
- Decision shipped on 19-10-2026 (18.6.0 / 17.6.0) with Automate actions. Extraction is the
  missing half: Decision lets an automation choose, Extraction lets it fill in.
- Nothing equivalent is coming from Microsoft soon. M.E.AI has a document-reading proposal
  ([dotnet/extensions#7587](https://github.com/dotnet/extensions/issues/7587)) that only mentions
  typed-field extraction as a "future sibling proposal", not filed.

### Success looks like

1. A developer installs a new `Umbraco.AI.Cloudflare` provider package, turns on the Extraction
   experimental flag, creates a Cloudflare connection and an Extraction profile, and sets it as the
   default Extraction profile.
2. They get typed values back for every supported field type, live against Clairic, from:
   - **C#**: an `IAIExtractionService` (profile by id, alias, or default).
   - **TypeScript**: a public client exported from `@umbraco-ai/core`, like Decision's.
3. A field the content doesn't contain comes back as "not found", never a guessed value.
4. *Nice to have:* the same request also works against **any chat profile** (the chat fallback),
   so customers without Cloudflare can still use the feature. They lose confidence scores and text
   positions, not the API. Low priority: dropped or deferred if design finds it messy or costly
   (see Riskiest unknowns).
5. Umbraco Automate has an **Extract Values** action: a list of fields, bindable content, optional
   profile. Each field becomes an output that later steps can bind to and branch on.
6. At least three of the idea's Automate recipes can be built end to end on the demo site with no
   code (for example: webhook → extract → create content; audio → transcribe → extract → create
   content; ticket → extract → pick-one → approval on low confidence).
7. With the flag off (the default), Extraction is fully hidden and inert, like Decision.
8. Deploy carries the connection, profile and default-profile setting between environments.
9. Usage appears in analytics and audit logs like other capabilities.
10. Ships on **v18 and v17 together**, in the same release round, `Umbraco.AI.Automate` included.
11. Public docs for both lines, marked experimental, including the Automate action and at least one
    recipe.

> ASSUMPTION: Field types for v1 are text, number, date/time, duration and list. Clairic's
> `estimate` and `forecast` types are out (see Non-goals).

> ASSUMPTION: The first version accepts text input only. Images (Clairic takes up to four) are
> left out of v1.

### Constraints

- **Stays experimental**, the same way Decision is: a compile-time `[Experimental]` id and a
  runtime flag, default off. If Microsoft ships an abstraction, we migrate to it later.
- **Public API rules** apply to anything outside the experimental surface (settings fields, Deploy
  artifacts).
- **Provider naming:** the package is named after the company, `Umbraco.AI.Cloudflare`.
- **No .NET SDK from Cloudflare** (official SDKs are Go, TypeScript and Python). Clairic is called
  directly over HTTP, the way `Umbraco.AI.TypeSafe` calls Jev.
- **Cloudflare connection needs an account ID plus an API token.** The account ID is part of the
  URL (`https://api.cloudflare.com/client/v4/accounts/{account_id}/ai/run/@cf/cloudflare/clairic`).
- **Targets:** `net10.0`, CMS 18.x on v18 and CMS 17.x on v17. No forward-merge; v17 gets its own
  port branch and PR.
- **TypeSafe can't be a provider.** Its `/v1/systemone` API only supports yes/no, choice, score and
  image-box questions ([docs](https://docs.typesafe.ai/api)), checked 20-10-2026.

### Riskiest unknowns

1. **Clairic is in preview.** Its API may change, and there's no stated date for general
   availability. TODO: decide whether we're willing to ship (experimental) on a preview API, and
   what we do if it changes under us.
2. **Whether the chat fallback is worth it.** Capabilities today come from providers. A fallback
   that makes every chat profile "support Extraction" may confuse the connection and profile
   pickers. It's low priority: if design finds it messy or a lot of effort, downgrade it to a
   follow-up rather than bend the design around it. TODO: design decides keep or defer.
3. **Automate's dynamic outputs.** The action needs one output per field, typed. `Ask Questions`
   already does dynamic outputs for Decision, but unknown whether number/date/list outputs bind
   cleanly into later steps (Automate only resolves bindings on string settings today, per
   `AskDecisionsSettings`). TODO: check in design.
4. **Real cost and speed.** The only figures are Cloudflare's own ($0.10 per million input tokens,
   about 3x faster than an all-LLM pipeline in their example). Not measured by us.
5. **v17 parity.** Unknown whether Automate's dynamic-output support is the same on the v17 line.

> ASSUMPTION: The smallest version worth shipping is Core capability + Cloudflare (Clairic) +
> the Automate action. The chat fallback is not required. Without the Automate action the upsell doesn't exist, so
> it isn't optional.

> ASSUMPTION: Kill criteria: (a) Cloudflare withdraws Clairic or makes it unavailable outside
> Workers; (b) the chat fallback turns out good enough that a dedicated provider adds nothing
> customers would pay for; (c) Automate can't expose typed per-field outputs without major
> Automate changes.

## Non-goals

- **Cloudflare chat, embeddings and Clef (decision).** A full Cloudflare provider is a separate
  feature. This one adds only what Extraction needs. Keeps scope small and lets the provider grow
  later.
- **Estimate and forecast field types.** They return distributions, not values, and serve a
  different use case. Revisit if a real need shows up.
- **Image input.** Kept out of v1 to keep the request shape and the Automate settings simple.
- **Copilot, agent tools, Prompts.** Listed in the idea, but not needed for the Automate upsell.
- **Reading files.** Getting text out of PDFs and Office files is the existing file-processing
  pipeline's job (`IAIFileProcessingHandler`). Extraction takes text that's already been read.
- **Changes to Decision.** Extraction is its own capability, not new Decision question types.
