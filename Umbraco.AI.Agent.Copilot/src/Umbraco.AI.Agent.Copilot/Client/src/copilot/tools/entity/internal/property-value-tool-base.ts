import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "@umbraco-ai/agent-ui";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "../../../contexts/entity-adapter.context-token.js";
import {
    invokePropertyValueOperation,
    type DocumentMetadata,
    type PropertyOperation,
    type PropertyPathSegment,
    type PropertyValueOperationResponse,
    type VariantId,
} from "./property-value-operation.client.js";
import { normalizeVariantForProperty } from "./variant-normalization.js";

export { normalizeVariantForProperty } from "./variant-normalization.js";

/**
 * Loose structural shape of a workspace context's `getValues()` entry. Both document and block
 * workspaces expose values in this shape.
 */
interface PropertyValueEntry {
    alias: string;
    value?: unknown;
    culture: string | null;
    segment: string | null;
    editorAlias: string;
}

/**
 * Loose structural shape of the workspace context fields we need to read property values and
 * resolve metadata. Mirrored without strict typing to avoid coupling to a specific workspace
 * implementation (document vs. block vs. media).
 */
interface WorkspaceContextLike {
    getValues?: () => PropertyValueEntry[] | undefined;
    getContentTypeUnique?: () => string | undefined;
    getName?: (variantId?: unknown) => string | undefined;
    splitView?: {
        getActiveVariants?: () =>
            | Array<{ culture: string | null; segment: string | null }>
            | undefined;
    };
    variants?: unknown;
    // Content/media/block workspaces expose a structure manager that knows each property's
    // variance. Used to avoid staging an invariant property under a culture/segment.
    structure?: {
        getPropertyStructureByAlias?: (
            alias: string,
        ) => Promise<{ variesByCulture?: boolean; variesBySegment?: boolean } | undefined>;
    };
}

/**
 * Shared base for property-value-operation tools.
 *
 * Each subclass implements {@link buildOperation} to map the LLM-supplied args onto the
 * dispatch operation + path. The base handles the common flow:
 * <ol>
 *   <li>Resolve the entity adapter context.</li>
 *   <li>Read the staged root value from the workspace.</li>
 *   <li>Build the document metadata.</li>
 *   <li>POST to the property value operation endpoint.</li>
 *   <li>Apply the returned new value via <c>applyValueChange</c> (workspace staging).</li>
 *   <li>Return a JSON-stringified summary to the LLM.</li>
 * </ol>
 */
