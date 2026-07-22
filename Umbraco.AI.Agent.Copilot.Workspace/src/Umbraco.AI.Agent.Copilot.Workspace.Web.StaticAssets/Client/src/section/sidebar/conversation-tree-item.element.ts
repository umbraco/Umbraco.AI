import { css, customElement, html, nothing, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UaiConversationEntityContext } from "../../conversation/entity/conversation-entity.context.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import { UAI_CONVERSATION_ENTITY_TYPE } from "../../constants.js";
import { copilotWorkspaceConversationPath } from "../../paths.js";

/**
 * A single conversation node in the sidebar tree. Provides the standard `UMB_ENTITY_CONTEXT` plus the
 * per-node conversation context, so the shared entity-action system renders the ⋯ menu (pin / rename /
 * move / archive / delete) and the state conditions can gate the toggle pairs. Navigation is via the
 * `uui-menu-item` href; the shell renders the chat.
 */
@customElement("uai-copilot-workspace-conversation-tree-item")
export class UaiCopilotWorkspaceConversationTreeItemElement extends UmbLitElement {
    #entityContext = new UmbEntityContext(this);
    #conversationContext = new UaiConversationEntityContext(this);

    @property({ attribute: false })
    conversation?: ConversationResponseModel;

    /** Current router path, for active highlighting. */
    @property({ type: String })
    activePath?: string;

    override willUpdate(changed: Map<PropertyKey, unknown>) {
        super.willUpdate(changed);
        if (changed.has("conversation") && this.conversation) {
            this.#entityContext.setEntityType(UAI_CONVERSATION_ENTITY_TYPE);
            this.#entityContext.setUnique(this.conversation.id);
            this.#conversationContext.setModel(this.conversation);
        }
    }

    override render() {
        const conversation = this.conversation;
        if (!conversation) return nothing;
        const href = copilotWorkspaceConversationPath(conversation.id);
        const active = this.activePath?.includes(href) ?? false;
        const title = conversation.title?.trim() || this.localize.term("uaiCopilotWorkspace_untitledConversation");

        return html`
            <uui-menu-item label=${title} href=${href} ?active=${active}>
                <uui-icon slot="icon" name=${conversation.isPinned ? "icon-pushpin" : "icon-chat"}></uui-icon>
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
        `,
    ];
}

export default UaiCopilotWorkspaceConversationTreeItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-conversation-tree-item": UaiCopilotWorkspaceConversationTreeItemElement;
    }
}
