import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type {
    UaiOrchestrationFunctionNodeEditorModalData,
    UaiOrchestrationFunctionNodeEditorModalValue,
} from "./function-node-editor-modal.token.js";
import type { UaiOrchestrationNode, UaiFunctionNodeConfig } from "../../types.js";
import "@umbraco-ai/core";

/**
 * Modal for editing Function node configuration.
 * Allows selecting a registered AI tool to execute.
 */
@customElement("uai-orchestration-function-node-editor-modal")
export class UaiOrchestrationFunctionNodeEditorModalElement extends UmbModalBaseElement<
    UaiOrchestrationFunctionNodeEditorModalData,
    UaiOrchestrationFunctionNodeEditorModalValue
> {
    @state()
    private _node!: UaiOrchestrationNode;

    connectedCallback() {
        super.connectedCallback();
        if (this.data?.node) {
            this._node = structuredClone(this.data.node);
        }
    }

    #onLabelChange(event: Event) {
        this._node = { ...this._node, label: (event.target as HTMLInputElement).value };
    }

    get #config(): UaiFunctionNodeConfig {
        return this._node.config as UaiFunctionNodeConfig;
    }

    #onToolChange(event: UmbChangeEvent) {
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        const toolIds = picker.value ?? [];
        const config: UaiFunctionNodeConfig = { ...this.#config, toolIds, toolName: toolIds[0] ?? null };
        this._node = { ...this._node, config };
    }

    #onDelete() {
        this.value = { node: this._node, deleted: true };
        this.modalContext?.submit();
    }

    #onSubmit() {
        this.value = { node: this._node };
        this.modalContext?.submit();
    }

    render() {
        if (!this._node) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-body-layout headline="Function Node">
                <div id="main">
                    <uui-box>
                        <umb-property-layout label="Label" description="Display name for this node">
                            <uui-input
                                slot="editor"
                                .value=${this._node.label}
                                @input=${this.#onLabelChange}
                                placeholder="Function"
                            ></uui-input>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Tool"
                            description="Select the registered AI tool to execute"
                        >
                            <uai-tool-picker
                                slot="editor"
                                .value=${this.#config.toolIds ?? (this.#config.toolName ? [this.#config.toolName] : undefined)}
                                @change=${this.#onToolChange}
                            ></uai-tool-picker>
                        </umb-property-layout>
                    </uui-box>
                </div>
                <div slot="actions">
                    <uui-button
                        color="danger"
                        look="primary"
                        @click=${this.#onDelete}
                        label="Delete"
                    ></uui-button>
                    <uui-button @click=${this._rejectModal} label="Cancel"></uui-button>
                    <uui-button
                        look="primary"
                        color="positive"
                        @click=${this.#onSubmit}
                        label="Save"
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            uui-input {
                width: 100%;
            }
        `,
    ];
}

export default UaiOrchestrationFunctionNodeEditorModalElement;
