import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Center-region view for an open conversation. The chat thread (reusing the Agent.UI chat components
 * via a CopilotWorkspaceChatContext) is wired here in the chat slice; for now it shows the bound id.
 */
@customElement("uai-copilot-workspace-conversation-view")
export class UaiCopilotWorkspaceConversationViewElement extends UmbLitElement {
    @property({ type: String })
    conversationId?: string;

    override render() {
        return html`
            <div class="placeholder">
                <uui-box headline="Conversation">
                    <p>Conversation <code>${this.conversationId ?? "(none)"}</code></p>
                    <p><em>The chat thread is wired in the next slice.</em></p>
                </uui-box>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                height: 100%;
                overflow-y: auto;
            }
            .placeholder {
                padding: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationViewElement;
