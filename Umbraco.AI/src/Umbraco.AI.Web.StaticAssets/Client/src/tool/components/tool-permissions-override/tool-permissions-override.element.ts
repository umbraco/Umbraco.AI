import {
	css,
	customElement,
	html,
	nothing,
	property,
	repeat,
	state,
	when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";

/**
 * Permission state for a tool override.
 */
export type UaiToolPermissionState = "inherited" | "allowed" | "denied";

/**
 * Tool with permission state.
 */
export interface UaiToolPermission {
	toolId: string;
	state: UaiToolPermissionState;
}

/**
 * Component for managing individual tool permission overrides.
 * Displays inherited tools with "Deny" buttons and allows adding new tools with "Allow" permission.
 *
 * @fires change - Fires when permissions change
 *
 * @public
 */
@customElement("uai-tool-permissions-override")
export class UaiToolPermissionsOverrideElement extends UmbLitElement {
	/**
	 * Inherited tool IDs from agent defaults.
	 */
	@property({ type: Array })
	inheritedToolIds: string[] = [];

	/**
	 * Tool IDs that are explicitly allowed (additive).
	 */
	@property({ type: Array })
	allowedToolIds: string[] = [];

	/**
	 * Tool IDs that are explicitly denied (subtractive).
	 */
	@property({ type: Array })
	deniedToolIds: string[] = [];

	/**
	 * Whether the component is in readonly mode.
	 */
	@property({ type: Boolean })
	readonly = false;

	/**
	 * Input value for adding new tool ID.
	 */
	@state()
	private _newToolId = "";

	/**
	 * Computed list of all tools with their permission states.
	 */
	private get _allTools(): UaiToolPermission[] {
		const tools = new Map<string, UaiToolPermission>();

		// Add inherited tools
		for (const toolId of this.inheritedToolIds) {
			tools.set(toolId, { toolId, state: "inherited" });
		}

		// Add explicitly allowed tools
		for (const toolId of this.allowedToolIds) {
			tools.set(toolId, { toolId, state: "allowed" });
		}

		// Mark denied tools (overrides inherited/allowed)
		for (const toolId of this.deniedToolIds) {
			const existing = tools.get(toolId);
			if (existing) {
				existing.state = "denied";
			} else {
				// Tool was never inherited/allowed, but is explicitly denied
				// This is unusual but handle it
				tools.set(toolId, { toolId, state: "denied" });
			}
		}

		return Array.from(tools.values());
	}

	/**
	 * Handle new tool ID input change.
	 */
	private _onNewToolIdInput(event: UUIInputEvent): void {
		const input = event.target as UUIInputElement;
		this._newToolId = input.value as string;
	}

	/**
	 * Add a new allowed tool.
	 */
	private _addAllowedTool(): void {
		const toolId = this._newToolId.trim();
		if (!toolId) {
			return;
		}

		// Check if already exists
		if (this.allowedToolIds.includes(toolId) || this.inheritedToolIds.includes(toolId)) {
			this._newToolId = "";
			return;
		}

		this.allowedToolIds = [...this.allowedToolIds, toolId];
		this._newToolId = "";
		this._dispatchChangeEvent();
	}

	/**
	 * Handle Enter key in input to add tool.
	 */
	private _onNewToolIdKeydown(event: KeyboardEvent): void {
		if (event.key === "Enter") {
			event.preventDefault();
			this._addAllowedTool();
		}
	}

	/**
	 * Deny a tool (move to denied list).
	 */
	private _denyTool(toolId: string): void {
		// Add to denied list if not already there
		if (!this.deniedToolIds.includes(toolId)) {
			this.deniedToolIds = [...this.deniedToolIds, toolId];
		}

		// Remove from allowed list if present
		this.allowedToolIds = this.allowedToolIds.filter((id) => id !== toolId);

		this._dispatchChangeEvent();
	}

	/**
	 * Remove a tool from denied list (allow it again).
	 */
	private _allowTool(toolId: string): void {
		this.deniedToolIds = this.deniedToolIds.filter((id) => id !== toolId);
		this._dispatchChangeEvent();
	}

	/**
	 * Remove an explicitly allowed tool.
	 */
	private _removeAllowedTool(toolId: string): void {
		this.allowedToolIds = this.allowedToolIds.filter((id) => id !== toolId);
		this._dispatchChangeEvent();
	}

	/**
	 * Dispatch change event.
	 */
	private _dispatchChangeEvent(): void {
		this.dispatchEvent(new UmbChangeEvent());
	}

	/**
	 * Render a tool permission item.
	 */
	private _renderToolItem(tool: UaiToolPermission) {
		return html`
			<uui-ref-node name=${tool.toolId}>
				<umb-icon slot="icon" name="icon-wand"></umb-icon>
				${when(
					tool.state === "inherited",
					() => html`
						<uui-tag slot="tag" look="secondary">${this.localize.term("uai_inherited")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uai_deny")}
										color="danger"
										@click=${() => this._denyTool(tool.toolId)}>
										${this.localize.term("uai_deny")}
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
				${when(
					tool.state === "allowed",
					() => html`
						<uui-tag slot="tag" look="positive">${this.localize.term("uai_allowed")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("general_remove")}
										@click=${() => this._removeAllowedTool(tool.toolId)}>
										<uui-icon name="icon-trash"></uui-icon>
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
				${when(
					tool.state === "denied",
					() => html`
						<uui-tag slot="tag" look="negative">${this.localize.term("uai_denied")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uai_allow")}
										color="positive"
										@click=${() => this._allowTool(tool.toolId)}>
										${this.localize.term("uai_allow")}
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
			</uui-ref-node>
		`;
	}

	override render() {
		return html`
			<div class="tool-list">
				${when(
					this._allTools.length > 0,
					() => html`
						<uui-ref-list>
							${repeat(
								this._allTools,
								(tool) => tool.toolId,
								(tool) => this._renderToolItem(tool)
							)}
						</uui-ref-list>
					`,
					() => html`<p class="empty-message">${this.localize.term("uai_noToolsConfigured")}</p>`
				)}
				${when(
					!this.readonly,
					() => html`
						<div class="add-tool">
							<uui-input
								.value=${this._newToolId}
								@input=${this._onNewToolIdInput}
								@keydown=${this._onNewToolIdKeydown}
								placeholder=${this.localize.term("uai_enterToolId")}
								label=${this.localize.term("uai_toolId")}>
							</uui-input>
							<uui-button
								look="primary"
								@click=${this._addAllowedTool}
								label=${this.localize.term("uai_addTool")}
								?disabled=${!this._newToolId.trim()}>
								<uui-icon name="icon-add"></uui-icon>
								${this.localize.term("uai_addTool")}
							</uui-button>
						</div>
					`
				)}
			</div>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
			}

			.tool-list {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-4);
			}

			.empty-message {
				margin: var(--uui-size-space-4) 0;
				color: var(--uui-color-text-alt);
			}

			.add-tool {
				display: flex;
				gap: var(--uui-size-space-3);
				align-items: flex-end;
			}

			.add-tool uui-input {
				flex: 1;
			}
		`,
	];
}

declare global {
	interface HTMLElementTagNameMap {
		"uai-tool-permissions-override": UaiToolPermissionsOverrideElement;
	}
}
