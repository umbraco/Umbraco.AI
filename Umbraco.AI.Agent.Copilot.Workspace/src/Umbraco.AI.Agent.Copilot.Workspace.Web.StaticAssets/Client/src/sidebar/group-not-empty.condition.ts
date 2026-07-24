import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "./sidebar.context.js";

export const UAI_SIDEBAR_GROUP_NOT_EMPTY_CONDITION = "Uai.CopilotWorkspace.Condition.SidebarGroupNotEmpty";

export type UaiSidebarGroup = "pinned" | "projects" | "recent";

export interface UaiSidebarGroupNotEmptyConditionConfig extends UmbConditionConfigBase {
    /** Which slice of the shared sidebar model gates this group. */
    match: UaiSidebarGroup;
}

/**
 * Hides a sidebar group (Pinned / Projects / Recent) when its slice of the shared sidebar model is
 * empty, so we don't show a bare heading. Fail-open: permitted defaults to true, so a group is never
 * hidden if the context hasn't resolved yet.
 */
export class UaiSidebarGroupNotEmptyCondition
    extends UmbConditionBase<UaiSidebarGroupNotEmptyConditionConfig>
    implements UmbExtensionCondition
{
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiSidebarGroupNotEmptyConditionConfig>) {
        super(host, args);
        this.permitted = true;
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            if (!context) return;
            const slice =
                this.config.match === "pinned"
                    ? context.pinned
                    : this.config.match === "projects"
                      ? context.projects
                      : context.recent;
            this.observe(slice, (items) => {
                this.permitted = (items?.length ?? 0) > 0;
            });
        });
    }
}

export { UaiSidebarGroupNotEmptyCondition as api };
