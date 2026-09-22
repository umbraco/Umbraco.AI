---
name: backport
description: Port a fix or feature from one Umbraco.AI version line to the other active line (v17 <-> v18). Use when asked to backport, port, or apply a change to "the other version" / "both lines". Always ends in a draft PR — never merges automatically.
argument-hint: [commit/PR/description of the change] [target version, e.g. v17]
---

# Backport a Fix Across Version Lines

Port an already-written fix or feature from one `vN/dev` line to the other active line. This has been done by hand roughly a dozen times across past sessions with no automation and a few repeat mistakes (skipped version numbers, a worktree created nested inside another worktree). This skill exists to make those mistakes structurally harder, not just documented.

## Before starting

1. **Confirm which line(s) actually need it.** Check the Multi-Version Support table in the root `CLAUDE.md` for each version's current phase:
   - Active support → port features and fixes.
   - Security phase → security patches only.
   - EOL → skip unless the user explicitly asks anyway.
   If genuinely unsure whether a change applies to the other line (e.g. it touches something version-specific, or memory notes a reason NOT to port — check for a "do NOT port" note before assuming), ask the user rather than guessing.

2. **Identify the exact change to port** — a commit SHA, PR number, or a clear diff. Don't re-derive the fix from a vague description if a concrete commit/PR exists; read it.

## Steps

1. **From the main repo root** (never from inside another worktree — see CLAUDE.local.md's absolute-path rule), enter a fresh worktree for the target line:
   ```
   EnterWorktree with name: <descriptive-name>-<target-version>
   ```
   This branches from the target line's `vN/dev` by default. If it doesn't (check `git branch --show-current` after entering), stop and fix the base before doing anything else — porting on top of the wrong base is worse than not porting at all.

2. **Apply the change:**
   - If it's a single, clean commit: `git cherry-pick <sha>`.
   - If it needs adaptation (different file layout, version-specific API), apply the diff manually and adjust — don't force a cherry-pick that half-conflicts.
   - If cherry-picking, resolve conflicts by hand; never take a side blindly with `--ours`/`--theirs` without reading what the conflict actually is.

3. **Verify, don't assert** (per root `CLAUDE.md`): actually run the build and relevant tests for the target line before going further.
   ```bash
   dotnet build <Product>/<Product>.slnx
   dotnet test <Product>/<Product>.slnx
   ```
   For a frontend change: `npm run build:<target>` (build core first if needed).

4. **Commit** using the same conventional-commit type as the original change, scoped to the target line's context. Don't invent a new commit message from scratch if the original already describes the change well — adapt it.

5. **Push and open a draft PR.** Never merge automatically, even if the build and tests pass — a human approves the merge.
   ```bash
   git push -u origin <branch-name>
   gh pr create --draft --base <target-version>/dev --title "<type>(<scope>): <description>" --body "..."
   ```
   If there's a corresponding PR on the original line, cross-link them in both descriptions (e.g. "Backport of #123 to v17") — this is the existing convention in this repo's history and makes the pair easy to find later.

6. **Clean up.** Once the PR is open, either exit the worktree with `keep` (if more work on it is expected) or run `/worktree-cleanup` once the PR has actually merged. Don't leave the worktree dangling indefinitely — that's exactly how this repo ended up with 25 stale worktrees.

## Guardrails

- **Never auto-merge.** This skill's job ends at a draft PR. Merging is always a separate, explicit, human-approved step.
- **Never skip a version-line's phase rule.** Don't port a feature into a security-phase or EOL line without an explicit ask.
- **Never reuse one worktree for two version lines.** Each target line gets its own worktree with its own absolute path — reusing one, or creating a second worktree from inside the first, is the exact mechanism that has produced a worktree nested inside a worktree before.
- **Never blindly trust "no PR found" as "safe to skip."** A change with no corresponding PR on the other line might mean it hasn't been ported yet (the normal case this skill handles) — check history/memory for an explicit reason before concluding it doesn't apply.