export abstract class PropertyValueOperationToolBase
    extends UmbControllerBase
    implements UaiAgentToolApi
{
    /**
     * Map the LLM-supplied args to the dispatch operation, path, and operation-specific args.
     * Returns <c>null</c> when args are invalid; the base class then returns a structured
     * argument-validation error to the LLM.
     */
    protected abstract buildOperation(
        args: Record<string, unknown>,
    ):
        | {
              operation: PropertyOperation;
              path: PropertyPathSegment[];
              args?: unknown;
              variant?: VariantId;
          }
        | { error: { code: string; message: string } };

    async execute(args: Record<string, unknown>): Promise<string> {
        const built = this.buildOperation(args);
        if ("error" in built) {
            return JSON.stringify({ success: false, error: built.error });
        }

        const adapterContext = await this.getContext(UAI_ENTITY_ADAPTER_CONTEXT);
        if (!adapterContext) {
            return JSON.stringify({
                success: false,
                error: {
                    code: "no-adapter-context",
                    message:
                        "Entity adapter context not available. This tool requires an active entity editor.",
                },
            });
        }

        const selected = adapterContext.getSelectedEntity();
        if (!selected) {
            return JSON.stringify({
                success: false,
                error: {
                    code: "no-selected-entity",
                    message: "No entity is currently selected.",
                },
            });
        }

        // Resolve the root property the path begins in.
        const rootProperty = built.path[0];
        if (typeof rootProperty !== "string") {
            return JSON.stringify({
                success: false,
                error: {
                    code: "invalid-path",
                    message: "Path must begin with a property alias segment.",
                },
            });
        }

        const workspace = selected.workspaceContext as WorkspaceContextLike;
        const values = workspace.getValues?.() ?? [];

        const variantHint = built.variant ?? this.#resolveActiveVariant(workspace);

        // Normalise the variant against the ROOT property's variance. Falling back to the active
        // variant (above) tags EVERY value with the active culture/segment — but an invariant
        // (shared) property must be staged with culture/segment = null, or the save fails with
        // PropertyTypeCultureVarianceMismatch. Genuinely variant properties keep their variant.
        let rootPropertyType: { variesByCulture?: boolean; variesBySegment?: boolean } | undefined;
        try {
            rootPropertyType = await workspace.structure?.getPropertyStructureByAlias?.(rootProperty);
        } catch (error) {
            // Keep execute()'s convention: every failure path returns a structured error to the agent.
            return JSON.stringify({
                success: false,
                error: {
                    code: "property-variance-unresolved",
                    message:
                        `Failed to resolve the variance of property '${rootProperty}' from the workspace ` +
                        `structure: ${error instanceof Error ? error.message : String(error)}. Refusing to ` +
                        `stage the value to avoid a culture variance mismatch on save.`,
                },
            });
        }
        if (!rootPropertyType) {
            // Do NOT silently fall back to the active variant: if the property is actually invariant,
            // staging it under a culture reintroduces the PropertyTypeCultureVarianceMismatch this
            // guards against. Fail deterministically instead so the miss is visible, not intermittent.
            return JSON.stringify({
                success: false,
                error: {
                    code: "property-variance-unresolved",
                    message:
                        `Could not resolve the variance of property '${rootProperty}' from the workspace ` +
                        `structure. Refusing to stage the value, because staging under the wrong culture/segment ` +
                        `would fail on save with a culture variance mismatch. Verify the property exists on the ` +
                        `content type.`,
                },
            });
        }
        const variant = normalizeVariantForProperty(
            variantHint,
            rootPropertyType.variesByCulture ?? false,
            rootPropertyType.variesBySegment ?? false,
        );

        // The workspace's getValues() only lists properties with staged values; properties the
        // user hasn't touched are absent. The dispatcher canonicalises root editor alias resolution
        // server-side from documentMetadata.contentTypeKey + path[0], so we only need to send the
        // staged root value (or undefined for never-touched properties).
        const rootEntry = this.#findValueEntry(values, rootProperty, variant);
        const rootValue = rootEntry?.value;

        const documentMetadata: DocumentMetadata = {
            contentTypeKey: workspace.getContentTypeUnique?.() ?? "",
            variants: this.#resolveVariants(workspace),
            isVariant: variant?.culture != null,
            isSegmented: variant?.segment != null,
            name: workspace.getName?.(),
        };

        const response: PropertyValueOperationResponse = await invokePropertyValueOperation(this, {
            path: built.path,
            operation: built.operation,
            args: built.args,
            rootValue,
            documentMetadata,
        });

        if (!response.success || response.error) {
            return JSON.stringify({ success: false, error: response.error });
        }

        // Apply the mutated root value back to the workspace via the existing staging path.
        const applyResult = await adapterContext.applyValueChange({
            path: rootProperty,
            value: response.newRootValue,
            culture: variant?.culture ?? undefined,
            segment: variant?.segment ?? undefined,
        });

        if (!applyResult.success) {
            return JSON.stringify({
                success: false,
                error: {
                    code: "apply-failed",
                    message: applyResult.error ?? "Failed to apply staged value to workspace.",
                },
            });
        }

        return JSON.stringify({
            success: true,
            blockKey: response.blockKey,
            message: this.successMessage(built.operation, response),
        });
    }

    /**
     * Override to customise the success message returned to the LLM. Default: a short summary.
     */
    protected successMessage(operation: PropertyOperation, response: PropertyValueOperationResponse): string {
        if (operation === "AddItem" && response.blockKey) {
            return `Item added (blockKey ${response.blockKey}). Changes are staged - user must save to persist.`;
        }

        return `Operation ${operation} succeeded. Changes are staged - user must save to persist.`;
    }

    #findValueEntry(
        values: PropertyValueEntry[],
        propertyAlias: string,
        variant: VariantId | undefined,
    ): PropertyValueEntry | undefined {
        let invariantFallback: PropertyValueEntry | undefined;
        for (const entry of values) {
            if (entry.alias.toLowerCase() !== propertyAlias.toLowerCase()) {
                continue;
            }

            if (
                variant === undefined ||
                (entry.culture === variant.culture && entry.segment === variant.segment)
            ) {
                return entry;
            }

            if (entry.culture === null && entry.segment === null) {
                invariantFallback = entry;
            }
        }

        return invariantFallback;
    }

    #resolveActiveVariant(workspace: WorkspaceContextLike): VariantId | undefined {
        const active = workspace.splitView?.getActiveVariants?.() ?? [];
        const first = active[0];
        if (!first) {
            return undefined;
        }
        return { culture: first.culture, segment: first.segment };
    }

    #resolveVariants(workspace: WorkspaceContextLike): VariantId[] {
        const active = workspace.splitView?.getActiveVariants?.() ?? [];
        if (active.length === 0) {
            return [{ culture: null, segment: null }];
        }
        return active.map((v) => ({ culture: v.culture, segment: v.segment }));
    }
}
