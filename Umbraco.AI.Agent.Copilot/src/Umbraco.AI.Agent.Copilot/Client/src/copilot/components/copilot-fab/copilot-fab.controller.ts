import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiCopilotContext } from "../../copilot.context.js";

/**
 * Owns the single copilot floating action button's visibility.
 *
 * Drives the button purely from the copilot's `isSupportedWorkspace$` signal — true when copilot has
 * detected an entity it can act on in the current workspace(s). This derives "supported workspace"
 * from the entity-adapter registry (Document/Media/Block, plus any third-party adapter) rather than a
 * hard-coded alias list, aggregates split-view panes for free, and debounces the hide edge so
 * supported⇄supported hops don't flicker the button (see UaiCopilotContext).
 *
 * The FAB element is mounted once by the sidebar entry point and never unmounted; this controller
 * only toggles its `visible` attribute. Clicking it toggles the copilot sidebar.
 */
export class UaiCopilotFabController extends UmbControllerBase {
    constructor(host: UmbControllerHost, copilot: UaiCopilotContext, fab: HTMLElement) {
        super(host);

        fab.addEventListener("click", () => copilot.toggle());

        this.observe(copilot.isSupportedWorkspace$, (supported) => {
            fab.toggleAttribute("visible", supported);
        });
    }
}

export default UaiCopilotFabController;
