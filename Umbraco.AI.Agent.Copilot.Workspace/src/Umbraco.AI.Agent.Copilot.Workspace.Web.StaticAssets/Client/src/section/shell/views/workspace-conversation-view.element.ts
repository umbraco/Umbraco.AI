import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { CopilotWorkspaceChatContext } from "../../../chat/copilot-workspace-chat.context.js";

/**
 * Center-region view for an open conversation. Hosts a {@link CopilotWorkspaceChatContext} (which
 * provides `UAI_CHAT_CONTEXT` + `UAI_HITL_CONTEXT` to the subtree) and renders the shared Agent.UI
 * `<uai-chat>` against it. The router reuses this element across conversations, so a changed
 * `conversationId` rebinds the context to the new conversation (loads its persisted history).
 */
@customElement("uai-copilot-workspace-conversation-view")
export class UaiCopilotWorkspaceConversationViewElement extends UmbLitElement {
    #context = new CopilotWorkspaceChatContext(this);
    #agentsLoaded = false;

    #conversationId?: string;

    @property({ type: String })
    get conversationId(): string | undefined {
        return this.#conversationId;
    }
    set conversationId(value: string | undefined) {
        const previous = this.#conversationId;
        this.#conversationId = value;
        if (value && value !== previous) {
            this.#ensureAgentsLoaded();
            void this.#context.setConversation(value);
        }
        this.requestUpdate("conversationId", previous);
    }

    #ensureAgentsLoaded() {
        if (this.#agentsLoaded) return;
        this.#agentsLoaded = true;
        void this.#context.loadAgents();
    }

    override render() {
        return html`<uai-chat></uai-chat>`;
    }

    static override styles = [
        css`
            :host {
                display: block;
                height: 100%;
                min-height: 0;
            }
            uai-chat {
                display: block;
                height: 100%;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationViewElement;
