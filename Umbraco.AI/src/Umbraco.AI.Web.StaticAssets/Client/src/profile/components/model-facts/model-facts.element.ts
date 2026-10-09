import { css, customElement, html, nothing, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiConnectionModelFactsRepository } from "../../../connection/repository/model-facts/connection-model-facts.repository.js";
import type { UaiModelFactModel } from "../../../connection/types.js";
import { isSafeFactUrl, selectDisplayFacts } from "./model-facts.logic.js";

const elementName = "uai-model-facts";
let instanceCounter = 0;

/**
 * Shows the facts known about the selected model (context window, price, ...) under the Model field.
 * Renders nothing until connection, capability and model are all set, or when there are no facts
 * or the request fails.
 *
 * @example
 * ```html
 * <uai-model-facts connection-id="..." capability="Chat" model-id="gpt-4o"></uai-model-facts>
 * ```
 */
@customElement(elementName)
export class UaiModelFactsElement extends UmbLitElement {
    #repository = new UaiConnectionModelFactsRepository(this);
    #requestId = 0;
    // Unique per element so popover ids never collide when several instances share a page.
    readonly #idPrefix = `uai-model-facts-${++instanceCounter}`;

    @property({ type: String, attribute: "connection-id" })
    connectionId?: string;

    @property({ type: String })
    capability?: string;

    @property({ type: String, attribute: "model-id" })
    modelId?: string;

    @state()
    private _facts: UaiModelFactModel[] = [];

    @state()
    private _loading = false;

    protected override willUpdate(changed: Map<PropertyKey, unknown>) {
        if (changed.has("connectionId") || changed.has("capability") || changed.has("modelId")) {
            void this.#loadFactsAsync();
        }
    }

    async #loadFactsAsync() {
        const requestId = ++this.#requestId;
        this._facts = [];

        const { connectionId, capability, modelId } = this;
        if (!connectionId || !capability || !modelId) {
            this._loading = false;
            return;
        }

        this._loading = true;
        const { data, error } = await this.#repository.requestModelFacts({ connectionId, capability, modelId });

        // A newer request superseded this one; its result owns the state now.
        if (requestId !== this.#requestId) return;

        this._loading = false;
        if (error || !data) {
            console.warn("Could not load model facts; showing none.", error);
            this._facts = [];
            return;
        }

        const selected = data.find((item) => item.model.modelId === modelId);
        this._facts = selectDisplayFacts(selected?.facts ?? []);
    }

    override render() {
        if (!this._loading && this._facts.length === 0) return nothing;

        return html`
            <div class="facts" data-model-facts>
                ${this._loading
                    ? html`<uui-loader-bar></uui-loader-bar>`
                    : repeat(
                          this._facts,
                          (fact) => fact.key,
                          (fact, index) => this.#renderFact(fact, index),
                      )}
            </div>
        `;
    }

    #renderFact(fact: UaiModelFactModel, index: number) {
        const label = this.localize.string(fact.label);
        const detail = fact.detail ? this.localize.string(fact.detail) : undefined;
        const popoverId = `${this.#idPrefix}-detail-${index}`;

        return html`
            <div class="fact" data-fact-key=${fact.key}>
                <span class="label">${label}</span>
                ${fact.tone === "Warning"
                    ? html`<uui-tag color="warning" look="primary">${fact.value}</uui-tag>`
                    : html`<span class="value">${fact.value}</span>`}
                ${detail
                    ? html`
                          <uui-button
                              class="detail-trigger"
                              compact
                              look="default"
                              popovertarget=${popoverId}
                              label=${this.localize.term("uaiModelFacts_about", label)}>
                              <uui-icon name="icon-info"></uui-icon>
                          </uui-button>
                          <uui-popover-container id=${popoverId} placement="bottom-start">
                              <umb-popover-layout>
                                  <div class="detail">${detail}</div>
                              </umb-popover-layout>
                          </uui-popover-container>
                      `
                    : nothing}
                ${isSafeFactUrl(fact.url)
                    ? html`<a href=${fact.url} target="_blank" rel="noopener noreferrer"
                          >${this.localize.term("uaiModelFacts_learnMore")}</a
                      >`
                    : nothing}
            </div>
        `;
    }

    static override readonly styles = [
        css`
            :host {
                display: block;
            }

            .facts {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-2);
                font-size: var(--uui-type-small-size);
            }

            .fact {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
            }

            .label {
                color: var(--uui-color-text-alt);
            }

            .value {
                font-weight: bold;
            }

            /* Borderless, text-sized info button so rows with a detail trigger match plain rows. */
            .detail-trigger {
                --uui-button-height: 0;
                --uui-button-border-width: 0;
                --uui-button-padding-left-factor: 0;
                --uui-button-padding-right-factor: 0;
                --uui-button-border-radius: var(--uui-border-radius-2);
                --uui-button-contrast: var(--uui-color-text-alt);
                --uui-button-contrast-hover: var(--uui-color-interactive-emphasis);
                font-size: inherit;
                align-self: center;
            }

            .detail {
                padding: var(--uui-size-space-4);
                max-width: 20rem;
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiModelFactsElement;
    }
}

export default UaiModelFactsElement;
