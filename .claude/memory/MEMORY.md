# Memory index

One line per file in this folder, newest relevant first. See `README.md` for the format.

- [Custom AG-UI Implementation](custom-agui-implementation.md) — why `Umbraco.AI.AGUI` is hand-built rather than reusing Microsoft Agent Framework's (internal-only) AG-UI types
- [Provider-Hosted Tools Deferred](provider-hosted-tools-deferred.md) — MEAI hosted web search/code interpreter/remote MCP deferred until concrete demand; revisit triggers listed
- [Vector Store Abstraction](vector-store-abstraction.md) — keep custom `IAIVectorStore` over `Microsoft.Extensions.VectorData`; adapter (not replace) is the path for external backends later
- [Frontend Entry-Point Architecture](frontend-entry-points.md) — which of the five Client-package entry points (`manifests.ts`/`app.ts`/`exports.ts`/`index.ts`/`internal-components.ts`) a new export belongs in
