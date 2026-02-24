import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { TestGraderModel } from "../../../../api/types.gen.js";
import type { UaiTestDetailModel, UaiTestGraderConfig } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import type { UaiGraderConfigBuilderElement } from "../../../components/grader-config-builder/grader-config-builder.element.js";

@customElement("umbraco-ai-test-scoring-workspace-view")
export class UmbracoAITestScoringWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
	#workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;

	@state()
	private _model?: UaiTestDetailModel;

	constructor() {
		super();
        this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (test) => {
            if (test) {
                this.#workspaceContext = test;
                this.observe(test.model, (model) => {
                    this._model = model;
                });
            }
        });
	}

	protected override firstUpdated(_changedProperties: any) {
		super.firstUpdated(_changedProperties);
		const graderBuilder = this.shadowRoot?.querySelector<UaiGraderConfigBuilderElement>("uai-grader-config-builder");
		if (graderBuilder) this.addFormControlElement(graderBuilder as any);
	}

	#onGradersChange(event: Event) {
		const target = event.target as any;
		const graderConfigs = target.graders as UaiTestGraderConfig[];

		// Convert UaiTestGraderConfig[] to TestGraderModel[]
		const graderModels: TestGraderModel[] = graderConfigs.map(config => ({
			id: config.id,
			graderTypeId: config.graderTypeId,
			name: config.name,
			description: config.description || null,
			config: config.config ?? undefined,
			negate: config.negate,
			severity: config.severity,
			weight: config.weight,
		}));

		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ graders: graderModels }, "graders"),
		);
	}

	#getGraderConfigs(): UaiTestGraderConfig[] {
		if (!this._model) return [];

		// Convert TestGraderModel[] to UaiTestGraderConfig[]
		return this._model.graders.map(grader => {
			return {
				id: grader.id,
				graderTypeId: grader.graderTypeId,
				name: grader.name,
				description: grader.description || undefined,
				config: grader.config as Record<string, unknown> || undefined,
				negate: grader.negate,
				severity: grader.severity as "Info" | "Warning" | "Error",
				weight: grader.weight,
			};
		});
	}

	render() {
		if (!this._model) return html`<uui-loader></uui-loader>`;

		return html`
			<uui-box headline="Graders">
				<umb-property-layout
					label="Graders"
					description="Configure graders to validate test outputs"
					mandatory>
					<uai-grader-config-builder
						slot="editor"
						.graders=${this.#getGraderConfigs()}
						required
						.requiredMessage=${this.localize.term("uaiValidation_gradersRequired")}
						@change=${this.#onGradersChange}
					></uai-grader-config-builder>
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

			uui-box:not(:first-child) {
				margin-top: var(--uui-size-layout-1);
			}

            uai-model-editor {
                margin-top: var(--uui-size-layout-1);
            }

			uui-loader {
				display: block;
				margin: auto;
				position: absolute;
				top: 50%;
				left: 50%;
				transform: translate(-50%, -50%);
			}

			.target-input {
				display: flex;
				gap: var(--uui-size-space-3);
				align-items: center;
			}

			.target-input uui-input {
				flex: 1;
			}

			uui-select,
			uui-input,
			uui-textarea {
				width: 100%;
			}
		`,
	];
}

export default UmbracoAITestScoringWorkspaceViewElement;

declare global {
	interface HTMLElementTagNameMap {
		"umbraco-ai-test-scoring-workspace-view": UmbracoAITestScoringWorkspaceViewElement;
	}
}
