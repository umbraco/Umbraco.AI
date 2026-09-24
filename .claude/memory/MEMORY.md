# Memory index

One line per file in this folder, newest relevant first. See `README.md` for the format.

- [Custom AG-UI Implementation](custom-agui-implementation.md) — why `Umbraco.AI.AGUI` is hand-built rather than reusing Microsoft Agent Framework's (internal-only) AG-UI types
- [Provider-Hosted Tools Deferred](provider-hosted-tools-deferred.md) — MEAI hosted web search/code interpreter/remote MCP deferred until concrete demand; revisit triggers listed
- [Vector Store Abstraction](vector-store-abstraction.md) — keep custom `IAIVectorStore` over `Microsoft.Extensions.VectorData`; adapter (not replace) is the path for external backends later
- [Plan-Folder Docs Are Snapshots](plan-doc-architecture-spec-are-snapshots.md) — `BRIEF.md`/`ARCHITECTURE.md`/`SPEC.md` describe current state only; revision history belongs in `DECISION-LOG.md`
- [Frontend Entry-Point Architecture](frontend-entry-points.md) — which of the five Client-package entry points (`manifests.ts`/`app.ts`/`exports.ts`/`index.ts`/`internal-components.ts`) a new export belongs in
- [OTel test shared ActivitySource](otel-test-shared-activity-source.md) — filter ActivityListener captures by span name; all capability middleware share the "Umbraco.AI" source
- [Provider contract checks inside tracking](provider-contract-checks-inside-tracking.md) — reject bad provider responses inside tracking (error classifier), caller input outside it
- [Controller ctor change keeps obsolete](controller-ctor-change-keep-obsolete.md) — new controller dependency: keep old ctor [Obsolete], mark new one [ActivatorUtilitiesConstructor]
- [Polymorphic response DeclaredType](polymorphic-response-declared-type.md) — Ok(derived) drops "$type" on root polymorphic responses; set DeclaredType to the base
