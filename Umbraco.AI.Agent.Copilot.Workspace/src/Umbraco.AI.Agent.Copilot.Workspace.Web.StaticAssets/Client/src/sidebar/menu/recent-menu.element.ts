import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UaiConversationListMenuElementBase } from "./conversation-list-menu.element.js";

/**
 * Menu element for the Recent sidebar group. Renders the project-less conversations slice of the shared
 * sidebar context as a flat, most-recent-first list — see {@link UaiConversationListMenuElementBase}.
 */
@customElement("uai-copilot-workspace-recent-menu")
export class UaiCopilotWorkspaceRecentMenuElement extends UaiConversationListMenuElementBase {
    protected override readonly slice = "recent" as const;
}

export default UaiCopilotWorkspaceRecentMenuElement;
