> **Status:** Plan, 29-10-2026. Nothing built yet beyond the prerequisites in Phase 1. Owners are
> proposals until agreed.

# Autonomous releases

Let an agent take an Umbraco.AI or Umbraco.Automate release from "please release" to packages on
NuGet.org and npm, with no person needed for routine releases, and a person still approving the risky
ones.

## Desired end state

- A maintainer opens an issue titled `release <YYYY.MM.N>` and labels it `ops/auto-release`. That label is
  the human decision to release.
- An agent prepares the release branch, gets CI green, has the release reviewed by a second agent, then
  publishes through the shared Azure pipeline. After that it merges back, bumps dev versions and closes
  the issue.
- Patch and minor releases publish with no further human step. A major release, a product's first
  release, or a breaking change waits for a person to approve in Azure, and the agent picks up again once
  they have.
- The agent can start pipeline runs but can never approve one.
- Turning automatic publishing off is a single switch in Azure, with no code change.
- Releases done by hand keep working exactly as they do today (`/release-prepare`, `/release-publish`,
  `/release-cleanup`).

## How it fits together

| Piece | Where it lives | Exists? |
|-------|----------------|---------|
| Trigger (issue label → agent routine) | each repo's `.github/workflows/loop-dispatch.yml`, calling `umbraco/umbraco-ai-ops` | Automate yes, AI no |
| Release loop (order + gates: CI, AI release review) | `umbraco/umbraco-ai-ops` `ops-release-loop` + `release-reviewer` agent | Yes |
| Release steps (plan, cut, publish, sync) | each repo's `.claude/skills/ops-release` | Automate yes (needs changes), AI no |
| Build, pack, publish, tag, GitHub release | Umbraco.Build `devops/monorepo/` shared pipeline | Yes, in PR #3 / #15 |
| Approval with no person for routine releases | Umbraco.Build release stage + an Azure environment | No |
| Agent identity, environments, checks | Azure DevOps and GitHub settings | No |

## Decisions

- [ ] **Approval model.** Recommended: the label counts as approval for patch and minor releases, and a
      person approves majors, first releases and breaking changes in Azure. Alternatives: the label is the
      only approval for every release; or a person approves every release in Azure (not really
      unattended).
- [ ] **Policy sign-off.** Whoever owns release policy (likely Nathan or Umbraco HQ) agrees that NuGet.org
      and npm releases may go out with no person approving. Needed before Phase 7.
- [ ] **Who may apply `ops/auto-release`.** Recommended: maintainers only (GitHub triage rights or
      above).
- [ ] **Docs/chore-only products.** `/release-prepare` asks today. Recommended default for the agent:
      leave the product out of the release (add to `exclude`) rather than patch-bump it.
- [ ] **What counts as "risky".** Recommended: any product's major version goes up, any product has no
      earlier `<Product>@` tag, or any commit in the release is `feat!:` or has `BREAKING CHANGE`.

## Phase 1: Land the shared pipeline (prerequisite)

Owner: Matt, with Nathan and Rick reviewing.

- [ ] Merge Umbraco.Build #3, then retarget and merge #15.
- [ ] Point Umbraco.AI and Umbraco.Automate's `build` resource `ref` at Umbraco.Build `main`.
- [ ] Merge both trial branches (`v18/feature/monorepo-shared-ci`) into `v18/dev`.
- [ ] Rick fixes Automate's flaky tests (`ApprovalOutcomeTests`, `TerminalStepFailureTests`,
      `ScriptExecutorTests.ExecuteAsync_FetchJson_ParsesResponseBody`), since tests now block a release.
- [ ] Do the first release of each repo by hand through the YAML pipeline (`/release-publish`).
- [ ] Retire the classic release definitions and the old Slack webhook.
- [ ] Run `/github subscribe umbraco/Umbraco.AI releases` (and Automate) in the release channel.
- [ ] Port the pipeline switch-over to the v17 lines.

## Phase 2: Pipeline mode with no approval wait (Umbraco.Build)

Owner: Matt. Lives in `devops/monorepo/`, so it doesn't touch the main template.

- [ ] Spike: confirm Azure doesn't run an environment's checks (or ask for approval) for a stage that is
      skipped by its runtime condition. The design below depends on it. **Unverified.**
- [ ] Add a "Release readiness" job at the end of the Pack stage that reads `pack-manifest` and the
      repo's tags and sets `releaseTier` to `routine` or `review`, using the "risky" rule from Decisions.
- [ ] Split the release stage in two: one uses `release_environment` (person approves) when the tier is
      `review`; the other uses a new `release_environment_auto` parameter (no approvers) when the tier is
      `routine`. Leaving `release_environment_auto` empty keeps today's behaviour: every release waits for
      a person.
- [ ] The tier is decided by the pipeline, not by whoever starts the run, so an agent can't choose to skip
      approval.
- [ ] Document both parameters and the tier rule in `devops/monorepo/README.md` and `CLAUDE.md`.
- [ ] Compile-check both repos and test a `routine` and a `review` release (Phase 7).

