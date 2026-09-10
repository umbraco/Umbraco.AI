import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiEntityAdapterContext } from "./entity-adapter.context.js";

/**
 * Context token for the entity adapter context.
 *
 * The instance itself is provided by whichever surface owns the user's entity selection --
 * today, only `UaiCopilotContext` in `Umbraco.AI.Agent.Copilot`, via its own identically-aliased
 * token in `contexts/entity-adapter.context-token.ts`. This package can't import that token
 * directly: Core sits below Agent/Agent.Copilot in the dependency graph, and (per that token's own
 * doc comment) a single shared `UmbContextToken` object resolves to nominally distinct types when
 * imported across workspace packages anyway. `consumeContext`/`getContext` match a provided
 * context by the token's alias string, not by object identity, so a second token here sharing the
 * same `"UaiEntityAdapterContext"` alias still resolves to the one instance Copilot provided.
 *
 * See umbraco/Umbraco.AI#353: a contributor that instead constructed its own
 * `UaiEntityAdapterContext` had no way to hear about the user's selection and silently fell back
 * to auto-selecting the last-detected entity.
 */
export const UAI_ENTITY_ADAPTER_CONTEXT = new UmbContextToken<UaiEntityAdapterContext>(
    "UaiEntityAdapterContext",
);
