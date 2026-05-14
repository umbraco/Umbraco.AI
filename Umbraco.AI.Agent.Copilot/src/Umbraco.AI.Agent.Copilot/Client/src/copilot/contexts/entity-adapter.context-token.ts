import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiEntityAdapterContext } from "@umbraco-ai/core";

/**
 * Context token for the entity adapter context.
 *
 * Provided by the copilot in {@link UaiCopilotContext}; consumed by tools that need to read full
 * property envelopes via `serializeSelectedEntity()` and apply value changes via
 * `applyValueChange()` (the property value operation tools introduced for AI-driven authoring).
 *
 * Lives in the copilot package rather than `@umbraco-ai/core` because the same `UmbContextToken`
 * class resolves to nominally distinct types when imported from different workspace packages
 * (TypeScript's `#private` field tracks each path separately). Hosting the token in the package
 * that actually `provideContext`s and consumes it sidesteps that resolution mismatch. Other
 * surfaces in `Umbraco.AI.Agent.UI` continue to use the thinner `UAI_ENTITY_CONTEXT` shared
 * contract.
 */
export const UAI_ENTITY_ADAPTER_CONTEXT = new UmbContextToken<UaiEntityAdapterContext>(
    "UaiEntityAdapterContext",
);
