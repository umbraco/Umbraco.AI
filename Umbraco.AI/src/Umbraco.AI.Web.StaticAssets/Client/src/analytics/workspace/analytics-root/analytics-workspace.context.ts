import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UmbBasicState } from "@umbraco-cms/backoffice/observable-api";
import { UmbViewContext } from "@umbraco-cms/backoffice/view";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import type { UauiCompare, UauiDateRange } from "@umbraco/analytics-ui";
import { UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_ANALYTICS_ROOT_ENTITY_TYPE } from "../../entity.js";
import { defaultUsageDateRange } from "../../usage/usage-date-range.js";
import type { UaiUsageDateRange } from "../../usage/usage-date-range.js";

type UmbWorkspaceContext = typeof UMB_WORKSPACE_CONTEXT.TYPE;

/**
 * Workspace context of the analytics root. It is the default workspace context plus the date range the
 * header's picker sets and the dashboard reads.
 */
export class UaiAnalyticsWorkspaceContext extends UmbContextBase implements UmbWorkspaceContext {
    public readonly IS_ANALYTICS_WORKSPACE_CONTEXT = true;

    public readonly workspaceAlias = UAI_ANALYTICS_ROOT_WORKSPACE_ALIAS;
    public readonly view = new UmbViewContext(this, null);

    #entityContext = new UmbEntityContext(this);

    #dateRange = new UmbBasicState<UaiUsageDateRange>(defaultUsageDateRange());
    readonly dateRange = this.#dateRange.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UMB_WORKSPACE_CONTEXT);
        this.#entityContext.setEntityType(UAI_ANALYTICS_ROOT_ENTITY_TYPE);
    }

    set manifest(manifest: { meta: { headline: string } }) {
        this.view.setTitle(manifest.meta.headline);
    }

    getEntityType(): string {
        return UAI_ANALYTICS_ROOT_ENTITY_TYPE;
    }

    setDateRange(range: UauiDateRange, compare?: UauiCompare): void {
        this.#dateRange.setValue({ range, compare });
    }
}

export { UaiAnalyticsWorkspaceContext as api };
