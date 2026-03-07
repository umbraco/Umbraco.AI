import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type {
    UaiOrchestrationAgentNodeEditorModalData,
    UaiOrchestrationAgentNodeEditorModalValue,
} from "./agent-node-editor-modal.token.js";
import type { UaiOrchestrationNode } from "../../types.js";

/**
 * Modal for editing Agent node configuration.
 * Allows selecting an agent to reference and setting the node label.
 */
@customElement("uai-orchestration-agent-node-editor-modal")
export class UaiOrchestrationAgentNodeEditorModalElement extends UmbModalBaseElement<
    UaiOrchestrationAgentNodeEditorModalData,
    UaiOrchestrationAgentNodeEditorModalValue
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

    #onAgentIdChange(event: UmbChangeEvent) {
        const picker = event.target as HTMLElement & { value: string | undefined };
        this._node = {
            ...this._node,
            config: { ...this._node.config, agentId: picker.value ?? null },
        };
    }

    #onSubmit() {
        this.value = { node: this._node };
        this.modalContext?.submit();
    }

    render() {
        if (!this._node) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-body-layout headline="Agent Node">
                <div id="main">
                    <umb-property-layout label="Label" description="Display name for this node">
                        <uui-input
                            slot="editor"
                            .value=${this._node.label}
                            @input=${this.#onLabelChange}
                            placeholder="Agent"
                        ></uui-input>
                    </umb-property-layout>

                    <umb-property-layout
                        label="Agent"
                        description="Select the AI agent to execute at this step"
                    >
                        <uai-agent-picker
                            slot="editor"
                            .value=${this._node.config?.agentId || undefined}
                            @change=${this.#onAgentIdChange}
                        ></uai-agent-picker>
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

export default UaiOrchestrationAgentNodeEditorModalElement;
