export interface UaiCommand {
    correlationId?: string;
    execute(receiver: unknown): void;
}

export abstract class UaiCommandBase<TReceiver> implements UaiCommand {
    correlationId?: string;

    constructor(correlationId?: string) {
        this.correlationId = correlationId;
    }

    abstract execute(receiver: TReceiver): void;
}
