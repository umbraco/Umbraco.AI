import { css, html, customElement, property, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import "@umbraco-cms/backoffice/components";
import type { UaiStarterPrompt } from "../../types.js";

/** Also the chip window, so a single selected agent's starters always fit on one page (Phase 3). */
export const UAI_MAX_STARTER_PROMPTS = 4;

/** Checked here because `umb-input-multiple-text-string` has no `maxlength` of its own. */
export const UAI_MAX_STARTER_PROMPT_LENGTH = 200;

/**
 * Editor for an agent's starter prompts: a thin wrapper over the CMS's own repeatable-string list
 * (`umb-input-multiple-text-string`, the component behind the Repeatable Text String property
 * editor), which supplies add / remove / drag-to-reorder and the max-item-count validator.
 *
 * The wrapper exists to map `string[]` to `UaiStarterPrompt[]` and back (kept as an object rather
 * than a bare string so an optional `label` can be added later with no data migration), and to
 * enforce the 200-character cap the CMS component does not know about.
 *
 * Drafting starters from the agent's instructions is a property action
 * (`UmbracoAIAgent.PropertyAction.SuggestStarterPrompts`), not part of this element — it writes
 * straight to the workspace model through the `...` menu on the property label.
 *
 * @fires change - Fires with the updated `UaiStarterPrompt[]` once every entry is within the
 * character cap. An edit that pushes an entry over the cap is never propagated — the CMS component
 * still shows what was typed (it owns its own internal state), but the value seen by the workspace
 * stays unchanged until the author fixes it.
 */
@customElement("uai-agent-starter-prompts-editor")
export class UaiAgentStarterPromptsEditorElement extends UmbLitElement {
    @property({ type: Array })
    prompts: UaiStarterPrompt[] = [];

    @state()
    private _error: string | null = null;

    #onItemsChange(event: Event) {
        event.stopPropagation();
        const input = event.target as HTMLElement & { items?: string[] };
        const items = input.items ?? [];

        const overLong = items.some((item) => item.length > UAI_MAX_STARTER_PROMPT_LENGTH);
        if (overLong) {
            this._error = this.localize.term("uaiAgent_starterPromptTooLong", [UAI_MAX_STARTER_PROMPT_LENGTH]);
            return;
        }

        this._error = null;
        this.#dispatchChange(items.map((prompt) => ({ prompt })));
    }

    #dispatchChange(prompts: UaiStarterPrompt[]) {
        this.dispatchEvent(
            new CustomEvent<UaiStarterPrompt[]>("change", {
                detail: prompts,
                bubbles: true,
                composed: true,
            }),
        );
    }

    render() {
        return html`
            <umb-input-multiple-text-string
                .items=${this.prompts.map((p) => p.prompt)}
                max=${UAI_MAX_STARTER_PROMPTS}
                @change=${this.#onItemsChange}
            ></umb-input-multiple-text-string>
            ${this._error ? html`<p class="error">${this._error}</p>` : nothing}
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .error {
                color: var(--uui-color-danger-standalone, var(--uui-color-danger));
                font-size: var(--uui-type-small-size);
                margin: var(--uui-size-space-2) 0 0;
            }
        `,
    ];
}

export default UaiAgentStarterPromptsEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-starter-prompts-editor": UaiAgentStarterPromptsEditorElement;
    }
}
