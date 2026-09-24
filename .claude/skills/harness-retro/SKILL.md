---
name: harness-retro
description: Run a retrospective on a just-completed (or recently completed) agentic coding task and turn the friction found into a harness-improvement skill — new or updated. Use this whenever the user asks to "write down what it learned", "capture lessons from this session as a skill", "what would make this faster next time", "turn this into a skill", "do a retro/post-mortem", or wants the next run of a similar task to be quicker, cheaper, or more direct. Trigger it proactively at the end of a substantial multi-step build if the user asks how to speed up future runs — don't wait for them to name "harness" or "skill" explicitly.
---

# Harness Retro

## Why this exists

A harness (notes, a plan, bundled skills, curated snippets) is what turns a slow first
attempt at a task into a fast, direct one the second time. But a harness only gets better
if someone turns real friction from a real run into something concrete the next run can
use. This skill is that conversion step: it takes a task that just happened — with all its
wrong turns, rediscovered patterns, and environment traps still fresh in context — and
distills it into a skill (or an update to one) that removes that same friction next time.

The test for whether this worked is not "did we write down what happened" — it's "if we
handed this skill to a fresh agent doing the same class of task, would it skip the mistakes
we just made and go straight to the working answer." Journaling is not the goal; friction
*removal* is.

## The three buckets, and why they need different remedies

Not all friction is the same shape, and each shape wants a different kind of artifact.
Sort what you found into these before writing anything:

1. **Outside what the platform actually supports.** Code, an import, or an assumption that
   looked right but broke (or would have broken, if you caught it before running) because
   of a real constraint you didn't know about — a reference doc ahead of what's actually
   installed, a runtime-vs-published mismatch, a framework gotcha (e.g. an attribute binding
   silently stringifying `undefined`). The remedy is usually **a named rule or a "verify
   against X before writing this" step** — not a snippet, because the code itself was fine
   once corrected; it's the *assumption* that needs fixing.

2. **Process and sequencing.** Wasted round trips: doing things in the wrong order, an
   environment/config trap that only announces itself as a confusing symptom (a 404 with no
   clue why, a port collision), redundant back-and-forth that a different ordering would
   have skipped entirely. The remedy is **an ordered checklist with the trap named at the
   exact step it bites** — not buried in prose, because the value is entirely in *when* you
   read it.

3. **Reusable domain knowledge and code that no installed skill covers.** This has two
   sources, and only one of them involves friction: patterns that took real digging to find
   (grepping a large reference repo, reading XML doc comments out of a NuGet package,
   reconstructing an API shape by trial), *and* patterns that were written correctly on the
   first try but still aren't covered by any marketplace or project skill. The second kind
   is easy to miss precisely because nothing went wrong — but "no friction this time" isn't
   the same as "no value in capturing it." A strong model got it right through careful
   reasoning; a weaker one, or the same model in a hurry, might not next time, and either
   way it's wasted effort to re-derive something already solved. Explicitly cross-check what
   you wrote against the installed skill set (marketplace skills *and* any project skills
   already in this harness) — don't only mine for pain. The remedy for both is the same: **a
   bundled reference/snippet file** so the next run reads it instead of re-deriving it.

   A useful trigger question for this one: *"Is there anything I wrote — even something that
   just worked — that came from this org's or project's own standing requirements rather
   than from Umbraco/framework knowledge?"* Org-level formatting/compliance rules (a fixed
   date format, a unit system, a data-residency constraint) are a common source: no backoffice
   skill will ever know about them, so code written to satisfy them is coverage the harness
   has to supply itself.

Mixing these into one undifferentiated "lessons learned" list is why retros so often
produce a skill nobody reads: a rule, a checklist step, and a code snippet all want to look
different on the page. Keep them visibly separate in the output.

## Procedure

1. **Mine the current conversation first.** If the task just happened in this session, you
   already have everything you need in context — don't re-open files or re-run searches to
   "verify" what you remember; that defeats the point. For each incident, capture three
   things: what happened, the concrete cost it caused (extra tool calls, a wrong guess that
   had to be un-guessed, a debugging loop), and the general rule it reveals. If the task
   happened in an earlier session, ask the user for the transcript, PR, or a description
   before proceeding — don't invent incidents.

2. **Apply a "would this actually have helped" filter.** For every candidate finding, ask:
   if the *next* run had this written down, would it skip real work? Drop anything where the
   honest answer is "maybe, a little" — a lean skill that's read beats a thorough one that
   isn't. This is the same principle as writing code: three real incidents don't need a
   fourth invented one for completeness.

3. **Decide the target before drafting.** Three options, and they're not interchangeable:
   - **A new skill**, when the lessons form a coherent, reusable procedure for a class of
     task that doesn't have one yet.
   - **An update to an existing project skill**, when one already covers this ground —
     merge and dedupe rather than writing a second, competing skill. Skill sprawl (two
     skills half-covering the same task) is worse than one imperfect skill.
   - **A gap in a third-party/marketplace skill** — something you'd normally fix by editing
     it, except it lives in a plugin cache that an update will silently overwrite. Document
     the gap and its workaround inside *your own* project skill instead, and if a feedback
     mechanism is available, offer to draft it — never silently patch a cached plugin skill.

   Ask the user where the resulting skill should live only if it's genuinely ambiguous
   (project-scoped vs. shared across a family of similar projects) — most of the time the
   right place is obvious from where the task happened.

