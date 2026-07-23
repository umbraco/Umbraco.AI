import { html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "../sidebar.context.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import "../../conversation/tree-item.element.js";

/**
 * Shared base for the flat conversation-list sidebar menus (Pinned, Recent). Subclasses declare only
 * which slice of the shared sidebar model they render; the base owns context consumption, active-path
 * tracking, hide-when-empty, and the tree-item list. Each group is also hidden entirely when its slice
 * is empty (group-not-empty condition on its sidebar app) — the `nothing` guard here is belt-and-braces.
 */
export abstract class UaiConversationListMenuElementBase extends UmbLitElement {
    /** Which slice of the sidebar model this menu renders. Set by the concrete subclass. */
    protected abstract readonly slice: "pinned" | "recent";

    @state() private _items: ConversationResponseModel[] = [];
    @state() private _activePath?: string;

    constructor() {
        super();
        // Runs after subclass field initializers, so `this.slice` is set by the time it fires.
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            const slice$ = this.slice === "pinned" ? context?.pinned : context?.recent;
            this.observe(slice$, (items) => (this._items = items ?? []));
            this.observe(context?.activePath, (path) => (this._activePath = path));
        });
    }

    override render() {
        if (this._items.length === 0) return nothing;
        return repeat(
            this._items,
            (c) => c.id,
            (c) => html`
                <uai-copilot-workspace-conversation-tree-item
                    .conversation=${c}
                    .activePath=${this._activePath}
                ></uai-copilot-workspace-conversation-tree-item>
            `,
        );
    }
}
