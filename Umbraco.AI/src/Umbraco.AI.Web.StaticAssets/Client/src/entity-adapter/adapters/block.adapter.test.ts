import { describe, expect, it } from "vitest";
import { firstValueFrom, of } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiBlockAdapter } from "./block.adapter.js";

/**
 * getNameObservable's whole job is telling two blocks apart in the Copilot's context selector —
 * regression here is exactly what shipped as every chip reading the literal "Block" (umbraco/Umbraco.AI#343).
 *
 * Confirmed live in the backoffice, not just against mocks, in two stages:
 * 1. Reaching UmbBlockEntryContext requires going through the workspace context's `modalContext` (its own
 *    getContext() never finds a provider — see BlockWorkspaceContextLike.modalContext in block.adapter.ts).
 * 2. getContext() REJECTS (not resolves undefined) when no provider answers. An earlier version of this
 *    fix never caught that rejection, so the whole observable errored out instead of emitting — silently
 *    skipping every fallback below it, no matter how the entry context was being looked up. The first test
 *    below covers that regression directly.
 */
describe("UaiBlockAdapter.getNameObservable", () => {
    const adapter = new UaiBlockAdapter();

    it("falls back instead of erroring out when modalContext.getContext rejects (no provider found)", async () => {
        const ctx = {
            getName: () => "",
            modalContext: { getContext: () => Promise.reject(new Error("Context could not be found.")) },
            name: of("#general_edit Blog Articles"),
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("Blog Articles");
    });

    it("emits the owning entry context's resolved label when modalContext.getContext resolves", async () => {
        const ctx = {
            getName: () => "",
            modalContext: { getContext: async () => ({ label: of("USP Block") }) },
            name: of(undefined),
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("USP Block");
    });

    it("falls back to the stripped workspace name when there is no modalContext at all", async () => {
        const ctx = {
            getName: () => "",
            name: of("#general_add USP Block"),
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("USP Block");
    });

    it("falls back to the literal 'Block' when no source has anything", async () => {
        const ctx = {
            getName: () => "",
            modalContext: { getContext: () => Promise.reject(new Error("Context could not be found.")) },
            name: of(undefined),
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("Block");
    });
});
