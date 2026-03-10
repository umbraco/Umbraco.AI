import { html, customElement, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type { UaiAgentType } from "../../types.js";
import type {
    UaiAgentCreateOptionsModalData,
    UaiAgentCreateOptionsModalValue,
} from "./agent-create-options-modal.token.js";

@customElement("uai-agent-create-options-modal")
export class UaiAgentCreateOptionsModalElement extends UmbModalBaseElement<
    UaiAgentCreateOptionsModalData,
    UaiAgentCreateOptionsModalValue
> {
    #onSelect(agentType: UaiAgentType) {
        this.value = { agentType };
        this.modalContext?.submit();
    }

    override render() {
        return html`
            <uui-dialog-layout headline=${this.data?.headline ?? "Select Agent Type"}>
                <uui-ref-list>
                    <uui-ref-node
                        name="Standard Agent"
                        detail="An agent with instructions, context, and tool permissions"
                        select-only
                        selectable
                        @selected=${() => this.#onSelect("standard")}
                        @open=${() => this.#onSelect("standard")}
                    >
                        <umb-icon slot="icon" name="icon-bot"></umb-icon>
                    </uui-ref-node>
                    <uui-ref-node
                        name="Orchestrated Agent"
                        detail="A workflow that composes multiple agents into a graph"
                        select-only
                        selectable
                        @selected=${() => this.#onSelect("orchestrated")}
                        @open=${() => this.#onSelect("orchestrated")}
                    >
                        <umb-icon slot="icon" name="icon-mindmap"></umb-icon>
                    </uui-ref-node>
                </uui-ref-list>
                <uui-button slot="actions" label="Cancel" @click=${() => this.modalContext?.reject()}>
                    Cancel
                </uui-button>
            </uui-dialog-layout>
        `;
    }

    static override styles = [
        css`
            uui-ref-list {
                display: block;
                min-width: 300px;
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-create-options-modal": UaiAgentCreateOptionsModalElement;
    }
}

export default UaiAgentCreateOptionsModalElement;