4. **Draft it lean.** Structure the output skill as:
   - An **ordered checklist** for process/sequencing findings — each trap named at the step
     it applies to, not a separate "gotchas" appendix the reader has to cross-reference.
   - A **snippet/reference section** for rediscovered domain knowledge — actual working
     code, not a description of where to find it.
   - A short **known-good vs. uncertain** note for anything in bucket 1 that's a standing
     risk (e.g. "these three icon names are confirmed; don't guess others").
   Keep the main file under the usual skill size guidance (short, imperative, explain *why*
   a step matters rather than issuing bare MUSTs) and push bulky reference material to a
   `references/` file if the main file would otherwise sprawl.

5. **Optional challenge pass, for a substantial or risky build.** A single self-review
   shares its own blind spots — the same reasoning that missed something while building
   will often miss it again while reviewing, because "propose the retro" and "find what's
   wrong with the retro" are the same task run twice, not two different ones. Fork yourself
   (`subagent_type: "fork"`) — it inherits the full transcript and shares your prompt cache,
   so this costs a fraction of a fresh re-read. **Paste the drafted retro into the fork's
   prompt text itself — do not refer to it as "the draft above" or "my immediately
   preceding message."** A fork does not see text you wrote earlier in the *same*
   assistant turn as the one that dispatches it, only prior completed turns; if the draft
   and the `Agent` call happen in the same turn (the common case — you write the draft,
   then immediately fork to challenge it), a fork told to "challenge the draft above" will
   report back that no draft exists, having only the real working transcript to re-scan.
   Give it a directive to attack the *draft* retro (included verbatim in the prompt), not
   write a new one:
   - **Missing findings**: re-scan the transcript for incidents that happened but didn't
     make it into any bucket (a wrong guess that got silently corrected, a rediscovered
     pattern that didn't seem worth noting at the time).
   - **Padding**: anything in the draft that doesn't trace to a real incident — check it
     against the "don't invent generic advice" / "don't overfit" anti-patterns below.
   - **Duplication**: anything already covered by an existing marketplace or project skill,
     which belongs as an update to that skill (or gets dropped), not a new entry.

   The fork returns a short verdict (add / cut / merge-elsewhere), not a rewritten retro —
   apply its findings to your draft yourself. Skip this step for a small, low-stakes task:
   one structured pass is proportionate there, and a mandatory challenge pass on every retro
   would make the retro process itself the thing eating the time savings it exists to
   produce. This is one pass, not a loop — apply it once and move on rather than chaining
   challenge passes to convergence.

6. **Present it, then write it.** Show the user the drafted skill (or diff, if updating one)
   before or right as you write it — this workshop's own convention is "check the new skills
   are right and fix them," and that habit is worth keeping regardless of project. If there's
   a documented "carry forward" step for this environment (e.g. copying a new skill into
   another section's `.claude/skills/`), remind the user of it rather than doing it
   unprompted — moving files into a sibling project is a bigger action than writing one in
   the current project.

   Draft and write the skill directly rather than routing through `skill-creator` — its
   default workflow (interview, test cases, often a parallel benchmark loop) is built for a
   skill meant to be used at scale by many people on many prompts, which is disproportionate
   for a single retro-derived skill you already validated against a real transcript. Once a
   produced skill has actually been reused a few times, or its triggering reliability
   specifically matters, that's a reasonable moment to hand it to `skill-creator` for
   description-triggering optimization or a proper eval pass — but that's a deliberate later
   step, not part of this procedure.

## Output template

Use this shape for the retro you show the user before turning it into a skill:

```markdown
## What went outside supported platform behavior
- <incident> → cost: <what it cost> → rule: <the generalizable fix>

## Process / sequencing friction
- <incident> → cost: <what it cost> → checklist step: <where this belongs and what it says>

## Reusable knowledge rediscovered by search
- <pattern> → cost: <how it was found> → snippet: <captured for reuse>

## Proposed skill(s)
- <name> (new | update to <existing skill>): <one-line purpose>
```

## Anti-patterns to avoid

- **Don't journal.** "We ran the server, then generated the client, then wrote the
  frontend" is narrative, not a lesson, unless a step in that sequence is where something
  broke or could break again.
- **Don't invent generic advice.** Every line in the output skill should trace back to a
  real incident from this task. If you find yourself writing something that sounds like
  general best-practice advice unconnected to anything that actually happened, cut it.
- **Don't overfit to the one example.** State the general rule the incident reveals (e.g.
  "verify a constant is actually exported by the installed package, not just present in a
  reference source repo"), not the literal specific ("import X from Y"), unless the literal
  specific genuinely is the reusable thing (a real snippet, a real command).
- **Don't skip bucket 3 because nothing "broke."** Time spent re-deriving correct-but-hard-
  to-find knowledge is real cost even when the final code was right the first time — and
  code with zero friction but zero skill coverage is still a gap, not a non-finding.
