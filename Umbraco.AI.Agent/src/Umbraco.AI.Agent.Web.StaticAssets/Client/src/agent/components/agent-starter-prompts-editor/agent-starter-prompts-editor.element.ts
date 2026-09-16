import { css, html, customElement, property, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import type { UUIButtonState } from "@umbraco-cms/backoffice/external/uui";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UAI_EMPTY_GUID } from "@umbraco-ai/core";
import "@umbraco-cms/backoffice/components";
import { AgentsService } from "../../../api/sdk.gen.js";
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
 * @fires change - Fires with the updated `UaiStarterPrompt[]` once every entry is within the
 * character cap. An edit that pushes an entry over the cap is never propagated — the CMS component
 * still shows what was typed (it owns its own internal state), but the value seen by the workspace
 * stays unchanged until the author fixes it.
 */
@customElement("uai-agent-starter-prompts-editor")
export class UaiAgentStarterPromptsEditorElement extends UmbLitElement {
    @property({ type: Array })
    prompts: UaiStarterPrompt[] = [];

    /**
     * The agent's persisted unique ID (or alias). `Suggest starters` calls the server for this
     * agent's own `Instructions`, so it has nothing to call until the agent has been saved at least
     * once — the button is disabled while this is unset or still the new-agent placeholder.
     */
    @property({ type: String })
    agentId?: string;

    /**
     * The agent's own profile, if one is set. Known locally, so checking it never needs a round trip —
     * only the "is there a default chat profile" half of the availability check does.
     */
    @property({ type: String })
    profileId?: string | null;

    @state()
    private _error: string | null = null;

    @state()
    private _hasResolvableProfile = false;

    @state()
    private _checkingAvailability = false;

    @state()
    private _suggestButtonState?: UUIButtonState;

    #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;

    constructor() {
        super();
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });
    }

    protected override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);
        if (changedProperties.has("agentId") || changedProperties.has("profileId")) {
            this.#refreshAvailability();
        }
    }

    async #refreshAvailability() {
        if (this.profileId) {
            // Known locally — no need to ask the server.
            this._hasResolvableProfile = true;
            return;
        }

        if (!this.#hasSavedAgent) {
            this._hasResolvableProfile = false;
            return;
        }

        this._checkingAvailability = true;
        const { data } = await tryExecute(
            this,
            AgentsService.getSuggestStartersAvailability({ path: { agentIdOrAlias: this.agentId! } }),
        );
        this._hasResolvableProfile = data === true;
        this._checkingAvailability = false;
    }

    get #hasSavedAgent(): boolean {
        return !!this.agentId && this.agentId !== UAI_EMPTY_GUID;
    }

    /** Reason the Suggest starters button is disabled, or undefined when it can be used. */
    get #suggestDisabledReason(): string | undefined {
        if (this._suggestButtonState === "waiting" || this._checkingAvailability) {
            return undefined; // Not a "disabled" state — the button shows its own loading state.
        }
        if (!this.#hasSavedAgent) {
            return this.localize.term("uaiAgent_suggestStartersUnsavedAgent");
        }
        if (!this._hasResolvableProfile) {
            return this.localize.term("uaiAgent_suggestStartersNoProfile");
        }
        return undefined;
    }

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

    async #onSuggestClick() {
        // Guards against a double-fire from a fast double-click — the button is also visually
        // disabled while waiting, but state changes lag a frame behind the click event.
        if (this._suggestButtonState === "waiting" || !this.#hasSavedAgent) return;

        this._suggestButtonState = "waiting";

        const { data, error } = await tryExecute(
            this,
            AgentsService.suggestStarters({ path: { agentIdOrAlias: this.agentId! } }),
        );

        if (error || !data) {
            this._suggestButtonState = "failed";
            this.#notificationContext?.peek("danger", {
                data: { message: this.localize.string("#uaiAgent_suggestStartersFailed") },
            });
            this.#resetSuggestButtonState();
            return;
        }

        this._error = null;
        this._suggestButtonState = "success";
        this.#dispatchChange(data.starters.map((prompt) => ({ prompt })));
        this.#resetSuggestButtonState();
    }

    #resetSuggestButtonState() {
        setTimeout(() => {
            this._suggestButtonState = undefined;
        }, 2000);
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
        const disabledReason = this.#suggestDisabledReason;

        return html`
            <umb-input-multiple-text-string
                .items=${this.prompts.map((p) => p.prompt)}
                max=${UAI_MAX_STARTER_PROMPTS}
                @change=${this.#onItemsChange}
            ></umb-input-multiple-text-string>
            ${this._error ? html`<p class="error">${this._error}</p>` : nothing}

            <uui-button
                id="suggest-button"
                look="secondary"
                label=${this.localize.string("#uaiAgent_suggestStarters")}
                .state=${this._suggestButtonState}
                ?disabled=${!!disabledReason}
                title=${disabledReason ?? ""}
                @click=${this.#onSuggestClick}
            >
                <uui-icon name="icon-lab"></uui-icon>
                ${this.localize.string("#uaiAgent_suggestStarters")}
            </uui-button>
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

            #suggest-button {
                margin-top: var(--uui-size-space-4);
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
