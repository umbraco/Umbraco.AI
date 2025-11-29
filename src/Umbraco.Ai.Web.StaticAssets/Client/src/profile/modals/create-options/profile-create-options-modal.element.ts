import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UaiConnectionCapabilityRepository } from "../../../connection/repository/capability/connection-capability.repository.js";
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

    #onSelectCapability(capability: string) {
        this.value = { capability };
        this.modalContext?.submit();
    }

    #getCapabilityLabel(capability: string): string {
        return this.#localize.term(`uaiCapabilities_${capability.toLowerCase()}`);
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

    override render() {
        return html`
            <uui-dialog-layout headline=${this.data?.headline ?? "Select Capability"}>
                ${this.#renderContent()}
                <div slot="actions">
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
