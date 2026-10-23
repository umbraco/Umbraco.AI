import { customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "../sidebar.context.js";
import type { SharedConversationSummaryResponseModel } from "../../conversation/types.js";
import "../../conversation/shared-with-me-tree-item.element.js";

/**
 * Menu element for the "Shared with me" sidebar group (STORIES.md US3). Not a subclass of
 * `UaiConversationListMenuElementBase` — that base is hard-typed to `ConversationResponseModel` and the
 * owner-only "pinned"/"recent" slices, whereas this group's data comes from `requestSharedWithMe()`
 * (`SharedConversationSummaryResponseModel`, a different shape entirely) — so it mirrors that base's
 * pattern directly (context consumption, active-path tracking, hide-when-empty) rather than forcing a
 * shared generic over two unrelated data shapes.
 */
@customElement("uai-copilot-workspace-shared-with-me-menu")
export class UaiCopilotWorkspaceSharedWithMeMenuElement extends UmbLitElement {
    @state() private _items: SharedConversationSummaryResponseModel[] = [];
    @state() private _activePath?: string;

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.observe(context?.sharedWithMe, (items) => (this._items = items ?? []));
            this.observe(context?.activePath, (path) => (this._activePath = path));
        });
    }

    override render() {
        if (this._items.length === 0) return nothing;
        return repeat(
            this._items,
            (c) => c.conversationId,
            (c) => html`
                <uai-copilot-workspace-shared-with-me-tree-item
                    .item=${c}
                    .activePath=${this._activePath}
                ></uai-copilot-workspace-shared-with-me-tree-item>
            `,
        );
    }
}

export default UaiCopilotWorkspaceSharedWithMeMenuElement;
