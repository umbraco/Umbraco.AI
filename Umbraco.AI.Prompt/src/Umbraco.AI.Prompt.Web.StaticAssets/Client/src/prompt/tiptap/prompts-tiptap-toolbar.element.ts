import { createRef, css, customElement, html, nothing, property, ref, state, type Ref } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/content-type';
import { UMB_CONTENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/content';
import { UMB_BLOCK_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/block';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { tryExecute } from '@umbraco-cms/backoffice/resources';
import type { Editor } from '@umbraco-cms/backoffice/tiptap';
import type { UmbTiptapToolbarElementApi } from '@umbraco-cms/backoffice/tiptap';
import { createEntityContextItem, resolveEntityAdapterByType } from '@umbraco-ai/core';
import { PromptsService } from '../../api/index.js';
import { isPromptAllowed, type PropertyActionContext } from '../property-actions/prompt-scope-matcher.js';
import { UAI_PROMPT_PREVIEW_MODAL } from '../property-actions/prompt-preview-modal.token.js';
import type {
    UaiPromptScope,
    UaiPromptContextItem,
    UaiPromptPreviewModalData,
} from '../property-actions/types.js';

/**
 * Minimal duck-typed interface for workspace contexts.
 * Both content and block workspace contexts satisfy this interface.
 */
interface WorkspaceContextLike {
    getUnique(): string | null | undefined;
    getEntityType(): string;
}

interface TipTapPromptItem {
    unique: string;
    alias: string;
    name: string;
    description: string | null;
    instructions: string;
    scope: UaiPromptScope | null;
    profileId: string | null;
    optionCount: number;
}

/**
 * Custom toolbar element that shows a dropdown of AI prompts filtered to TipTapTool display mode.
 * Receives `api`, `editor`, and `manifest` properties from the toolbar.
 */
@customElement('uai-prompts-tiptap-toolbar')
export class UaiPromptsTiptapToolbarElement extends UmbLitElement {
    public api?: UmbTiptapToolbarElementApi;

    @property({ attribute: false })
    public editor?: Editor;

    @property({ attribute: false })
    public manifest?: any;

    @state()
    private _prompts: TipTapPromptItem[] = [];

    @state()
    private _loading = true;

    #popoverRef: Ref<HTMLElement> = createRef();

    #propertyAlias: string | null = null;
    #propertyEditorUiAlias: string | null = null;
    #contentTypeAliases: string[] = [];
    #workspaceContext?: WorkspaceContextLike;
    #savedSelection?: { from: number; to: number; empty: boolean; text: string };

    constructor() {
        super();

        this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
            if (!context) return;
            this.#propertyAlias = context.getAlias() ?? null;
            this.observe(context.editorManifest, (manifest) => {
                this.#propertyEditorUiAlias = manifest?.alias ?? null;
                this.#filterPrompts();
            });
        });

        // Get content type aliases from structure workspace context (documents/media)
        this.consumeContext(UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.observe(context.structure.contentTypeAliases, (aliases) => {
                this.#contentTypeAliases = aliases ?? [];
                this.#filterPrompts();
            });
        });

        // Workspace context resolution: race content vs block workspace
        this.consumeContext(UMB_CONTENT_WORKSPACE_CONTEXT, (context) => {
            if (!this.#workspaceContext) {
                this.#workspaceContext = context;
            }
        });

        this.consumeContext(UMB_BLOCK_WORKSPACE_CONTEXT, (context) => {
            if (!this.#workspaceContext) {
                this.#workspaceContext = context;
                // For blocks, get content type aliases from block's content structure
                if (context) {
                    this.observe(context.content.structure.contentTypeAliases, (aliases) => {
                        if (this.#contentTypeAliases.length === 0) {
                            this.#contentTypeAliases = aliases ?? [];
                            this.#filterPrompts();
                        }
                    });
                }
            }
        });

        this.#loadPrompts();
    }

    #allPrompts: TipTapPromptItem[] = [];

    async #loadPrompts() {
        this._loading = true;

        // Fetch all prompts, then filter to TipTapTool display mode
        const { data } = await tryExecute(
            this,
            PromptsService.getAllPrompts({ query: { skip: 0, take: 1000 } }),
        );

        if (!data?.items) {
            this._loading = false;
            return;
        }

        // Fetch details for active prompts
        const activeItems = data.items.filter((item) => item.isActive);
        const details = await Promise.all(
            activeItems.map(async (item) => {
                const { data: detail } = await tryExecute(
                    this,
                    PromptsService.getPromptByIdOrAlias({ path: { promptIdOrAlias: item.id } }),
                );
                return detail;
            }),
        );

        this.#allPrompts = details
            .filter((d): d is NonNullable<typeof d> => d !== null && d !== undefined)
            .filter((d) => d.displayMode === 'TipTapTool')
            .map((d) => ({
                unique: d.id,
                alias: d.alias,
                name: d.name,
                description: d.description ?? null,
                instructions: d.instructions,
                scope: d.scope ?? null,
                profileId: d.profileId ?? null,
                optionCount: d.optionCount ?? 1,
            }));

        this._loading = false;
        this.#filterPrompts();
    }

    #filterPrompts() {
        if (!this.#propertyEditorUiAlias || !this.#propertyAlias) {
            this._prompts = [];
            return;
        }

        const context: PropertyActionContext = {
            propertyEditorUiAlias: this.#propertyEditorUiAlias,
            propertyAlias: this.#propertyAlias,
            contentTypeAliases: this.#contentTypeAliases,
        };

        this._prompts = this.#allPrompts.filter((p) => isPromptAllowed(p.scope, context));
    }

    /**
     * Capture editor selection state before the popover opens and steals focus.
     * Then restore the selection so it remains visually highlighted.
     */
    #onToolbarButtonClick() {
        if (!this.editor) return;
        const { from, to, empty } = this.editor.state.selection;
        this.#savedSelection = {
            from,
            to,
            empty,
            text: empty
                ? this.editor.getHTML()
                : this.editor.state.doc.textBetween(from, to, ' '),
        };
        // Restore the selection after the popover steals focus.
        // The popover opens asynchronously, so we need to wait for it to settle
        // before restoring the editor selection.
        if (!empty) {
            setTimeout(() => {
                this.editor?.chain().focus().setTextSelection({ from, to }).run();
            }, 50);
        }
    }

    async #onPromptSelect(prompt: TipTapPromptItem) {
        // Close the popover
        this.#popoverRef.value?.hidePopover();

        if (!this.editor || !this.#workspaceContext) return;

        // Use selection captured when the toolbar button was clicked
        const { from, to, empty, text: selectedText } = this.#savedSelection ?? {
            from: 0,
            to: 0,
            empty: true,
            text: this.editor.getHTML(),
        };

        // Restore the editor selection so it's visually highlighted while the modal is open
        if (!empty) {
            this.editor.chain().focus().setTextSelection({ from, to }).run();
        }

        // Build context items
        const contextItems: UaiPromptContextItem[] = [];

        // Add selection/content as context (structured for SelectionContextContributor)
        contextItems.push({
            description: empty ? 'Full editor content' : 'Selected text from editor',
            value: JSON.stringify({ selection: selectedText }),
        });

        // Add entity context
        const entityContext = await this.#serializeEntityContext();
        if (entityContext) {
            contextItems.push(...entityContext);
        }

        let entityId: string | null | undefined;
        try {
            entityId = this.#workspaceContext.getUnique();
        } catch {
            // getUnique() can throw in block workspaces if contentKey is not yet available
        }
        const entityType = this.#workspaceContext.getEntityType();

        if (!entityId || !entityType || !this.#propertyAlias) return;

        const data: UaiPromptPreviewModalData = {
            promptUnique: prompt.unique,
            promptName: prompt.name,
            promptDescription: prompt.description,
            entityId,
            entityType,
            propertyAlias: this.#propertyAlias,
            contentTypeAlias: this.#contentTypeAliases[0] ?? "",
            context: contextItems,
            optionCount: prompt.optionCount,
        };

        try {
            const result = await umbOpenModal(this, UAI_PROMPT_PREVIEW_MODAL, { data });

            if (result.action === 'insert' && result.valueChanges?.length) {
                const valueChange = result.valueChanges[0];
                const rawValue = typeof valueChange.value === 'string'
                    ? valueChange.value
                    : String(valueChange.value ?? '');
                const isHtml = /<[a-z][\s\S]*>/i.test(rawValue);
                const responseHtml = isHtml ? rawValue : rawValue.replace(/\n/g, '<br>');

                if (empty) {
                    // No selection - append response at the end
                    const endPos = this.editor.state.doc.content.size;
                    this.editor.chain().focus().insertContentAt(endPos, responseHtml).run();
                } else {
                    // Replace selected text
                    this.editor.chain().focus().insertContentAt({ from, to }, responseHtml).run();
                }
            }
        } catch {
            // Modal was cancelled
        }
    }

    async #serializeEntityContext(): Promise<UaiPromptContextItem[] | undefined> {
        if (!this.#workspaceContext) return undefined;

        const entityType = this.#workspaceContext.getEntityType();
        if (!entityType) return undefined;

        try {
            const adapter = await resolveEntityAdapterByType(entityType);
            if (!adapter?.canHandle(this.#workspaceContext)) return undefined;

            const serializedEntity = await adapter.serializeForLlm(this.#workspaceContext);
            return [createEntityContextItem(serializedEntity)];
        } catch {
            return undefined;
        }
    }

    override render() {
        // Hide button when no prompts match
        if (!this._loading && this._prompts.length === 0) return nothing;

        return html`
            <uui-button
                compact
                look="default"
                label="AI"
                title="AI Prompts"
                popovertarget="ai-prompts-popover"
                .disabled=${this._loading}
                @click=${this.#onToolbarButtonClick}>
                <umb-icon name="icon-wand"></umb-icon>
                <uui-symbol-expand slot="extra" open></uui-symbol-expand>
            </uui-button>
            <uui-popover-container id="ai-prompts-popover" placement="bottom-start" ${ref(this.#popoverRef)}>
                <div class="dropdown">
                    ${this._prompts.map(
                        (prompt) => html`
                            <button
                                class="dropdown-item ${!prompt.description ? 'dropdown-item--name-only' : ''}"
                                @click=${() => this.#onPromptSelect(prompt)}>
                                <umb-icon name="icon-wand"></umb-icon>
                                <div class="dropdown-item-content">
                                    <span class="dropdown-item-name">${prompt.name}</span>
                                    ${prompt.description
                                        ? html`<span class="dropdown-item-description">${prompt.description}</span>`
                                        : nothing}
                                </div>
                            </button>
                        `,
                    )}
                </div>
            </uui-popover-container>
        `;
    }

    static override styles = css`
        :host {
            --uui-button-font-weight: normal;
            margin-left: var(--uui-size-space-1);
            margin-bottom: var(--uui-size-space-1);
        }

        uui-button > uui-symbol-expand {
            margin-left: var(--uui-size-space-2);
        }

        .dropdown {
            min-width: 220px;
            max-width: 320px;
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            box-shadow: var(--uui-shadow-depth-3);
            padding: var(--uui-size-space-1) 0;
        }

        .dropdown-item {
            display: flex;
            align-items: flex-start;
            gap: var(--uui-size-space-2);
            width: 100%;
            padding: var(--uui-size-space-3) var(--uui-size-space-4);
            border: none;
            background: none;
            cursor: pointer;
            text-align: left;
            font-family: inherit;
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text);
        }

        .dropdown-item:hover {
            background: var(--uui-color-surface-emphasis);
        }

        .dropdown-item umb-icon {
            flex-shrink: 0;
            font-size: 16px;
            margin-top: 2px;
        }

        .dropdown-item-content {
            display: flex;
            flex-direction: column;
            gap: 2px;
        }

        .dropdown-item--name-only {
            align-items: center;
            font-size: var(--uui-type-default-size);
        }

        .dropdown-item--name-only umb-icon {
            margin-top: 0;
        }

        .dropdown-item-name {
            font-weight: 500;
        }

        .dropdown-item--name-only .dropdown-item-name {
            font-weight: normal;
        }

        .dropdown-item-description {
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
            opacity: 0.7;
        }
    `;
}

export { UaiPromptsTiptapToolbarElement as element };

declare global {
    interface HTMLElementTagNameMap {
        'uai-prompts-tiptap-toolbar': UaiPromptsTiptapToolbarElement;
    }
}
