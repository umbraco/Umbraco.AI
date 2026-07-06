# Bringing AI-Native Workflows to Content Editing

**Draft Sets & Content Validators — a functional vision for Umbraco CMS**

- **Status:** Draft for AI-team discussion, ahead of a pitch to the CMS team
- **Date:** 2026-07-02
- **Authors:** Umbraco AI team
- **Altitude:** Functional & conceptual. Describes the problems, capabilities, and editor experience — deliberately *not* a database schema or API design. Where we name the hard technical challenges, we frame them as design questions for the CMS team rather than prescribing answers.

---

## 1. The thesis

Two things have been genuinely transformational in how developers adopt AI:

1. **Isolation.** Git worktrees and branches let us develop several changes at once, each in its own sandbox. A bad direction is thrown away with zero cost to the working tree. Nothing is at risk until we choose to merge.
2. **Self-validation.** Linters, tests, and coding standards give the AI an objective, machine-checkable definition of "correct." The AI generates output, checks it against those rules, and *iterates on its own* until it passes — before a human ever looks at it.

Together these change the character of the work: you can move fast because mistakes are cheap and quality is enforced automatically.

**Content editors have neither.** Umbraco gives an editor a single shared draft per content item and no repeatable, machine-checkable definition of "good content." Every sweeping change is made directly against the one draft everyone shares, and quality lives only in people's heads and review checklists.

This document proposes the two editor-facing equivalents:

| Developer capability | Editor equivalent | What it gives editors |
|----------------------|-------------------|-----------------------|
| Worktrees / branches (isolation) | **Draft Sets** | Make grouped, sweeping content changes safely; preview them as a whole; apply them as one reviewed unit. |
| Linters / tests (self-validation) | **Content Validators** | Capture "what good content looks like" as a triggerable check that returns pass/fail plus actionable feedback. |

Both are valuable to **every** editor on their own merits — you do not need AI to benefit from staging a set of changes safely, or from a repeatable content-quality check. But they are also the two capabilities the **Umbraco AI** and **Umbraco MCP** efforts need most: with them, AI can generate content in isolation, validate its own work against an agreed standard, iterate until it passes, and present a single, safe, pre-checked unit of work for human approval. Without them, AI content operations are either unsafe (writing straight to the live draft) or unaccountable (no shared definition of "good").

We are pitching them as **core CMS capabilities with clean extension points**, where Umbraco AI is simply the first and most demanding consumer.

---

## 2. Feature: Draft Sets

### 2.1 The problem

