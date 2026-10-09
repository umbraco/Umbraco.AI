import { customElement, property, state, css, html, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiStarterPromptEntry } from "../context.js";
import { getStarterPromptsPage, hasMultipleStarterPromptPages } from "../services/starter-prompts.controller.js";

/**
 * Dumb chip list for starter prompts: entries in, a `select` event out. Renders nothing when there are
 * no entries, so an agent with none leaves the empty state exactly as it is today.
 *
 * Windowing is purely presentational, local state: the controller hands over the full merged list (a
 * single selected agent's own list never exceeds the window), and this element shows one page of it at
 * a time plus a rotate control once there's more than one page.
 *
 * @fires select - Dispatched when a chip is clicked, detail: the {@link UaiStarterPromptEntry} clicked.
 */
@customElement("uai-starter-prompts")
export class UaiStarterPromptsElement extends UmbLitElement {
    @property({ type: Array })
    entries: UaiStarterPromptEntry[] = [];

    @state()
    private _page = 0;

    #handleClick(entry: UaiStarterPromptEntry) {
        this.dispatchEvent(
            new CustomEvent<UaiStarterPromptEntry>("select", {
                detail: entry,
                bubbles: true,
                composed: true,
            }),
        );
    }

    #handleRotate() {
        this._page += 1;
    }

    override willUpdate(changedProperties: Map<string, unknown>) {
        // A fresh entries list (agent switched, catalog changed) always starts back on page one --
        // otherwise a shorter list could strand the user mid-rotation.
        if (changedProperties.has("entries")) {
            this._page = 0;
        }
    }

    override render() {
        if (this.entries.length === 0) return nothing;

        const visible = getStarterPromptsPage(this.entries, this._page);
        const showRotate = hasMultipleStarterPromptPages(this.entries.length);

        return html`
            <div class="starter-prompts">
                <div class="chips">
                    ${repeat(
                        visible,
                        (entry, index) => `${entry.agentId ?? ""}:${index}:${entry.prompt}`,
                        (entry) => html`
                            <button type="button" class="starter-chip" @click=${() => this.#handleClick(entry)}>
                                ${entry.agentName
                                    ? html`<span class="agent-attribution">
                                          <uui-icon name="icon-bot"></uui-icon>
                                          ${entry.agentName}
                                      </span>`
                                    : nothing}
                                <span class="prompt">${entry.display}</span>
                            </button>
                        `,
                    )}
                </div>
                ${showRotate
                    ? html`
                          <uui-button compact look="secondary" @click=${this.#handleRotate} title="Show more suggestions">
                              <uui-icon name="icon-sync"></uui-icon>
                          </uui-button>
                      `
                    : nothing}
            </div>
        `;
    }

    static override styles = css`
        /*
         * The host is the query container, so the chips respond to the space the surface actually gives
         * them rather than to the viewport: full width in the narrow Copilot sidebar, hugging their own
         * text once there's room (Copilot Workspace caps its column at 860px).
         *
         * The explicit width matters: the host is a flex item of a centred column (.empty-state uses
         * align-items: center), so it would otherwise be sized shrink-to-fit -- and inline-size
         * containment makes a shrink-to-fit box report no intrinsic width at all, collapsing the chips
         * into one-word columns.
         */
        :host {
            display: block;
            width: 100%;
            align-self: stretch;
            container-type: inline-size;
        }

        /* Column so the rotate control always sits on its own row under the chips, never beside them. */
        .starter-prompts {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--uui-size-space-4);
            margin-top: var(--uui-size-space-6);
        }

        .chips {
            display: flex;
            flex-wrap: wrap;
            justify-content: center;
            align-items: stretch;
            gap: var(--uui-size-space-3);
            width: 100%;
        }

        .starter-chip {
            display: flex;
            flex-direction: column;
            align-items: flex-start;
            gap: var(--uui-size-space-1);
            flex: 1 1 100%;
            font: inherit;
            color: var(--uui-color-text);
            background: var(--uui-color-surface-alt);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            padding: var(--uui-size-space-3) var(--uui-size-space-4);
            cursor: pointer;
            text-align: left;
            transition:
                background-color 0.15s ease,
                border-color 0.15s ease;
        }

        @container (min-width: 560px) {
            .starter-chip {
                flex: 0 1 auto;
            }
        }

        .starter-chip:hover,
        .starter-chip:focus-visible {
            background: var(--uui-color-surface);
            border-color: var(--uui-color-focus);
        }

        /* Same treatment as the agent attribution above an assistant message, for one visual language. */
        .agent-attribution {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-1);
            font-size: 0.75rem;
            color: var(--uui-color-text-alt);
            opacity: 0.8;
        }

        .agent-attribution uui-icon {
            font-size: 0.875rem;
        }
    `;
}

export default UaiStarterPromptsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-starter-prompts": UaiStarterPromptsElement;
    }
}
