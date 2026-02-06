import {
	css,
	customElement,
	html,
	property,
	when,
	type TemplateResult,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UaiUserGroupSettingsConfig } from "@umbraco-ai/core";
import type { UaiUserGroupPermissionsModel, UaiUserGroupPermissionsMap } from "../../user-group-permissions.js";
import { UAI_TOOL_PERMISSIONS_OVERRIDE_EDITOR_MODAL } from "../../modals/tool-permissions-override-editor/tool-permissions-override-editor-modal.token.js";

/**
 * Wrapper component for managing user group-specific tool permissions.
 * Configures the generic user-group-settings-list for tool permissions use case.
 *
 * @fires change - Fires when permissions change
 *
 * @public
 */
@customElement("uai-user-group-tool-permissions")
export class UaiUserGroupToolPermissionsElement extends UmbLitElement {
	/**
	 * User group permissions map.
	 */
	@property({ type: Object })
	value: UaiUserGroupPermissionsMap = {};

	/**
	 * Agent default permissions.
	 */
	@property({ type: Object })
	agentDefaults: {
		allowedToolIds: string[];
		allowedToolScopeIds: string[];
	} = {
		allowedToolIds: [],
		allowedToolScopeIds: [],
	};

	/**
	 * Whether the component is in readonly mode.
	 */
	@property({ type: Boolean })
	readonly = false;

	/**
	 * Render summary for list item.
	 */
	private _renderSummary(settings: UaiUserGroupPermissionsModel): string {
		const additions =
			(settings.allowedToolIds?.length ?? 0) + (settings.allowedToolScopeIds?.length ?? 0);
		const restrictions =
			(settings.deniedToolIds?.length ?? 0) + (settings.deniedToolScopeIds?.length ?? 0);

		if (additions > 0 && restrictions > 0) {
			return `${additions} additions, ${restrictions} restrictions`;
		} else if (additions > 0) {
			return `${additions} additions`;
		} else if (restrictions > 0) {
			return `${restrictions} restrictions`;
		} else {
			return "No changes";
		}
	}

	/**
	 * Render tags for list item.
	 */
	private _renderTags(settings: UaiUserGroupPermissionsModel): TemplateResult {
		const additions =
			(settings.allowedToolIds?.length ?? 0) + (settings.allowedToolScopeIds?.length ?? 0);
		const restrictions =
			(settings.deniedToolIds?.length ?? 0) + (settings.deniedToolScopeIds?.length ?? 0);
		return html`
			${when(
				additions > 0,
				() => html` <uui-tag color="positive">+${additions}</uui-tag> `
			)}
			${when(
				restrictions > 0,
				() => html` <uui-tag color="danger">-${restrictions}</uui-tag> `
			)}
		`;
	}

	/**
	 * Lifecycle hook to clean up orphaned overrides when agent defaults change.
	 */
	override updated(changedProperties: Map<string, unknown>): void {
		super.updated(changedProperties);

		// Clean up orphaned overrides when agent defaults change
		if (changedProperties.has("agentDefaults")) {
			const oldDefaults = changedProperties.get("agentDefaults") as typeof this.agentDefaults | undefined;
			if (oldDefaults) {
				const cleaned = this._cleanupOrphanedOverrides(this.value, oldDefaults, this.agentDefaults);
				if (cleaned) {
					this.value = cleaned.value;
					if (cleaned.changed) {
						this.dispatchEvent(new UmbChangeEvent());
					}
				}
			}
		}
	}