## Phase 3: Azure DevOps and GitHub settings

Owner: Matt (AI), Rick (Automate). Settings, not code.

- [ ] Create environments "Umbraco AI Release (auto)" and "Umbraco Automate Release (auto)", with no
      approvers.
- [ ] Add checks to each auto environment:
  - "Required template": only runs that extend `devops/monorepo/templates/pipeline.yml@build` from
    Umbraco.Build `main` can use it.
  - "Branch control": only `refs/heads/v*/release/*` and `refs/heads/v*/hotfix/*`.
- [ ] On the existing (person-approved) environments, turn off "Allow approvers to approve their own
      runs".
- [ ] Create an identity for the agent with permission to queue builds and read results on pipelines
      694 (AI) and 714 (Automate), and nothing else. No environment admin, no approver role.
- [ ] Store that identity's token where the agent routine can read it. Never in a repo.
- [ ] Umbraco.AI: add the `LOOP_DISPATCH_FIRE_URL` and `LOOP_DISPATCH_TOKEN` secrets and the
      `ops/auto-release` and release-blocked labels.

## Phase 4: Automate's `ops-release` (Umbraco.Automate)

Owner: Rick.

- [ ] `publish`: queue pipeline 714 on the release branch with `publish=release` and wait for it, instead
      of tagging and creating the GitHub release itself. The pipeline now does both.
- [ ] `publish`: if the run waits for approval (a `review` release), stop with a clear "waiting for
      approval" result and the run link, rather than failing. A later call checks the run again.
- [ ] `publish`: idempotency check becomes "do the `<Product>@<version>` tags exist", since the GitHub
      release name now comes from the publish date, not from the release number.
- [ ] Drop the separate `<release-number>` tag step. The pipeline's GitHub release creates a
      `YYYY.MM.N` tag, numbered by publish date.
- [ ] Rename its delegated skills to `release-prepare` / `release-cleanup` if Automate adopts the new
      names (optional).

## Phase 5: Umbraco.AI onboarding

Owner: Matt.

- [ ] Add `.github/workflows/loop-dispatch.yml` (the locked template from `umbraco-ai-ops`) and
      `.claude/ops-repo-meta.json` (`live: [v17, v18]`, `primary: v18`).
- [ ] Add `.claude/skills/ops-release` with `plan`, `cut`, `publish`, `sync`, built on the local skills:
  - `plan`: `/release-prepare`'s change detection, read only.
  - `cut`: `/release-prepare` with every answer passed in, then open the release PR.
  - `publish`: `/release-publish` with the target `release`, run with no go-ahead question.
  - `sync`: `/release-cleanup`.
- [ ] `/release-prepare`: accept a full plan as input (line, release number, products, bumps) and never
      ask when one is given. Apply the docs/chore default from Decisions.
- [ ] `/release-publish`: a non-interactive mode for when `ops-release` calls it. It still runs every
      Phase 2 pre-check and still never approves.
- [ ] `/release-cleanup`: a non-interactive mode (no "proceed?" questions; delete the release branch only
      if told to).
- [ ] Optional, later: once AI has acceptance tests, run them against the MyGet prerelease packages before
      a release, as Automate's acceptance stage does today.

## Phase 6: Release loop handles "waiting for approval" (umbraco-ai-ops)

Owner: Matt.

- [ ] `ops-release-loop`: when `publish` reports "waiting for approval", comment on the trigger issue with
      the run link, notify, and stop without failing.
- [ ] Resume when the run finishes (the `check_suite` wake-up already exists for CI; confirm whether Azure
      run completion reaches it, or add a re-fire). **Unverified.**
- [ ] Add an eval for the waiting path to `evals/release.eval.json`.

## Phase 7: Try it

- [ ] Dry run on Automate: a prerelease-only release (`publish=prerelease`) through the whole loop, to
      check the steps without touching NuGet.org.
- [ ] First real `routine` release: a patch on one AI provider package, with someone watching.
- [ ] First `review` release: confirm it stops for approval, comments on the issue, and finishes after a
      person approves.
- [ ] Only then, use the label for normal releases.

## Undo and safety

- **Switch off:** remove the auto environment's name from `release_environment_auto`, or add an
  approver to the auto environment. Every release then waits for a person.
- **Bad release:** NuGet.org and npm don't allow deleting a version. Unlist it on NuGet.org, deprecate it
  on npm, and release a fixed patch. Write this as a short runbook in `devops/monorepo/README.md`.
- **Audit:** each GitHub release links its changelogs, and its notes carry the Azure build id; the trigger
  issue links the release PR and the run.

## Risks

- An agent misreads a version bump and ships the wrong version. Mitigation: the release reviewer gate, the
  "risky" tier rule, and the NuGet.org/npm pre-checks in `/release-publish`.
- Azure behaviour assumed above (skipped stages and checks, run completion wake-ups) turns out
  different. Mitigation: the Phase 2 and Phase 6 spikes come first.
- Flaky tests block releases. Mitigation: fix them in Phase 1; a release refuses `runTests=skip`.
