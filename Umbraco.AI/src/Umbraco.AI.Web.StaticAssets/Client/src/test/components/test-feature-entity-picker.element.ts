import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { umbExtensionsRegistry, createExtensionApiByAlias } from "@umbraco-cms/backoffice/extension-registry";
import { UAI_ITEM_PICKER_MODAL } from "../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../core/modals/item-picker/types.js";
import type { ManifestUaiTestFeatureEntityRepository } from "../extensions/uai-test-feature-entity-repository.extension.js";
import type {
	UaiTestFeatureEntityData,
	UaiTestFeatureEntityRepositoryApi,
} from "../test-feature-entity-repository.js";

const elementName = "uai-test-feature-entity-picker";

/**
 * Picker for selecting test feature entities.
 * Dynamically discovers available entities based on the test feature type.
 *
 * @fires change - Fires when the selection changes (UmbChangeEvent).
 *
 * @example
 * ```html
 * <uai-test-feature-entity-picker
 *   .testFeatureId=${"prompt-completion"}
 *   .value=${"my-prompt-alias"}
 *   @change=${(e) => console.log(e.target.value)}
 * ></uai-test-feature-entity-picker>
 * ```
 */
@customElement(elementName)
export class UaiTestFeatureEntityPickerElement extends UmbFormControlMixin<
	string | undefined,
	typeof UmbLitElement,
	undefined
