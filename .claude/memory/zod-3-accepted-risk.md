---
name: zod-3-accepted-risk
description: v17/v18 keep zod 3 (GHSA-mrhx-c7xc-m3w5) because the fix needs the breaking AG-UI 1.x move; risk accepted, revisit triggers listed
type: decision
---

**Decision:** On the 17 and 18 lines, keep `@ag-ui/client` 0.0.57 and the zod 3.25.76 it pulls in,
and accept advisory GHSA-mrhx-c7xc-m3w5 (zod up to 4.6.5 can run out of memory validating a large
array with many failing elements). Decided 23-11-2026. The 19 line moved to zod 4 in #791.

**Why:**
- The only route to zod 4 on these lines is AG-UI 1.x, and `@ag-ui/core` 0.0.57 needs zod 3. AG-UI
  1.x changes types that `@umbraco-ai/agent` re-exports (`EventType`, `ToolMessage.content`,
  `RunFinishedOutcome`, `RunAgentInput`). v19 shipped that move as a breaking change; doing it on a
  stable line breaks consumers. Community PR #781 proposed it for v18 and was not taken.
- The exposure is small. zod runs only in the editor's browser, where the AG-UI client parses each
  event streamed back from our own server (`EventSchemas.parse`). Our server builds those events
  from typed C# models, so their arrays are always well-formed. Model output and tool results land
  in string fields or schema-less state, which never produce a flood of failing array elements.
- Worst case: someone who already controls the server's response (a compromised site, or a site's
  own custom code) freezes one editor's chat tab. Reloading fixes it. No data leaves, nothing
  server-side is affected.
- The cost of the alternative is real: a breaking frontend API change on a stable line, against a
  high-severity rating that does not match our actual exposure.

**Revisit if:**
- An AG-UI 0.0.x release (or a zod 3.x patch) appears that fixes the advisory without the 1.x type
  changes.
- The browser starts parsing events that don't come from our own server (for example, a remote
  agent or MCP server streaming straight to the client), or parses user-supplied JSON with zod.
- A customer or security scan requires the advisory to be cleared on 17/18.
- A new zod advisory with a more serious impact (code execution, data exposure) lands.

**How to apply:** Don't re-raise the zod 3 alert on v17/v18 as new work, and don't take AG-UI 1.x
on those lines without a breaking-change decision. Point security questions about it here.