	/**
	 * Clean up orphaned overrides when agent defaults change.
	 * Returns { value, changed } where changed indicates if any cleanup occurred.
	 */
	private _cleanupOrphanedOverrides(
		permissions: UaiUserGroupPermissionsMap,
		_oldDefaults: typeof this.agentDefaults,
		newDefaults: typeof this.agentDefaults
	): { value: UaiUserGroupPermissionsMap; changed: boolean } | null {
		let hasChanges = false;
		const cleaned: UaiUserGroupPermissionsMap = {};

		// Get sets for efficient lookup
		const newAllowedToolIds = new Set(newDefaults.allowedToolIds.map((id) => id.toLowerCase()));
		const newAllowedScopeIds = new Set(newDefaults.allowedToolScopeIds.map((id) => id.toLowerCase()));

		for (const [userGroupId, userGroupPerms] of Object.entries(permissions)) {
			const cleanedPerms: UaiUserGroupPermissionsModel = { ...userGroupPerms };

			// Clean up denied tool IDs (remove if no longer in base allowed list)
			if (cleanedPerms.deniedToolIds && cleanedPerms.deniedToolIds.length > 0) {
				const filteredDenied = cleanedPerms.deniedToolIds.filter((toolId) =>
					newAllowedToolIds.has(toolId.toLowerCase())
				);
				if (filteredDenied.length !== cleanedPerms.deniedToolIds.length) {
					cleanedPerms.deniedToolIds = filteredDenied;
					hasChanges = true;
				}
			}

			// Clean up denied scope IDs (remove if no longer in base allowed list)
			if (cleanedPerms.deniedToolScopeIds && cleanedPerms.deniedToolScopeIds.length > 0) {
				const filteredDenied = cleanedPerms.deniedToolScopeIds.filter((scopeId) =>
					newAllowedScopeIds.has(scopeId.toLowerCase())
				);
				if (filteredDenied.length !== cleanedPerms.deniedToolScopeIds.length) {
					cleanedPerms.deniedToolScopeIds = filteredDenied;
					hasChanges = true;
				}
			}

			// Clean up allowed tool IDs (remove if now in base allowed list - they're inherited)
			if (cleanedPerms.allowedToolIds && cleanedPerms.allowedToolIds.length > 0) {
				const filteredAllowed = cleanedPerms.allowedToolIds.filter(
					(toolId) => !newAllowedToolIds.has(toolId.toLowerCase())
				);
				if (filteredAllowed.length !== cleanedPerms.allowedToolIds.length) {
					cleanedPerms.allowedToolIds = filteredAllowed;
					hasChanges = true;
				}
			}

			// Clean up allowed scope IDs (remove if now in base allowed list - they're inherited)
			if (cleanedPerms.allowedToolScopeIds && cleanedPerms.allowedToolScopeIds.length > 0) {
				const filteredAllowed = cleanedPerms.allowedToolScopeIds.filter(
					(scopeId) => !newAllowedScopeIds.has(scopeId.toLowerCase())
				);
				if (filteredAllowed.length !== cleanedPerms.allowedToolScopeIds.length) {
					cleanedPerms.allowedToolScopeIds = filteredAllowed;
					hasChanges = true;
				}
			}

			cleaned[userGroupId] = cleanedPerms;
		}

		return hasChanges ? { value: cleaned, changed: true } : null;
	}

	/**
	 * Handle change event from user-group-settings-list.
	 */
	private _onChange(event: UmbChangeEvent): void {
		event.stopPropagation();
		const component = event.target as any;
		this.value = component.value;
		this.dispatchEvent(new UmbChangeEvent());
	}

	override render() {
		const config: UaiUserGroupSettingsConfig<UaiUserGroupPermissionsModel> = {
			editorModal: {
				token: UAI_TOOL_PERMISSIONS_OVERRIDE_EDITOR_MODAL,
				createData: (userGroupId, userGroupName, existing) => ({
					userGroupId,
					userGroupName,
					agentDefaults: this.agentDefaults,
					currentPermissions: existing,
				}),
				extractValue: (result) => result.permissions,
			},
			display: {
				renderSummary: (settings) => this._renderSummary(settings),
				renderTags: (settings) => this._renderTags(settings),
			}
		};

		return html`
			<uai-user-group-settings-list
				.value=${this.value}
				.config=${config}
				.readonly=${this.readonly}
				@change=${this._onChange}>
			</uai-user-group-settings-list>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
			}
		`,
	];
}

declare global {
	interface HTMLElementTagNameMap {
		"uai-user-group-tool-permissions": UaiUserGroupToolPermissionsElement;
	}
}
