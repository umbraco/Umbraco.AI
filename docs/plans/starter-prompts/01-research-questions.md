---
type: research-questions
---

# Research Questions

1. **How does the Copilot sidebar render its pre-conversation / start state end to end?** In `Umbraco.AI.Agent.Copilot/src/Umbraco.AI.Agent.Copilot/Client/src/copilot/`, trace `copilot-sidebar.element.ts` and `copilot.context.ts` through to `<uai-chat>` in `Umbraco.AI.Agent.UI/.../chat/components/chat.element.ts`: what decides the empty-state is shown, what agent/entity data is in scope at that moment (`loadAgents()`, `selectAgent()`, `UaiCopilotEntityContext`), and what extension points, slots, or manifest kinds already exist for contributing content into that view.

2. **How does a user message travel from the chat input to a run, and what is retained client-side?** Trace `chat/components/input.element.ts` → `UAI_CHAT_CONTEXT` (`chat/context.ts`) → `chat/services/run.controller.ts` in Umbraco.AI.Agent.UI. What shape is a posted message (`UaiChatMessage` in `chat/types/index.ts`), where does the transcript live, what per-message UI affordances already exist (`message.element.ts`, `message-copy-button.element.ts`, `message-regenerate-button.element.ts`), and how are per-message actions registered and rendered?

3. **What is the complete vertical slice for the `AIAgent` entity, layer by layer?** In Umbraco.AI.Agent, document the full chain: `AIAgent.cs` and the `IAIAgentService`/`IAIAgentRepository` boundary, `AIAgentEntity` + `AIAgentEntityFactory` + `UmbracoAIAgentDbContext`, the paired SqlServer/Sqlite migration convention (`UmbracoAIAgent_*`, e.g. how `AddContextIds` or `AddGuardrailIds` was rolled out across both providers), the management API controllers/models and `AgentMapDefinition.cs`, the `@hey-api` generated client in `Agent.Web.StaticAssets/Client/src/api/`, and the frontend collection/workspace/repository/store structure. Which layers are mandatory when a field is added, and which are optional?

4. **How does `AIAgent` model, persist, and edit structured or collection-shaped configuration today?** Examine `AIAgentScope`/`AIAgentScopeRule`, `AIFrontendTool`, `AIAgentUserGroupPermissions`, `AIStandardAgentConfig`/`AIOrchestratedAgentConfig` and `AIAgentConfigSerializer.cs`: which values get their own columns versus serialized JSON on `AIAgentEntity`, how they round-trip through `AgentResponseModel`/`AgentItemResponseModel`, how they are surfaced in the workspace views (`agent-details-workspace-view.element.ts`, `agent-availability-workspace-view.element.ts`, `components/agent-scope-rules-editor/`), and how they flow through versioning (`AIAgentVersionableEntityAdapter.cs`) and Deploy (`AIAgentArtifact.cs`, `UmbracoAIAgentServiceConnector.cs`).

5. **What per-user-scoped persistence, identity, and authorization mechanisms already exist across the stack?** Cover the server side (`UserContextContributor.cs` in Umbraco.AI.Core, `IBackOfficeSecurityAccessor` usage, `CreatedByUserId`/`ModifiedByUserId` on `AIAgentEntity`, the `OwnerKey` ownership model in `Umbraco.AI.Agent.Core/FileStore/AIFileStore.cs`, audit log repositories) and the client side (how the current backoffice user is obtained in Lit components). Separately, research what Umbraco CMS 17/18 itself offers for storing per-user backoffice data (e.g. user data / user settings management APIs, `UmbUserDataRepository` or equivalent, current-user context in `@umbraco-cms/backoffice`) and the documented constraints on it.

6. **How does the Copilot package — which ships no backend of its own — reach server data, register extensions, and export components?** Examine how it consumes `@umbraco-ai/agent` and `@umbraco-ai/agent-ui` (`agentClientReady`, `copilot-section-registry.ts`, `copilot-agent.repository.ts`), how manifests are registered (`manifests.ts` chain, `copilot/components/manifests.ts`), the barrel/entry-point rules described in `.claude/memory/frontend-entry-points.md` (`app.ts` / `exports.ts` / `index.ts` / `internal-components.ts`), and how localization strings are declared (`lang/en.ts`). What are the practical limits of a frontend-only package here, and where does new server-backed data have to live instead?

7. **What design system, component library, and styling conventions govern this backoffice UI?** Identify which `@umbraco-cms/backoffice` UI/UUI components are used for lists, cards, chips, buttons, modals, and inline editing across Copilot, Agent.UI, and the Agent workspace; the patterns for CSS custom properties (colours with values, typography, spacing, border-radius, shadows), theming/dark-mode handling, and how modals/dialogs are declared and opened (e.g. `agent/modals/tool-permissions-override-editor/`, `create-options/`). What existing components come closest to a compact, selectable, user-manageable list of short labelled items?

## Key Context Pointers

- Repositories: `/Users/matt/Documents/Work/Umbraco/Umbraco.AI` (monorepo; current branch `v17/dev`)
- Libraries / dependencies: `@umbraco-cms/backoffice`, `@umbraco-ai/core`, `@umbraco-ai/agent`, `@umbraco-ai/agent-ui`, Lit, `@hey-api/openapi-ts`, EF Core, Microsoft.Extensions.AI
- Filepaths:
  - `Umbraco.AI.Agent.Copilot/src/Umbraco.AI.Agent.Copilot/Client/src/copilot/` (`copilot.context.ts`, `components/sidebar/copilot-sidebar.element.ts`, `components/entity-selector/`, `repository/copilot-agent.repository.ts`, `services/copilot-section-registry.ts`, `lang/en.ts`)
  - `Umbraco.AI.Agent.UI/src/Umbraco.AI.Agent.UI/Client/src/chat/` (`components/chat.element.ts`, `components/input.element.ts`, `components/message.element.ts`, `context.ts`, `services/run.controller.ts`, `types/index.ts`)
  - `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/` (`AIAgent.cs`, `AIAgentConfigSerializer.cs`, `AIAgentService.cs`, `AIAgentVersionableEntityAdapter.cs`, `AIFrontendTool.cs`)
  - `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Persistence/Agents/` + `Umbraco.AI.Agent.Persistence.{SqlServer,Sqlite}/Migrations/`
  - `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web/Api/Management/Agent/` (Controllers, Models, `Mapping/AgentMapDefinition.cs`)
  - `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/src/agent/` (`workspace/agent/views/`, `api/`, repositories)
  - `Umbraco.AI.Agent.Deploy/src/Umbraco.AI.Agent.Deploy/Artifacts/AIAgentArtifact.cs`
  - `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/Contributors/UserContextContributor.cs`
  - `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/FileStore/AIFileStore.cs`
  - `.claude/memory/frontend-entry-points.md`
  - `docs/plans/chat-history-reduction/PLAN.md`
