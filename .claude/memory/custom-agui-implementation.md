---
name: custom-agui-implementation
description: Why Umbraco.AI.AGUI is a hand-built AG-UI protocol package instead of reusing Microsoft Agent Framework's implementation
metadata:
  type: decision
---

**Decision:** Build and own `Umbraco.AI.AGUI` as a pure protocol package, independent of Microsoft
Agent Framework (MAF), rather than referencing or forking MAF's AG-UI implementation. Accepted
2026-01-10.

**Why:**
- MAF's AG-UI types (`RunAgentInput`, `AGUIServerSentEventsResult`, all event types) are marked
  `internal` — they cannot be referenced, inherited, or extended from outside MAF's own assembly.
  Filed [microsoft/agent-framework#2988](https://github.com/microsoft/agent-framework/issues/2988)
  requesting public types + dynamic agent resolution; still open/unresolved.
- Umbraco needs features MAF's AG-UI doesn't have at all: frontend tool interception (delegating
  tool calls to the browser), HITL interrupts (`AGUIInterruptInfo`/`AGUIResumeInfo`), chunk events,
  activity snapshot/delta events, messages-snapshot sync, custom/raw events, an outcome enum. These
  are core to Umbraco's agent architecture and need implementation control MAF doesn't expose.
- The resulting package has a clean layered shape: `Umbraco.AI.AGUI` (pure protocol, no MAF
  dependency) → `Umbraco.AI.Agent.Core` (MAF integration) → `Umbraco.AI.Agent.Web` (HTTP layer).
- We still adopted MAF's good patterns where compatible: direct streaming (no `Task.Run()`,
  preserves `AsyncLocal` context), a builder-style centralized event emitter, thin controllers.

**How to apply:**
- Don't try to reference or extend MAF's AG-UI types directly — they're internal and will not
  compile.
- Any new AG-UI feature (a new event type, an interrupt kind) is implemented in
  `Umbraco.AI.AGUI`/`Umbraco.AI.Agent.Core`, not bolted onto MAF.
- **Revisit trigger:** if microsoft/agent-framework#2988 is resolved and AG-UI types become public,
  evaluate whether MAF's types now meet Umbraco's needs before assuming the custom implementation
  must continue forever. Until then, this is stable and not up for reconsideration on a whim.
