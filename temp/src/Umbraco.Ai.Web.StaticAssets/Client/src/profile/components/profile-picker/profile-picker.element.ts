import { css, html, customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import { ProfilesService } from "../../../api/sdk.gen.js";
import type { ProfileItemResponseModel } from "../../../api/types.gen.js";
import { UaiSelectedEvent } from "../../../core";

/**
 * Profile picker component that allows selecting an AI profile.
 * Can be filtered by capability (e.g., "Chat", "Embedding").
 *
 * @fires selected - Fires when a profile is selected, with the profile id and item data.
 *
 * @example
 * ```html
 * <uai-profile-picker
 *   capability="Chat"
 *   .value=${"profile-id"}
 *   @selected=${(e) => console.log(e.unique, e.item)}
 * ></uai-profile-picker>
 * ```
 * @public
 */
@customElement("uai-profile-picker")
export class UaiProfilePickerElement extends UmbLitElement {
    /**
     * Filter profiles by capability. If not set, all profiles are shown.
     */
    @property({ type: String })
    capability?: string;

    /**
     * The currently selected profile id.
     */
    @property({ type: String })
    value?: string;

    /**
     * Placeholder text shown when no profile is selected.
     */
    @property({ type: String })
    placeholder = "-- Select Profile --";

    /**
     * Whether the picker is disabled.
     */
    @property({ type: Boolean })
    disabled = false;

    @state()
    private _profiles: ProfileItemResponseModel[] = [];

    @state()
    private _loading = true;

    @state()
    private _error?: string;

    connectedCallback(): void {
        super.connectedCallback();
        this.#loadProfiles();
    }

    async #loadProfiles() {
        this._loading = true;
        this._error = undefined;

        const { data, error } = await tryExecute(
            this,
            ProfilesService.getAllProfiles({
                query: {
                    skip: 0,
                    take: 1000,
                },
            })
        );

        this._loading = false;

        if (error || !data) {
            this._error = "Failed to load profiles";
            this._profiles = [];
            return;
        }

        // Filter by capability if specified
        if (this.capability) {
            this._profiles = data.items.filter(
                (p) => p.capability.toLowerCase() === this.capability!.toLowerCase()
            );
        } else {
            this._profiles = data.items;
        }
    }

    #onChange(event: UUISelectEvent) {
        event.stopPropagation();
        const selectedId = event.target.value as string;
        const selectedItem = this._profiles.find((p) => p.id === selectedId) ?? null;

        this.dispatchEvent(new UaiSelectedEvent(selectedId || null, selectedItem));
    }

    #getOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        const options: Array<{ name: string; value: string; selected?: boolean }> = [
            { name: this.placeholder, value: "" },
        ];

        for (const profile of this._profiles) {
            options.push({
                name: profile.name,
                value: profile.id,
                selected: profile.id === this.value,
            });
        }

        return options;
    }

    render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._error) {
            return html`<uui-tag color="danger">${this._error}</uui-tag>`;
        }

        if (this._profiles.length === 0) {
            return html`<uui-tag color="warning">No profiles available</uui-tag>`;
        }

        return html`
            <uui-select
                .value=${this.value ?? ""}
                .options=${this.#getOptions()}
                ?disabled=${this.disabled}
                @change=${this.#onChange}
            ></uui-select>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            uui-select {
                width: 100%;
            }

            uui-loader-bar {
                width: 100%;
            }
        `,
    ];
}

export default UaiProfilePickerElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-picker": UaiProfilePickerElement;
    }
}
