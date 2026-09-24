---
name: umb-build-loop-gotchas
description: Process checklist for running umbraco-claude-playbook's umb-explore/umb-design/umb-plan/umb-build-loop pipeline in this repo — worktree-creation trap, commit hygiene, orchestrator discipline, and the demo-site live-verification recipe. Documents gaps found in the cached playbook skills without editing them directly. Use whenever running that pipeline, not just for capability work.
---

# umb-build-loop workflow gotchas (this repo)

Findings from running the full `umb-explore → umb-design → umb-plan → umb-build-loop`
pipeline end to end on the `decision-capability` feature (2026-09). These are process
traps in *how the pipeline runs in this specific repo*, not in the skills' own written
instructions — `umbraco-claude-playbook` lives in a plugin cache that gets overwritten on
update, so these are captured here instead of edited into it directly.

## Before creating the worktree: push trunk first if the plan-folder commit is local-only

`umb-build-loop`'s Phase 0/4 commits the plan folder to trunk, then calls `EnterWorktree`.
**This repo's `WorktreeCreate` hook branches the new worktree off `origin/<default-branch>`,
not local HEAD.** If the plan-folder commit (or anything else) only exists locally when
`EnterWorktree` runs, it gets silently dropped from the new branch — the worktree simply
never had it. `git push` trunk after the plan-folder commit, before calling
`EnterWorktree`, every time.

## Commit each pending spec in the SAME commit as the production code that first makes it pass

When `bdd-specs` pre-writes specs for a whole feature up front (common with this
pipeline — every story gets a spec before any task starts), it's easy for a task to land
its production code while the spec file that now passes because of it stays uncommitted
"for a later cleanup commit." This happened twice in one build (two different tasks' spec
files sat uncommitted across a full extra review round each before a reviewer caught it).
The rule that actually prevents it: **the moment a task's production code makes its own
pending spec pass for the first time, stage and commit that spec file in the same commit
— never defer it.**

## Orchestrator discipline holds even mid a fast live-API-debugging loop

The loop's own rule ("you do not write feature code yourself, dispatch to builder/
reviewer") is easiest to break specifically during interactive, fast-iteration debugging
against a real external system — curl, read the error, tweak one line, rerun — because
handing each one-line tweak to a fresh builder feels slow in the moment. It broke exactly
here in this build: the orchestrator hand-edited three already-committed production files
directly while chasing a live API's real wire format, and had to stop and redo the fix
through a builder once caught. **Investigating (reading docs, curling an endpoint, reading
logs) is fine for the orchestrator to do directly. The moment a fix touches an
already-committed file, stop and dispatch — even if the fix is one line and you already
know exactly what it is.**

## Reviewer discipline: always independently rebuild/rerun, never trust a pasted number

This one worked, consistently, and is worth reinforcing rather than just noting: across
this build, having the reviewer independently rebuild and rerun the test suite (rather
than trusting the builder's self-reported pass/fail counts) caught a real, distinct bug
in nearly every review round. A reviewer that only reads the diff and trusts the numbers
in the builder's report is a materially weaker gate.

## Budget for at least one real fix-and-re-review round on anything that invents a new shape

Nine of twelve tasks in this build needed at least one genuine fix-and-re-review cycle —
not style nits, real bugs the first pass missed. The pattern: tasks that copied an
existing sibling's file-for-file structure tended to pass on the first review; tasks that
had to invent a genuinely new shape (a new type's mutability, a new wrapping order, a new
wire format) almost never did. When planning or estimating a similar build, expect the
"invents something new" tasks to cost 2-4x a single pass, not budget for one pass per
task uniformly.

## Live-verification recipe: `demos/vN/<site>/` is free, ungitignored scratch space

For "prove this actually works against a real running host" tasks (a real API key, a
real DB-backed connection/profile, a real end-to-end call) that don't need a full backoffice
UI flow:

- Add a throwaway `TEMP_<Name>.cs` in the demo site's project root: an `IHostedService`
  that runs once ~8s after startup, does the real work, and logs the result with a
  distinctive prefix (e.g. `[T11-SPIKE]`) via `ILogger.LogWarning` so it's easy to grep.
  Register it (and any throwaway provider) via a small `IComposer` in the same file.
- Add whatever temporary `ProjectReference`/`appsettings.Development.json` overrides the
  verification needs directly in the demo site's own files.
- **None of this needs cleanup or a commit** — the whole `demos/` tree is gitignored.
  Leave it, delete it, or leave it for the next person; it never touches git history.
- Iteration loop that actually works for a long-running host:
  ```bash
  pkill -f "Umbraco.AI.DemoSite" 2>/dev/null; sleep 2
  cd demos/vN/Umbraco.AI.DemoSite && dotnet build 2>&1 | grep -E "error|Error\(s\)"
  rm -f /tmp/verify.log
  dotnet run --no-build 2>&1 | tee /tmp/verify.log &   # background
  # separately, in a background Bash call:
  until grep -qE "\[T11-SPIKE\] (SUCCESS|FAILED)" /tmp/verify.log 2>/dev/null; do sleep 2; done
  grep "T11-SPIKE" /tmp/verify.log
  ```

## Stale persisted state can silently mask a real code fix

If the verification host persists what it creates to a real database (not an in-memory
mock — the demo site does), a **connection/profile created by an earlier, buggier run
stays in that database** across later runs. Fixing the code and rerunning without
touching that record means the fix never actually gets exercised — the old, wrong
settings are still what gets used. When iterating on a fix against a persisted-state
host, force-refresh anything the verification script creates every run (re-save the
settings unconditionally, don't find-or-create) rather than assuming "it already exists"
means "it's still correct."

## `add-ai-capability` cross-reference

If the task being run through this pipeline is specifically "add a new AICapability,"
see the `add-ai-capability` skill for the capability-specific engineering checklist
(wrapping order, M.E.AI-convention mirroring, the `IHttpClientFactory` rule) — this file
only covers the pipeline/process layer, not what the capability's own code should
actually look like.
