import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type {
    UmbTableColumn,
    UmbTableItem,
    UmbTableConfig,
    UmbTableSelectedEvent,
    UmbTableDeselectedEvent,
    UmbTableElement,
} from "@umbraco-cms/backoffice/components";
import type { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { formatDateTime } from "../../../../core/index.js";
import type { UaiContextItemModel } from "../../../types.js";
import { UAI_CONTEXT_ICON } from "../../../constants.js";
import { UAI_EDIT_CONTEXT_WORKSPACE_PATH_PATTERN } from "../../../workspace/context/paths.js";

/**
 * Table view for the Context collection.
 */
@customElement("uai-context-table-collection-view")
export class UaiContextTableCollectionViewElement extends UmbLitElement {
    #collectionContext?: UmbDefaultCollectionContext<UaiContextItemModel>;

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
        { name: "Resources", alias: "resourceCount" },
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
            (items) => this.#createTableItems(items as UaiContextItemModel[]),
            "umbCollectionItemsObserver",
        );

        this.observe(
            this.#collectionContext.selection.selection,
            (selection) => {
                this._selection = selection as string[];
            },
            "umbCollectionSelectionObserver",
        );
    }

    #createTableItems(items: UaiContextItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_CONTEXT_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href=${UAI_EDIT_CONTEXT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}
                        >${item.name}</a
                    >`,
                },
                {
                    columnAlias: "alias",
                    value: html`<uui-tag color="primary" look="secondary">${item.alias}</uui-tag>`,
                },
                {
                    columnAlias: "resourceCount",
                    value: item.resourceCount.toString(),
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
            @deselected=${this.#handleDeselect}
        ></umb-table>`;
    }

    static styles = [UmbTextStyles];
}

export default UaiContextTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-context-table-collection-view": UaiContextTableCollectionViewElement;
    }
}
