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
import { UaiToolRepository, type ToolItemResponseModel } from "../../repository/tool.repository.js";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { toCamelCase } from "../../utils.js";

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
	#toolRepository = new UaiToolRepository(this);

	/**
	 * Map of tool ID to full tool data.
	 */
	@state()
	private _toolDataMap = new Map<string, ToolItemResponseModel>();

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

	override connectedCallback(): void {
		super.connectedCallback();
		this._loadToolData();
	}

	override updated(changedProperties: Map<string, unknown>): void {
		super.updated(changedProperties);
		if (
			changedProperties.has("inheritedToolIds") ||
			changedProperties.has("allowedToolIds") ||
			changedProperties.has("deniedToolIds")
		) {
			this._loadToolData();
		}
	}

	/**
	 * Load full tool data for all tools.
	 */
	private async _loadToolData(): Promise<void> {
		const { data } = await this.#toolRepository.getTools();
		if (!data) return;

		const toolMap = new Map<string, ToolItemResponseModel>();
		for (const tool of data) {
			toolMap.set(tool.id, tool);
		}
		this._toolDataMap = toolMap;
	}

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
	 * Add a new allowed tool.
	 */
	private async _addAllowedTool(): Promise<void> {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
			data: {
				fetchItems: () => this._fetchAvailableTools(),
				selectionMode: "multiple",
				title: this.localize.term("uaiAgent_addTool") || "Add Tools",
				noResultsMessage: this.localize.term("uaiAgent_noToolsAvailable") || "No tools available",
			},
		});

		try {
			const result = await modal.onSubmit();
			if (result?.selection?.length) {
				// Add selected tools to allowed list (filter out duplicates)
				const newTools = result.selection
					.map((item: UaiPickableItemModel) => item.value)
					.filter(
						(toolId: string) => !this.allowedToolIds.includes(toolId) && !this.inheritedToolIds.includes(toolId)
					);

				if (newTools.length > 0) {
					this.allowedToolIds = [...this.allowedToolIds, ...newTools];
					this._dispatchChangeEvent();
				}
			}
		} catch {
			// Modal was cancelled
		}
	}

	/**
	 * Fetch available tools for the picker modal.
	 */
	private async _fetchAvailableTools(): Promise<UaiPickableItemModel[]> {
		const { data } = await this.#toolRepository.getTools();

		if (!data) return [];

		// Filter out already selected items and map to picker format
		return data
			.filter((tool: ToolItemResponseModel) =>
				!this.allowedToolIds.includes(tool.id) && !this.inheritedToolIds.includes(tool.id)
			)
			.map((tool: ToolItemResponseModel) => {
				const camelCaseId = toCamelCase(tool.id);
				const localizedName = this.localize.term(`uaiTool_${camelCaseId}Label`) || tool.name || tool.id;
				const localizedDescription =
					this.localize.term(`uaiTool_${camelCaseId}Description`) || tool.description || "";

				return {
					value: tool.id,
					label: localizedName,
					description: localizedDescription,
					icon: "icon-wand",
				};
			});
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
		const toolData = this._toolDataMap.get(tool.toolId);
		const camelCaseId = toCamelCase(tool.toolId);
		const name = this.localize.term(`uaiTool_${camelCaseId}Label`) || toolData?.name || tool.toolId;
		const description = this.localize.term(`uaiTool_${camelCaseId}Description`) || toolData?.description || "";

		return html`
			<uui-ref-node name=${name} detail=${description}>
				<umb-icon slot="icon" name="icon-wand"></umb-icon>
				${when(
					tool.state === "inherited",
					() => html`
						<uui-tag slot="tag" look="secondary">${this.localize.term("uaiGeneral_inherited")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uaiGeneral_deny")}
										color="danger"
										@click=${() => this._denyTool(tool.toolId)}>
                                        <uui-icon name="icon-block"></uui-icon>
									</uui-button>
								</uui-action-bar>
							`
						)}
					`
				)}
				${when(
					tool.state === "allowed",
					() => html`
						<uui-tag slot="tag" look="primary" color="positive">${this.localize.term("uaiGeneral_allowed")}</uui-tag>
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
						<uui-tag slot="tag" look="primary" color="danger">${this.localize.term("uaiGeneral_denied")}</uui-tag>
						${when(
							!this.readonly,
							() => html`
								<uui-action-bar slot="actions">
									<uui-button
										label=${this.localize.term("uaiGeneral_allow")}
										color="positive"
										@click=${() => this._allowTool(tool.toolId)}>
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
					() => html`<p class="empty-message">No tools configured</p>`
				)}
				${when(
					!this.readonly,
					() => html`
						<uui-button
							look="placeholder"
							@click=${this._addAllowedTool}
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

			.tool-list {
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
		"uai-tool-permissions-override": UaiToolPermissionsOverrideElement;
	}
}
