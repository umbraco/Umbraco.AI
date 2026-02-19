import { css, html, customElement, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_TEST_WORKSPACE_CONTEXT } from "./test-workspace.context-token.js";
import { UAI_TEST_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiTestDetailModel } from "../../types.js";
import { UaiPartialUpdateCommand } from "../../../core/command/implement/partial-update.command.js";
import { UAI_TEST_ROOT_WORKSPACE_PATH } from "../test-root/paths.js";

@customElement("umbraco-ai-test-workspace-editor")
export class UmbracoAITestWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
	#workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;

	@state()
	private _model?: UaiTestDetailModel;

	@state()
	private _isNew?: boolean;

	@state()
	private _aliasLocked = true;

	constructor() {
		super();

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
	}

	protected override firstUpdated(_changedProperties: any) {
		super.firstUpdated(_changedProperties);
		// Register form control elements to enable HTML5 validation
		const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");
		if (nameInput) this.addFormControlElement(nameInput as any);
	}

	#onNameChange(event: UUIInputEvent) {
		event.stopPropagation();
		const target = event.composedPath()[0] as UUIInputElement;
		const name = target.value.toString();

		// If alias is locked and creating new, generate alias from name
		if (this._aliasLocked && this._isNew) {
			const alias = this.#generateAlias(name);
			this.#workspaceContext?.handleCommand(
				new UaiPartialUpdateCommand<UaiTestDetailModel>({ name, alias }, "name-alias"),
			);
		} else {
			this.#workspaceContext?.handleCommand(
				new UaiPartialUpdateCommand<UaiTestDetailModel>({ name }, "name"),
			);
		}
	}

	#onAliasChange(event: UUIInputEvent) {
		event.stopPropagation();
		const target = event.composedPath()[0] as UUIInputElement;
		const alias = target.value.toString();

		this.#workspaceContext?.handleCommand(
			new UaiPartialUpdateCommand<UaiTestDetailModel>({ alias }, "alias"),
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
		`,
	];
}

export default UmbracoAITestWorkspaceEditorElement;

declare global {
	interface HTMLElementTagNameMap {
		"umbraco-ai-test-workspace-editor": UmbracoAITestWorkspaceEditorElement;
	}
}
