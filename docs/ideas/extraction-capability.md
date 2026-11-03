# Extraction Capability - Future Consideration

## Status: Under Consideration

Raised 20-10-2026 after Cloudflare announced Clairic, a "System One extraction model"
([blog post](https://blog.cloudflare.com/clairic-extraction-model/)). This document explores adding an
**Extraction** capability to Umbraco.AI as a sibling to the Decision capability.

Nothing here is verified against a live provider yet. Figures and API shapes for Clairic come from the
blog post only.

---

## The Opportunity

The Decision capability answers questions about content: yes/no, pick one option, or a score. It
cannot pull **values** out of content. Typical asks it can't serve:

- "What is the order number in this email?"
- "What date does this event start?"
- "How much did the customer pay?"
- "List the product names mentioned."

Today the only way to do this is a chat model with a JSON schema (M.E.AI structured output). That
works, but it is slow, pays for output tokens, and can invent values that are not in the source text.

A dedicated extraction model does this in one pass. Per Cloudflare's post, Clairic:

- Returns a typed value or `null`, never free text.
- Charges for input tokens only.
- Usually answers in under a second.
- Reports how sure it is, and where in the text the value came from.

Cloudflare's own worked example (extract → decide → write a reply) ran about 3x faster and about half
the cost of an all-LLM pipeline. That is their benchmark, not ours.

### Why this matters most for Automate

Automate is where Extraction earns its keep. An automation already knows *when* something happened
(a trigger) and *what to do* (actions). What it can't do today is read messy, unstructured input and
turn it into the clean values the next step needs.

Decision gave automations a way to **branch** on content. Extraction gives them a way to **fill in**
values from content. Together they close the loop:

```
Trigger (Webhook, Media Saved, Content Saved, Scheduled, ...)
   ↓
Extract Values      → orderId, amount, startDate, customerEmail   (Extraction)
   ↓
Ask Pick-One        → "refund" | "replace" | "escalate"           (Decision)
   ↓
Create Content / Update Content Property / Send Email / Request Approval
```

Every step in that chain is a structured, typed, cheap call. The LLM only appears if a step genuinely
needs to *write* something. That's the pitch: **"turn any inbound text into structured Umbraco content,
without writing code and without paying LLM prices for every step."**

---

## How It Fits Next to Decision

The post splits the work across three model types. Umbraco.AI already has two of them:

| Step | Model type | Umbraco.AI today |
|------|------------|------------------|
| Pull values out of content | Extraction (Clairic) | **Missing** |
| Pick between options | Decision (Clef, TypeSafe Jev) | `AICapability.Decision` |
| Write free text | LLM | `AICapability.Chat` |

Extraction should be its **own capability**, not more question types on Decision:

- The answer shapes are different. Decision returns a choice plus odds per option. Extraction returns
  a typed value plus a text span and a "not present" probability.
- Providers are different. Cloudflare ships extraction and decision as separate models.
- Folding it in would bloat `AIDecisionQuestion` with kinds most Decision providers can't answer.

---

## Proposed Shape

Follow the same pattern Decision uses (see `docs/archive/decision-capability/ARCHITECTURE.md`):
client interface → capability bases → factory → profile-resolving service → middleware collection →
tracking/telemetry middleware, in a new `Umbraco.AI.Core/Extraction/` feature folder.

Like Decision, M.E.AI has no abstraction for this, so the client would be Umbraco-AI-owned and marked
`[Experimental]`.

### Request: shared state + id'd fields

Mirrors `AIDecisionRequest` (shared state, many keyed items, one call):

```csharp
public sealed class AIExtractionRequest
{
    /// <summary>The content to extract from.</summary>
    public string? State { get; init; }

    /// <summary>The fields to extract, 1..n, unique non-blank ids.</summary>
    public required IReadOnlyList<AIExtractionField> Fields { get; init; }
}

public abstract class AIExtractionField
{
    public required string Id { get; init; }
    public required string Instructions { get; init; }
}

// One subclass per answer shape, the same way Decision has one question subclass per answer shape.
public sealed class AITextExtractionField : AIExtractionField<AITextExtractionAnswer> { }
public sealed class AINumberExtractionField : AIExtractionField<AINumberExtractionAnswer>
{
    public int? Decimals { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
}
public sealed class AIDateTimeExtractionField : AIExtractionField<AIDateTimeExtractionAnswer> { /* date | time | datetime */ }
public sealed class AIListExtractionField : AIExtractionField<AIListExtractionAnswer> { }
```

### Answer: typed value, or explicitly nothing

```csharp
public abstract class AIExtractionAnswer
{
    /// <summary>Provider's confidence, 0.0 to 1.0, when reported. Not comparable across providers.</summary>
    public double? Confidence { get; init; }

    /// <summary>Probability the value is not in the content at all, when reported.</summary>
    public double? NullProbability { get; init; }

    /// <summary>Character offsets in State the value came from, when reported.</summary>
    public (int Start, int End)? Span { get; init; }

    public object? RawRepresentation { get; init; }
}

public sealed class AITextExtractionAnswer : AIExtractionAnswer
{
    /// <summary>The value, or null when the content doesn't contain it.</summary>
    public string? Value { get; init; }
}
```

### Scope for a first version

**In:** text, number, date/time, duration, list.

**Out (for now):** Clairic's `estimate` and `forecast` types. They return distributions
(median, p10, p90) and are a different use case. Revisit if a real Umbraco need shows up.

---

## Fallback: Chat-Backed Extraction

Not every customer will have an extraction provider. A built-in fallback could answer an
`AIExtractionRequest` with any chat profile by:

1. Building a JSON schema from the fields.
2. Calling the chat model with structured output.
3. Mapping the result to typed answers, with `Confidence`, `NullProbability` and `Span` left `null`.

This keeps the API the same for every customer. They get the speed and cost win only if they pick a
dedicated extraction profile.

Open point: should this fallback live in core, or be opt-in? It means every chat provider would show up
as "supports Extraction", which may confuse the profile picker.

---

## Automate: The Headline Use Case

### New action: "Extract Values"

Modelled on the existing `Ask Questions` action (`Umbraco.AI.Automate/.../AskDecisionsAction.cs`),
which already uses `DynamicOutputActionBase` to expose one output per question alias.

- **Settings:** a list of fields (alias, type, instructions), a bindable `Context`, an optional
  Extraction profile.
- **Outputs:** one output per field alias, e.g. `${extract.orderId}`, `${extract.amount}`, plus a
  per-field `confidence` and `found` flag.
- **Group:** `AI`, next to Ask Yes/No, Ask Pick-One, Ask Score, Ask Questions, Transcribe Audio and
  Run Agent.
- **Behaviour on missing values:** a field the content doesn't contain comes back as "not found",
  never a guess. Later steps can branch on that.

Same rules as the Decision actions: experimental flag check at run time, validation errors map to
`StepRunErrorCategory.Validation`, default profile when none picked.

### Recipes to show customers

Each one uses triggers, actions and control flow (`If` / `Switch`) that exist on Automate's `v18/dev`
today, plus the one new action. These are
the demos and docs pages that sell the feature.

**1. Inbound email / webhook → structured content**
`Webhook` → **Extract Values** (name, company, budget, deadline) → `Create Content` (a Lead node) →
`Send Slack Message`.
"Paste your contact form or mailbox webhook in, get a properly filled Umbraco node out."

**2. Event listings from free text**
`Content Saved` on an "Event submission" doc type → **Extract Values** (title, venue, start date/time,
price) → `Update Content Property` on the same node → `Request Approval`.
Editors write naturally; Automate fills the structured fields.

**3. Voice notes to content**
`Media Saved` (audio) → `Transcribe Audio` → **Extract Values** → `Create Content`.
Chains two AI capabilities we already ship with the new one. Good live demo.

**4. Triage with a human safety net**
`Webhook` (support ticket) → **Extract Values** (order ID, amount) → `Ask Pick-One`
(refund / replace / escalate) → `If` on confidence → low confidence goes to `Request Approval`,
high confidence goes straight on.
Shows that confidence scores make AI automations *safe to trust*, not just fast.

**5. Media metadata enrichment**
`Media Saved` → **Extract Values** from the caption or document text (date taken, location, people,
licence) → `Update Media Property`.
Better search and filtering with no manual tagging. If images are supported in v1 (Clairic takes up
to four), this works straight from the image too.

**6. Scheduled clean-up**
`Scheduled Trigger` → `Find Content` (by name / content type) → `If` the date field is empty →
**Extract Values** from body text → `Update Content Property`.
Back-fills structured data on old content that was only ever written as prose.

### Why this sells

- **Cost story:** extraction models bill input tokens only. High-volume automations (every webhook,
  every save) are where LLM costs hurt most, so this is where the saving shows.
- **Speed story:** sub-second steps mean automations feel instant, not "queued".
- **Trust story:** typed values, "not found" instead of made-up answers, and a confidence score to
  route to a human. That answers the first objection most customers raise about AI in workflows.
- **Pairs with Decision:** Decision lets an automation choose; Extraction lets it fill in. Selling one
  makes the other easier to sell.

---

## Other Umbraco Use Cases

Ideas only, none scoped:

- **Content import:** pull title, date, author, price from pasted text into the right property fields.
- **Agent tools:** a cheap extraction tool an agent can call instead of reasoning over a long document.
- **Copilot:** "fill the empty fields on this page from the body text", as one typed call.

---

## Providers

| Provider | Model | Status |
|----------|-------|--------|
| Cloudflare Workers AI | `@cf/cloudflare/clairic` | Preview, per blog post. REST API available. Open weights planned. |
| TypeSafe | Jev | **Unknown.** Check whether the `/v1/systemone` endpoint supports extraction fields. |
| Any chat provider | via fallback above | Possible once the fallback exists. |

A Cloudflare provider package (`Umbraco.AI.Cloudflare`) would likely want both Extraction (Clairic)
and Decision (Clef) from day one. That is a separate idea.

---

## Open Questions

1. Does TypeSafe Jev already do extraction? If yes, we have a second provider for free.
2. Is Clairic's API stable enough to build on while in preview?
3. Chat fallback: in core, opt-in, or not at all?
4. Images: Clairic accepts up to four images with the text. Do we support that in v1, or text only?
5. Does Microsoft plan an M.E.AI abstraction for extraction, the same open question Decision has?
6. Guardrails: should extraction inputs go through the same guardrail pipeline as Decision?
7. Should `Span` be exposed in the Management API, or kept server-side?

---

## Next Step

If this moves forward, promote it to `docs/plans/extraction-capability/BRIEF.md` and run it through
the normal explore → design → plan flow, using the Decision capability as the template.
