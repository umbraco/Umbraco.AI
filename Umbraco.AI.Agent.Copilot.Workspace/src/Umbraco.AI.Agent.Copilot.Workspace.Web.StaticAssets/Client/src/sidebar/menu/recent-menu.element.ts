import { customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "../sidebar.context.js";
import type { ConversationResponseModel } from "../../conversation/types.js";
import "../../section/sidebar/conversation-tree-item.element.js";

/**
 * Menu element for the Recent sidebar group. Renders the project-less conversations from the shared
 * sidebar context as a flat, most-recent-first list. Hidden when empty (group-not-empty condition).
 */
@customElement("uai-copilot-workspace-recent-menu")
export class UaiCopilotWorkspaceRecentMenuElement extends UmbLitElement {
    @state() private _recent: ConversationResponseModel[] = [];
    @state() private _activePath?: string;

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.observe(context?.recent, (recent) => (this._recent = recent ?? []));
            this.observe(context?.activePath, (path) => (this._activePath = path));
        });
    }

    override render() {
        if (this._recent.length === 0) return nothing;
        return repeat(
            this._recent,
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

export default UaiCopilotWorkspaceRecentMenuElement;
