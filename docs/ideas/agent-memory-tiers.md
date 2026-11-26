# Agent Memory Tiers - Future Consideration

## Status: Under Consideration

Raised 26-11-2026 while planning agent memory v1 (`docs/plans/agent-memory/`). Not needed for v1:
v1 loads every memory into the prompt under small caps. This note keeps the idea and the
approaches found, for when the caps start to bite.

Sources for the products named below are in the agent memory plan's `RESEARCH.md`, gathered
26-11-2026. The Generative Agents paper is cited from memory and not re-checked.

---

## The idea

Split memory into two tiers:

- **Short term:** a small set always in the prompt. Cheap, always there.
- **Long term:** everything else, found with a `recall` tool when needed.

This also fixes the cap problem: once short term is full, older memories move to long term
instead of blocking new saves.

## Why it needs more than v1 has

### How big the always-loaded block gets

Each memory is at most 300 characters (about 75 tokens, plus a short id and kind label).
Estimates use about 4 characters per token; Danish and other non-English text uses more.

| Layer | Cap | Rough tokens when full |
|---|---|---|
| About you | 30 | ~2,500 |
| Project, or you + agent | 50 | ~4,200 |
| v1 worst case (two layers) | 80 | ~7,000 |
| With a company layer at 50 | 130 | ~11,000 |

A company layer isn't planned: AI Contexts already carry company-wide facts. What they lack is a
way for a user to propose an addition. That waits for someone to ask.

### Nothing says which memory got used

The hard part is deciding what moves between tiers.

- **Always-loaded memories:** the model reads them all, so nothing shows which one shaped an
  answer.
- **Asking the model to name the memory it used:** possible, but models do this unreliably.
- **Recall hits:** a reliable signal, but only for memories already in long term.
- **Edits, merges and re-saves:** reliable and already stored. They show a memory is still
  relevant, not that it got used.

So unless everything goes through `recall`, there's no signal for when to move a memory *down*.
A usage column added now would sit empty, so v1 doesn't add one.

One untested thought: hide a short-term memory at random and see whether it gets recalled. The
catch is that the agent may not know to look for it. No product found does this.

## Approaches found

No product measures usage to move memories between tiers. They avoid the question in one of
three ways.

### 1. The agent decides (Letta, formerly MemGPT)

The agent edits its own always-loaded memory with tools, and moves things to searchable storage
when space runs out.

- **Good:** closest to v1's design (inject all, edit by tool). Prompt stays stable, so caching
  works.
- **Bad:** a judgment call, not a measurement. Depends on the model managing its own memory well.

### 2. Nothing is fixed: search before each turn (Mem0, LangMem, MAF `ChatHistoryMemoryProvider`)

Before each turn, the system searches memory with the user's message and loads the best few
(MAF 3, Mem0 10-20, LangMem 10). `recall` stays for deeper digging.

- **Good:** answers "how would the agent know to recall": it doesn't have to, the system looks
  first. No tier moves to manage.
- **Bad:** the memory block changes every turn, so prompt caching gets weaker.
- **Bad:** a relevant memory whose wording doesn't match the message never loads.
- **Needs:** search by meaning to work well, which points at Umbraco.AI.Search's vector store.

### 3. Score at load time (Generative Agents paper, arXiv 2304.03442, 2023)

Each memory is ranked by how recent, how important and how relevant it is. Old, unused ones fade.

- **Good:** no hard tiers; fading handles demotion.
- **Bad:** "important" needs a model call or a guess when saving. Same caching cost as approach 2.

## Leaning

Approach 2 fits best when this is picked up: load the top few by relevance, keep a small pinned
set always loaded (rules and the user's own facts), and keep `recall` for the rest. The v1 table
and space model stay as they are; only what gets loaded changes.

Revisit when:

- users hit the caps often, or
- a company layer is planned, or
- the always-loaded block costs noticeably on real prompts.
