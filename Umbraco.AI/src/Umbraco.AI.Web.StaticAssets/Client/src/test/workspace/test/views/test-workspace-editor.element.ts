import { css, html, customElement, state, when, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import { UAI_TEST_WORKSPACE_ALIAS } from "../../../constants.js";
import type { TestResponseModel, TestGraderModel, TestFeatureInfoModel, TestGraderInfoModel } from "../../../../api/types.gen.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";
import { UAI_TEST_ROOT_WORKSPACE_PATH } from "../../test-root/paths.js";
import { AITestRepository } from "../../../repository/test.repository.js";

@customElement("umbraco-ai-test-workspace-editor")
export class UmbracoAITestWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
	#workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;
	#notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;
	#repository!: AITestRepository;

	@state()
	private _model?: TestResponseModel;

	@state()
	private _isNew?: boolean;

	@state()
	private _aliasLocked = true;

	@state()
	private _testFeatures: TestFeatureInfoModel[] = [];

	@state()
	private _testGraders: TestGraderInfoModel[] = [];

	@state()
	private _tagInput = "";

	constructor() {
		super();

		this.#repository = new AITestRepository(this);

		this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;
			this.#workspaceContext = context;
			this.observe(context.model, (model) => {
				this._model = model;
			});
			this.observe(context.isNew, (isNew) => {
				this._isNew = isNew;
				if (isNew) {
					requestAnimationFrame(() => {
						(this.shadowRoot?.querySelector("#name") as HTMLElement)?.focus();
					});
				}
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

	protected override firstUpdated(_changedProperties: any) {
		super.firstUpdated(_changedProperties);
		// Register form control elements to enable HTML5 validation
		const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");
		if (nameInput) this.addFormControlElement(nameInput as any);
	}

	async #loadMetadata() {
		try {
			this._testFeatures = await this.#repository.getAllTestFeatures();
			this._testGraders = await this.#repository.getAllTestGraders();
		} catch (error) {
			console.error("Failed to load metadata:", error);
			this.#notificationContext?.peek("danger", {
				data: { message: "Failed to load test metadata" },
			});
		}
	}

	#onNameChange(event: UUIInputEvent) {
		event.stopPropagation();
		const target = event.composedPath()[0] as UUIInputElement;
		const name = target.value.toString();

		// If alias is locked and creating new, generate alias from name
		if (this._aliasLocked && this._isNew) {
			const alias = this.#generateAlias(name);
			this.#workspaceContext?.handleCommand(
				new UaiPartialUpdateCommand<TestResponseModel>({ name, alias }, "name-alias"),
			);
		} else {
			this.#workspaceContext?.handleCommand(
				new UaiPartialUpdateCommand<TestResponseModel>({ name }, "name"),
			);
		}
	}

	#onAliasChange(event: UUIInputEvent) {
		event.stopPropagation();
		const target = event.composedPath()[0] as UUIInputElement;
		const alias = target.value.toString();

		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>({ alias }, "alias"),
		);
	}

	#onToggleAliasLock() {
		this._aliasLocked = !this._aliasLocked;
	}

	#generateAlias(name: string): string {
		return name
			.toLowerCase()
			.replace(/[^a-z0-9]+/g, "-")
			.replace(/^-|-$/g, "");
	}

	#onDescriptionChange(event: Event) {
		const target = event.target as HTMLTextAreaElement;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>({ description: target.value || null }, "description"),
		);
	}

	#onTestFeatureChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>({ testFeatureId: target.value }, "testFeatureId"),
		);
	}

	#onTargetIdChange(event: Event) {
		const target = event.target as HTMLInputElement;
		if (!this._model) return;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>(
				{
					target: {
						targetId: target.value,
						isAlias: this._model.target.isAlias,
					},
				},
				"target",
			),
		);
	}

	#onTargetIsAliasChange(event: Event) {
		const target = event.target as HTMLInputElement;
		if (!this._model) return;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>(
				{
					target: {
						targetId: this._model.target.targetId,
						isAlias: target.checked,
					},
				},
				"target",
			),
		);
	}

	#onTestCaseJsonChange(event: Event) {
		const target = event.target as HTMLTextAreaElement;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>({ testCaseJson: target.value }, "testCaseJson"),
		);
	}

	#onRunCountChange(event: Event) {
		const target = event.target as HTMLInputElement;
		const runCount = parseInt(target.value) || 1;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>({ runCount }, "runCount"),
		);
	}

	#onAddTag() {
		if (!this._model || !this._tagInput) return;
		if (this._model.tags.includes(this._tagInput)) {
			this.#notificationContext?.peek("warning", {
				data: { message: "Tag already exists" },
			});
			return;
		}
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>({ tags: [...this._model.tags, this._tagInput] }, "tags"),
		);
		this._tagInput = "";
	}

	#onRemoveTag(tag: string) {
		if (!this._model) return;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>(
				{ tags: this._model.tags.filter((t) => t !== tag) },
				"tags",
			),
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
			new UaiPartialUpdateCommand<TestResponseModel>({ graders: [...this._model.graders, newGrader] }, "graders"),
		);
	}

	#onRemoveGrader(index: number) {
		if (!this._model) return;
		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<TestResponseModel>(
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
			new UaiPartialUpdateCommand<TestResponseModel>({ graders: updated }, "graders"),
		);
	}

	render() {
		if (!this._model) return html`<uui-loader></uui-loader>`;

		return html`
			<umb-workspace-editor alias="${UAI_TEST_WORKSPACE_ALIAS}">
				<div id="header" slot="header">
					<uui-button href=${UAI_TEST_ROOT_WORKSPACE_PATH} label="Back to tests" compact>
						<uui-icon name="icon-arrow-left"></uui-icon>
					</uui-button>
					<uui-input
						id="name"
						.value=${this._model.name}
						@input="${this.#onNameChange}"
						label="Name"
						placeholder="Enter test name"
						required
						maxlength="255"
						.requiredMessage=${this.localize.term("uaiValidation_required")}
						.maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 255)}
						${umbBindToValidation(this, "$.name", this._model.name)}
					>
						<uui-input-lock
							slot="append"
							id="alias"
							name="alias"
							label="Alias"
							placeholder="Enter alias"
							.value=${this._model.alias}
							?auto-width=${!!this._model.name}
							?locked=${this._aliasLocked}
							?readonly=${this._aliasLocked || !this._isNew}
							@input=${this.#onAliasChange}
							@lock-change=${this.#onToggleAliasLock}
							required
							maxlength="100"
							pattern="^[a-z0-9\\-]+$"
							.requiredMessage=${this.localize.term("uaiValidation_required")}
							.maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 100)}
							.patternMessage=${this.localize.term("uaiValidation_aliasFormat")}
							${umbBindToValidation(this, "$.alias", this._model.alias)}
						>
						</uui-input-lock>
					</uui-input>
				</div>

				${when(
					!this._isNew && this._model,
					() => html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`,
				)}

				<div slot="footer-info" id="footer">
					<a href=${UAI_TEST_ROOT_WORKSPACE_PATH}>Tests</a>
					/ ${this._model.name || "Untitled"}
				</div>
			</umb-workspace-editor>

			<uui-box headline="Test Details">
				<div class="form-section">
					<uui-form-layout-item>
						<uui-label for="description" slot="label">Description</uui-label>
						<uui-textarea
							id="description"
							.value=${this._model.description || ""}
							@input=${this.#onDescriptionChange}
							placeholder="Enter test description (optional)"
						></uui-textarea>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label for="testFeatureId" slot="label" required>Test Type</uui-label>
						<uui-select
							id="testFeatureId"
							.value=${this._model.testFeatureId}
							@change=${this.#onTestFeatureChange}
							placeholder="Select test type"
						>
							${this._testFeatures.map(
								(feature) =>
									html`<uui-select-option .value=${feature.id}>${feature.name}</uui-select-option>`,
							)}
						</uui-select>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label for="targetId" slot="label" required>Target</uui-label>
						<div class="target-input">
							<uui-input
								id="targetId"
								.value=${this._model.target.targetId}
								@input=${this.#onTargetIdChange}
								placeholder="Enter prompt or agent ID/alias"
							></uui-input>
							<uui-toggle
								label="Is Alias"
								.checked=${this._model.target.isAlias}
								@change=${this.#onTargetIsAliasChange}
							>
								Is Alias
							</uui-toggle>
						</div>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label for="testCaseJson" slot="label" required>Test Case (JSON)</uui-label>
						<uui-textarea
							id="testCaseJson"
							.value=${this._model.testCaseJson}
							@input=${this.#onTestCaseJsonChange}
							placeholder='{"key": "value"}'
							rows="10"
						></uui-textarea>
						<small slot="description">Test case data in JSON format</small>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label for="runCount" slot="label">Run Count</uui-label>
						<uui-input
							id="runCount"
							type="number"
							.value=${this._model.runCount.toString()}
							@input=${this.#onRunCountChange}
							min="1"
							max="100"
						></uui-input>
						<small slot="description">Number of times to run this test (for pass@k calculation)</small>
					</uui-form-layout-item>
				</div>
			</uui-box>

			<uui-box headline="Tags">
				<div class="form-section">
					<div class="tags-input">
						<uui-input
							.value=${this._tagInput}
							@input=${(e: UUIInputEvent) => (this._tagInput = (e.target as UUIInputElement).value.toString())}
							@keypress=${(e: KeyboardEvent) => e.key === "Enter" && this.#onAddTag()}
							placeholder="Add tag and press Enter"
						></uui-input>
						<uui-button @click=${this.#onAddTag} label="Add tag" look="primary">Add</uui-button>
					</div>
					<div class="tags-list">
						${repeat(
							this._model.tags,
							(tag) => tag,
							(tag) => html`
								<uui-tag>
									${tag}
									<uui-button
										slot="actions"
										@click=${() => this.#onRemoveTag(tag)}
										label="Remove tag"
										compact
									>
										<uui-icon name="icon-remove"></uui-icon>
									</uui-button>
								</uui-tag>
							`,
						)}
					</div>
				</div>
			</uui-box>

			<uui-box>
				<div slot="headline" class="box-headline">
					<span>Graders</span>
					<uui-button @click=${this.#onAddGrader} label="Add grader" look="primary" compact>
						<uui-icon name="icon-add"></uui-icon>
						Add Grader
					</uui-button>
				</div>
				<div class="form-section">
					${repeat(
						this._model.graders,
						(grader) => grader.id,
						(grader, index) => this.#renderGrader(grader, index),
					)}
					${when(
						this._model.graders.length === 0,
						() => html`<div class="empty-state">No graders configured. Click "Add Grader" to get started.</div>`,
					)}
				</div>
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

				<div class="grader-content">
					<uui-form-layout-item>
						<uui-label for="graderType-${index}" slot="label">Grader Type</uui-label>
						<uui-select
							id="graderType-${index}"
							.value=${grader.graderTypeId}
							@change=${(e: Event) =>
								this.#onGraderChange(index, "graderTypeId", (e.target as HTMLSelectElement).value)}
						>
							${this._testGraders.map(
								(g) => html`<uui-select-option .value=${g.id}>${g.name}</uui-select-option>`,
							)}
						</uui-select>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label for="graderName-${index}" slot="label">Name</uui-label>
						<uui-input
							id="graderName-${index}"
							.value=${grader.name}
							@input=${(e: UUIInputEvent) =>
								this.#onGraderChange(index, "name", (e.target as UUIInputElement).value)}
						></uui-input>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label for="graderConfig-${index}" slot="label">Config (JSON)</uui-label>
						<uui-textarea
							id="graderConfig-${index}"
							.value=${grader.configJson || "{}"}
							@input=${(e: Event) =>
								this.#onGraderChange(index, "configJson", (e.target as HTMLTextAreaElement).value)}
							rows="3"
						></uui-textarea>
					</uui-form-layout-item>

					<div class="grader-options">
						<uui-form-layout-item>
							<uui-label for="graderSeverity-${index}" slot="label">Severity</uui-label>
							<uui-select
								id="graderSeverity-${index}"
								.value=${grader.severity}
								@change=${(e: Event) =>
									this.#onGraderChange(index, "severity", (e.target as HTMLSelectElement).value)}
							>
								<uui-select-option value="Info">Info</uui-select-option>
								<uui-select-option value="Warning">Warning</uui-select-option>
								<uui-select-option value="Error">Error</uui-select-option>
							</uui-select>
						</uui-form-layout-item>

						<uui-form-layout-item>
							<uui-label for="graderWeight-${index}" slot="label">Weight</uui-label>
							<uui-input
								id="graderWeight-${index}"
								type="number"
								.value=${grader.weight.toString()}
								@input=${(e: UUIInputEvent) =>
									this.#onGraderChange(index, "weight", parseFloat((e.target as UUIInputElement).value.toString()) || 1.0)}
								min="0"
								max="1"
								step="0.1"
							></uui-input>
						</uui-form-layout-item>

						<uui-form-layout-item>
							<uui-label for="graderNegate-${index}" slot="label">Negate</uui-label>
							<uui-toggle
								id="graderNegate-${index}"
								label="Negate Result"
								.checked=${grader.negate}
								@change=${(e: Event) =>
									this.#onGraderChange(index, "negate", (e.target as HTMLInputElement).checked)}
							>
								Negate Result
							</uui-toggle>
						</uui-form-layout-item>
					</div>
				</div>
			</uui-box>
		`;
	}

	static styles = [
		UmbTextStyles,
		css`
			:host {
				display: block;
				width: 100%;
				height: 100%;
			}

			#header {
				display: flex;
				flex: 1 1 auto;
				gap: var(--uui-size-space-3);
			}

			#name {
				width: 100%;
				flex: 1 1 auto;
				align-items: center;
			}

			#footer {
				padding: 0 var(--uui-size-layout-1);
			}

			uui-loader {
				display: block;
				margin: auto;
				position: absolute;
				top: 50%;
				left: 50%;
				transform: translate(-50%, -50%);
			}

			uui-box {
				margin-bottom: var(--uui-size-space-5);
			}

			.form-section {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-4);
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

			.tags-input {
				display: flex;
				gap: var(--uui-size-space-3);
			}

			.tags-input uui-input {
				flex: 1;
			}

			.tags-list {
				display: flex;
				flex-wrap: wrap;
				gap: var(--uui-size-space-2);
			}

			.grader-box {
				margin-bottom: var(--uui-size-space-4);
			}

			.grader-headline {
				display: flex;
				justify-content: space-between;
				align-items: center;
				width: 100%;
			}

			.grader-content {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-3);
			}

			.grader-options {
				display: grid;
				grid-template-columns: 1fr 1fr 1fr;
				gap: var(--uui-size-space-3);
			}

			.empty-state {
				padding: var(--uui-size-space-5);
				text-align: center;
				color: var(--uui-color-text-alt);
			}

			small {
				display: block;
				color: var(--uui-color-text-alt);
				font-size: 0.875rem;
			}
		`,
	];
}

export default UmbracoAITestWorkspaceEditorElement;

declare global {
	interface HTMLElementTagNameMap {
		"umbraco-ai-test-workspace-editor": UmbracoAITestWorkspaceEditorElement;
	}
}
