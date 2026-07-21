import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Right region: the context panel (project instructions + attachments), shown when the open
 * conversation belongs to a project. Collapse/resize chrome is owned by the shell; this element
 * renders the header (with a collapse control that raises a bubbling <c>collapse</c> event) and the
 * body. Populated with project data in Phase 6.
 */
@customElement("uai-copilot-workspace-context-panel")
export class UaiCopilotWorkspaceContextPanelElement extends UmbLitElement {
    #collapse() {
        this.dispatchEvent(new CustomEvent("collapse", { bubbles: true, composed: true }));
    }

    override render() {
        return html`
            <div class="header">
                <span>Context</span>
                <uui-button
                    compact
                    look="secondary"
                    label="Collapse context panel"
                    title="Collapse"
                    @click=${this.#collapse}
                >
                    <uui-icon name="icon-navigation-right"></uui-icon>
                </uui-button>
            </div>
            <div class="body">
                <p class="muted">Project instructions and attachments appear here.</p>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex-direction: column;
                height: 100%;
                background: var(--uui-color-surface);
            }
            .header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                padding: var(--uui-size-space-2) var(--uui-size-space-4);
                font-weight: bold;
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .body {
                flex: 1;
                overflow-y: auto;
                padding: var(--uui-size-space-4);
            }
            .muted {
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceContextPanelElement;
