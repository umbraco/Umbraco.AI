import {
	css,
	customElement,
	html,
	property,
	repeat,
	state,
	when,
	type TemplateResult,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UMB_USER_GROUP_PICKER_MODAL, UmbUserGroupItemRepository } from "@umbraco-cms/backoffice/user-group";

/**
 * Configuration for the user group settings list component.
 * @template TSettings - The type of settings managed for each user group
 * @public
 */
export interface UaiUserGroupSettingsConfig<TSettings> {
	/** Modal configuration for editing settings */
	editorModal: {
		/** Modal token to open for editing */
		token: UmbModalToken<any, any>;

		/** Factory to create modal data */
		createData: (userGroupId: string, userGroupName: string, existingSettings?: TSettings) => any;

		/** Extract settings from modal result */
		extractValue: (modalResult: any) => TSettings;
	};

	/** Display configuration */
	display: {
		/** Render summary text/content for list item detail */
		renderSummary: (settings: TSettings) => TemplateResult | string;

		/** Optional: Render tags/badges in list item */
		renderTags?: (settings: TSettings) => TemplateResult;
	};

	/** Localization (all optional with sensible defaults) */
	labels?: {
		/** Box headline (default: "User Group Overrides") */
		headline?: string;
		/** Add button label (default: "Add User Group Override") */
		addButton?: string;
		/** Empty state message (default: "No user group overrides configured.") */
		noGroupsMessage?: string;
	};
}

/**
 * Generic reusable component for managing user-group-specific settings for any feature.
 * Handles list display, add/edit/remove workflows, and user group picker integration.
 *
 * @fires change - Fires when the value changes
 *
 * @example
 * ```typescript
 * const config: UaiUserGroupSettingsConfig<MySettings> = {
 *   editorModal: {
 *     token: MY_EDITOR_MODAL_TOKEN,
 *     createData: (id, name, existing) => ({ userGroupId: id, userGroupName: name, settings: existing }),
 *     extractValue: (result) => result.settings,
 *   },
 *   display: {
 *     renderSummary: (settings) => `${settings.count} items configured`,
 *     renderTags: (settings) => html`<uui-tag>Active</uui-tag>`,
 *   },
 *   labels: {
 *     headline: 'Custom Settings',
 *     addButton: 'Add Custom Setting',
 *   },
 * };
 * ```
 *
 * @public
 */
@customElement("uai-user-group-settings-list")
export class UaiUserGroupSettingsListElement<TSettings> extends UmbLitElement {
	/**
	 * Map of user group IDs to their settings.
	 * Dictionary key is the user group ID (GUID as string).
	 */
	@property({ type: Object })
	value: Record<string, TSettings> = {};

	/**
	 * Configuration for the component (modal, display, labels).
	 */
	@property({ type: Object })
	config!: UaiUserGroupSettingsConfig<TSettings>;

	/**
	 * Whether the component is in readonly mode.
	 */
	@property({ type: Boolean })
	readonly = false;

	/**
	 * Internal state for resolved user group names.
	 */
	@state()
	private _userGroupNames: Map<string, string> = new Map();

	/**
	 * User group repository for fetching user group data.
	 */
	#userGroupRepository = new UmbUserGroupItemRepository(this);

	/**
	 * Computed labels with defaults.
	 */
	private get _labels() {
		return {
			headline: this.config.labels?.headline ?? this.localize.term("uai_userGroupOverrides"),
			addButton: this.config.labels?.addButton ?? this.localize.term("uai_addUserGroupOverride"),
			noGroupsMessage: this.config.labels?.noGroupsMessage ?? this.localize.term("uai_noUserGroupOverridesConfigured"),
		};
	}

	/**
	 * Check if there are any user groups configured.
	 */
	private get _hasUserGroups(): boolean {
		return Object.keys(this.value).length > 0;
	}

	/**
	 * Get user group entries as array for rendering.
	 */
	private get _userGroupEntries(): [string, TSettings][] {
		return Object.entries(this.value);
	}

	override connectedCallback(): void {
		super.connectedCallback();
		// Load user group names when component mounts
		this._loadUserGroupNames();
	}

	/**
	 * Load user group names for all configured user groups.
	 */
	private async _loadUserGroupNames(): Promise<void> {
		const userGroupIds = Object.keys(this.value);
		for (const userGroupId of userGroupIds) {
			const name = await this._getUserGroupName(userGroupId);
			if (name) {
				this._userGroupNames = new Map(this._userGroupNames).set(userGroupId, name);
			}
		}
	}

