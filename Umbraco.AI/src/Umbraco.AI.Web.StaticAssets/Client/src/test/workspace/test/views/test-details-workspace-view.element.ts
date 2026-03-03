import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { UaiTestDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";

@customElement("umbraco-ai-test-details-workspace-view")
export class UmbracoAITestDetailsWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
	#workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;

	@state()
	private _model?: UaiTestDetailModel;

	constructor() {
		super();

		this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;
			this.#workspaceContext = context;
			this.observe(context.model, (model) => {
				this._model = model;
			});
		});
	}

	#onDescriptionChange(event: Event) {
		const target = event.target as HTMLTextAreaElement;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ description: target.value || null }, "description"),
		);
	}

	#onTargetChange(event: Event) {
		event.stopPropagation();
		const target = event.target as HTMLElement & { value?: string };
		const testTargetId = target.value || "";

		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ testTargetId }, "testTargetId"),
		);
	}

	render() {
		if (!this._model) return html`<uui-loader></uui-loader>`;

		return html`
			<uui-box headline="General">
				<umb-property-layout label="Description" description="Optional description of this test">
					<uui-textarea
						slot="editor"
						.value=${this._model.description || ""}
						@input=${this.#onDescriptionChange}
						placeholder="Enter test description (optional)"
					></uui-textarea>
				</umb-property-layout>

				<umb-property-layout label="Target" description="The entity to test" mandatory>
					<uai-test-feature-entity-picker
						slot="editor"
						.testFeatureId=${this._model.testFeatureId}
						.value=${this._model.testTargetId}
						@change=${this.#onTargetChange}
					></uai-test-feature-entity-picker>
				</umb-property-layout>
			</uui-box>
		`;
	}

	static styles = [
		UmbTextStyles,
		css`
			:host {
				display: block;
				padding: var(--uui-size-layout-1);
			}

			uui-box {
				--uui-box-default-padding: 0 var(--uui-size-space-5);
			}

			uui-loader {
				display: block;
				margin: auto;
				position: absolute;
				top: 50%;
				left: 50%;
				transform: translate(-50%, -50%);
			}

			uui-select,
			uui-input,
			uui-textarea {
				width: 100%;
			}
		`,
	];
}

export default UmbracoAITestDetailsWorkspaceViewElement;

declare global {
	interface HTMLElementTagNameMap {
		"umbraco-ai-test-details-workspace-view": UmbracoAITestDetailsWorkspaceViewElement;
	}
}
