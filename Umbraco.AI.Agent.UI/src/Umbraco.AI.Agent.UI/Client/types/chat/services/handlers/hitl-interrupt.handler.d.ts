import type { UaiInterruptContext, UaiInterruptHandler } from "../interrupt.types.js";
import type { UaiInterruptInfo } from "../../types/index.js";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
/**
 * Handles server-side HITL (human_approval) interrupts.
 * Delegates to UaiHitlContext to show the interrupt UI.
 */
export declare class UaiHitlInterruptHandler extends UmbControllerBase implements UaiInterruptHandler {
    #private;
    readonly reason = "human_approval";
    constructor(host: UmbControllerHost);
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}
//# sourceMappingURL=hitl-interrupt.handler.d.ts.map