import { customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Presentational floating action button for the copilot — a fixed bottom-right overlay.
 *
 * Intentionally "dumb": it is mounted once by the sidebar entry point, and UaiCopilotFabController
 * listens for `click`, toggles its `visible` attribute, and mirrors the sidebar's open state onto
 * `expanded`. It slides/fades in when `visible` is set and slides back out when it is removed. Uses
 * the "sparkles" AI glyph (inline SVG, `currentColor`); there is no built-in backoffice sparkles
 * icon, so it is inlined here rather than resolved through the icon registry.
 *
 * `shadowRootOptions.delegatesFocus` lets the controller call `fab.focus()` (on the host) to move
 * focus onto the inner button when the sidebar closes.
 */
@customElement("uai-copilot-fab")
export class UaiCopilotFabElement extends UmbLitElement {
    static override readonly shadowRootOptions = {
        ...UmbLitElement.shadowRootOptions,
        delegatesFocus: true,
    };

    /** Reflects the copilot sidebar's open state — drives `aria-expanded` and the action label. */
    @property({ type: Boolean, reflect: true })
    expanded = false;

    override render() {
        const label = this.localize.term(this.expanded ? "uaiCopilot_closeLabel" : "uaiCopilot_openLabel");
        return html`
            <button
                type="button"
                class="fab"
                title=${this.localize.term("uaiCopilot_name")}
                aria-label=${label}
                aria-expanded=${this.expanded}>
                <svg
                    class="fab-icon"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    aria-hidden="true">
                    <path
                        d="M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z"></path>
                    <path d="M20 3v4"></path>
                    <path d="M22 5h-4"></path>
                    <path d="M4 17v2"></path>
                    <path d="M5 18H3"></path>
                </svg>
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
               stacking context as the sidebar (the sidebar entry point mounts both). */
            z-index: 999;
            /* Off-screen (slid out to the right) until the controller sets [visible]. visibility
               hidden (delayed until the slide-out finishes) takes the button out of the tab order and
               the accessibility tree while hidden — otherwise a keyboard/screen-reader user could reach
               an invisible button in an unsupported workspace. */
            transform: translateX(calc(100% + 24px));
            opacity: 0;
            visibility: hidden;
            pointer-events: none;
            transition: transform 220ms ease, opacity 220ms ease, visibility 0s linear 220ms;
        }
        :host([visible]) {
            transform: translateX(0);
            opacity: 1;
            visibility: visible;
            pointer-events: auto;
            transition: transform 220ms ease, opacity 220ms ease, visibility 0s linear 0s;
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
        .fab-icon {
            width: 22px;
            height: 22px;
        }
    `;
}

export default UaiCopilotFabElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-fab": UaiCopilotFabElement;
    }
}
