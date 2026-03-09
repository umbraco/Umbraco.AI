import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
	UaiOrchestrationCommunicationBusNodeEditorModalData,
	UaiOrchestrationCommunicationBusNodeEditorModalValue,
} from "./communication-bus-node-editor-modal.token.js";
import type { UaiOrchestrationNode, UaiCommunicationBusNodeConfig } from "../../types.js";
import { isCommunicationBusNodeConfig } from "../../types.js";

/**
 * Modal for editing Communication Bus node configuration.
 * Configures max iterations and termination message for group chat / handoff patterns.
 */
@customElement("uai-orchestration-communication-bus-node-editor-modal")
export class UaiOrchestrationCommunicationBusNodeEditorModalElement extends UmbModalBaseElement<
	UaiOrchestrationCommunicationBusNodeEditorModalData,
	UaiOrchestrationCommunicationBusNodeEditorModalValue
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

	get #config(): UaiCommunicationBusNodeConfig {
		if (isCommunicationBusNodeConfig(this._node.config)) {
			return this._node.config;
		}
		return { $type: "communicationBus", maxIterations: 40 };
	}

	#onMaxIterationsChange(event: Event) {
		const value = parseInt((event.target as HTMLInputElement).value, 10);
		const config: UaiCommunicationBusNodeConfig = {
			...this.#config,
			maxIterations: isNaN(value) ? 40 : value,
		};
		this._node = { ...this._node, config };
	}

	#onTerminationMessageChange(event: Event) {
		const value = (event.target as HTMLInputElement).value;
		const config: UaiCommunicationBusNodeConfig = {
			...this.#config,
			terminationMessage: value || null,
		};
		this._node = { ...this._node, config };
	}
	#onSubmit() {
		this.value = { node: this._node };
		this.modalContext?.submit();
	}

	render() {
		if (!this._node) return html`<uui-loader></uui-loader>`;

		return html`
			<umb-body-layout headline="Communication Bus Node">
				<div id="main">
					<uui-box headline="General">
						<umb-property-layout label="Label" description="Display name for this node">
							<uui-input
								slot="editor"
								.value=${this._node.label}
								@input=${this.#onLabelChange}
								placeholder="Communication Bus"
							></uui-input>
						</umb-property-layout>

						<umb-property-layout
							label="Max Iterations"
							description="Maximum number of iterations before the bus terminates. Connected agents with 'Is Manager' enabled act as the group chat manager."
						>
							<uui-input
								slot="editor"
								type="number"
								min="1"
								.value=${String(this.#config.maxIterations ?? 40)}
								@input=${this.#onMaxIterationsChange}
							></uui-input>
						</umb-property-layout>

						<umb-property-layout
							label="Termination Message"
							description="Optional message that signals the bus should stop"
						>
							<uui-input
								slot="editor"
								.value=${this.#config.terminationMessage ?? ""}
								@input=${this.#onTerminationMessageChange}
								placeholder="TERMINATE"
							></uui-input>
						</umb-property-layout>
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
			uui-input {
				width: 100%;
			}
		`,
	];
}

export default UaiOrchestrationCommunicationBusNodeEditorModalElement;
