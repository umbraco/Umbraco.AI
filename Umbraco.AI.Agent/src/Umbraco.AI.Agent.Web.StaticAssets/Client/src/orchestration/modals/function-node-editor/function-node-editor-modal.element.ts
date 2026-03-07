import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
    UaiOrchestrationFunctionNodeEditorModalData,
    UaiOrchestrationFunctionNodeEditorModalValue,
} from "./function-node-editor-modal.token.js";
import type { UaiOrchestrationNode } from "../../types.js";

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

    #onToolNameChange(event: Event) {
        const value = (event.target as HTMLInputElement).value;
        this._node = {
            ...this._node,
            config: { ...this._node.config, toolName: value || null },
        };
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
                    <umb-property-layout label="Label" description="Display name for this node">
                        <uui-input
                            slot="editor"
                            .value=${this._node.label}
                            @input=${this.#onLabelChange}
                            placeholder="Function"
                        ></uui-input>
                    </umb-property-layout>

                    <umb-property-layout
                        label="Tool Name"
                        description="Name of the registered AI tool to execute"
                    >
                        <uui-input
                            slot="editor"
                            .value=${this._node.config?.toolName ?? ""}
                            @input=${this.#onToolNameChange}
                            placeholder="e.g. umbraco-content-get"
                        ></uui-input>
                    </umb-property-layout>
                </div>
                <div slot="actions">
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
