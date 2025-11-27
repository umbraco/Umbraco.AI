import { UaiCommandBase } from "../command.base.js";

export class UaiPartialUpdateCommand<TReceiver> extends UaiCommandBase<TReceiver> {
    #partial: Partial<TReceiver>;

    constructor(partial: Partial<TReceiver>, correlationId?: string) {
        super(correlationId);
        this.#partial = partial;
    }

    execute(receiver: TReceiver) {
        Object.keys(this.#partial)
            .filter(key => this.#partial[key as keyof TReceiver] !== undefined)
            .forEach(key => {
                receiver[key as keyof TReceiver] = this.#partial[key as keyof TReceiver]!;
            });
    }
}
