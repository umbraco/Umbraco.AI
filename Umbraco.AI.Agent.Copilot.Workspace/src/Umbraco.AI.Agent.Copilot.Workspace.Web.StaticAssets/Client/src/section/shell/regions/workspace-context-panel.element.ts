import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Right region: the collapsible context panel (project instructions + attachments), shown when the
 * open conversation belongs to a project. Placeholder for now; populated in Phase 6.
 */
@customElement("uai-copilot-workspace-context-panel")
export class UaiCopilotWorkspaceContextPanelElement extends UmbLitElement {
    override render() {
        return html`
            <div class="header">Context</div>
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
                border-left: 1px solid var(--uui-color-divider);
                background: var(--uui-color-surface);
            }
            .header {
                padding: var(--uui-size-space-4);
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
