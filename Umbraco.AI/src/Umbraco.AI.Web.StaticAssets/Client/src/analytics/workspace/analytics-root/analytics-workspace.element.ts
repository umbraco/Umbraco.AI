import { css, customElement, html, ifDefined, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UauiCompare, UauiDateRange } from "@umbraco/analytics-ui";
import { USAGE_DATE_PRESETS, defaultUsageDateRange } from "../../usage/usage-date-range.js";
import { UAI_ANALYTICS_WORKSPACE_CONTEXT } from "./analytics-workspace.context-token.js";

/**
 * The analytics root workspace: the standard workspace editor, with the date range picker in its header.
 * The workspace views (Usage) are routed by the editor as for any workspace.
 */
@customElement("uai-analytics-workspace")
export class UaiAnalyticsWorkspaceElement extends UmbLitElement {
    @property({ type: Object, attribute: false })
    manifest?: { meta: { headline: string } };

    #context?: typeof UAI_ANALYTICS_WORKSPACE_CONTEXT.TYPE;

    @state() private _range: UauiDateRange = defaultUsageDateRange().range;
    @state() private _compare?: UauiCompare;

    constructor() {
        super();
        this.consumeContext(UAI_ANALYTICS_WORKSPACE_CONTEXT, (context) => {
            this.#context = context;
            this.observe(context?.dateRange, (dateRange) => {
                if (!dateRange) return;
                this._range = dateRange.range;
                this._compare = dateRange.compare;
            });
        });
    }

    #onDateRange(event: CustomEvent<{ value: UauiDateRange; compare?: UauiCompare }>) {
        this.#context?.setDateRange(event.detail.value, event.detail.compare);
    }

    override render() {
        const headline = this.manifest?.meta.headline;
        return html`<umb-workspace-editor headline=${ifDefined(headline ? this.localize.string(headline) : undefined)}>
            <div slot="action-menu" class="actions">
                <uaui-date-range
                    .presets=${USAGE_DATE_PRESETS}
                    .value=${this._range}
                    .compare=${this._compare}
                    @change=${this.#onDateRange}
                ></uaui-date-range>
            </div>
        </umb-workspace-editor>`;
    }

    static override styles = [
        css`
            .actions {
                display: flex;
                align-items: center;
                padding-inline-end: var(--uui-size-layout-1);
            }
        `,
    ];
}

export { UaiAnalyticsWorkspaceElement as element };
export default UaiAnalyticsWorkspaceElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-workspace": UaiAnalyticsWorkspaceElement;
    }
}
