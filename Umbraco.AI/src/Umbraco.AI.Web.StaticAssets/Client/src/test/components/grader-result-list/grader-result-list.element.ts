import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property } from "@umbraco-cms/backoffice/external/lit";
import type { TestGraderResultResponseModel } from "../../../api/types.gen.js";


/**
 * List wrapper for grader results.
 * Renders cards with auto-expand for failed graders.
 */
@customElement("uai-grader-result-list")
export class UaiGraderResultListElement extends LitElement {
    @property({ attribute: false })
    results: TestGraderResultResponseModel[] = [];

    render() {
        if (!this.results || this.results.length === 0) {
            return html`<div class="section-empty">No grader results</div>`;
        }

        return html`
            <div class="graders-list">
                ${this.results.map(
                    (r) => html`
                        <uai-grader-result-card
                            .result=${r}
                            ?expanded=${!r.passed}
                        ></uai-grader-result-card>
                    `,
                )}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }

        .graders-list {
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .section-empty {
            color: var(--uui-color-text-alt);
            text-align: center;
            padding: 20px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-grader-result-list": UaiGraderResultListElement;
    }
}
