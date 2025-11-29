import { html, customElement, state, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UaiConnectionCapabilityRepository } from "../../../connection/repository/capability/connection-capability.repository.js";
import type { UaiConnectionItemModel } from "../../../connection/types.js";
import type {
    UaiProfileCreateOptionsModalData,
    UaiProfileCreateOptionsModalValue,
} from "./profile-create-options-modal.token.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../../../connection/workspace/connection-root/paths.js";

@customElement("uai-profile-create-options-modal")
export class UaiProfileCreateOptionsModalElement extends UmbModalBaseElement<
    UaiProfileCreateOptionsModalData,
    UaiProfileCreateOptionsModalValue
> {
    #capabilityRepository = new UaiConnectionCapabilityRepository(this);
    #localize = new UmbLocalizationController(this);

    @state()
    private _availableCapabilities: string[] = [];

    @state()
    private _selectedCapability: string | null = null;

    @state()
    private _filteredConnections: UaiConnectionItemModel[] = [];

    @state()
    private _loading = true;

    override async firstUpdated() {
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
        this.value = {
            capability: this._selectedCapability,
            connectionId: connectionId,
        };
        this.modalContext?.submit();
    }

    #onBack() {
        this._selectedCapability = null;
        this._filteredConnections = [];
    }

    #getHeadline(): string {
        if (this._selectedCapability) {
            return "Select Connection";
        }
        return this.data?.headline ?? "Select Capability";
    }

    #renderContent() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        // No capabilities available
        if (this._availableCapabilities.length === 0) {
            return html`
                <div class="empty-state">
                    <p>No capabilities available from your connections.</p>
                    <p>Please <a href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH} @click=${() => this.modalContext?.reject()}>create a connection</a> first.</p>
                </div>
            `;
        }

        // Step 1: Select capability
        if (!this._selectedCapability) {
            return html`
                <uui-ref-list>
                    ${this._availableCapabilities.map(
                        (cap) => html`
                            <uui-ref-node
                                name=${this.#getCapabilityLabel(cap)}
                                select-only
                                selectable
                                @selected=${() => this.#onSelectCapability(cap)}
                                @open=${() => this.#onSelectCapability(cap)}
                            >
                                <umb-icon slot="icon" name="icon-wand"></umb-icon>
                            </uui-ref-node>
                        `
                    )}
                </uui-ref-list>
            `;
        }

        // Step 2: Select connection
        return html`
            <uui-ref-list>
                ${this._filteredConnections.map(
                    (conn) => html`
                        <uui-ref-node
                            name=${conn.name}
                            detail=${conn.providerId}
                            select-only
                            selectable
                            @selected=${() => this.#onSelectConnection(conn.unique)}
                            @open=${() => this.#onSelectConnection(conn.unique)}
                        >
                            <umb-icon slot="icon" name="icon-plug"></umb-icon>
                        </uui-ref-node>
                    `
                )}
            </uui-ref-list>
        `;
    }

    override render() {
        return html`
            <uui-dialog-layout headline=${this.#getHeadline()}>
                ${this.#renderContent()}
                <div slot="actions">
                    ${this._selectedCapability
                        ? html`<uui-button label="Back" @click=${this.#onBack}>Back</uui-button>`
                        : nothing}
                    <uui-button label="Cancel" @click=${() => this.modalContext?.reject()}>Cancel</uui-button>
                </div>
            </uui-dialog-layout>
        `;
    }

    static override styles = [
        css`
            uui-loader {
                display: block;
                margin: var(--uui-size-space-4) auto;
            }

            .empty-state {
                color: var(--uui-color-text-alt);
                text-align: center;
                padding: var(--uui-size-space-4);
            }

            .empty-state a {
                color: var(--uui-color-interactive);
            }

            uui-ref-list {
                display: block;
                min-width: 300px;
            }

            [slot="actions"] {
                display: flex;
                gap: var(--uui-size-space-2);
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-create-options-modal": UaiProfileCreateOptionsModalElement;
    }
}

export default UaiProfileCreateOptionsModalElement;
