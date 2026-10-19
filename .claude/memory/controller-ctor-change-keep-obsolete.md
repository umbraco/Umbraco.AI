---
name: controller-ctor-change-keep-obsolete
description: Adding a dependency to a public Management API controller needs the old ctor kept as [Obsolete] plus [ActivatorUtilitiesConstructor] on the new one
type: gotcha
---

When a public Management API controller needs a new constructor dependency, keep the old
public constructor. Mark it `[Obsolete("... Will be removed in vX.")]` (X = current major + 2,
per the root `CLAUDE.md` deprecation-window convention) and have it chain to
the new one, resolving the new dependency via
`StaticServiceProvider.Instance.GetRequiredService<T>()`. Put `[ActivatorUtilitiesConstructor]`
on the new constructor.

**Why:** controllers are public API under the root `CLAUDE.md` backwards-compatibility rule.
With two public constructors, MVC's `ActivatorUtilities` activation is ambiguous and fails at
runtime unless one is marked. c92f9452 ("Restore controller activation for the AG-UI stream
endpoint") was that exact outage, and c0834532 (`StreamAgentAGUIController`) is the reference
shape. A reviewer caught a builder silently dropping the old constructor on
`AllProviderController` in the decision-capability-release build (T8).

**How to apply:** any diff that changes a public controller's constructor. `StaticServiceProvider`
isn't set in unit hosts, but a unit test can swap `StaticServiceProvider.Instance` to exercise the
obsolete constructor if it joins the shared non-parallel `StaticServiceProviderTestCollection`
(`Umbraco.AI.Tests.Unit/Api/Management/`) and restores the instance in `Dispose`. That proves the
fallback; only a live request through the demo site proves MVC picks the new constructor.
