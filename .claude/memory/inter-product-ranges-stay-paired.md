---
name: inter-product-ranges-stay-paired
description: Directory.Packages.props (.NET) and root package.json's peerDependencyVersions (npm) are parallel structures - any inter-product version floor change needs both updated together
type: gotcha
---

Umbraco.AI's inter-product dependency floors live in two parallel files that must be kept in
sync by hand whenever a product's version changes relative to its dependents:

- `Directory.Packages.props` (root) — NuGet `PackageVersion` floors, e.g.
  `<PackageVersion Include="Umbraco.AI.Core" Version="[18.4.0, 18.999.999)" />`
- `package.json` (root) — npm `peerDependencyVersions`, e.g. `"@umbraco-ai/core": "^18.4.0"`,
  consumed by the shared `cleanse-package-json.mjs` (umbraco/Umbraco.Build) to resolve each package's
  `workspace:*`/`*` dependencies into real `peerDependencies` ranges at pack time.

**Why:** During the 2026.08.1 release finalize (RC -> stable), `Directory.Packages.props`
floors got raised from `-rc.4` to the final stable versions — needed to fix a real NuGet pack
failure (`Umbraco.AI.Agent` failed to compile against the stale `Umbraco.AI.Core` floor). The
equivalent `package.json` fix was never made, because the finalize was an ad-hoc fix done
outside the normal `/release-prepare` skill (whose Phase 4 and Phase 4.5 already update
both files together for a *normal* release). Result: `@umbraco-ai/agent@18.2.0`,
`@umbraco-ai/agent-ui@18.1.0`, and `@umbraco-ai/agent-copilot@18.1.0` all shipped to npm with
`-rc.4` still showing in their declared `peerDependencies`. Not actually install-breaking —
verified `semver.satisfies('18.4.0', '^18.4.0-rc.4') === true`, and the registry's `latest`
dist-tag correctly pointed at the stable version — but confusing, and since npm packages are
immutable once published, it can only be corrected by a future release, not by fixing what's
already out.

**How to apply:**
- Any time a product's version floor is raised in `Directory.Packages.props` *outside* the
  normal `/release-prepare` flow (a manual release finalize, a hotfix, a backport that
  changes a floor), grep `package.json` for the same product's stale entry in
  `peerDependencyVersions` and raise it to match, in the same commit.
- `/release-prepare`'s own Phase 4 (.NET) and Phase 4.5 (npm) already do this together for a
  normal release — this gotcha is specifically about work that bypasses that skill.
