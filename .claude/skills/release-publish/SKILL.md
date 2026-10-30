---
name: release-publish
description: Publishes a pushed release, hotfix or other branch to a package feed by queuing an Azure pipeline run with the "Publish packages to" choice (release = NuGet.org + npm after approval, prerelease = MyGet prereleases, nightly = MyGet nightly). Checks nothing is already published, watches the run, tells the user when it waits for approval, and confirms the tags and GitHub release. Use after /release-prepare has pushed a release branch, to re-publish a new RC, or to put a branch on MyGet.
---

# Release Publish

You publish a branch's packages through the Umbraco.AI Azure pipeline. Publishing is never automatic on a push: a run only publishes when it is queued with the `publish` parameter set. This skill queues that run and follows it through.

## Facts

| Thing | Value |
|-------|-------|
| Organization | `https://dev.azure.com/umbraco` |
| Project | `Umbraco AI` |
| Pipeline | `Umbraco.AI`, id `694` (`azure-pipelines.yml`) |
| Publish choice | `publish` parameter: `none`, `nightly`, `prerelease`, `release` |
| Release approval | Azure DevOps environment `Umbraco AI Release` |
| After a release | `<Product>@<version>` tag per product, plus one GitHub release named `YYYY.MM.N` |

| Target | Where it goes | Approval | Branches |
|--------|---------------|----------|----------|
| `release` | NuGet.org and npm | Yes, a person approves in Azure | `vN/release/*`, `vN/hotfix/*` only |
| `prerelease` | MyGet prereleases feed | No | any branch that packs |
| `nightly` | MyGet nightly feed | No | any branch that packs |

The GitHub release is named from the date it is published (next free `YYYY.MM.N` for that month), not from the branch name. They often match, but a branch cut in one month and published in the next, or a release pushed out while another is open, gets a different number. That is expected: the GitHub release is the record of when it shipped.

## Workflow

### Phase 1: Work out what to publish

1. **Branch.** Use the branch the user names, or the current branch:
   ```bash
   git branch --show-current
   ```
2. **Target.** Use the one the user names. Otherwise default to `release` on `vN/release/*` and `vN/hotfix/*`, and ask on any other branch (`nightly` or `prerelease`).
3. **Refuse `release` off a release or hotfix branch.** The pipeline refuses it too, at compile time.
4. **Check the branch is pushed and current.** The run builds what is on the remote, not local changes:
   ```bash
   git fetch origin <branch>
   git status -sb   # must not say "ahead" or show uncommitted changes the user expects to ship
   git rev-parse HEAD origin/<branch>   # must match
   ```
   If they differ, stop and tell the user. Do not push for them without asking.
5. **Topic branches** only pack with `forcePackAll=true`. Add it for `nightly`/`prerelease` from `vN/topic/*` (or the older `vN/feature/*`), and say so.

### Phase 2: Check nothing is already published (`release` only)

NuGet.org refuses a version that already exists, so a run that repeats one fails partway.

1. **List the products.** `include` from `release-manifest.json`. On a hotfix without a manifest, use each product whose `version.json` version has no `<Product>@<version>` tag yet.
2. **For each product,** read the version and check three places:
   ```bash
   jq -r .version <Product>/version.json
   git ls-remote --tags origin "refs/tags/<Product>@<version>"
   curl -s https://api.nuget.org/v3-flatcontainer/<product-lowercase>/index.json | jq -r '.versions[]' | grep -Fx "<version>"
   ```
   Products with an npm package (`has_npm: true` in `azure-pipelines.yml`; the name is in `<Product>/src/*/Client/package.json`, e.g. `@umbraco-ai/core`) also:
   ```bash
   npm view <npm-package>@<version> version
   ```
   A 404 from NuGet means the package has never been published, which is fine for a new product.
3. **If any version is already out,** stop and show which. Usually this means the release already went through (run `/release-cleanup`), or an RC needs its next number (see the RC rule in root `CLAUDE.md`). Never queue a release that repeats a published version.

### Phase 3: Confirm, then queue

1. **Show the user** the branch, the target, the product list with versions, and where they go. Ask for a go-ahead. `nightly` and `prerelease` publish with no further gate, so this is the only check before they go out.
2. **Queue the run:**
   ```bash
   az pipelines run --org https://dev.azure.com/umbraco --project "Umbraco AI" --id 694 \
     --branch <branch> --parameters publish=<target> -o json
   ```
   Add `forcePackAll=true` to `--parameters` for a feature branch. Never pass `runTests=skip` with `release`; the pipeline refuses it.
3. **Give the user the run link:** `https://dev.azure.com/umbraco/Umbraco%20AI/_build/results?buildId=<id>`.

### Phase 4: Watch the run

Poll the stages every few minutes (a full release run takes about 30 minutes before approval). Use the Monitor tool or a background loop, not foreground sleeps:

```bash
az devops invoke --org https://dev.azure.com/umbraco --area build --resource timeline \
  --route-parameters project="Umbraco AI" buildId=<id> --api-version 7.1 -o json \
  | jq -r '.records[] | select(.type=="Stage" or .type=="Checkpoint.Approval") | "\(.type)\t\(.name)\t\(.state)\t\(.result)"'
```

- **Approval waiting** (`release` only): a `Checkpoint.Approval` record is `inProgress`. Tell the user in one line: "The release is waiting for your approval in Azure: <run link>". **Never approve it yourself**, even if you have the access. Keep watching.
- **A stage fails:** say which stage and which task failed in one line, using the failed record's name and its log. Do not queue another run to retry. If the Release stage failed after some packages were pushed, a new run would repeat them and fail; stop and ask the user how to continue.
- **Approval rejected or the run cancelled:** report it and stop.

### Phase 5: Confirm what went out

For `release`:

1. **Tags:**
   ```bash
   git fetch origin --tags
   git tag --list "<Product>@<version>"
   ```
2. **GitHub release:** `gh release list --repo umbraco/Umbraco.AI -L 3`. Give the user its name and link.
3. **Feeds:** re-run the NuGet.org and npm checks from Phase 2. NuGet.org can take 10 to 20 minutes to list a new version, so a miss right after the run is not a failure; say so and check once more later.
4. **Slack:** the GitHub app posts the release in the release channel. Nothing to do.

For `nightly` and `prerelease`, give the user the versions from the run's `pack-manifest` artifact and the feed they went to.

### Phase 6: Next step

- `release`: tell the user the next step is `/release-cleanup` on the same branch.
- `prerelease` or `nightly`: nothing else. The branch stays open.

## Called by another skill

When another skill (such as `ops-release`) calls this one with the branch and target, run it with no
questions:

- Skip the go-ahead in Phase 3. The caller's own trigger is the go-ahead.
- Still run every check in Phases 1 and 2, and stop on any of them.
- When the run waits for approval, return straight away with the run link, rather than watching.
  The caller checks back later.
- Report back as facts (run id and link, state, versions, tags, GitHub release link), not as a
  question.

## Rules

- One run per invocation. Do not queue a second run while one for the same branch is in progress.
- Never approve a release, and never publish without the user's go-ahead in Phase 3.
- Never push packages by hand (`dotnet nuget push`, `npm publish`). The pipeline is the only route.
- Publishing an RC to NuGet.org counts as a real release for the RC numbering rule. A MyGet publish does not.
