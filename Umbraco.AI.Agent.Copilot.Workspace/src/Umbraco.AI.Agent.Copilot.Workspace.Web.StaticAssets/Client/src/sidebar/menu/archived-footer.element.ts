import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "../sidebar.context.js";
import type { UaiArchivedConversation } from "../../conversation/grouping.js";
import "../../conversation/archived-tree-item.element.js";

const STORAGE_EXPANDED = "uai-cw-archived-expanded";

/**
 * The Archived (recycle-bin) node — a single collapsible `uui-menu-item` anchored to the foot of the
 * sidebar. Deliberately subtle: it renders nothing at all until there is at least one archived
 * conversation, so it costs no space in the common case. Expanding it reveals the archived
 * conversations as a flat child list (project shown as a chip on each), mirroring the CMS recycle bin.
 * Expansion state is persisted, and the node auto-opens while one of its children is the active route.
 */
@customElement("uai-copilot-workspace-archived-footer")
export class UaiCopilotWorkspaceArchivedFooterElement extends UmbLitElement {
    @state() private _items: UaiArchivedConversation[] = [];
    @state() private _activePath?: string;

    #expanded = readExpanded();

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.observe(context?.archived, (items) => (this._items = items ?? []));
            this.observe(context?.activePath, (path) => (this._activePath = path));
        });
    }

    #hasActiveChild(): boolean {
        const path = this._activePath;
        if (!path) return false;
        return this._items.some((i) => path.includes(i.conversation.id));
    }

    #toggle = (event: Event) => {
        event.preventDefault();
        event.stopPropagation();
        this.#expanded = !this.#expanded;
        writeExpanded(this.#expanded);
        this.requestUpdate();
    };

    override render() {
        if (this._items.length === 0) return nothing;
        const open = this.#expanded || this.#hasActiveChild();
        const label = this.localize.term("uaiCopilotWorkspace_groupArchived");

        return html`
            <uui-menu-item
                label=${label}
                .hasChildren=${true}
                .showChildren=${open}
                @show-children=${this.#toggle}
                @hide-children=${this.#toggle}
            >
                <uui-icon slot="icon" name="icon-box"></uui-icon>
                ${open
                    ? repeat(
                          this._items,
                          (i) => i.conversation.id,
                          (i) => html`
                              <uai-copilot-workspace-archived-tree-item
                                  .item=${i}
                                  .activePath=${this._activePath}
                              ></uai-copilot-workspace-archived-tree-item>
                          `,
                      )
                    : nothing}
            </uui-menu-item>
            <span class="count"><uui-tag look="secondary">${this._items.length}</uui-tag></span>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                position: relative;
            }
            /* Right-aligned count pinned to the label row. The row is min-height:--uui-size-12 and the
               caret sits on the left, so the right edge is free; matching that height centres the badge
               vertically without a hardcoded offset. pointer-events:none keeps the row fully clickable. */
            .count {
                position: absolute;
                top: 0;
                right: var(--uui-size-space-4);
                height: var(--uui-size-12);
                display: flex;
                align-items: center;
                pointer-events: none;
            }
            /* A standard uui-tag, made compact via its own custom properties (rather than reformatting it
               by hand) so it stays on-brand with the rest of the backoffice. */
            .count uui-tag {
                --uui-tag-font-size: var(--uui-type-small-size);
                --uui-tag-padding: 2px var(--uui-size-space-2);
            }
        `,
    ];
}

function readExpanded(): boolean {
    try {
        return localStorage.getItem(STORAGE_EXPANDED) === "true";
    } catch {
        return false;
    }
}

function writeExpanded(value: boolean): void {
    try {
        localStorage.setItem(STORAGE_EXPANDED, String(value));
    } catch {
        /* storage unavailable — in-session only */
    }
}

export default UaiCopilotWorkspaceArchivedFooterElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-archived-footer": UaiCopilotWorkspaceArchivedFooterElement;
    }
}
