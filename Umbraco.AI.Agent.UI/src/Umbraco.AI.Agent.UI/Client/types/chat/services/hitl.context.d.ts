import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiInterruptInfo } from "../types/index.js";
import type { UaiInterruptContext } from "./interrupt.types.js";
export interface PendingApproval {
    interrupt: UaiInterruptInfo;
    targetMessageId?: string;
}
export declare class UaiHitlContext extends UmbControllerBase {
    #private;
    readonly interrupt$: import("rxjs").Observable<UaiInterruptInfo | undefined>;
    readonly targetMessageId$: import("rxjs").Observable<string | undefined>;
    readonly pendingApproval$: import("rxjs").Observable<{
        interrupt: UaiInterruptInfo;
        targetMessageId: string | undefined;
    } | undefined>;
    constructor(host: UmbControllerHost);
    setInterrupt(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
    respond(response: string): void;
}
export declare const UAI_HITL_CONTEXT: UmbContextToken<UaiHitlContext, UaiHitlContext>;
export default UaiHitlContext;
//# sourceMappingURL=hitl.context.d.ts.map