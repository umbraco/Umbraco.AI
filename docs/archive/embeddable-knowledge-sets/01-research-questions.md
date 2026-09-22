---
type: research-questions
---

# Research Questions

1. In `Umbraco.AI/src/Umbraco.AI.Core/Contexts/`, explain end to end how a Context and its Context Resources work today — from the domain models (`AIContext`, `AIContextResource`, `AIContextResourceInjectionMode`, `AIContextSource`) through service layer (`IAIContextService`/`AIContextService`), to how a resource's data is resolved and formatted for the LLM (`AIContextProcessor`, `AIResolvedContext`, `AIResolvedResource`). What is the full lifecycle of a single context resource from definition to being consumed by a request?

2. How does context resolution work at request time? Detail `IAIContextResolutionService`/`AIContextResolutionService`, the `IAIContextResolver` extension point, `AIContextResolverCollection(Builder)`, and the built-in resolvers (`ProfileContextResolver`, `ContentContextResolver`) — including ordering, how resolvers select/merge contexts, and the override sentinel behavior. How does `Umbraco.AI.Agent`'s `AgentContextResolver` contribute into this same pipeline (`builder.AIContextResolvers().Append<>()`)?

3. In `Umbraco.AI/src/Umbraco.AI.Core/Contexts/ResourceTypes/`, how does the resource-type extension point work? Cover `IAIContextResourceType`, `AIContextResourceTypeBase`, the `[AIContextResourceType]` attribute + `IDiscoverable` auto-discovery via `TypeLoader`, the settings/data model contract (`ResolveDataAsync`, `FormatDataForLlm`), and the built-in `TextResourceType` and `BrandVoiceResourceType`. What exactly must a package supply to register a new resource "kind"?

4. How is resolved context injected into the LLM, and what supports on-demand retrieval? Trace `AIContextInjectingChatMiddleware`/`AIContextInjectingChatClient` (its position in the chat middleware pipeline), how `AIContextResourceInjectionMode` distinguishes "always injected into system prompt" vs on-demand, and how `IAIContextAccessor` + the tools `ListContextResourcesTool`/`GetContextResourceTool` expose resources to the model during a request.

5. What are the established patterns for a package/provider to contribute code-defined items that Core enumerates at runtime (as opposed to DB-persisted, user-authored entities)? Compare the `IDiscoverable` + attribute discovery pattern (`IAIProvider`/`[AIProvider]`, `IAITool`/`[AITool]`) with the `LazyCollectionBuilderBase` vs `OrderedCollectionBuilderBase` collection-builder patterns, the composer/`[ComposeAfter]` conventions, and how these registrations are consumed via `BuilderCollectionBase<T>` injection. Which of these currently allow items to originate purely from an add-on package with no server-side registration code?

6. How are Contexts persisted, mapped, and integrated across the wider system today? Cover the `InMemoryAIContextRepository` (Core default) vs `EFCoreAIContextRepository` override in `Umbraco.AI.Persistence`, the `IAIContextFactory` entity↔domain mapping (incl. encryption), the migrations, the save/delete notifications, and the `Umbraco.AI.Deploy` connectors/artifacts (`AIContextArtifact`, `UmbracoAIContextServiceConnector`). Which parts of this pipeline assume a context is user-created and mutable vs read-only?

7. In `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/context/` (and `context-resource-type/`), how is the Context backoffice UI structured — collection view, workspace/detail editor, resource-list management, and the resource-type picker — and what design system / component library, theming, and manifest (`umbraco-package.json` extension) conventions does it follow? How does a package currently ship frontend extensions and localization into the backoffice (Razor Class Library `wwwroot` + `StaticWebAssetBasePath` + package manifest)?

8. How do comparable systems bundle static, code-shipped "knowledge" with plugins/extensions for LLM grounding — e.g. Microsoft.Extensions.AI's handling of static context/resources, and the Model Context Protocol (MCP) notion of server-provided "resources"? What capabilities or constraints of these (and of M.E.AI generally, given the repo's "thin wrapper" philosophy) are relevant to how read-only, package-embedded knowledge would be surfaced to a model?

## Key Context Pointers

- Ticket: `.humanlayer/tasks/design-embeddable-knowledge-sets-for-llm-integration/ticket.md` (knowledge sets = like contexts/context resources but purely embeddable by package providers; example subject: "Umbraco Engage")
- Internal design doc: `docs/internal/core/ideas/ai-context.md`
- Core context system: `Umbraco.AI/src/Umbraco.AI.Core/Contexts/` (Resolvers, ResourceTypes, Middleware subfolders)
- Runtime context (adjacent concept): `Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/`
- On-demand context tools: `Umbraco.AI/src/Umbraco.AI.Core/Tools/Context/`
- DI / extension points: `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.Context.cs`, `UmbracoBuilderExtensions.Collections.cs`, `UmbracoBuilderExtensions.Providers.cs`
- Persistence: `Umbraco.AI/src/Umbraco.AI.Persistence/Context/`
- Web/API: `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Context/` and `.../ContextResourceTypes/`
- Frontend: `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/context/`, `.../context-resource-type/`
- Agent integration: `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Context/AgentContextResolver.cs`
- Deploy: `Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/Artifacts/AIContextArtifact.cs`, `.../Connectors/ServiceConnectors/UmbracoAIContextServiceConnector.cs`
- Provider/collection-builder reference patterns: `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/OpenAIProvider.cs`, `Umbraco.AI/src/Umbraco.AI.Core/Providers/`, `Umbraco.AI/src/Umbraco.AI.Core/Tools/`
- Package static-asset shipping: `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/Umbraco.AI.OpenAI.csproj`, `.../wwwroot/umbraco-package.json`
- Libraries: Microsoft.Extensions.AI (M.E.AI, "thin wrapper" philosophy); Model Context Protocol (MCP) resources
