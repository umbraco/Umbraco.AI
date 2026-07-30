import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { UAI_COPILOT_CONTEXT, type UaiCopilotContext } from "../../copilot.context.js";

// Workspaces we know support copilot. (Kept explicit rather than inferred so the FAB never appears in a
// workspace copilot can't actually work against.)
const SUPPORTED_WORKSPACE_ALIASES = ["Umb.Workspace.Document", "Umb.Workspace.Media"];

// Debounce before sliding the FAB out. Moving between two supported workspaces tears down one injector
// and creates the next with a brief gap; debouncing means the shared button stays put (no out/in
// "double animation") — it only slides out if nothing re-acquires within this window.
const HIDE_DEBOUNCE_MS = 250;
const REMOVE_FALLBACK_MS = 400;

/**
 * Single shared floating button, ref-counted across per-workspace injectors.
 *
 * Each supported workspace's injector `acquire()`s on enter and `release()`s on leave. The button is
 * created once and persists while at least one supported workspace is active — so moving between two
 * supported workspaces (Media ⇄ Content) keeps the same button (no slide-out/slide-in), and it only
 * animates out when you leave to a non-supported workspace (or none). Also means split-view (two
 * supported workspaces) shows exactly one button.
 */
const fabManager = {
    count: 0,
    fab: undefined as HTMLElement | undefined,
    onToggle: undefined as (() => void) | undefined,
    hideTimer: 0,
    removeTimer: 0,

    acquire(onToggle: () => void) {
        this.onToggle = onToggle;
        this.count++;
        window.clearTimeout(this.hideTimer);
        window.clearTimeout(this.removeTimer);
        this.ensureFab();
        // Force a reflow in the closed state, then open → slide/fade in (or reverse a pending slide-out).
        const fab = this.fab!;
        void fab.offsetWidth;
        fab.setAttribute("open", "");
    },

    release() {
        this.count = Math.max(0, this.count - 1);
        if (this.count > 0) return;
        window.clearTimeout(this.hideTimer);
        this.hideTimer = window.setTimeout(() => this.hide(), HIDE_DEBOUNCE_MS);
    },

    ensureFab() {
        if (this.fab?.isConnected) return;
        const fab = document.createElement("uai-copilot-fab");
        fab.addEventListener("click", () => this.onToggle?.());
        // Co-locate with the sidebar so z-index ordering between them is well-defined.
        const sidebar = deepQuerySelector(document.body, "uai-copilot-sidebar");
        const parent = (sidebar?.parentNode as ParentNode | null) ?? document.body;
        parent.appendChild(fab);
        this.fab = fab;
    },

    hide() {
        const fab = this.fab;
        if (!fab || this.count > 0) return;
        fab.removeAttribute("open");
        window.clearTimeout(this.removeTimer);
        const remove = () => {
            if (this.count > 0) return; // re-acquired mid-slide-out → keep it
            fab.remove();
            if (this.fab === fab) this.fab = undefined;
        };
        fab.addEventListener("transitionend", remove, { once: true });
        this.removeTimer = window.setTimeout(remove, REMOVE_FALLBACK_MS);
    },
};

/**
 * Per-workspace injector, registered as a `workspaceContext` — created once per workspace, destroyed
 * when that workspace closes. It acquires/releases the shared FAB when the workspace is one copilot
 * supports (Document/Media), so the button is contextual.
 */
export class UaiCopilotFabInjector extends UmbControllerBase {
    #copilot?: UaiCopilotContext;
    #acquired = false;

    constructor(host: UmbControllerHost) {
        super(host);

        this.consumeContext(UMB_WORKSPACE_CONTEXT, (ctx) => {
            const supported = !!ctx && SUPPORTED_WORKSPACE_ALIASES.includes(ctx.workspaceAlias);
            if (supported) this.#acquire();
            else this.#release();
        });

        this.consumeContext(UAI_COPILOT_CONTEXT, (ctx) => {
            this.#copilot = ctx ?? undefined;
        });
    }

    #acquire() {
        if (this.#acquired) return;
        this.#acquired = true;
        fabManager.acquire(() => this.#copilot?.toggle());
    }

    #release() {
        if (!this.#acquired) return;
        this.#acquired = false;
        fabManager.release();
    }

    override destroy(): void {
        this.#release();
        super.destroy();
    }
}

/** Descend through open shadow roots (breadth-first) to find the first matching element. */
function deepQuerySelector(root: ParentNode, selector: string): HTMLElement | undefined {
    const direct = (root as Element).shadowRoot?.querySelector(selector) ?? root.querySelector?.(selector);
    if (direct) return direct as HTMLElement;

    const start = (root as Element).shadowRoot ?? root;
    const queue: Element[] = Array.from(start.querySelectorAll("*"));
    while (queue.length) {
        const el = queue.shift()!;
        const sr = el.shadowRoot;
        if (sr) {
            const hit = sr.querySelector(selector);
            if (hit) return hit as HTMLElement;
            queue.push(...Array.from(sr.querySelectorAll("*")));
        }
    }
    return undefined;
}

export { UaiCopilotFabInjector as api };
export default UaiCopilotFabInjector;
