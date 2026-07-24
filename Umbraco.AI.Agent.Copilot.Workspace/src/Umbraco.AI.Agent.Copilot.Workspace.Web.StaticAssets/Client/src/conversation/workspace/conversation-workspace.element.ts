import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiConversationWorkspaceContext } from "./conversation-workspace.context.js";
import "../../layout/context-panel-layout.element.js";
import "./conversation-chat-view.element.js";
import "./conversation-context-panel.element.js";

/**
 * The conversation workspace — mounted by the `conversation/*` routes. It provides the reactive
 * {@link UaiConversationWorkspaceContext} store (the single source of truth for the open conversation)
 * and lays out its two regions via the reusable slide-out layout: the chat in `main`, the context panel
 * in `aside`. The router reuses this element across conversations, so the setter/`startDraft` rebind the
 * store; the panel slide-out/resize mechanics belong entirely to the layout.
 */
@customElement("uai-copilot-workspace-conversation-workspace")
export class UaiCopilotWorkspaceConversationWorkspaceElement extends UmbLitElement {
    #store = new UaiConversationWorkspaceContext(this);

    /** Opens a persisted conversation (set by the `conversation/:id` route). */
    set conversationId(value: string | undefined) {
        if (value) void this.#store.setConversationId(value);
    }

    /** Starts a draft (set by the `conversation/new` route); an optional project pre-attaches it. */
    startDraft(projectId?: string): void {
        void this.#store.startDraft(projectId);
    }

    override render() {
        return html`
            <uai-copilot-workspace-context-panel-layout>
                <uai-copilot-workspace-conversation-chat-view slot="main"></uai-copilot-workspace-conversation-chat-view>
                <uai-copilot-workspace-conversation-context-panel
                    slot="aside"
                ></uai-copilot-workspace-conversation-context-panel>
            </uai-copilot-workspace-context-panel-layout>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                height: 100%;
                min-height: 0;
            }
            uai-copilot-workspace-context-panel-layout {
                height: 100%;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationWorkspaceElement;
