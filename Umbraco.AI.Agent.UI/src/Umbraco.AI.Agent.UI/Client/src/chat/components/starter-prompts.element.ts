import { customElement, property, css, html, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiStarterPromptEntry } from "../context.js";

/**
 * Dumb chip list for starter prompts: entries in, a `select` event out. Renders nothing when there are
 * no entries, so an agent with none leaves the empty state exactly as it is today.
 *
 * @fires select - Dispatched when a chip is clicked, detail: the {@link UaiStarterPromptEntry} clicked.
 */
@customElement("uai-starter-prompts")
export class UaiStarterPromptsElement extends UmbLitElement {
    @property({ type: Array })
    entries: UaiStarterPromptEntry[] = [];

    #handleClick(entry: UaiStarterPromptEntry) {
        this.dispatchEvent(
            new CustomEvent<UaiStarterPromptEntry>("select", {
                detail: entry,
                bubbles: true,
                composed: true,
            }),
        );
    }

    override render() {
        if (this.entries.length === 0) return nothing;

        return html`
            <div class="starter-prompts">
                ${repeat(
                    this.entries,
                    (entry, index) => `${entry.agentId ?? ""}:${index}:${entry.prompt}`,
                    (entry) => html`
                        <button type="button" class="starter-chip" @click=${() => this.#handleClick(entry)}>
                            ${entry.display}
                        </button>
                    `,
                )}
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
    `;
}

export default UaiStarterPromptsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-starter-prompts": UaiStarterPromptsElement;
    }
}
