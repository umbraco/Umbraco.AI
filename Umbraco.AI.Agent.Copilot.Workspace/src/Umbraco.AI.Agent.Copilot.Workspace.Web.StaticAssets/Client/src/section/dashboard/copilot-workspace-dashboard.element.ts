import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

/**
 * Placeholder landing view for the Copilot Workspace section. Phase 5 replaces this with the
 * three-region shell (conversation list · chat thread · context panel).
 */
@customElement("uai-copilot-workspace-dashboard")
export class UaiCopilotWorkspaceDashboardElement extends UmbLitElement {
    override render() {
        return html`
            <uui-box headline="Copilot Workspace">
                <p>
                    Your persisted, system-wide AI chat — conversations and projects that stay with you
                    across the backoffice.
                </p>
                <p><em>The full workspace experience is coming soon.</em></p>
            </uui-box>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-dashboard": UaiCopilotWorkspaceDashboardElement;
    }
}
