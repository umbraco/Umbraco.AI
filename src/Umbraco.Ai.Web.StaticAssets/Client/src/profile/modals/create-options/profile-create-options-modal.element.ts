import { html, customElement, state, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UaiConnectionCollectionRepository } from "../../../connection/repository/collection/connection-collection.repository.js";
import { UaiProviderCollectionRepository } from "../../../provider/repository/collection/provider-collection.repository.js";
import type { UaiConnectionItemModel } from "../../../connection/types.js";
import type { UaiProviderItemModel } from "../../../provider/types.js";
import type {
    UaiProfileCreateOptionsModalData,
    UaiProfileCreateOptionsModalValue,
} from "./profile-create-options-modal.token.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../../../connection/workspace/connection-root/paths.js";

interface CapabilityOption {
    id: string;
    label: string;
    icon: string;
}

@customElement("uai-profile-create-options-modal")
export class UaiProfileCreateOptionsModalElement extends UmbModalBaseElement<
    UaiProfileCreateOptionsModalData,
    UaiProfileCreateOptionsModalValue
> {
    #connectionRepository = new UaiConnectionCollectionRepository(this);
    #providerRepository = new UaiProviderCollectionRepository(this);

    @state()
    private _connections: UaiConnectionItemModel[] = [];

    @state()
    private _providers: UaiProviderItemModel[] = [];

    @state()
    private _availableCapabilities: CapabilityOption[] = [];

    @state()
    private _selectedCapability: string | null = null;

    @state()
    private _filteredConnections: UaiConnectionItemModel[] = [];

    @state()
    private _loading = true;

    private readonly _capabilityOptions: CapabilityOption[] = [
        { id: "chat", label: "Chat", icon: "icon-chat" },
        { id: "embedding", label: "Embedding", icon: "icon-nodes" },
    ];

    override async firstUpdated() {
        await this.#loadData();
    }

    async #loadData() {
        this._loading = true;

        const [connectionsResult, providersResult] = await Promise.all([
            this.#connectionRepository.requestCollection({ skip: 0, take: 100 }),
            this.#providerRepository.requestCollection({ skip: 0, take: 100 }),
        ]);

        this._connections = connectionsResult.data?.items ?? [];
        this._providers = providersResult.data?.items ?? [];

        this.#updateAvailableCapabilities();
        this._loading = false;
    }

    #updateAvailableCapabilities() {
        const availableCapabilities = new Set<string>();

        for (const connection of this._connections) {
            const provider = this._providers.find((p) => p.providerId === connection.providerId);
            if (provider?.capabilities) {
                provider.capabilities.forEach((cap) => availableCapabilities.add(cap));
            }
        }

        this._availableCapabilities = this._capabilityOptions.filter((opt) =>
            availableCapabilities.has(opt.id)
        );
    }

    #filterConnectionsByCapability(capability: string) {
        this._filteredConnections = this._connections.filter((conn) => {
            const provider = this._providers.find((p) => p.providerId === conn.providerId);
            return provider?.capabilities?.includes(capability);
        });
    }

    #onSelectCapability(capability: string) {
        this._selectedCapability = capability;
        this.#filterConnectionsByCapability(capability);
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

        // No connections configured at all
        if (this._connections.length === 0) {
            return html`
                <div class="empty-state">
                    <p>No connections configured.</p>
                    <p>Please <a href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH} @click=${() => this.modalContext?.reject()}>create a connection</a> first.</p>
                </div>
            `;
        }

        // No capabilities available
        if (this._availableCapabilities.length === 0) {
            return html`
                <div class="empty-state">
                    <p>No capabilities available from your connections.</p>
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
                                name=${cap.label}
                                select-only
                                selectable
                                @selected=${() => this.#onSelectCapability(cap.id)}
                                @open=${() => this.#onSelectCapability(cap.id)}
                            >
                                <umb-icon slot="icon" name=${cap.icon}></umb-icon>
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
