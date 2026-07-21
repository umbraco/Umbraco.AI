import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Center-region view for a project (its conversations + settings). Fleshed out in Phase 6; for now it
 * shows the bound id.
 */
@customElement("uai-copilot-workspace-project-view")
export class UaiCopilotWorkspaceProjectViewElement extends UmbLitElement {
    @property({ type: String })
    projectId?: string;

    override render() {
        return html`
            <div class="placeholder">
                <uui-box headline="Project">
                    <p>Project <code>${this.projectId ?? "(none)"}</code></p>
                    <p><em>Project details arrive in Phase 6.</em></p>
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

export default UaiCopilotWorkspaceProjectViewElement;
