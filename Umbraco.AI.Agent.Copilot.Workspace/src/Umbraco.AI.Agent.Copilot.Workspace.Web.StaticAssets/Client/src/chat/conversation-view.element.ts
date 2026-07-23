import { css, customElement, html, property, query } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiCopilotWorkspaceChatContext } from "./copilot-workspace-chat.context.js";

/**
 * Center-region view for an open conversation. Hosts a {@link UaiCopilotWorkspaceChatContext} (which
 * provides `UAI_CHAT_CONTEXT` + `UAI_HITL_CONTEXT` to the subtree) and renders the shared Agent.UI
 * `<uai-chat>` against it. The router reuses this element across conversations, so a changed
 * `conversationId` rebinds the context to the new conversation (loads its persisted history).
 */
@customElement("uai-copilot-workspace-conversation-view")
export class UaiCopilotWorkspaceConversationViewElement extends UmbLitElement {
    #context = new UaiCopilotWorkspaceChatContext(this);
    #agentsLoaded = false;

    /** The shared chat element; used to focus its composer when the conversation changes. */
    @query("uai-chat")
    private _chat?: HTMLElement & { focusComposer?: () => void };

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
            // Focus the composer on every open/switch (the view is reused across conversations, so
            // the input doesn't remount — its own first-mount focus wouldn't fire on a switch).
            this.updateComplete.then(() => this._chat?.focusComposer?.());
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
            /* Let the chat fill the full width/height so its scroll bar sits at the far edge, and
               constrain only the inner message list + composer to a comfortable reading width
               (the workspace area can get very wide, especially when the context panel is collapsed). */
            uai-chat {
                display: block;
                height: 100%;
                width: 100%;
                --uai-chat-content-max-width: 860px;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationViewElement;
