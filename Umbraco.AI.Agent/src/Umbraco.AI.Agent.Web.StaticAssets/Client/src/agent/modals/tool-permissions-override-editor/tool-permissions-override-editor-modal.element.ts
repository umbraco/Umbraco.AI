import {
	css,
	customElement,
	html,
	property,
	state,
	when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
	UaiToolScopePermissionsOverrideElement,
	UaiToolPermissionsOverrideElement,
} from "@umbraco-ai/core";
import type { UaiUserGroupPermissionsModel } from "../../user-group-permissions.js";

/**
 * Modal data for tool permissions override editor.
 */
export interface UaiToolPermissionsOverrideEditorModalData {
	/** User group ID */
	userGroupId: string;
	/** User group name for display */
	userGroupName: string;
	/** Agent's default permissions */
	agentDefaults: {
		allowedToolIds: string[];
		allowedToolScopeIds: string[];
	};
	/** Current permissions for this user group (if editing) */
	currentPermissions?: UaiUserGroupPermissionsModel;
}

/**
 * Modal value returned when confirmed.
 */
export interface UaiToolPermissionsOverrideEditorModalValue {
	permissions: UaiUserGroupPermissionsModel;
}

/**
 * Modal for editing user group tool permission overrides.
 *
 * @element uai-tool-permissions-override-editor-modal
 */
@customElement("uai-tool-permissions-override-editor-modal")
export class UaiToolPermissionsOverrideEditorModalElement extends UmbModalBaseElement<
	UaiToolPermissionsOverrideEditorModalData,
	UaiToolPermissionsOverrideEditorModalValue
