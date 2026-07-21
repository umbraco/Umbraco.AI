import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbSectionSidebarAppElement } from "@umbraco-cms/backoffice/section";

/**
 * The conversation list, rendered as a section-sidebar app so it uses the CMS's standard section
 * sidebar chrome (placement, width, global collapse) and stays consistent with the rest of the
 * backoffice. This placeholder establishes the layout (New Chat + search header, scrollable list);
 * the data-bound list — grouped by project then date bucket, with pinned + per-item pin/archive/
 * rename/delete, wired over the generated client and navigating the main-area router — is the next slice.
 */
@customElement("uai-copilot-workspace-conversation-list")
export class UaiCopilotWorkspaceConversationListElement
    extends UmbLitElement
    implements UmbSectionSidebarAppElement
{
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
                min-height: 0;
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
