import type { UaiCommand } from "./command.base.js";

/**
 * A store for UaiCommand instances, allowing addition, retrieval, muting, and clearing of commands.
 * @public
 */
export class UaiCommandStore {
    #muted = false;
    #commands: UaiCommand[] = [];

    add(command: UaiCommand) {
        if (this.#muted) return;
        // Replace command with same correlationId or append
        this.#commands = [
            ...this.#commands.filter((x) => !command.correlationId || x.correlationId !== command.correlationId),
            command,
        ];
    }

    getAll(): UaiCommand[] {
        return this.#muted ? [] : [...this.#commands];
    }

    mute() {
        this.#muted = true;
    }
    unmute() {
        this.#muted = false;
    }
    clear() {
        this.#commands = [];
    }
    reset() {
        this.clear();
        this.unmute();
    }
}
