import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Left region: the conversation list. This placeholder establishes the layout (New Chat + search
 * header, scrollable list area); the data-bound list — grouped by project then date bucket, with
 * pin/archive/rename/delete — is wired over the generated client in the next slice.
 */
@customElement("uai-copilot-workspace-conversation-list")
export class UaiCopilotWorkspaceConversationListElement extends UmbLitElement {
    override render() {
        return html`
            <div class="header">
                <uui-button look="primary" label="New chat">
                    <uui-icon name="icon-add"></uui-icon>
                    New chat
                </uui-button>
            </div>
            <div class="list">
                <p class="muted">Your conversations will appear here.</p>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex-direction: column;
                height: 100%;
                border-right: 1px solid var(--uui-color-divider);
                background: var(--uui-color-surface);
            }
            .header {
                padding: var(--uui-size-space-4);
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .header uui-button {
                width: 100%;
            }
            .list {
                flex: 1;
                overflow-y: auto;
                padding: var(--uui-size-space-3);
            }
            .muted {
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationListElement;
