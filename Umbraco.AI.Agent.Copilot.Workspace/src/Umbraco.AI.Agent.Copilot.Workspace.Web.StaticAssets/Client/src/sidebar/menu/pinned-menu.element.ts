import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UaiConversationListMenuElementBase } from "./conversation-list-menu.element.js";

/**
 * Menu element for the Pinned sidebar group (a `menu` referenced by its sectionSidebarApp). Renders the
 * pinned conversations slice of the shared sidebar context — see {@link UaiConversationListMenuElementBase}.
 */
@customElement("uai-copilot-workspace-pinned-menu")
export class UaiCopilotWorkspacePinnedMenuElement extends UaiConversationListMenuElementBase {
    protected override readonly slice = "pinned" as const;
}

export default UaiCopilotWorkspacePinnedMenuElement;
