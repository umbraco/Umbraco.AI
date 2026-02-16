import {
	css,
	customElement,
	html,
	property,
	repeat,
	state,
	when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { type ToolScopeItemResponseModel } from "../../repository/tool.repository.js";
import { UaiToolController } from "../../controllers/tool.controller.js";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { toCamelCase } from "../../utils.js";

/**
 * Permission state for a tool scope override.
 */
export type UaiToolScopePermissionState = "inherited" | "allowed" | "denied";

/**
 * Tool scope with permission state.
 */
export interface UaiToolScopePermission {
	scopeId: string;
	state: UaiToolScopePermissionState;
}

/**
 * Component for managing tool scope permission overrides.
 * Displays inherited scopes with "Deny" buttons and allows adding new scopes with "Allow" permission.
 *
 * @fires change - Fires when permissions change
 *
 * @public
 */
@customElement("uai-tool-scope-permissions-override")
export class UaiToolScopePermissionsOverrideElement extends UmbLitElement {
	#toolController = new UaiToolController(this);

	/**
	 * Map of scope ID to full scope data.
	 */
	@state()
	private _scopeDataMap = new Map<string, ToolScopeItemResponseModel>();

	/**
	 * Tool counts by scope ID.
	 */
	@state()
	private _toolCounts: Record<string, number> = {};

	/**
	 * Inherited scope IDs from agent defaults.
	 */
	@property({ type: Array })
	inheritedScopeIds: string[] = [];

	/**
	 * Scope IDs that are explicitly allowed (additive).
	 */
	@property({ type: Array })
	allowedScopeIds: string[] = [];

	/**
	 * Scope IDs that are explicitly denied (subtractive).
	 */
	@property({ type: Array })
	deniedScopeIds: string[] = [];

	/**
	 * Whether the component is in readonly mode.
	 */
	@property({ type: Boolean })
	readonly = false;

	/**
	 * Hide scopes that have no tools.
	 */
	@property({ type: Boolean })
	hideEmptyScopes = false;

	override connectedCallback(): void {
		super.connectedCallback();
		this._loadScopeData();
		this._loadToolCounts();
	}

	override updated(changedProperties: Map<string, unknown>): void {
		super.updated(changedProperties);
		if (
			changedProperties.has("inheritedScopeIds") ||
			changedProperties.has("allowedScopeIds") ||
			changedProperties.has("deniedScopeIds")
		) {
			this._loadScopeData();
		}
	}

	/**
	 * Load full scope data for all scopes.
	 */
	private async _loadScopeData(): Promise<void> {
		const { data } = await this.#toolController.getToolScopes();
		if (!data) return;

		const scopeMap = new Map<string, ToolScopeItemResponseModel>();
		for (const scope of data) {
			scopeMap.set(scope.id, scope);
		}
		this._scopeDataMap = scopeMap;
	}

	/**
	 * Load tool counts for all scopes.
	 */
	private async _loadToolCounts(): Promise<void> {
		this._toolCounts = await this.#toolController.getToolCountsByScope();
	}

	/**
	 * Computed list of all scopes with their permission states.
	 */
	private get _allScopes(): UaiToolScopePermission[] {
		const scopes = new Map<string, UaiToolScopePermission>();

		// Add inherited scopes
		for (const scopeId of this.inheritedScopeIds) {
			scopes.set(scopeId, { scopeId, state: "inherited" });
		}

		// Add explicitly allowed scopes
		for (const scopeId of this.allowedScopeIds) {
			scopes.set(scopeId, { scopeId, state: "allowed" });
		}

		// Mark denied scopes (overrides inherited/allowed)
		for (const scopeId of this.deniedScopeIds) {
			const existing = scopes.get(scopeId);
			if (existing) {
				existing.state = "denied";
			} else {
				// Scope was never inherited/allowed, but is explicitly denied
				// This is unusual but handle it
				scopes.set(scopeId, { scopeId, state: "denied" });
			}
		}

		const allScopes = Array.from(scopes.values());

		// Filter empty scopes if hideEmptyScopes is true
		if (this.hideEmptyScopes) {
			return allScopes.filter(scope => (this._toolCounts[scope.scopeId] ?? 0) > 0);
		}

		return allScopes;
	}

	/**
	 * Add a new allowed scope.
	 */
	private async _addAllowedScope(): Promise<void> {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
			data: {
				fetchItems: () => this._fetchAvailableScopes(),
				selectionMode: "multiple",
				title: this.localize.term("uaiAgent_addScope") || "Add Tool Scopes",
				noResultsMessage: this.localize.term("uaiAgent_noToolScopesAvailable") || "No tool scopes available",
			},
		});

		try {
			const result = await modal.onSubmit();
			if (result?.selection?.length) {
				// Add selected scopes to allowed list (filter out duplicates)
				const newScopes = result.selection
					.map((item: UaiPickableItemModel) => item.value)
					.filter(
						(scopeId: string) => !this.allowedScopeIds.includes(scopeId) && !this.inheritedScopeIds.includes(scopeId)
					);

				if (newScopes.length > 0) {
					this.allowedScopeIds = [...this.allowedScopeIds, ...newScopes];
					this._dispatchChangeEvent();
				}
			}
		} catch {
			// Modal was cancelled
		}
	}

	/**
	 * Fetch available tool scopes for the picker modal.
	 */
	private async _fetchAvailableScopes(): Promise<UaiPickableItemModel[]> {
		const { data } = await this.#toolController.getToolScopes();

		if (!data) return [];

		// Filter out already selected items and map to picker format
		return data
			.filter((scope: ToolScopeItemResponseModel) =>
				!this.allowedScopeIds.includes(scope.id) && !this.inheritedScopeIds.includes(scope.id)
			)
			.filter((scope: ToolScopeItemResponseModel) =>
				!this.hideEmptyScopes || (this._toolCounts[scope.id] ?? 0) > 0
			)
			.map((scope: ToolScopeItemResponseModel) => {
				const camelCaseId = toCamelCase(scope.id);
				const localizedName = this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.id;
				const localizedDescription =
					this.localize.term(`uaiToolScope_${camelCaseId}Description`) || "";
				const toolCount = this._toolCounts[scope.id] ?? 0;

				return {
					value: scope.id,
					label: `${localizedName} (${toolCount})`,
					description: localizedDescription,
					icon: scope.icon || "icon-wand",
				};
			});
	}

	/**
	 * Deny a scope (move to denied list).
	 */
	private _denyScope(scopeId: string): void {
		// Add to denied list if not already there
		if (!this.deniedScopeIds.includes(scopeId)) {
			this.deniedScopeIds = [...this.deniedScopeIds, scopeId];
		}

		// Remove from allowed list if present
		this.allowedScopeIds = this.allowedScopeIds.filter((id) => id !== scopeId);

		this._dispatchChangeEvent();
	}

	/**
	 * Remove a scope from denied list (allow it again).
	 */
	private _allowScope(scopeId: string): void {
		this.deniedScopeIds = this.deniedScopeIds.filter((id) => id !== scopeId);
		this._dispatchChangeEvent();
	}

	/**
	 * Remove an explicitly allowed scope.
	 */
	private _removeAllowedScope(scopeId: string): void {
		this.allowedScopeIds = this.allowedScopeIds.filter((id) => id !== scopeId);
		this._dispatchChangeEvent();
	}

	/**
	 * Dispatch change event.
	 */
	private _dispatchChangeEvent(): void {
		this.dispatchEvent(new UmbChangeEvent());
	}

	/**
	 * Render a scope permission item.
	 */
	private _renderScopeItem(scope: UaiToolScopePermission) {
		const scopeData = this._scopeDataMap.get(scope.scopeId);
		const camelCaseId = toCamelCase(scope.scopeId);
		const name = this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.scopeId;
		const description = this.localize.term(`uaiToolScope_${camelCaseId}Description`) || "";
		const icon = scopeData?.icon || "icon-wand";
		const toolCount = this._toolCounts[scope.scopeId] ?? 0;

		return html`
			<uui-ref-node name="${name} (${toolCount})" detail=${description}>
				<umb-icon slot="icon" name=${icon}></umb-icon>
				${when(
					scope.state === "inherited",
					() => html`
						<uui-tag slot="tag" look="secondary">${this.localize.term("uaiGeneral_inherited")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uaiGeneral_deny")}
										color="danger"
										@click=${() => this._denyScope(scope.scopeId)}>
                                        <uui-icon name="icon-block"></uui-icon>
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
				${when(
					scope.state === "allowed",
					() => html`
						<uui-tag slot="tag" look="primary" color="positive">${this.localize.term("uaiGeneral_allowed")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uaiGeneral_remove")}
										@click=${() => this._removeAllowedScope(scope.scopeId)}>
										<uui-icon name="icon-trash"></uui-icon>
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
				${when(
					scope.state === "denied",
					() => html`
						<uui-tag slot="tag" look="primary" color="danger">${this.localize.term("uaiGeneral_denied")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uaiGeneral_allow")}
										color="positive"
										@click=${() => this._allowScope(scope.scopeId)}>
                                        <uui-icon name="icon-check"></uui-icon>
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
			<div class="scope-list">
				${when(
					this._allScopes.length > 0,
					() => html`
						<uui-ref-list>
							${repeat(
								this._allScopes,
								(scope) => scope.scopeId,
								(scope) => this._renderScopeItem(scope)
							)}
						</uui-ref-list>
					`,
					() => html`<p class="empty-message">No tool scopes configured</p>`
				)}
				${when(
					!this.readonly,
					() => html`
						<uui-button
							look="placeholder"
							@click=${this._addAllowedScope}
							label=${this.localize.term("general_add")}>
							<uui-icon name="icon-add"></uui-icon>
							${this.localize.term("general_add")}
						</uui-button>
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

			.scope-list {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-4);
			}

			.empty-message {
				margin: var(--uui-size-space-4) 0;
				color: var(--uui-color-text-alt);
			}

			uui-button[look="placeholder"] {
				width: 100%;
			}
		`,
	];
}

declare global {
	interface HTMLElementTagNameMap {
		"uai-tool-scope-permissions-override": UaiToolScopePermissionsOverrideElement;
	}
}