- Umbraco supports a **single draft** per content item. There is no way to have two independent in-progress versions of the same page.
- A change that spans many items — a seasonal campaign, a rebrand, a restructure, an AI-generated content refresh — has to be made piecemeal against live drafts. There is no unit that says "these twenty edits belong together and should go live together."
- This makes sweeping changes **risky** (a half-finished change is visible in the shared draft and can be published by accident) and **all-or-nothing to review** (there's no single artifact to approve).
- For AI in particular, there is nowhere safe to work. An agent editing content is editing the same draft a human relies on.

### 2.2 The concept

A **Draft Set** is a named unit of work that groups draft changes across the editorial surface — like a content branch.

- A Draft Set can contain draft changes to **content nodes**, **media**, and the **Library** (shared/reusable content blocks used across pages).
- A single content item can carry drafts in **multiple sets at once** (analogous to the same file differing across git branches).
- Changes inside a set are **inert** — they have no effect on the live site or on other sets until the set is applied.
- The live, published site is the shared baseline ("trunk"). Sets branch from it and merge back into it.

### 2.3 Lifecycle

```
create set  →  edit within set  →  preview whole-site as-if-applied  →  apply / merge  →  close
```

1. **Create** — an editor (or an AI session) opens a named set with a description of the intended work.
2. **Edit within the set** — all edits made while "in" the set are captured as drafts belonging to that set, not to the shared draft.
3. **Preview** — launch the site in preview mode **scoped to a single set**, so you see the entire site exactly as it would look if that set were applied. This is the safety and confidence mechanism.
4. **Apply / merge** — promote the set's changes to the live site as one reviewed operation.
5. **Close** — the set is applied or discarded. Discarding costs nothing and touches nothing else.

### 2.4 Editor experience

- **Set switcher** — a clear, always-visible indication of which set you are working in (or "live/trunk"), and the ability to switch.
- **Working inside a set** — editing feels normal; the difference is that saves land in the set. Items changed in the current set are visibly marked.
- **Whole-site preview for one set** — the flagship UX: see the finished result before anything is live.
- **Apply / review step** — a summary of everything the set changes (added, edited, moved, deleted, across content/media/Library), reviewed and approved as a unit.

### 2.5 The hard part (named, not solved)

The branch/merge model raises genuine design challenges that belong to the CMS team. We name them so the pitch is honest about scope; we are **not** prescribing the resolution:

- **Concurrent sets on the same item** — two open sets both edit the same node. What happens on apply of the second?
- **Trunk drift** — the published/live content changes *underneath* an open set (a human publishes an edit while a set is in progress). Is the set rebased, flagged, or re-previewed?
- **Deletes vs edits** — an item is deleted in trunk but edited in a set, or vice versa.
- **Shared Library items** — a reusable block edited in a set is referenced by pages both inside and outside that set. What is the blast radius, and what does preview show?
- **Referential integrity** — links, pickers, and relations that point at items which exist only within a set.

These are the core design questions. A first release may legitimately constrain them (e.g. detect and block conflicting applies rather than auto-merging) — the point is that the resolution strategy is an explicit, deliberate decision.

### 2.6 AI as the headline use case

An AI session opens a Draft Set, makes sweeping content changes across many nodes entirely within the set, and the human only ever needs to approve the **apply**. Everything before that is inherently safe because it is inert. This directly addresses two AI-adoption blockers:

- **Safety** — the AI cannot damage the live site or a human's working draft; a bad direction is discarded with the set.
- **Approval fatigue** — instead of approving hundreds of individual edits, the human approves one reviewed set at apply-time. Edits *within* a set need no approval because they are safe by construction.

### 2.7 Non-goals (for the first phase)

- **Structural / schema changes** (document types, templates, data types) are **out of scope for Phase 1.** A set captures editorial content, media, and Library items. Staging structural changes in a set is a compelling future phase (a set would then be a true "branch of the whole site"), but it is a much deeper change and a much harder merge problem, and should not gate the near-term ask.
- Draft Sets are not a full approval/workflow engine (see §5).

---

## 3. Feature: Content Validators

### 3.1 The problem

- "Good content" for a given section is real but **implicit** — it lives in editor onboarding, style guides, and reviewers' heads. Examples: *pages in this section must have a main image; SEO fields must be present and contain the target keywords; hero blocks must use an approved layout; the tone must match the brand; the piece must actually be valuable.*
- There is no **repeatable, triggerable** way to check content against those expectations.
- Crucially for AI: an agent generating content has **no target to iterate against.** It produces something plausible and stops. Give it an objective definition of "good" and it can check its own output and improve it — exactly what tests and linters do for code.

### 3.2 The concept

A **Content Validator** captures "what good looks like" for a section or document type as a check that returns a **pass/fail result with actionable feedback**. Validators can be composed into a validation profile for a section, run on demand, and — most importantly — called as a step in an automated loop.

### 3.3 Two tiers behind one contract

Validators come in two kinds, exposed through **one common validator contract** so callers (and the AI loop) treat them uniformly:

1. **Deterministic validators — CMS core.**
   Rule-based, objective checks that need no AI:
   - Required fields present (e.g. main image, meta description).
   - Block / layout rules (e.g. hero must use an approved layout; no more than N call-to-action blocks).
   - SEO / keyword rules (e.g. target keyword appears in title and first paragraph; meta description length).

2. **AI-judged validators — Umbraco.AI extension.**
   Subjective checks evaluated by an AI model scoring the content against a **rubric**: tone, readability, brand alignment, overall value/quality.
   - **The CMS provides the validator framework and the extension point. The AI-judged validator implementation ships in Umbraco.AI**, where it can use existing AI profiles/agents to score against the rubric and return structured feedback.
   - This keeps the AI dependency out of CMS core while giving the AI product a natural place to plug in — the same "core provides the seam, Umbraco.AI provides the AI" pattern used elsewhere.

Both tiers produce the same shape of result (pass/fail + per-rule messages), so a section's validation profile can freely mix deterministic and AI-judged rules.

### 3.4 Trigger points

- **On demand** — an editor runs validation against the current content and sees what passes and what needs work.
- **On save / publish** — validation runs automatically, as **advisory** (warn but allow) or **blocking** (must pass to publish), configurable per section/rule.
- **As a step in an AI loop** — the pivotal capability. An agent can call the validator, read the structured feedback, revise the content, and re-run — iterating until the content passes, before any human sees it.

### 3.5 Editor experience

- **Authoring rules** — define a section's validation profile: which deterministic rules apply, and (where Umbraco.AI is installed) which AI-judged rubrics.
- **Seeing results** — a clear pass/fail panel with specific, actionable feedback per rule ("missing main image", "tone reads too casual for this section", "target keyword not in the first paragraph").
- **The AI iteration loop, made visible** — when AI generates content, the editor can see it validate and self-correct, and is left with content that already meets the agreed bar.

### 3.6 Non-goals

- Not a replacement for existing property-level validation (mandatory fields, regex, etc.) — validators operate at the "is this good content" level, above individual field constraints.
- Not a workflow/approval/publishing-permission engine.

---

## 4. How the two features combine

The features are strongest together. The flagship end-to-end scenario:

> An AI agent **opens a Draft Set** → generates and edits content across many nodes, media, and Library blocks entirely within the set → **runs the section's Content Validators** → reads the feedback and **iterates until every validator passes** → presents a single, validated Draft Set for **one-click human approval at apply-time.**

This is precisely the developer loop — *branch, build, let the tooling check and correct the work, then submit one reviewed unit* — brought to content editing. **Isolation** makes it safe; **self-validation** makes it good; the combination is what lets AI operate on content at scale without either endangering the site or lowering the quality bar.

---

## 5. Rollout, risks & open questions

### 5.1 Suggested phasing

- **Draft Sets, Phase 1:** content nodes, media, and Library items. Structural/schema changes deferred to a later phase.
- **Content Validators, Phase 1:** deterministic validators in CMS core + the AI-judged extension point, with Umbraco.AI shipping the first AI-judged validator.
- The two features can be built independently and deliver value independently, but should be designed with the combined loop (§4) as the north star.

### 5.2 Key design questions for the CMS team

- **Merge / conflict resolution** for Draft Sets (§2.5) — the central technical challenge. What is the Phase 1 stance (block-on-conflict vs. rebase vs. flag-and-reconcile)?
- **Trunk drift** — how an open set reacts when the live site changes beneath it.
- **Library blast radius** — previewing and applying changes to shared blocks referenced inside and outside a set.
- **Preview at scale** — rendering the whole site "as-if-applied" for one set; performance and infrastructure implications.
- **Permissions model** — who can create, edit, preview, and (critically) *apply* a set; how this maps to existing user/section permissions and reduces approval fatigue rather than adding a new gate.
- **Validator authoring surface** — how rules and rubrics are defined (config, UI, code), and how deterministic vs AI-judged rules are registered and composed.
- **The CMS-core / Umbraco.AI boundary** — confirming the validator framework and extension point sit in core, with the AI-judged implementation in Umbraco.AI.

### 5.3 Risks

- Draft Sets touch content versioning — a foundational, sensitive area of the CMS. The merge model must be scoped conservatively for a first release.
- AI-judged validation is non-deterministic; it must be advisory-capable and its rubrics transparent, so editors trust and can override it.
- Blocking validation on publish could frustrate editors if rules are poorly authored — advisory-by-default with opt-in blocking is the safer stance.

### 5.4 The ask

Agreement from the CMS team that these two capabilities are worth scoping as core CMS features with the extension points described, so that Umbraco AI (and the MCP project) can build the safe, self-validating content workflows on top of them.
