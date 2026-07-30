import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Presentational floating action button for the copilot — a fixed bottom-right overlay.
 *
 * Intentionally "dumb": the injector (UaiCopilotFabInjector) mounts/unmounts it and listens for
 * `click`. It slides/fades in when the `open` attribute is set (the injector sets it after mounting,
 * and removes it before un-mounting so it slides back out). Uses `<uui-icon name="icon-chat">` for
 * icon consistency with the rest of the backoffice (the FAB is mounted inside the app tree, so the
 * icon registry resolves).
 */
@customElement("uai-copilot-fab")
export class UaiCopilotFabElement extends UmbLitElement {
    override render() {
        return html`
            <button type="button" class="fab" title="AI Assistant" aria-label="Open AI Assistant">
                <uui-icon name="icon-chat"></uui-icon>
            </button>`;
    }

    static override styles = css`
        :host {
            display: block;
            position: fixed;
            /* Fixed gap from the right edge — a middle value that reads well whether or not a scrollbar
               is present (a normal chat-widget FAB may briefly overlap a transient scrollbar). */
            right: 24px;
            /* Sit above the workspace footer (Save/publish) bar rather than overlapping it. */
            bottom: calc(var(--umb-footer-layout-height, 70px) + var(--uui-size-space-5, 18px));
            /* Just BELOW the copilot sidebar (z-index 1000) so the sidebar occludes the FAB while open
               and reveals it as the sidebar animates out. Relies on the FAB being mounted in the same
               stacking context as the sidebar (see the injector). */
            z-index: 999;
            /* Off-screen (slid out to the right) until the injector sets [open]. */
            transform: translateX(calc(100% + 24px));
            opacity: 0;
            pointer-events: none;
            transition: transform 220ms ease, opacity 220ms ease;
        }
        :host([open]) {
            transform: translateX(0);
            opacity: 1;
            pointer-events: auto;
        }
        @media (prefers-reduced-motion: reduce) {
            :host {
                transition: none;
            }
        }
        .fab {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 52px;
            height: 52px;
            padding: 0;
            border: none;
            border-radius: 50%;
            cursor: pointer;
            color: var(--uui-color-surface, #fff);
            background: var(--uui-color-default, #3544b1);
            box-shadow: var(--uui-shadow-depth-3, 0 6px 16px rgba(0, 0, 0, 0.25));
            transition: transform 120ms ease, background 120ms ease;
        }
        .fab:hover {
            background: var(--uui-color-default-emphasis, #283991);
            transform: translateY(-1px);
        }
        uui-icon {
            font-size: 22px;
        }
    `;
}

export default UaiCopilotFabElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-fab": UaiCopilotFabElement;
    }
}
