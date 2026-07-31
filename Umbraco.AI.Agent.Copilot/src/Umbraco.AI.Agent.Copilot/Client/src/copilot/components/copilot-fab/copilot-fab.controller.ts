import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { map, distinctUntilChanged } from "@umbraco-cms/backoffice/external/rxjs";
import type { UaiCopilotContext } from "../../copilot.context.js";

// When leaving a supported workspace the FAB hides after this delay. Only the hide edge is delayed
// (show is immediate): moving between two supported workspaces briefly empties the detection list as
// one workspace tears down before the next registers, and this window lets the new detection arrive
// and cancel the pending hide — so the button stays put rather than flickering out and back in.
const HIDE_DELAY_MS = 200;

/**
 * Owns the single copilot floating action button's visibility.
 *
 * Drives the button purely from `detectedEntities$` — the entities copilot can act on in the
 * current workspace(s). Non-empty ⇒ visible. This derives "supported workspace" from the
 * entity-adapter registry (Document/Media/Block, plus any third-party adapter) rather than a
 * hard-coded alias list, and aggregates split-view panes for free (any supported pane ⇒ visible).
 * New/unsaved entities are covered too, since adapters detect them before they have a unique.
 *
 * The FAB element is mounted once by the sidebar entry point and never unmounted; this controller
 * only toggles its `visible` attribute. Clicking it toggles the copilot sidebar.
 */
export class UaiCopilotFabController extends UmbControllerBase {
    #hideTimer = 0;

    constructor(host: UmbControllerHost, copilot: UaiCopilotContext, fab: HTMLElement) {
        super(host);

        fab.addEventListener("click", () => copilot.toggle());

        this.observe(
            copilot.detectedEntities$.pipe(
                map((entities) => entities.length > 0),
                distinctUntilChanged(),
            ),
            (supported) => {
                window.clearTimeout(this.#hideTimer);
                if (supported) {
                    // Show immediately (and cancel any pending hide from a just-left workspace).
                    fab.toggleAttribute("visible", true);
                } else {
                    // Delay the hide so supported⇄supported hops don't flicker the button out and in.
                    this.#hideTimer = window.setTimeout(() => fab.toggleAttribute("visible", false), HIDE_DELAY_MS);
                }
            },
        );
    }

    override destroy(): void {
        window.clearTimeout(this.#hideTimer);
        super.destroy();
    }
}

export default UaiCopilotFabController;
