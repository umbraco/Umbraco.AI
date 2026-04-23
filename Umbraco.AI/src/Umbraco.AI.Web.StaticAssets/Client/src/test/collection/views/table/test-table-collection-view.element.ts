import { html, css, customElement, state } from "@umbraco-cms/backoffice/external/lit";
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
import type { UaiTestItemModel } from "../../../types.js";
import { UAI_TEST_ICON } from "../../../constants.js";
import { UAI_EDIT_TEST_WORKSPACE_PATH_PATTERN } from "../../../workspace/paths.js";
import "./test-run-inline-button.element.js";

/**
 * Table view for the Test collection.
 */
@customElement("uai-test-table-collection-view")
export class UaiTestTableCollectionViewElement extends UmbLitElement {
    @state()
    private _tableConfig: UmbTableConfig = {
        allowSelection: true,
    };

    @state()
    private _items: UmbTableItem[] = [];

    @state()
    private _selection: Array<string> = [];

    #collectionContext?: UmbDefaultCollectionContext<UaiTestItemModel>;

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Alias", alias: "alias" },
        { name: "Feature", alias: "testFeature" },
        { name: "Run Count", alias: "runCount" },
        { name: "Modified", alias: "dateModified" },
        { name: "", alias: "entityActions", align: "right" },
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
            (items) => this.#createTableItems(items as UaiTestItemModel[]),
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

    #createTableItems(items: UaiTestItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_TEST_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href=${UAI_EDIT_TEST_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}
                        >${item.name}</a
                    >`,
                },
                {
                    columnAlias: "alias",
                    value: html`<uui-tag color="primary" look="secondary">${item.alias}</uui-tag>`,
                },
                {
                    columnAlias: "testFeature",
                    value: item.testFeatureId,
                },
                {
                    columnAlias: "runCount",
                    value: item.runCount,
                },
                {
                    columnAlias: "dateModified",
                    value: item.dateModified ? formatDateTime(item.dateModified) : "-",
                },
                {
                    columnAlias: "entityActions",
                    value: html`<div class="row-actions">
                        <uai-test-run-inline-button .unique=${item.unique}></uai-test-run-inline-button>
                        <umb-entity-actions-table-column-view
                            .value=${{ entityType: item.entityType, unique: item.unique }}
                        ></umb-entity-actions-table-column-view>
                    </div>`,
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

    static styles = [
        UmbTextStyles,
        css`
            uui-tag {
                white-space: nowrap;
            }

            .row-actions {
                display: inline-flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                justify-content: flex-end;
            }
        `,
    ];
}

export default UaiTestTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-table-collection-view": UaiTestTableCollectionViewElement;
    }
}
