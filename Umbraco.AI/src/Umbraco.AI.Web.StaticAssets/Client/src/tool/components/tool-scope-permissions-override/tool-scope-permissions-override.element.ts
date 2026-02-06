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
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UaiToolRepository, type ToolScopeItemResponseModel } from "../../repository/tool.repository.js";
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
	#toolRepository = new UaiToolRepository(this);

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

		return Array.from(scopes.values());
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
		const { data } = await this.#toolRepository.getToolScopes();

		if (!data) return [];

		// Filter out already selected items and map to picker format
		return data
			.filter((scope: ToolScopeItemResponseModel) =>
				!this.allowedScopeIds.includes(scope.id) && !this.inheritedScopeIds.includes(scope.id)
			)
			.map((scope: ToolScopeItemResponseModel) => {
				const camelCaseId = toCamelCase(scope.id);
				const localizedName = this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.id;
				const localizedDescription =
					this.localize.term(`uaiToolScope_${camelCaseId}Description`) || scope.description || "";

				return {
					value: scope.id,
					label: localizedName,
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
		return html`
			<uui-ref-node name=${scope.scopeId}>
				<umb-icon slot="icon" name="icon-box"></umb-icon>
				${when(
					scope.state === "inherited",
					() => html`
						<uui-tag slot="tag" look="secondary">${this.localize.term("uai_inherited")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uai_deny")}
										color="danger"
										@click=${() => this._denyScope(scope.scopeId)}>
										${this.localize.term("uai_deny")}
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
				${when(
					scope.state === "allowed",
					() => html`
						<uui-tag slot="tag" look="positive">${this.localize.term("uai_allowed")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("general_remove")}
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
						<uui-tag slot="tag" look="negative">${this.localize.term("uai_denied")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uai_allow")}
										color="positive"
										@click=${() => this._allowScope(scope.scopeId)}>
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
					() => html`<p class="empty-message">${this.localize.term("uai_noToolScopesConfigured")}</p>`
				)}
				${when(
					!this.readonly,
					() => html`
						<uui-button
							look="placeholder"
							@click=${this._addAllowedScope}
							label=${this.localize.term("uai_addToolScope")}>
							<uui-icon name="icon-add"></uui-icon>
							${this.localize.term("uai_addToolScope")}
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
