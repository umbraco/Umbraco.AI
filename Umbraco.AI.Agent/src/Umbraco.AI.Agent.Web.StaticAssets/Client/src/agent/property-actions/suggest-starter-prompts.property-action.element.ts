import { customElement, html, ifDefined, property, state, when } from "@umbraco-cms/backoffice/external/lit";
import type { UUIMenuItemEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbActionExecutedEvent } from "@umbraco-cms/backoffice/event";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { ManifestPropertyActionDefaultKind } from "@umbraco-cms/backoffice/property-action";
import type { UaiSuggestStarterPromptsPropertyAction } from "./suggest-starter-prompts.property-action.js";

/**
 * Menu item for the Suggest starters property action.
 *
 * The default property-action element would do, except it can neither explain why the action is
 * unavailable nor show that a suggestion is in flight — and the call is a live model request that
 * takes a few seconds. Both are carried here, in the same shape as the CMS's own custom-element
 * property action (`umb-property-sort-mode-property-action`).
 */
@customElement("uai-suggest-starter-prompts-property-action")
export class UaiSuggestStarterPromptsPropertyActionElement extends UmbLitElement {
    @property({ attribute: false })
    public manifest?: ManifestPropertyActionDefaultKind;

    @property({ attribute: false })
    public set api(value: UaiSuggestStarterPromptsPropertyAction | undefined) {
        this.#api = value;
        this.observe(value?.disabledReasonKey, (key) => (this._disabledReasonKey = key), "_observeDisabledReason");
    }
    public get api(): UaiSuggestStarterPromptsPropertyAction | undefined {
        return this.#api;
    }

    #api?: UaiSuggestStarterPromptsPropertyAction;

    @state()
    private _disabledReasonKey?: string;

    @state()
    private _isRunning = false;

    async #onClickLabel(event: UUIMenuItemEvent) {
        event.stopPropagation();
        // The popover stays open until the executed event below, so a second click can land while
        // the first call is still in flight.
        if (this._isRunning || this._disabledReasonKey) return;

        this._isRunning = true;
        try {
            await this.#api?.execute().catch(() => {});
        } finally {
            this._isRunning = false;
        }
        this.dispatchEvent(new UmbActionExecutedEvent());
    }

    // Matches the CMS property-action elements: stops the raw click reaching whatever hosts the menu.
    #onClick(event: PointerEvent) {
        event.stopPropagation();
    }

    override render() {
        const disabledReason = this._disabledReasonKey ? this.localize.term(this._disabledReasonKey) : undefined;

        return html`
            <uui-menu-item
                label=${this.localize.string(this.manifest?.meta.label ?? "")}
                title=${ifDefined(disabledReason)}
                ?disabled=${!!disabledReason}
                ?loading=${this._isRunning}
                @click-label=${this.#onClickLabel}
                @click=${this.#onClick}
            >
                ${when(
                    this.manifest?.meta.icon,
                    (icon) => html`<umb-icon slot="icon" name=${icon}></umb-icon>`,
                )}
            </uui-menu-item>
        `;
    }
}

export { UaiSuggestStarterPromptsPropertyActionElement as element };
export default UaiSuggestStarterPromptsPropertyActionElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-suggest-starter-prompts-property-action": UaiSuggestStarterPromptsPropertyActionElement;
    }
}
