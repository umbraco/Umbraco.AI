import { InterruptContext, InterruptHandler } from "../types.ts";
import { InterruptInfo } from "../../types.ts";

export class DefaultInterruptHandler implements InterruptHandler {
    
    readonly reason = "*";
    handle(_interrupt: InterruptInfo, context: InterruptContext): void {
        context.setAgentState(undefined);
    }
}