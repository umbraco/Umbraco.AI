import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UaiInterruptInfo } from "../types/index.js";
import type { UaiInterruptContext } from "./interrupt.types.js";
import { BehaviorSubject, combineLatest, map } from "rxjs";

export interface PendingApproval {
    interrupt: UaiInterruptInfo;
    targetMessageId?: string;
}

export class UaiHitlContext extends UmbControllerBase {
    #pendingContext?: UaiInterruptContext;
    #interrupt$ = new BehaviorSubject<UaiInterruptInfo | undefined>(undefined);
    #targetMessageId$ = new BehaviorSubject<string | undefined>(undefined);

    readonly interrupt$ = this.#interrupt$.asObservable();
    readonly targetMessageId$ = this.#targetMessageId$.asObservable();

    // Combined observable for rendering HITL inline
    readonly pendingApproval$ = combineLatest([this.#interrupt$, this.#targetMessageId$]).pipe(
        map(([interrupt, targetMessageId]) => (interrupt ? { interrupt, targetMessageId } : undefined)),
    );

    constructor(host: UmbControllerHost) {
        super(host);
    }

    setInterrupt(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void {
        this.#pendingContext = context;
        this.#targetMessageId$.next(context.lastAssistantMessageId);
        context.setAgentState(undefined);
        this.#interrupt$.next(interrupt);
    }

    respond(response: string): void {
        this.#interrupt$.next(undefined);
        this.#targetMessageId$.next(undefined);
        this.#pendingContext?.resume(response);
        this.#pendingContext = undefined;
    }
}

export const UAI_HITL_CONTEXT = new UmbContextToken<UaiHitlContext>("UaiHitlContext");

export default UaiHitlContext;
