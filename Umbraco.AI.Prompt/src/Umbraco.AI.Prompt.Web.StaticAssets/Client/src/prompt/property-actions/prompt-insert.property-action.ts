import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbPropertyActionBase, type UmbPropertyActionArgs } from "@umbraco-cms/backoffice/property-action";
import { UMB_PROPERTY_CONTEXT } from "@umbraco-cms/backoffice/property";
import { UMB_CONTENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/content";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { createEntityContextItem, resolveEntityAdapterByType, type UaiEntityAdapterApi } from "@umbraco-ai/core";
import { UAI_PROMPT_PREVIEW_MODAL, UAI_PROMPT_PREVIEW_SIDEBAR } from "./prompt-preview-modal.token.js";
import type { UaiPromptPropertyActionMeta, UaiPromptContextItem, UaiPromptPreviewModalData } from "./types.js";

/**
 * Property action that opens a modal or sidebar to preview and insert prompt content.
 * The UI mode is determined by the meta.uiMode property:
 * - 'modal' (default): Centered dialog
 * - 'panel': Slide-in sidebar from the right
 */
export class UaiPromptInsertPropertyAction extends UmbPropertyActionBase<UaiPromptPropertyActionMeta> {
    #propertyContext?: typeof UMB_PROPERTY_CONTEXT.TYPE;
    #workspaceContext?: typeof UMB_CONTENT_WORKSPACE_CONTEXT.TYPE;
    #init: Promise<unknown>;
    #workspaceAdapter?: UaiEntityAdapterApi;

    constructor(host: UmbControllerHost, args: UmbPropertyActionArgs<UaiPromptPropertyActionMeta>) {
        super(host, args);

        this.#init = Promise.all([
            this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
                this.#propertyContext = context;
            }).asPromise({ preventTimeout: true }),
            this.consumeContext(UMB_CONTENT_WORKSPACE_CONTEXT, (context) => {
                this.#workspaceContext = context;
            }).asPromise({ preventTimeout: true }),
        ]);
    }

    override async execute() {
        await this.#init;

        if (!this.#propertyContext) {
            throw new Error("Property context is not available");
        }

        if (!this.#workspaceContext) {
            throw new Error("Workspace context is not available");
        }

        const meta = this.args.meta;
        if (!meta) {
            throw new Error("Property action meta is not available");
        }

        // Resolve required entity context for prompt execution
        const entityId = this.#workspaceContext.getUnique();
        const entityType = this.#workspaceContext.getEntityType();
        const propertyAlias = this.#propertyContext.getAlias();

        if (!entityId) {
            throw new Error("Entity ID is not available");
        }

        if (!entityType) {
            throw new Error("Entity type is not available");
        }

        if (!propertyAlias) {
            throw new Error("Property alias is not available");
        }

        // Serialize document context for AI operations
        const context = await this.#serializeEntityContext();

        // Get maxChars from property editor config (if available)
        const config = this.#propertyContext.getConfig();
        const maxCharsConfig = config?.find((c) => c.alias === "maxChars");
        const maxChars = typeof maxCharsConfig?.value === "number" ? maxCharsConfig.value : undefined;

        // Build modal data
        const data: UaiPromptPreviewModalData = {
            promptUnique: meta.promptUnique,
            promptName: meta.label,
            promptDescription: meta.promptDescription,
            entityId,
            entityType,
            propertyAlias,
            culture: this.#propertyContext.getVariantId?.()?.culture ?? undefined,
            segment: this.#propertyContext.getVariantId?.()?.segment ?? undefined,
            context,
            maxChars,
            optionCount: 1, // Default value, actual value determined by prompt configuration
        };

        // Select modal token based on UI mode
        const uiMode = meta.uiMode ?? "modal";
        const modalToken = uiMode === "panel" ? UAI_PROMPT_PREVIEW_SIDEBAR : UAI_PROMPT_PREVIEW_MODAL;

        try {
            const result = await umbOpenModal(this, modalToken, { data });

            if (result.action === "insert") {
                // Apply any value changes returned by the AI
                if (result.valueChanges?.length && this.#workspaceContext) {
                    const adapter = await this.#resolveAdapter();
                    if (adapter?.applyValueChange) {
                        for (const change of result.valueChanges) {
                            await adapter.applyValueChange(this.#workspaceContext, change);
                        }
                    }
                }
            }
        } catch {
            // Modal was rejected/cancelled - do nothing
        }
    }

    /**
     * Resolve the entity adapter for the current workspace context.
     * Caches the adapter instance for reuse within this action.
     */
    async #resolveAdapter(): Promise<UaiEntityAdapterApi | undefined> {
        if (this.#workspaceAdapter) {
            return this.#workspaceAdapter;
        }

        if (!this.#workspaceContext) {
            return undefined;
        }

        const entityType = this.#workspaceContext.getEntityType();
        if (!entityType) {
            return undefined;
        }

        this.#workspaceAdapter = await resolveEntityAdapterByType(entityType);
        return this.#workspaceAdapter;
    }

    /**
     * Serialize the current entity for AI context injection.
     * Resolves the appropriate adapter based on the workspace entity type.
     */
    async #serializeEntityContext(): Promise<UaiPromptContextItem[] | undefined> {
        if (!this.#workspaceContext) {
            return undefined;
        }

        const adapter = await this.#resolveAdapter();
        if (!adapter || !adapter.canHandle(this.#workspaceContext)) {
            return undefined;
        }

        try {
            const serializedEntity = await adapter.serializeForLlm(this.#workspaceContext);
            return [createEntityContextItem(serializedEntity)];
        } catch {
            // Serialization failed - continue without context
            return undefined;
        }
    }
}

export { UaiPromptInsertPropertyAction as api };
