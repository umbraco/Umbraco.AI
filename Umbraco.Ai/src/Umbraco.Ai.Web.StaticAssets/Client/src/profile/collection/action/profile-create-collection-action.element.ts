import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UaiConnectionCapabilityRepository } from "../../../connection/repository/capability/connection-capability.repository.js";
import { UAI_CREATE_PROFILE_WORKSPACE_PATH_PATTERN } from "../../workspace/profile/paths.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../../../connection/workspace/connection-root/paths.js";

/**
 * Collection action element for creating a new profile.
 * Single-step flow: select capability.
 */
@customElement("uai-profile-create-collection-action")
export class UaiProfileCreateCollectionActionElement extends UmbLitElement {
    #capabilityRepository = new UaiConnectionCapabilityRepository(this);
    #localize = new UmbLocalizationController(this);

    @state()
    private _availableCapabilities: string[] = [];

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

    #getCapabilityLabel(capability: string): string {
        return this.#localize.term(`uaiCapabilities_${capability.toLowerCase()}`);
    }

    #onSelectCapability(capability: string) {
        this._popoverOpen = false;
        const path = UAI_CREATE_PROFILE_WORKSPACE_PATH_PATTERN.generateAbsolute({
            capability,
        });
        history.pushState(null, "", path);
    }

    #onToggle() {
        this._popoverOpen = !this._popoverOpen;
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