>(UmbLitElement, undefined) {
	/**
	 * The test feature type ID to load entities for.
	 * When this changes, the picker will reload entities from the corresponding repository.
	 */
	@property({ type: String })
	public testFeatureId = "";

	/**
	 * Readonly mode - cannot change selection.
	 */
	@property({ type: Boolean, reflect: true })
	public readonly = false;

	/**
	 * The selected entity ID.
	 */
	override set value(val: string | undefined) {
		this.#setValue(val);
	}
	override get value(): string | undefined {
		return this._selectedId;
	}

	@state()
	private _selectedId?: string;

	@state()
	private _selectedItem?: UaiTestFeatureEntityData;

	@state()
	private _loading = false;

	@state()
	private _repository?: UaiTestFeatureEntityRepositoryApi;

	override updated(changedProperties: Map<PropertyKey, unknown>) {
		super.updated(changedProperties);

		// Reload repository when test feature type changes
		if (changedProperties.has("testFeatureId")) {
			this.#loadRepository();
		}
	}

	#setValue(val: string | undefined) {
		if (val === this._selectedId) return;

		this._selectedId = val;

		if (!val) {
			this._selectedItem = undefined;
			return;
		}

		this.#loadSelectedItem();
	}

	async #loadRepository() {
		this._repository = undefined;
		this._selectedItem = undefined;

		if (!this.testFeatureId) {
			return;
		}

		try {
			// Find repository manifest for this test feature type
			const manifests = umbExtensionsRegistry.getByTypeAndFilter<
				"repository",
				ManifestUaiTestFeatureEntityRepository
			>("repository", (m) => m.alias?.startsWith("Uai.Repository.TestFeatureEntity.") && m.meta?.testFeatureType === this.testFeatureId);

			if (manifests.length === 0) {
				console.warn(`No entity repository found for test feature type: ${this.testFeatureId}`);
				return;
			}

			// Use the first matching repository
			this._repository = await createExtensionApiByAlias<UaiTestFeatureEntityRepositoryApi>(
				this,
				manifests[0].alias,
			);

			// Reload selected item if we have a value
			if (this._selectedId) {
				await this.#loadSelectedItem();
			}
		} catch (error) {
			console.error("Failed to load test feature entity repository:", error);
		}
	}

	async #loadSelectedItem() {
		if (!this._selectedId || !this._repository) {
			this._selectedItem = undefined;
			return;
		}

		this._loading = true;
		try {
			this._selectedItem = await this._repository.getEntity(this._selectedId);
		} catch (error) {
			console.error("Failed to load selected entity:", error);
			this._selectedItem = undefined;
		} finally {
			this._loading = false;
		}
	}

	async #openPicker() {
		if (!this._repository) return;

		const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalManager) return;

		const modal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
			data: {
				fetchItems: () => this.#fetchAvailableEntities(),
				selectionMode: "single",
				title: this.localize.term("uaiTest_selectTargetEntity"),
				noResultsMessage: this.localize.term("uaiTest_noEntitiesAvailable"),
			},
		});

		try {
			const result = await modal.onSubmit();
			if (result?.selection?.length) {
				this.#setSelection(result.selection[0].value);
			}
		} catch {
			// Modal was cancelled
		}
	}

	async #fetchAvailableEntities(): Promise<UaiPickableItemModel[]> {
		if (!this._repository) return [];

		try {
			const entities = await this._repository.getEntities();

			return entities.map((entity) => ({
				value: entity.id,
				label: entity.name,
				description: entity.description,
				icon: entity.icon,
			}));
		} catch (error) {
			console.error("Failed to fetch entities:", error);
			return [];
		}
	}

	#setSelection(id: string) {
		this._selectedId = id;
		this.#loadSelectedItem();
		this.dispatchEvent(new UmbChangeEvent());
	}

	#onRemove() {
		this._selectedId = undefined;
		this._selectedItem = undefined;
		this.dispatchEvent(new UmbChangeEvent());
	}

	override render() {
		if (!this.testFeatureId) {
			return html`
				<div class="empty-state">
					<p>${this.localize.term("uaiTest_selectTestTypeFirst")}</p>
				</div>
			`;
		}

		if (!this._repository) {
			return html`
				<div class="empty-state">
					<p>${this.localize.term("uaiTest_noEntitiesAvailableForType")}</p>
					<p><small>${this.localize.term("uaiTest_ensurePackageInstalled")}</small></p>
				</div>
			`;
		}

		return html`
			<div class="container">
				${this.#renderSelectedItem()} ${this.#renderAddButton()}
			</div>
		`;
	}

	#renderSelectedItem() {
		if (this._loading) {
			return html`<uui-loader-bar></uui-loader-bar>`;
		}

		if (!this._selectedItem) return nothing;

		return html`
			<uui-ref-node name=${this._selectedItem.name} detail=${this._selectedItem.description || ""} readonly>
				<umb-icon slot="icon" name=${this._selectedItem.icon}></umb-icon>
				${!this.readonly
					? html`
							<uui-action-bar slot="actions">
								<uui-button
									label=${this.localize.term("general_remove")}
									@click=${(e: Event) => {
										e.stopPropagation();
										this.#onRemove();
									}}>
									<uui-icon name="icon-trash"></uui-icon>
								</uui-button>
							</uui-action-bar>
						`
					: nothing}
			</uui-ref-node>
		`;
	}

	#renderAddButton() {
		if (this.readonly || this._selectedItem) return nothing;

		return html`
			<uui-button
				id="btn-add"
				look="placeholder"
				@click=${this.#openPicker}
				label=${this.localize.term("uaiTest_selectTarget")}>
				<uui-icon name="icon-add"></uui-icon>
				${this.localize.term("uaiTest_selectTarget")}
			</uui-button>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
			}

			.container {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-3);
			}

			.empty-state {
				padding: var(--uui-size-space-4);
				text-align: center;
				color: var(--uui-color-text-alt);
				background: var(--uui-color-surface-alt);
				border-radius: var(--uui-border-radius);
			}

			.empty-state p {
				margin: 0;
			}

			.empty-state p + p {
				margin-top: var(--uui-size-space-2);
			}

			#btn-add {
				width: 100%;
			}

			uui-ref-node {
				padding: var(--uui-size-space-3);
			}

			uui-ref-node::before {
				border-radius: var(--uui-border-radius);
				border: 1px solid var(--uui-color-divider-standalone);
			}
		`,
	];
}

export default UaiTestFeatureEntityPickerElement;

declare global {
	interface HTMLElementTagNameMap {
		[elementName]: UaiTestFeatureEntityPickerElement;
	}
}
