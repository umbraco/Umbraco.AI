import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
	UaiOrchestrationRouterEdgeConditionEditorModalData,
	UaiOrchestrationRouterEdgeConditionEditorModalValue,
} from "./router-edge-condition-editor-modal.token.js";
import type { UaiOrchestrationRouteCondition } from "../../types.js";

const OPERATORS = [
	{ name: "Equals", value: "Equals" },
	{ name: "Contains", value: "Contains" },
	{ name: "Starts With", value: "StartsWith" },
	{ name: "Matches (Regex)", value: "Matches" },
];

/**
 * Modal for editing a router edge condition.
 * Allows configuring the routing condition (label, field, operator, value),
 * priority, default flag, and approval requirement.
 */
@customElement("uai-orchestration-router-edge-condition-editor-modal")
export class UaiOrchestrationRouterEdgeConditionEditorModalElement extends UmbModalBaseElement<
	UaiOrchestrationRouterEdgeConditionEditorModalData,
	UaiOrchestrationRouterEdgeConditionEditorModalValue
> {
	@state()
	private _condition: UaiOrchestrationRouteCondition = {
		label: "",
		field: "",
		operator: "Equals",
		value: "",
	};

	@state()
	private _isDefault = false;

	@state()
	private _priority: number | null = null;

	@state()
	private _requiresApproval = false;

	connectedCallback() {
		super.connectedCallback();
		if (this.data) {
			this._condition = this.data.condition
				? structuredClone(this.data.condition)
				: { label: "", field: "", operator: "Equals", value: "" };
			this._isDefault = this.data.isDefault;
			this._priority = this.data.priority;
			this._requiresApproval = this.data.requiresApproval;
		}
	}

	#onLabelChange(event: Event) {
		this._condition = { ...this._condition, label: (event.target as HTMLInputElement).value };
	}

	#onFieldChange(event: Event) {
		this._condition = { ...this._condition, field: (event.target as HTMLInputElement).value };
	}

	#onOperatorChange(event: Event) {
		this._condition = { ...this._condition, operator: (event.target as HTMLSelectElement).value };
	}

	#onValueChange(event: Event) {
		this._condition = { ...this._condition, value: (event.target as HTMLInputElement).value };
	}

	#onIsDefaultChange(event: Event) {
		this._isDefault = (event.target as HTMLInputElement).checked;
	}

	#onPriorityChange(event: Event) {
		const value = parseInt((event.target as HTMLInputElement).value, 10);
		this._priority = isNaN(value) ? null : value;
	}

	#onRequiresApprovalChange(event: Event) {
		this._requiresApproval = (event.target as HTMLInputElement).checked;
	}

	#onSubmit() {
		const condition = this._isDefault
			? null
			: (this._condition.label || this._condition.field)
				? this._condition
				: null;
		this.value = {
			condition,
			isDefault: this._isDefault,
			priority: this._priority,
			requiresApproval: this._requiresApproval,
		};
		this.modalContext?.submit();
	}

	render() {
		return html`
			<umb-body-layout headline="Route Condition">
				<div id="main">
					<uui-box headline="General">
						<umb-property-layout
							label="Default Route"
							description="When enabled, this route is the fallback when no other conditions match"
						>
							<uui-toggle
								slot="editor"
								.checked=${this._isDefault}
								@change=${this.#onIsDefaultChange}
							></uui-toggle>
						</umb-property-layout>

						<umb-property-layout
							label="Priority"
							description="Evaluation order (lower = evaluated first)"
						>
							<uui-input
								slot="editor"
								type="number"
								min="0"
								.value=${this._priority != null ? String(this._priority) : ""}
								@input=${this.#onPriorityChange}
								placeholder="0"
							></uui-input>
						</umb-property-layout>

						<umb-property-layout
							label="Requires Approval"
							description="Pause for human approval before traversing this route"
						>
							<uui-toggle
								slot="editor"
								.checked=${this._requiresApproval}
								@change=${this.#onRequiresApprovalChange}
							></uui-toggle>
						</umb-property-layout>
					</uui-box>

					${!this._isDefault
						? html`
							<uui-box headline="Condition">
								<umb-property-layout
									label="Label"
									description="Display label shown on the edge"
								>
									<uui-input
										slot="editor"
										.value=${this._condition.label}
										@input=${this.#onLabelChange}
										placeholder="e.g. Billing query"
									></uui-input>
								</umb-property-layout>

								<umb-property-layout
									label="Field"
									description="Output field to evaluate from the previous node"
								>
									<uui-input
										slot="editor"
										.value=${this._condition.field}
										@input=${this.#onFieldChange}
										placeholder="e.g. category"
									></uui-input>
								</umb-property-layout>

								<umb-property-layout
									label="Operator"
									description="Comparison operator"
								>
									<uui-select
										slot="editor"
										.options=${OPERATORS.map((o) => ({
											...o,
											selected: o.value === this._condition.operator,
										}))}
										@change=${this.#onOperatorChange}
									></uui-select>
								</umb-property-layout>

								<umb-property-layout
									label="Value"
									description="Expected value to match against"
								>
									<uui-input
										slot="editor"
										.value=${this._condition.value}
										@input=${this.#onValueChange}
										placeholder="e.g. billing"
									></uui-input>
								</umb-property-layout>
							</uui-box>
						`
						: ""}
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
		`,
	];
}

export default UaiOrchestrationRouterEdgeConditionEditorModalElement;