	/**
	 * Get user group name by ID.
	 */
	private async _getUserGroupName(userGroupId: string): Promise<string> {
		// Check cache first
		if (this._userGroupNames.has(userGroupId)) {
			return this._userGroupNames.get(userGroupId)!;
		}

		// Fetch from user group repository
		try {
			const { data } = await this.#userGroupRepository.requestItems([userGroupId]);
			if (data && data.length > 0 && data[0]) {
				const name = data[0].name ?? userGroupId;
				this._userGroupNames = new Map(this._userGroupNames).set(userGroupId, name);
				return name;
			}
		} catch (error) {
			console.error(`Failed to fetch user group name for ${userGroupId}:`, error);
		}

		// Fallback to ID if fetch fails
		return userGroupId;
	}

	/**
	 * Add a new user group override.
	 */
	private async _addUserGroup(): Promise<void> {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		// 1. Open user group picker
		const pickerResult = await modalManager.open(this, UMB_USER_GROUP_PICKER_MODAL, {
			data: {
				multiple: false,
			},
		});

		const pickerValue = await pickerResult?.onSubmit();
		if (!pickerValue || !pickerValue.selection || pickerValue.selection.length === 0) {
			return;
		}

		const userGroupId = pickerValue.selection[0];

		// Check if already configured
		if (this.value[userGroupId]) {
			// TODO: Show notification that user group is already configured
			return;
		}

		// 2. Get user group name
		const userGroupName = await this._getUserGroupName(userGroupId);

		// 3. Open editor modal
		const modalData = this.config.editorModal.createData(userGroupId, userGroupName);
		const modal = modalManager.open(this, this.config.editorModal.token, {
			data: modalData,
		});

		const modalResult = await modal.onSubmit();
		if (!modalResult) {
			return;
		}

		// 4. Extract settings and update value
		const settings = this.config.editorModal.extractValue(modalResult);
		this.value = { ...this.value, [userGroupId]: settings };
		this._dispatchChangeEvent();
	}

	/**
	 * Edit an existing user group override.
	 */
	private async _editUserGroup(userGroupId: string): Promise<void> {
		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const userGroupName = await this._getUserGroupName(userGroupId);
		const existingSettings = this.value[userGroupId];

		const modalData = this.config.editorModal.createData(userGroupId, userGroupName, existingSettings);
		const modal = modalManager.open(this, this.config.editorModal.token, {
			data: modalData,
		});

		const modalResult = await modal.onSubmit();
		if (!modalResult) {
			return;
		}

		const settings = this.config.editorModal.extractValue(modalResult);
		this.value = { ...this.value, [userGroupId]: settings };
		this._dispatchChangeEvent();
	}

	/**
	 * Remove a user group override.
	 */
	private _removeUserGroup(userGroupId: string): void {
		const { [userGroupId]: _, ...remaining } = this.value;
		this.value = remaining;
		this._dispatchChangeEvent();
	}

	/**
	 * Dispatch change event.
	 */
	private _dispatchChangeEvent(): void {
		this.dispatchEvent(new UmbChangeEvent());
	}

	override render() {
		return html`
			<uui-box headline=${this._labels.headline}>
				${when(
					this._hasUserGroups,
					() => html`
						<uui-ref-list>
							${repeat(
								this._userGroupEntries,
								([id]) => id,
								([id, settings]) => html`
									<uui-ref-node
										name=${this._userGroupNames.get(id) ?? id}
										detail=${this.config.display.renderSummary(settings)}>
										<umb-icon slot="icon" name="icon-users"></umb-icon>
										${when(this.config.display.renderTags, () => this.config.display.renderTags!(settings))}
										${when(
											!this.readonly,
											() => html`
												<uui-action-bar slot="actions">
													<uui-button
														label=${this.localize.term("general_edit")}
														@click=${() => this._editUserGroup(id)}>
														<uui-icon name="icon-edit"></uui-icon>
													</uui-button>
													<uui-button
														label=${this.localize.term("general_remove")}
														@click=${() => this._removeUserGroup(id)}>
														<uui-icon name="icon-trash"></uui-icon>
													</uui-button>
												</uui-action-bar>
											`
										)}
									</uui-ref-node>
								`
							)}
						</uui-ref-list>
					`,
					() => html`<p>${this._labels.noGroupsMessage}</p>`
				)}
				${when(
					!this.readonly,
					() => html`
						<uui-button
							look="placeholder"
							@click=${this._addUserGroup}
							label=${this._labels.addButton}>
							<uui-icon name="icon-add"></uui-icon>
							${this._labels.addButton}
						</uui-button>
					`
				)}
			</uui-box>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
			}

			uui-box {
				margin-bottom: var(--uui-size-space-4);
			}

			p {
				margin: var(--uui-size-space-4) 0;
				color: var(--uui-color-text-alt);
			}

			uui-button[look="placeholder"] {
				width: 100%;
				margin-top: var(--uui-size-space-4);
			}
		`,
	];
}

declare global {
	interface HTMLElementTagNameMap {
		"uai-user-group-settings-list": UaiUserGroupSettingsListElement<any>;
	}
}
