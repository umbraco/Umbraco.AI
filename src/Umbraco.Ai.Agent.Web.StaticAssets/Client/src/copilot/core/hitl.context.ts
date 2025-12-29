import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { InterruptInfo } from "./types.ts";
import { InterruptContext } from "./interrupts/types.ts";
import { BehaviorSubject } from "rxjs";

export class UaiHitlContext extends UmbControllerBase {

    #pendingContext?: InterruptContext;
    #interrupt$ = new BehaviorSubject<InterruptInfo | undefined>(undefined);

    readonly interrupt$ = this.#interrupt$.asObservable();
    
    constructor(host: UmbControllerHost) {
        super(host);
    }

    setInterrupt(interrupt: InterruptInfo, context: InterruptContext): void {
        this.#pendingContext = context;
        context.setAgentState(undefined);
        this.#interrupt$.next(interrupt);
    }

    respond(response: string): void {
        this.#interrupt$.next(undefined);
        this.#pendingContext?.resume(response);
        this.#pendingContext = undefined;
    }
}

export const UAI_HITL_CONTEXT =
    new UmbContextToken<UaiHitlContext>("UaiHitlContext");

export default UaiHitlContext;