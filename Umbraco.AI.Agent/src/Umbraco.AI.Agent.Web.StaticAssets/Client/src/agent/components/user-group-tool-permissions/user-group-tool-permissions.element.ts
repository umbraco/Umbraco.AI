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
				() => html` <uui-tag slot="tag" look="positive">+${additions}</uui-tag> `
			)}
			${when(
				restrictions > 0,
				() => html` <uui-tag slot="tag" look="negative">-${restrictions}</uui-tag> `
			)}
		`;
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
