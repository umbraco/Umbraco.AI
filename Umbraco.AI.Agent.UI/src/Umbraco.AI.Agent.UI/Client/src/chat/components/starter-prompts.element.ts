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
                ${repeat(
                    visible,
                    (entry, index) => `${entry.agentId ?? ""}:${index}:${entry.prompt}`,
                    (entry) => html`
                        <button type="button" class="starter-chip" @click=${() => this.#handleClick(entry)}>
                            ${entry.display}
                            ${entry.agentName ? html`<uui-tag look="secondary">${entry.agentName}</uui-tag>` : nothing}
                        </button>
                    `,
                )}
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
        :host {
            display: block;
        }

        .starter-prompts {
            display: flex;
            flex-wrap: wrap;
            justify-content: center;
            gap: var(--uui-size-space-3);
            margin-top: var(--uui-size-space-5);
        }

        .starter-chip {
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

        .starter-chip:hover,
        .starter-chip:focus-visible {
            background: var(--uui-color-surface);
            border-color: var(--uui-color-focus);
        }

        .starter-chip uui-tag {
            margin-left: var(--uui-size-space-3);
            vertical-align: middle;
        }
    `;
}

export default UaiStarterPromptsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-starter-prompts": UaiStarterPromptsElement;
    }
}
