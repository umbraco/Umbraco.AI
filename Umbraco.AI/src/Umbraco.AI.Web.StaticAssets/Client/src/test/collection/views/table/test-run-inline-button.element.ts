import { html, customElement, state, property } from "@umbraco-cms/backoffice/external/lit";
import type { UUIButtonState } from "@umbraco-cms/backoffice/external/uui";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiTestRunEntityAction } from "../../../entity-actions/test-run.action.js";
import { UAI_TEST_ENTITY_TYPE } from "../../../constants.js";

/**
 * Inline Run button rendered per-row in the test collection table.
 * Mirrors the Run button on the test workspace editor by exposing
 * waiting/success/failed progress state via UUIButtonState.
 */
@customElement("uai-test-run-inline-button")
export class UaiTestRunInlineButtonElement extends UmbLitElement {
    @property({ type: String })
    unique?: string;

    @state()
    private _state?: UUIButtonState;

    async #onClick(event: Event) {
        event.stopPropagation();
        event.preventDefault();
        if (!this.unique) return;

        this._state = "waiting";

        try {
            const action = new UaiTestRunEntityAction(this, {
                unique: this.unique,
                entityType: UAI_TEST_ENTITY_TYPE,
                meta: undefined as never,
            });
            await action.execute();
            this._state = "success";
        } catch {
            this._state = "failed";
        }

        setTimeout(() => {
            this._state = undefined;
        }, 2000);
    }

    render() {
        return html`
            <uui-button
                compact
                look="secondary"
                label="Run"
                .state=${this._state}
                @click=${this.#onClick}
            >
                <uui-icon name="icon-play"></uui-icon>
            </uui-button>
        `;
    }
}

export default UaiTestRunInlineButtonElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-inline-button": UaiTestRunInlineButtonElement;
    }
}
