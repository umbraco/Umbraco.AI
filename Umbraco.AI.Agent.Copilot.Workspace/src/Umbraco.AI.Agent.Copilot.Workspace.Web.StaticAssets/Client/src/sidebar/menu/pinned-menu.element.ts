import { customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "../sidebar.context.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import "../../section/sidebar/conversation-tree-item.element.js";

/**
 * Menu element for the Pinned sidebar group (a `menu` referenced by its sectionSidebarApp). Renders
 * the pinned conversations from the shared sidebar context as flat tree items. The group is hidden
 * entirely when empty (see the group-not-empty condition on its sidebar app).
 */
@customElement("uai-copilot-workspace-pinned-menu")
export class UaiCopilotWorkspacePinnedMenuElement extends UmbLitElement {
    @state() private _pinned: ConversationResponseModel[] = [];
    @state() private _activePath?: string;

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.observe(context?.pinned, (pinned) => (this._pinned = pinned ?? []));
            this.observe(context?.activePath, (path) => (this._activePath = path));
        });
    }

    override render() {
        if (this._pinned.length === 0) return nothing;
        return repeat(
            this._pinned,
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

export default UaiCopilotWorkspacePinnedMenuElement;
