import { describe, expect, it } from "vitest";
import { firstValueFrom, of } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiBlockAdapter } from "./block.adapter.js";

/**
 * getNameObservable's whole job is telling two blocks apart in the Copilot's context selector —
 * regression here is exactly what shipped as every chip reading the literal "Block" (umbraco/Umbraco.AI#343).
 */
describe("UaiBlockAdapter.getNameObservable", () => {
    const adapter = new UaiBlockAdapter();

    it("emits the owning entry context's resolved label", async () => {
        const ctx = {
            getName: () => "",
            getContext: async () => ({ label: of("USP Block") }),
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("USP Block");
    });

    it("falls back to the workspace's own name when no entry context is reachable", async () => {
        const ctx = {
            getName: () => "Fallback Name",
            getContext: async () => undefined,
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("Fallback Name");
    });

    it("falls back to the literal 'Block' when neither source has a name", async () => {
        const ctx = {
            getName: () => "",
            getContext: async () => ({ label: of("") }),
        };

        const name = await firstValueFrom(adapter.getNameObservable(ctx)!);

        expect(name).toBe("Block");
    });
});
