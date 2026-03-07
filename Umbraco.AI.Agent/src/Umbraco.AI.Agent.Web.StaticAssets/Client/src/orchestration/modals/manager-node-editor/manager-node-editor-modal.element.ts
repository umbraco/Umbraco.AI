import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type {
    UaiOrchestrationManagerNodeEditorModalData,
    UaiOrchestrationManagerNodeEditorModalValue,
} from "./manager-node-editor-modal.token.js";
import type { UaiOrchestrationNode } from "../../types.js";

/**
 * Modal for editing Manager node configuration (Magentic pattern).
 * Allows setting coordination instructions and a profile.
 */
@customElement("uai-orchestration-manager-node-editor-modal")
export class UaiOrchestrationManagerNodeEditorModalElement extends UmbModalBaseElement<
    UaiOrchestrationManagerNodeEditorModalData,
    UaiOrchestrationManagerNodeEditorModalValue
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

    #onInstructionsChange(event: Event) {
        const value = (event.target as HTMLTextAreaElement).value;
        this._node = {
            ...this._node,
            config: { ...this._node.config, managerInstructions: value || null },
        };
    }

    #onProfileChange(event: UmbChangeEvent) {
        const picker = event.target as HTMLElement & { value: string | undefined };
        this._node = {
            ...this._node,
            config: { ...this._node.config, managerProfileId: picker.value ?? null },
        };
    }

    #onSubmit() {
        this.value = { node: this._node };
        this.modalContext?.submit();
    }

    render() {
        if (!this._node) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-body-layout headline="Manager Node">
                <div id="main">
                    <umb-property-layout label="Label" description="Display name for this node">
                        <uui-input
                            slot="editor"
                            .value=${this._node.label}
                            @input=${this.#onLabelChange}
                            placeholder="Manager"
                        ></uui-input>
                    </umb-property-layout>

                    <umb-property-layout
                        label="Instructions"
                        description="Coordination instructions for the manager agent"
                    >
                        <uui-textarea
                            slot="editor"
                            .value=${this._node.config?.managerInstructions ?? ""}
                            @input=${this.#onInstructionsChange}
                            placeholder="Tell the manager how to delegate work..."
                        ></uui-textarea>
                    </umb-property-layout>

                    <umb-property-layout
                        label="AI Profile"
                        description="Profile for the manager's LLM calls"
                    >
                        <uai-profile-picker
                            slot="editor"
                            .value=${this._node.config?.managerProfileId || undefined}
                            @change=${this.#onProfileChange}
                        ></uai-profile-picker>
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
            uui-input,
            uui-textarea {
                width: 100%;
            }

            uui-textarea {
                min-height: 120px;
            }
        `,
    ];
}

export default UaiOrchestrationManagerNodeEditorModalElement;
