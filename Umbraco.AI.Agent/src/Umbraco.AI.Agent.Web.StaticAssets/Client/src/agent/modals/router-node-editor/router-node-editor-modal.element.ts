import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
	UaiOrchestrationRouterNodeEditorModalData,
	UaiOrchestrationRouterNodeEditorModalValue,
} from "./router-node-editor-modal.token.js";
import type { UaiOrchestrationNode } from "../../types.js";

/**
 * Modal for editing Router node configuration.
 * The router itself is simple — routing conditions are defined on outgoing edges.
 */
@customElement("uai-orchestration-router-node-editor-modal")
export class UaiOrchestrationRouterNodeEditorModalElement extends UmbModalBaseElement<
	UaiOrchestrationRouterNodeEditorModalData,
	UaiOrchestrationRouterNodeEditorModalValue
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
	#onSubmit() {
		this.value = { node: this._node };
		this.modalContext?.submit();
	}

	render() {
		if (!this._node) return html`<uui-loader></uui-loader>`;

		return html`
			<umb-body-layout headline="Router Node">
				<div id="main">
					<uui-box headline="General">
						<umb-property-layout label="Label" description="Display name for this node">
							<uui-input
								slot="editor"
								.value=${this._node.label}
								@input=${this.#onLabelChange}
								placeholder="Router"
							></uui-input>
						</umb-property-layout>
					</uui-box>

					<uui-box headline="Routing">
						<p class="info-text">
							Routing conditions are configured on the outgoing edges of this node.
							Double-click an outgoing edge in the graph to set its condition, operator, and priority.
						</p>
					</uui-box>
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
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

			uui-input {
				width: 100%;
			}

			#main {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-4);
			}

			.info-text {
				color: var(--uui-color-text-alt);
				font-style: italic;
			}
		`,
	];
}

export default UaiOrchestrationRouterNodeEditorModalElement;
