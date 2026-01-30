import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UmbTableColumn, UmbTableItem, UmbTableConfig, UmbTableSelectedEvent, UmbTableDeselectedEvent, UmbTableElement } from "@umbraco-cms/backoffice/components";
import type { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { formatDateTime } from "../../../../core/index.js";
import type { UaiProfileItemModel } from "../../../types.js";
import { UAI_PROFILE_ICON } from "../../../constants.js";
import { UAI_EDIT_PROFILE_WORKSPACE_PATH_PATTERN } from "../../../workspace/profile/paths.js";

/**
 * Table view for the Profile collection.
 */
@customElement("uai-profile-table-collection-view")
export class UaiProfileTableCollectionViewElement extends UmbLitElement {
    #localize = new UmbLocalizationController(this);
    #collectionContext?: UmbDefaultCollectionContext<UaiProfileItemModel>;

    @state()
    private _tableConfig: UmbTableConfig = {
        allowSelection: true,
    };

    @state()
    private _items: UmbTableItem[] = [];

    @state()
    private _selection: Array<string> = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Alias", alias: "alias" },
        { name: "Capability", alias: "capability" },
        { name: "Model", alias: "model" },
        { name: "Modified", alias: "dateModified" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (instance) => {
            this.#collectionContext = instance;
            this.#collectionContext?.selection.setSelectable(true);
            this.#observeCollectionItems();
        });
    }

    #observeCollectionItems() {
        if (!this.#collectionContext) return;

        this.observe(
            this.#collectionContext.items,
            (items) => this.#createTableItems(items as UaiProfileItemModel[]),
            "umbCollectionItemsObserver"
        );

        this.observe(
            this.#collectionContext.selection.selection,
            (selection) => {
                this._selection = selection as string[];
            },
            "umbCollectionSelectionObserver"
        );
    }

    #getCapabilityLabel(capability: string): string {
        return this.#localize.term(`uaiCapabilities_${capability.toLowerCase()}`);
    }

    #createTableItems(items: UaiProfileItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_PROFILE_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href=${UAI_EDIT_PROFILE_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}
                        >${item.name}</a
                    >`,
                },
                { 
                    columnAlias: "alias",
                    value: html`<uui-tag color="primary" look="secondary">${item.alias}</uui-tag>`,
                },
                {
                    columnAlias: "capability",
                    value: html`<uui-tag color="primary" look="outline">${this.#getCapabilityLabel(item.capability)}</uui-tag>`,
                },
                {
                    columnAlias: "model",
                    value: item.model ? `${item.model.providerId} / ${item.model.modelId}` : "-",
                },
                {
                    columnAlias: "dateModified",
                    value: item.dateModified ? formatDateTime(item.dateModified) : "-",
                },
            ],
        }));
    }

    #handleSelect(event: UmbTableSelectedEvent) {
        event.stopPropagation();
        const table = event.target as UmbTableElement;
        this.#collectionContext?.selection.setSelection(table.selection);
    }

    #handleDeselect(event: UmbTableDeselectedEvent) {
        event.stopPropagation();
        const table = event.target as UmbTableElement;
        this.#collectionContext?.selection.setSelection(table.selection);
    }

    render() {
        return html`<umb-table
            .config=${this._tableConfig}
            .columns=${this._columns}
            .items=${this._items}
            .selection=${this._selection}
            @selected=${this.#handleSelect}
            @deselected=${this.#handleDeselect}></umb-table>`;
    }

    static styles = [UmbTextStyles];
}

export default UaiProfileTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-table-collection-view": UaiProfileTableCollectionViewElement;
    }
}
