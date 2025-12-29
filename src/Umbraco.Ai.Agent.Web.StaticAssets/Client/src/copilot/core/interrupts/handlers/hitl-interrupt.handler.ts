import { InterruptContext, InterruptHandler } from "../types.ts";
import { InterruptInfo } from "../../types.ts";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_HITL_CONTEXT, UaiHitlContext } from "../../hitl.context.ts";

export class HitlInterruptHandler extends UmbControllerBase implements InterruptHandler {
    readonly reason = "human_approval"; 

   #hitlContext?: UaiHitlContext;

    constructor(host: UmbControllerHost) {
        super(host);
        this.consumeContext(UAI_HITL_CONTEXT, (hitlContext) => {
            this.#hitlContext = hitlContext;
        });
    }

    handle(interrupt: InterruptInfo, context: InterruptContext): void {
        console.log("HitlInterruptHandler handling interrupt:", interrupt);
        this.#hitlContext?.setInterrupt(interrupt, context);
    }
}