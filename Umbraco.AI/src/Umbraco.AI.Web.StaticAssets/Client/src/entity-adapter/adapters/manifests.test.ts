import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { resolveEntityAdapterByType } from "../helpers.js";
import { entityAdapterManifests } from "./manifests.js";

/**
 * Prompts run inside a block look the adapter up by the workspace's entity type ("block"). When the
 * block adapter was dropped from these manifests to keep it out of the Copilot context selector,
 * that lookup returned nothing and the prompt sent no context at all (umbraco/Umbraco.AI#514).
 */
describe("entityAdapterManifests", () => {
    beforeAll(() => umbExtensionsRegistry.registerMany(entityAdapterManifests));
    afterAll(() => umbExtensionsRegistry.unregisterMany(entityAdapterManifests.map((m) => m.alias)));

    it("resolves the block adapter by entity type", async () => {
        const adapter = await resolveEntityAdapterByType("block");

        expect(adapter?.entityType).toBe("block");
    });

    it("keeps the block adapter out of context auto-detection", () => {
        const block = entityAdapterManifests.find((m) => m.forEntityType === "block");

        expect(block?.detectable).toBe(false);
    });
});
