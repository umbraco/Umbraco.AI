import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { debounce } from "@umbraco-cms/backoffice/utils";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, type UaiCopilotWorkspaceSidebarContext } from "./sidebar.context.js";
import { copilotWorkspaceProjectCreatePath } from "../paths.js";
import "../conversation/new-chat-button.element.js";

/**
 * Sidebar header (top sectionSidebarApp): the primary New chat split button (New chat / New chat in a
 * project), a secondary New project action, and the conversation search box — which writes the search
 * term to the shared sidebar context so all group menus filter together.
 */
@customElement("uai-copilot-workspace-sidebar-header")
export class UaiCopilotWorkspaceSidebarHeaderElement extends UmbLitElement {
    #context?: UaiCopilotWorkspaceSidebarContext;

    @state() private _search = "";

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.#context = context;
            this.observe(context?.search, (search) => (this._search = search ?? ""));
        });
    }

    #debouncedSearch = debounce((term: string) => this.#context?.setSearch(term), 250);

    #onSearchInput(event: InputEvent) {
        this.#debouncedSearch((event.target as UUIInputElement).value?.toString() ?? "");
    }

    #onNewProject() {
        window.history.pushState({}, "", copilotWorkspaceProjectCreatePath());
    }

    override render() {
        return html`
            <uai-copilot-workspace-new-chat-button></uai-copilot-workspace-new-chat-button>
            <uui-button
                class="new-project"
                look="secondary"
                label=${this.localize.term("uaiCopilotWorkspace_newProject")}
                @click=${this.#onNewProject}
            >
                <uui-icon name="icon-folder"></uui-icon>
                ${this.localize.term("uaiCopilotWorkspace_newProject")}
            </uui-button>
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
                /* Keep the create actions + search pinned while the group list scrolls. */
                position: sticky;
                top: 0;
                z-index: 2;
                background: var(--uui-color-surface);
            }
            .new-project {
                width: 100%;
            }
            uui-input {
                width: 100%;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceSidebarHeaderElement;
