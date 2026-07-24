import { css, customElement, html, nothing, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UaiConversationEntityContext } from "./entity/conversation-entity.context.js";
import type { UaiArchivedConversation } from "./grouping.js";
import { UAI_CONVERSATION_ENTITY_TYPE } from "../constants.js";
import { copilotWorkspaceConversationPath } from "../paths.js";

/**
 * A single archived conversation, rendered as a child of the sidebar's Archived (recycle-bin) node.
 *
 * Built on `uui-menu-item` (same chrome as the active conversation nodes) so it inherits proper tree
 * indentation, active highlighting and hover-revealed ⋯ actions. Provides the standard
 * `UMB_ENTITY_CONTEXT` plus the per-node conversation context, so the shared ⋯ entity-action menu
 * resolves — and with pin/rename/move gated to non-archived, an archived node offers only Restore + Delete.
 *
 * Opening navigates to the conversation route (same deep link as an active chat); the chat view renders
 * it read-only because it is archived. A conversation that belongs to a project carries a small folder
 * flag overlaid on its icon (rather than a text chip) to signal that, matching the CMS tree-item
 * "sign"/flag idiom while staying within the space of a menu row.
 */
@customElement("uai-copilot-workspace-archived-tree-item")
export class UaiCopilotWorkspaceArchivedTreeItemElement extends UmbLitElement {
    #entityContext = new UmbEntityContext(this);
    #conversationContext = new UaiConversationEntityContext(this);

    @property({ attribute: false })
    item?: UaiArchivedConversation;

    /** Current router path, for active highlighting. */
    @property({ type: String })
    activePath?: string;

    override willUpdate(changed: Map<PropertyKey, unknown>) {
        super.willUpdate(changed);
        if (changed.has("item") && this.item) {
            this.#entityContext.setEntityType(UAI_CONVERSATION_ENTITY_TYPE);
            this.#entityContext.setUnique(this.item.conversation.id);
            this.#conversationContext.setModel(this.item.conversation);
        }
    }

    override render() {
        const item = this.item;
        if (!item) return nothing;
        const conversation = item.conversation;
        const href = copilotWorkspaceConversationPath(conversation.id);
        const active = this.activePath?.includes(href) ?? false;
        const title = conversation.title?.trim() || this.localize.term("uaiCopilotWorkspace_untitledConversation");

        return html`
            <uui-menu-item label=${title} href=${href} ?active=${active}>
                <span slot="icon" class="icon ${item.projectName ? "has-flag" : ""}" title=${item.projectName ?? ""}>
                    <uui-icon name="icon-chat"></uui-icon>
                    ${item.projectName
                        ? html`<uui-icon class="flag" name="icon-folder"></uui-icon>`
                        : nothing}
                </span>
                <umb-entity-actions-dropdown slot="actions" compact .label=${title}>
                    <uui-symbol-more slot="label"></uui-symbol-more>
                </umb-entity-actions-dropdown>
            </uui-menu-item>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }
            /* A small folder "flag" overlaid on the bottom-right of the chat icon, ringed in the surface
               colour so it reads as a badge — the CMS tree-item sign idiom, done inline. */
            .icon {
                position: relative;
                display: inline-flex;
            }
            .icon .flag {
                position: absolute;
                right: -3px;
                bottom: -3px;
                font-size: 0.6em;
                color: var(--uui-color-text-alt);
                background: var(--uui-color-surface);
                border-radius: 50%;
                padding: 1px;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceArchivedTreeItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-archived-tree-item": UaiCopilotWorkspaceArchivedTreeItemElement;
    }
}
