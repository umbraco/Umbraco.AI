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
import { formatDateTime } from "@umbraco-ai/core";
import type { UaiAgentItemModel } from "../../../types.js";
import { UAI_AGENT_ICON } from "../../../constants.js";
import { UAI_EDIT_AGENT_WORKSPACE_PATH_PATTERN } from "../../../workspace/agent/paths.js";

/**
 * Table view for the Agent collection.
 */
@customElement("uai-agent-table-collection-view")
export class UaiAgentTableCollectionViewElement extends UmbLitElement {
    @state()
    private _tableConfig: UmbTableConfig = {
        allowSelection: true,
    };

    @state()
    private _items: UmbTableItem[] = [];

    @state()
    private _selection: Array<string> = [];

    #collectionContext?: UmbDefaultCollectionContext<UaiAgentItemModel>;

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Alias", alias: "alias" },
        { name: "Description", alias: "description" },
        { name: "Active", alias: "isActive" },
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
            (items) => this.#createTableItems(items as UaiAgentItemModel[]),
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

    #createTableItems(items: UaiAgentItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_AGENT_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href=${UAI_EDIT_AGENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}
                        >${item.name}</a
                    >`,
                },
                {
                    columnAlias: "alias",
                    value: html`<uui-tag color="primary" look="secondary">${item.alias}</uui-tag>`,
                },
                {
                    columnAlias: "description",
                    value: item.description ?? "-",
                },
                {
                    columnAlias: "isActive",
                    value: html`<uui-tag color=${item.isActive ? "positive" : "danger"}>
                        ${item.isActive ? "Active" : "Inactive"}
                    </uui-tag>`,
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

export default UaiAgentTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-table-collection-view": UaiAgentTableCollectionViewElement;
    }
}
