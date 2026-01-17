/**
 * Defines the contract for a command in the UAI system.
 * @public
 */
export interface UaiCommand {
    correlationId?: string;
    execute(receiver: unknown): void;
}

/**
 * Base class for UAI commands, providing common functionality.
 * @public
 */
export abstract class UaiCommandBase<TReceiver> implements UaiCommand {
    correlationId?: string;

    constructor(correlationId?: string) {
        this.correlationId = correlationId;
    }

    abstract execute(receiver: TReceiver): void;
}
