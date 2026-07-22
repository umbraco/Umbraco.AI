import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { debounce } from "@umbraco-cms/backoffice/utils";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, type UaiCopilotWorkspaceSidebarContext } from "./sidebar.context.js";
import { UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE } from "../constants.js";

/**
 * Sidebar header (top sectionSidebarApp): the section title, a create (+) menu backed by the
 * section-root entity actions (New chat), and the conversation search box — which writes the search
 * term to the shared sidebar context so all group menus filter together.
 */
@customElement("uai-copilot-workspace-sidebar-header")
export class UaiCopilotWorkspaceSidebarHeaderElement extends UmbLitElement {
    #rootEntityContext = new UmbEntityContext(this);
    #context?: UaiCopilotWorkspaceSidebarContext;

    @state() private _search = "";

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.#context = context;
            this.observe(context?.search, (search) => (this._search = search ?? ""));
        });
    }

    override connectedCallback() {
        super.connectedCallback();
        // Host the section-root entity type so the create (+) menu resolves the root entity actions.
        this.#rootEntityContext.setEntityType(UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE);
        this.#rootEntityContext.setUnique(null);
    }

    #debouncedSearch = debounce((term: string) => this.#context?.setSearch(term), 250);

    #onSearchInput(event: InputEvent) {
        this.#debouncedSearch((event.target as UUIInputElement).value?.toString() ?? "");
    }

    override render() {
        return html`
            <div class="title-row">
                <span class="title">${this.localize.term("uaiCopilotWorkspace_sectionLabel")}</span>
                <umb-entity-actions-dropdown compact .label=${this.localize.term("uaiCopilotWorkspace_treeCreate")}>
                    <span slot="label" class="create-trigger" title=${this.localize.term("uaiCopilotWorkspace_treeCreate")}>
                        <uui-icon name="icon-add"></uui-icon>
                    </span>
                </umb-entity-actions-dropdown>
            </div>
            <uui-input
                type="search"
                .value=${this._search}
                placeholder=${this.localize.term("uaiCopilotWorkspace_searchPlaceholder")}
                label=${this.localize.term("uaiCopilotWorkspace_searchPlaceholder")}
                @input=${this.#onSearchInput}
            ></uui-input>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-4);
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .title-row {
                display: flex;
                align-items: center;
                justify-content: space-between;
            }
            .title {
                font-weight: 700;
            }
            .create-trigger {
                display: inline-flex;
                align-items: center;
                justify-content: center;
                cursor: pointer;
                color: var(--uui-color-interactive);
            }
            .create-trigger:hover {
                color: var(--uui-color-interactive-emphasis);
            }
            uui-input {
                width: 100%;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceSidebarHeaderElement;
