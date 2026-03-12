import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN } from "../../workspace/agent/paths.js";
import { AgentsService } from "../../../api/sdk.gen.js";
import type { UaiAgentType } from "../../types.js";

/**
 * Collection action button for creating a new agent.
 * Shows a dropdown to select agent type (Standard / Orchestrated) when workflows are available.
 * Falls back to a simple create button for Standard agents when no workflows are registered.
 */
@customElement("uai-agent-create-collection-action")
export class UaiAgentCreateCollectionActionElement extends UmbLitElement {
    @state()
    private _popoverOpen = false;

    @state()
    private _hasWorkflows = false;

    override async connectedCallback() {
        super.connectedCallback();
        const { data } = await tryExecute(this, AgentsService.getAgentWorkflows());
        this._hasWorkflows = Array.isArray(data) && data.length > 0;
    }

    #navigate(agentType: UaiAgentType) {
        const path = UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ agentType });
        history.pushState(null, "", path);
        this._popoverOpen = false;
    }

    #onTogglePopover() {
        this._popoverOpen = !this._popoverOpen;
    }

    render() {
        if (!this._hasWorkflows) {
            return html`
                <uui-button
                    color="default"
                    look="outline"
                    label="Create"
                    @click=${() => this.#navigate("standard")}
                >
                    Create
                </uui-button>
            `;
        }

        return html`
            <uui-button
                popovertarget="create-agent-popover"
                color="default"
                look="outline"
                label="Create"
                @click=${this.#onTogglePopover}
            >
                Create
                <uui-symbol-expand slot="extra" .open=${this._popoverOpen}></uui-symbol-expand>
            </uui-button>
            <uui-popover-container id="create-agent-popover" placement="bottom-end">
                <umb-popover-layout>
                    <uui-menu-item label="Standard Agent" @click=${() => this.#navigate("standard")}>
                        <uui-icon slot="icon" name="icon-bot"></uui-icon>
                    </uui-menu-item>
                    <uui-menu-item label="Orchestrated Agent" @click=${() => this.#navigate("orchestrated")}>
                        <uui-icon slot="icon" name="icon-mindmap"></uui-icon>
                    </uui-menu-item>
                </umb-popover-layout>
            </uui-popover-container>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                height: 100%;
                display: flex;
                align-items: center;
            }
        `,
    ];
}

export default UaiAgentCreateCollectionActionElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-create-collection-action": UaiAgentCreateCollectionActionElement;
    }
}
