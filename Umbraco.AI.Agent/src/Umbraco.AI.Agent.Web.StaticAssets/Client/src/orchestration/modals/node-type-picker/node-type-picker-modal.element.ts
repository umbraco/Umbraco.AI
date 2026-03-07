import { css, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
    UaiOrchestrationNodeTypePickerModalData,
    UaiOrchestrationNodeTypePickerModalValue,
} from "./node-type-picker-modal.token.js";

@customElement("uai-orchestration-node-type-picker-modal")
export class UaiOrchestrationNodeTypePickerModalElement extends UmbModalBaseElement<
    UaiOrchestrationNodeTypePickerModalData,
    UaiOrchestrationNodeTypePickerModalValue
> {
    #onTypeSelected(type: string) {
        this.value = { selectedType: type };
        this.modalContext?.submit();
    }

    render() {
        return html`
            <umb-body-layout headline="Add Node">
                <div id="main">
                    <div class="node-type-list">
                        ${this.data?.nodeTypes.map(
                            (def) => html`
                                <button
                                    class="node-type-item"
                                    @click=${() => this.#onTypeSelected(def.type)}
                                >
                                    <span class="node-icon" style="background-color: ${def.color}"
                                        >${def.icon}</span
                                    >
                                    <div class="node-info">
                                        <strong>${def.label}</strong>
                                        <span class="node-description">${def.description}</span>
                                    </div>
                                </button>
                            `,
                        )}
                    </div>
                </div>
                <div slot="actions">
                    <uui-button @click=${this._rejectModal} label="Cancel"></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            .node-type-list {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
            }

            .node-type-item {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-4);
                padding: var(--uui-size-space-4);
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface);
                cursor: pointer;
                text-align: left;
                transition: border-color 0.15s;
            }

            .node-type-item:hover {
                border-color: var(--uui-color-interactive-emphasis);
            }

            .node-icon {
                display: flex;
                align-items: center;
                justify-content: center;
                width: 40px;
                height: 40px;
                border-radius: 50%;
                color: white;
                font-size: 18px;
                flex-shrink: 0;
            }

            .node-info {
                display: flex;
                flex-direction: column;
                gap: 2px;
            }

            .node-description {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
            }
        `,
    ];
}

export default UaiOrchestrationNodeTypePickerModalElement;