> {
	/**
	 * Allowed tool scope IDs (additive).
	 */
	@state()
	private _allowedToolScopeIds: string[] = [];

	/**
	 * Denied tool scope IDs (subtractive).
	 */
	@state()
	private _deniedToolScopeIds: string[] = [];

	/**
	 * Allowed tool IDs (additive).
	 */
	@state()
	private _allowedToolIds: string[] = [];

	/**
	 * Denied tool IDs (subtractive).
	 */
	@state()
	private _deniedToolIds: string[] = [];

	override connectedCallback(): void {
		super.connectedCallback();

		// Initialize from current permissions if editing
		if (this.data?.currentPermissions) {
			this._allowedToolScopeIds = [...this.data.currentPermissions.allowedToolScopeIds];
			this._deniedToolScopeIds = [...this.data.currentPermissions.deniedToolScopeIds];
			this._allowedToolIds = [...this.data.currentPermissions.allowedToolIds];
			this._deniedToolIds = [...this.data.currentPermissions.deniedToolIds];
		}
	}

	/**
	 * Handle tool scope permissions change.
	 */
	private _onToolScopePermissionsChange(event: Event): void {
		const element = event.target as UaiToolScopePermissionsOverrideElement;
		this._allowedToolScopeIds = element.allowedScopeIds;
		this._deniedToolScopeIds = element.deniedScopeIds;
	}

	/**
	 * Handle tool permissions change.
	 */
	private _onToolPermissionsChange(event: Event): void {
		const element = event.target as UaiToolPermissionsOverrideElement;
		this._allowedToolIds = element.allowedToolIds;
		this._deniedToolIds = element.deniedToolIds;
	}

	/**
	 * Handle confirm button click.
	 */
	private _onConfirm(): void {
		const permissions: UaiUserGroupPermissionsModel = {
			allowedToolIds: this._allowedToolIds,
			allowedToolScopeIds: this._allowedToolScopeIds,
			deniedToolIds: this._deniedToolIds,
			deniedToolScopeIds: this._deniedToolScopeIds,
		};

		this.value = { permissions };
		this.modalContext?.submit();
	}

	/**
	 * Handle cancel button click.
	 */
	private _onCancel(): void {
		this.modalContext?.reject();
	}

	override render() {
		if (!this.data) {
			return nothing;
		}

		return html`
			<umb-body-layout headline=${`Tool Permission Overrides: ${this.data.userGroupName}`}>
				<div class="content">
					<!-- Agent Defaults (Read-Only) -->
					<uui-box headline=${this.localize.term("uaiAgent_agentDefaults")}>
						<p class="description">${this.localize.term("uaiAgent_agentDefaultsDescription")}</p>
						${when(
							this.data.agentDefaults.allowedToolScopeIds.length > 0 ||
								this.data.agentDefaults.allowedToolIds.length > 0,
							() => html`
								<div class="defaults-content">
									${when(
										this.data!.agentDefaults.allowedToolScopeIds.length > 0,
										() => html`
											<div class="defaults-section">
												<strong>${this.localize.term("uaiAgent_allowedToolScopes")}:</strong>
												<div class="chips">
													${this.data!.agentDefaults.allowedToolScopeIds.map(
														(scope) => html`<uui-tag look="secondary">${scope}</uui-tag>`
													)}
												</div>
											</div>
										`
									)}
									${when(
										this.data!.agentDefaults.allowedToolIds.length > 0,
										() => html`
											<div class="defaults-section">
												<strong>${this.localize.term("uaiAgent_allowedToolIds")}:</strong>
												<p>${this.data!.agentDefaults.allowedToolIds.join(", ")}</p>
											</div>
										`
									)}
								</div>
							`,
							() => html`<p class="empty-message">${this.localize.term("uaiAgent_noDefaultPermissions")}</p>`
						)}
					</uui-box>

					<!-- Tool Scope Overrides -->
					<uui-box headline=${this.localize.term("uaiAgent_toolScopeOverrides")}>
						<p class="description">${this.localize.term("uaiAgent_toolScopeOverridesDescription")}</p>
						<uai-tool-scope-permissions-override
							.inheritedScopeIds=${this.data.agentDefaults.allowedToolScopeIds}
							.allowedScopeIds=${this._allowedToolScopeIds}
							.deniedScopeIds=${this._deniedToolScopeIds}
							@change=${this._onToolScopePermissionsChange}>
						</uai-tool-scope-permissions-override>
					</uui-box>

					<!-- Tool ID Overrides -->
					<uui-box headline=${this.localize.term("uaiAgent_toolIdOverrides")}>
						<p class="description">${this.localize.term("uaiAgent_toolIdOverridesDescription")}</p>
						<uai-tool-permissions-override
							.inheritedToolIds=${this.data.agentDefaults.allowedToolIds}
							.allowedToolIds=${this._allowedToolIds}
							.deniedToolIds=${this._deniedToolIds}
							@change=${this._onToolPermissionsChange}>
						</uai-tool-permissions-override>
					</uui-box>
				</div>

				<!-- Actions -->
				<div slot="actions">
					<uui-button
						label=${this.localize.term("general_cancel")}
						@click=${this._onCancel}>
						${this.localize.term("general_cancel")}
					</uui-button>
					<uui-button
						look="primary"
						color="positive"
						label=${this.localize.term("general_submit")}
						@click=${this._onConfirm}>
						${this.localize.term("general_submit")}
					</uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
			}

			.content {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-5);
			}

			uui-box {
				margin: 0;
			}

			.description {
				margin: 0 0 var(--uui-size-space-4) 0;
				color: var(--uui-color-text-alt);
				font-size: var(--uui-type-small-size);
			}

			.defaults-content {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-3);
			}

			.defaults-section {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-2);
			}

			.chips {
				display: flex;
				flex-wrap: wrap;
				gap: var(--uui-size-space-2);
			}

			.empty-message {
				margin: 0;
				color: var(--uui-color-text-alt);
				font-style: italic;
			}
		`,
	];
}

export default UaiToolPermissionsOverrideEditorModalElement;

declare global {
	interface HTMLElementTagNameMap {
		"uai-tool-permissions-override-editor-modal": UaiToolPermissionsOverrideEditorModalElement;
	}
}
