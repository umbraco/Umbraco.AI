import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Center-region view shown when no conversation is open — the "start a new chat" landing state.
 * (Placeholder; the New-Chat composer arrives with the chat slice.)
 */
@customElement("uai-copilot-workspace-empty-view")
export class UaiCopilotWorkspaceEmptyViewElement extends UmbLitElement {
    override render() {
        return html`
            <div class="empty">
                <uui-icon name="icon-chat"></uui-icon>
                <h3>Start a conversation</h3>
                <p>Pick a conversation on the left, or start a new one.</p>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: grid;
                place-items: center;
                height: 100%;
                color: var(--uui-color-text-alt);
            }
            .empty {
                text-align: center;
            }
            uui-icon {
                font-size: 3rem;
                opacity: 0.5;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceEmptyViewElement;
