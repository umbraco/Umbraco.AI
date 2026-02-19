import { css, html, customElement, state, when, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { TestGraderModel, TestGraderInfoModel } from "../../../../api/types.gen.js";
import type { UaiTestDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import { AITestRepository } from "../../../repository/test.repository.js";

@customElement("umbraco-ai-test-details-workspace-view")
export class UmbracoAITestDetailsWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
	#workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;
	#notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;
	#repository!: AITestRepository;

	@state()
	private _model?: UaiTestDetailModel;

	@state()
	private _testGraders: TestGraderInfoModel[] = [];

	constructor() {
		super();

		this.#repository = new AITestRepository(this);

		this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;
			this.#workspaceContext = context;
			this.observe(context.model, (model) => {
				this._model = model;
			});
		});

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});
	}

	override async connectedCallback() {
		super.connectedCallback();
		await this.#loadMetadata();
	}

	async #loadMetadata() {
		try {
			this._testGraders = await this.#repository.getAllTestGraders();
		} catch (error) {
			console.error("Failed to load metadata:", error);
			this.#notificationContext?.peek("danger", {
				data: { message: "Failed to load test metadata" },
			});
		}
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

	#onTestCaseJsonChange(event: Event) {
		const target = event.target as HTMLTextAreaElement;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ testCaseJson: target.value }, "testCaseJson"),
		);
	}

	#onRunCountChange(event: Event) {
		const target = event.target as HTMLInputElement;
		const runCount = parseInt(target.value) || 1;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ runCount }, "runCount"),
		);
	}

	#onTagsChange(event: CustomEvent) {
		const target = event.target as any;
		const tags = target.items as string[];
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ tags }, "tags"),
		);
	}

	#onAddGrader() {
		if (!this._model) return;
		const newGrader: TestGraderModel = {
			id: crypto.randomUUID(),
			graderTypeId: this._testGraders[0]?.id || "",
			name: "New Grader",
			description: null,
			configJson: "{}",
			negate: false,
			severity: "Error",
			weight: 1.0,
		};
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ graders: [...this._model.graders, newGrader] }, "graders"),
		);
	}

	#onRemoveGrader(index: number) {
		if (!this._model) return;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>(
				{ graders: this._model.graders.filter((_, i) => i !== index) },
				"graders",
			),
		);
	}

	#onGraderChange(index: number, field: keyof TestGraderModel, value: any) {
		if (!this._model) return;
		const updated = [...this._model.graders];
		(updated[index] as any)[field] = value;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ graders: updated }, "graders"),
		);
	}

	render() {
		if (!this._model) return html`<uui-loader></uui-loader>`;

		return html`
			<uui-box headline="Test Details">
				<umb-property-layout label="Description" description="Optional description of this test">
					<uui-textarea
						slot="editor"
						.value=${this._model.description || ""}
						@input=${this.#onDescriptionChange}
						placeholder="Enter test description (optional)"
					></uui-textarea>
				</umb-property-layout>

				<umb-property-layout label="Target Entity" description="The entity to test" mandatory>
					<uai-test-feature-entity-picker
						slot="editor"
						.testFeatureId=${this._model.testFeatureId}
						.value=${this._model.testTargetId}
						@change=${this.#onTargetChange}
					></uai-test-feature-entity-picker>
				</umb-property-layout>

				<umb-property-layout label="Test Case (JSON)" description="Test case data in JSON format" mandatory>
					<uui-textarea
						slot="editor"
						.value=${this._model.testCaseJson}
						@input=${this.#onTestCaseJsonChange}
						placeholder='{"key": "value"}'
						rows="10"
					></uui-textarea>
				</umb-property-layout>

				<umb-property-layout label="Run Count" description="Number of times to run this test (for pass@k calculation)">
					<uui-input
						slot="editor"
						type="number"
						.value=${this._model.runCount.toString()}
						@input=${this.#onRunCountChange}
						min="1"
						max="100"
					></uui-input>
				</umb-property-layout>
			</uui-box>

			<uui-box headline="Tags">
				<umb-property-layout label="Tags" description="Tags to categorize this test">
					<uai-tags-input
						slot="editor"
						.items=${this._model.tags}
						@change=${this.#onTagsChange}
						placeholder="Add tag"
					></uai-tags-input>
				</umb-property-layout>
			</uui-box>

			<uui-box>
				<div slot="headline" class="box-headline">
					<span>Graders</span>
					<uui-button @click=${this.#onAddGrader} label="Add grader" look="primary" compact>
						<uui-icon name="icon-add"></uui-icon>
						Add Grader
					</uui-button>
				</div>

				${repeat(
					this._model.graders,
					(grader) => grader.id,
					(grader, index) => this.#renderGrader(grader, index),
				)}
				${when(
					this._model.graders.length === 0,
					() => html`<div class="empty-state">No graders configured. Click "Add Grader" to get started.</div>`,
				)}
			</uui-box>
		`;
	}

	#renderGrader(grader: TestGraderModel, index: number) {
		return html`
			<uui-box class="grader-box">
				<div slot="headline" class="grader-headline">
					<strong>Grader ${index + 1}</strong>
					<uui-button
						@click=${() => this.#onRemoveGrader(index)}
						label="Remove grader"
						color="danger"
						look="secondary"
						compact
					>
						<uui-icon name="icon-delete"></uui-icon>
						Remove
					</uui-button>
				</div>

				<umb-property-layout label="Grader Type">
					<uui-select
						slot="editor"
						.value=${grader.graderTypeId}
						@change=${(e: Event) =>
							this.#onGraderChange(index, "graderTypeId", (e.target as HTMLSelectElement).value)}
					>
						${this._testGraders.map(
							(g) => html`<uui-select-option .value=${g.id}>${g.name}</uui-select-option>`,
						)}
					</uui-select>
				</umb-property-layout>

				<umb-property-layout label="Name">
					<uui-input
						slot="editor"
						.value=${grader.name}
						@input=${(e: UUIInputEvent) =>
							this.#onGraderChange(index, "name", (e.target as UUIInputElement).value)}
					></uui-input>
				</umb-property-layout>

				<umb-property-layout label="Config (JSON)">
					<uui-textarea
						slot="editor"
						.value=${grader.configJson || "{}"}
						@input=${(e: Event) =>
							this.#onGraderChange(index, "configJson", (e.target as HTMLTextAreaElement).value)}
						rows="3"
					></uui-textarea>
				</umb-property-layout>

				<div class="grader-options">
					<umb-property-layout label="Severity">
						<uui-select
							slot="editor"
							.value=${grader.severity}
							@change=${(e: Event) =>
								this.#onGraderChange(index, "severity", (e.target as HTMLSelectElement).value)}
						>
							<uui-select-option value="Info">Info</uui-select-option>
							<uui-select-option value="Warning">Warning</uui-select-option>
							<uui-select-option value="Error">Error</uui-select-option>
						</uui-select>
					</umb-property-layout>

					<umb-property-layout label="Weight">
						<uui-input
							slot="editor"
							type="number"
							.value=${grader.weight.toString()}
							@input=${(e: UUIInputEvent) =>
								this.#onGraderChange(index, "weight", parseFloat((e.target as UUIInputElement).value.toString()) || 1.0)}
							min="0"
							max="1"
							step="0.1"
						></uui-input>
					</umb-property-layout>

					<umb-property-layout label="Negate">
						<uui-toggle
							slot="editor"
							label="Negate Result"
							.checked=${grader.negate}
							@change=${(e: Event) =>
								this.#onGraderChange(index, "negate", (e.target as HTMLInputElement).checked)}
						>
							Negate Result
						</uui-toggle>
					</umb-property-layout>
				</div>
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

			uui-loader {
				display: block;
				margin: auto;
				position: absolute;
				top: 50%;
				left: 50%;
				transform: translate(-50%, -50%);
			}

			.box-headline {
				display: flex;
				justify-content: space-between;
				align-items: center;
				width: 100%;
			}

			.target-input {
				display: flex;
				gap: var(--uui-size-space-3);
				align-items: center;
			}

			.target-input uui-input {
				flex: 1;
			}

			.grader-box {
				--uui-box-default-padding: 0 var(--uui-size-space-5);
				margin-bottom: var(--uui-size-space-4);
			}

			.grader-headline {
				display: flex;
				justify-content: space-between;
				align-items: center;
				width: 100%;
			}

			.grader-options {
				display: grid;
				grid-template-columns: 1fr 1fr 1fr;
				gap: var(--uui-size-space-3);
			}

			.empty-state {
				padding: var(--uui-size-space-3) 0;
				text-align: center;
				color: var(--uui-color-text-alt);
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
