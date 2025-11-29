import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UaiConnectionCapabilityRepository } from "../../../connection/repository/capability/connection-capability.repository.js";
import type { UaiConnectionItemModel } from "../../../connection/types.js";
import { UAI_CREATE_PROFILE_WORKSPACE_PATH_PATTERN } from "../../workspace/profile/paths.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../../../connection/workspace/connection-root/paths.js";

/**
 * Collection action element for creating a new profile.
 * Two-step flow: select capability, then select connection.
 */
@customElement("uai-profile-create-collection-action")
export class UaiProfileCreateCollectionActionElement extends UmbLitElement {
    #capabilityRepository = new UaiConnectionCapabilityRepository(this);
    #localize = new UmbLocalizationController(this);

    @state()
    private _availableCapabilities: string[] = [];

    @state()
    private _selectedCapability: string | null = null;

    @state()
    private _filteredConnections: UaiConnectionItemModel[] = [];

    @state()
    private _popoverOpen = false;

    @state()
    private _loading = true;

    override async connectedCallback() {
        super.connectedCallback();
        await this.#loadCapabilities();
    }

    async #loadCapabilities() {
        this._loading = true;
        const result = await this.#capabilityRepository.requestAvailableCapabilities();
        this._availableCapabilities = result.data ?? [];
        this._loading = false;
    }

    async #loadConnectionsForCapability(capability: string) {
        const result = await this.#capabilityRepository.requestConnectionsByCapability(capability);
        this._filteredConnections = result.data ?? [];
    }

    async #onSelectCapability(capability: string) {
        this._selectedCapability = capability;
        await this.#loadConnectionsForCapability(capability);
    }

    #getCapabilityLabel(capability: string): string {
        return this.#localize.term(`uaiCapabilities_${capability.toLowerCase()}`);
    }

    #onSelectConnection(connectionId: string) {
        if (!this._selectedCapability) return;

        this._popoverOpen = false;
        const path = UAI_CREATE_PROFILE_WORKSPACE_PATH_PATTERN.generateAbsolute({
            capability: this._selectedCapability,
            connectionId: connectionId,
        });
        history.pushState(null, "", path);

        // Reset state after navigation
        this._selectedCapability = null;
        this._filteredConnections = [];
    }

    #onBack() {
        this._selectedCapability = null;
        this._filteredConnections = [];
    }

    #onToggle() {
        this._popoverOpen = !this._popoverOpen;
        if (!this._popoverOpen) {
            // Reset when closing
            this._selectedCapability = null;
            this._filteredConnections = [];
        }
    }

    #renderContent() {
        if (this._loading) {
            return html`<div class="loading"><uui-loader-bar></uui-loader-bar></div>`;
        }

        // No capabilities available from configured connections
        if (this._availableCapabilities.length === 0) {
            return html`
                <div class="empty-state">
                    <p>No capabilities available from your connections.</p>
                    <p>Please <a href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH}>create a connection</a> first.</p>
                </div>
            `;
        }

        // Step 1: Select capability
        if (!this._selectedCapability) {
            return this._availableCapabilities.map(
                (cap) => html`
                    <uui-menu-item
                        label=${this.#getCapabilityLabel(cap)}
                        @click=${() => this.#onSelectCapability(cap)}
                    >
                        <umb-icon slot="icon" name="icon-wand"></umb-icon>
                    </uui-menu-item>
                `
            );
        }

        // Step 2: Select connection
        return html`
            <div class="step-header">
                <uui-button look="placeholder" compact @click=${this.#onBack}>
                    <uui-icon name="icon-arrow-left"></uui-icon>
                </uui-button>
                <span>Select Connection</span>
            </div>
            ${this._filteredConnections.length === 0
                ? html`<div class="empty">No connections support this capability</div>`
                : this._filteredConnections.map(
                      (conn) => html`
                          <uui-menu-item
                              label=${conn.name}
                              @click=${() => this.#onSelectConnection(conn.unique)}
                          >
                              <umb-icon slot="icon" name="icon-plug"></umb-icon>
                          </uui-menu-item>
                      `
                  )}
        `;
    }

    override render() {
        return html`
            <uui-button
                look="outline"
                popovertarget="uai-create-profile-popover"
                @click=${this.#onToggle}
            >
                Create
                <uui-symbol-expand .open=${this._popoverOpen}></uui-symbol-expand>
            </uui-button>
            <uui-popover-container id="uai-create-profile-popover" placement="bottom-start">
                <umb-popover-layout>
                    ${this.#renderContent()}
                </umb-popover-layout>
            </uui-popover-container>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                align-items: center;
            }

            .step-header {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
                font-weight: bold;
                border-bottom: 1px solid var(--uui-color-border);
            }

            .empty {
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
                color: var(--uui-color-text-alt);
                font-style: italic;
            }

            .empty-state {
                padding: var(--uui-size-space-4);
                text-align: center;
            }

            .empty-state p {
                margin: var(--uui-size-space-2) 0;
            }

            .empty-state a {
                color: var(--uui-color-interactive);
            }

            .loading {
                padding: var(--uui-size-space-4);
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-create-collection-action": UaiProfileCreateCollectionActionElement;
    }
}

export default UaiProfileCreateCollectionActionElement;
