import type { UaiInterruptContext, UaiInterruptHandler } from "../types.js";
import type { UaiInterruptInfo } from "../../types.js";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_HITL_CONTEXT } from "../../hitl.context.js";
import type UaiHitlContext from "../../hitl.context.js";

/**
 * Handles server-side HITL (human_approval) interrupts.
 * Delegates to UaiHitlContext to show the interrupt UI.
 */
export class UaiHitlInterruptHandler extends UmbControllerBase implements UaiInterruptHandler {
    readonly reason = "human_approval";

    #hitlContext?: UaiHitlContext;

    constructor(host: UmbControllerHost) {
        super(host);
        this.consumeContext(UAI_HITL_CONTEXT, (hitlContext) => {
            this.#hitlContext = hitlContext;
        });
    }

    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void {
        this.#hitlContext?.setInterrupt(interrupt, context);
    }
}
