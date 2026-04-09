import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbPropertyActionBase, type UmbPropertyActionArgs } from "@umbraco-cms/backoffice/property-action";
import { UMB_PROPERTY_CONTEXT } from "@umbraco-cms/backoffice/property";
import { UMB_CONTENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/content";
import { UMB_BLOCK_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/block";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/document";
import { UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/content-type";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { createEntityContextItem, createElementContextItem, resolveEntityAdapterByType, type UaiEntityAdapterApi } from "@umbraco-ai/core";
import { UAI_PROMPT_PREVIEW_MODAL, UAI_PROMPT_PREVIEW_SIDEBAR } from "./prompt-preview-modal.token.js";
import type { UaiPromptPropertyActionMeta, UaiPromptContextItem, UaiPromptPreviewModalData } from "./types.js";

/**
 * Minimal duck-typed interface for workspace contexts.
 * Both content and block workspace contexts satisfy this interface.
 */
interface WorkspaceContextLike {
    getUnique(): string | null | undefined;
    getEntityType(): string;
}

/**
 * Property action that opens a modal or sidebar to preview and insert prompt content.
 * The UI mode is determined by the meta.uiMode property:
 * - 'modal' (default): Centered dialog
 * - 'panel': Slide-in sidebar from the right
 *
 * Supports both document/media workspaces (UMB_CONTENT_WORKSPACE_CONTEXT)
 * and block workspaces (UMB_BLOCK_WORKSPACE_CONTEXT).
 */
export class UaiPromptInsertPropertyAction extends UmbPropertyActionBase<UaiPromptPropertyActionMeta> {
    #propertyContext?: typeof UMB_PROPERTY_CONTEXT.TYPE;
    #workspaceContext?: WorkspaceContextLike;
    #parentDocumentContext?: WorkspaceContextLike;
    #isBlockWorkspace = false;
    #contentTypeAlias?: string;
    #init: Promise<unknown>;
    #workspaceAdapter?: UaiEntityAdapterApi;

    constructor(host: UmbControllerHost, args: UmbPropertyActionArgs<UaiPromptPropertyActionMeta>) {
        super(host, args);

        // Workspace context resolution: race content vs block workspace
        const workspaceResolved = new Promise<void>((resolve) => {
            this.consumeContext(UMB_CONTENT_WORKSPACE_CONTEXT, (ctx) => {
                if (!this.#workspaceContext) {
                    this.#workspaceContext = ctx;
                    resolve();
                }
            });

            this.consumeContext(UMB_BLOCK_WORKSPACE_CONTEXT, (ctx) => {
                if (!this.#workspaceContext) {
                    this.#workspaceContext = ctx;
                    this.#isBlockWorkspace = true;
                    // For blocks, get content type alias from the content element manager's structure
                    if (ctx) {
                        this.observe(
                            ctx.content.structure.contentTypeAliases,
                            (aliases) => {
                                this.#contentTypeAlias ??= aliases?.[0];
                            },
                        );
                    }
                    resolve();
                }
            });
        });

        // Observe the parent document context for blocks.
        // Uses passContextAliasMatches() to skip the block workspace alias match
        // and find the document workspace higher in the DOM tree.
        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (ctx) => {
            this.#parentDocumentContext = ctx as unknown as WorkspaceContextLike;
        }).passContextAliasMatches();

        this.#init = Promise.all([
            this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
                this.#propertyContext = context;
            }).asPromise({ preventTimeout: true }),
            workspaceResolved,
            // Get content type alias from the structure workspace context (available for documents/media, not blocks)
            this.consumeContext(UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT, (ctx) => {
                if (ctx) {
                    this.observe(
                        ctx.structure.contentTypeAliases,
                        (aliases) => {
                            this.#contentTypeAlias ??= aliases?.[0];
                        },
                    );
                }
            }).asPromise().catch(() => {
                // Not available inside block workspaces - contentTypeAlias resolved from block context instead
            }),
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

        // Resolve required context for prompt execution
        const propertyAlias = this.#propertyContext.getAlias();
        if (!propertyAlias) {
            throw new Error("Property alias is not available");
        }

        // For blocks: entity = parent document, element = block
        // For documents: entity = document, no element
        let entityId: string | null | undefined;
        let entityType: string;
        let elementId: string | undefined;
        let elementType: string | undefined;

        if (this.#isBlockWorkspace) {
            try {
                elementId = this.#workspaceContext.getUnique() ?? undefined;
            } catch {
                // getUnique() can throw for blocks if contentKey is not yet available
            }
            elementType = this.#workspaceContext.getEntityType();

            if (this.#parentDocumentContext) {
                entityId = this.#parentDocumentContext.getUnique();
                entityType = this.#parentDocumentContext.getEntityType();
            } else {
                // Fallback: if parent document couldn't be resolved, use block as entity
                entityId = elementId;
                entityType = elementType;
            }
        } else {
            entityId = this.#workspaceContext.getUnique();
            entityType = this.#workspaceContext.getEntityType();
        }

        if (!entityId) {
            throw new Error("Entity ID is not available");
        }

        if (!entityType) {
            throw new Error("Entity type is not available");
        }

        // Serialize entity and element context for AI operations
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
            contentTypeAlias: this.#contentTypeAlias ?? "",
            elementId,
            elementType,
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
     * Serialize the current entity (and element, if editing a block) for AI context injection.
     * For blocks: sends both the parent document (entity context) and the block (element context).
     * For documents: sends only the document (entity context).
     */
    async #serializeEntityContext(): Promise<UaiPromptContextItem[] | undefined> {
        if (!this.#workspaceContext) {
            return undefined;
        }

        const adapter = await this.#resolveAdapter();
        if (!adapter || !adapter.canHandle(this.#workspaceContext)) {
            return undefined;
        }

        const contextItems: UaiPromptContextItem[] = [];

        try {
            if (this.#isBlockWorkspace) {
                // Block: serialize block as element context
                const serializedElement = await adapter.serializeForLlm(this.#workspaceContext);
                contextItems.push(createElementContextItem(serializedElement));

                // Serialize parent document as entity context
                if (this.#parentDocumentContext) {
                    const docAdapter = await resolveEntityAdapterByType("document");
                    if (docAdapter?.canHandle(this.#parentDocumentContext)) {
                        const serializedEntity = await docAdapter.serializeForLlm(this.#parentDocumentContext);
                        contextItems.push(createEntityContextItem(serializedEntity));
                    }
                }
            } else {
                // Document/media: serialize as entity context (as before)
                const serializedEntity = await adapter.serializeForLlm(this.#workspaceContext);
                contextItems.push(createEntityContextItem(serializedEntity));
            }
        } catch {
            // Serialization failed - continue without context
            return undefined;
        }

        return contextItems.length > 0 ? contextItems : undefined;
    }
}

export { UaiPromptInsertPropertyAction as api };
