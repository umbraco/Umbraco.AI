// T15 — shared enabled-capabilities cache: one fetch per session, retried after a failure.
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerHostElementMixin } from "@umbraco-cms/backoffice/controller-api";

const getEnabledCapabilities = vi.fn();

// Stub at the lowest boundary that still exercises the repository's caching: the server
// data source. The generated OpenAPI SDK sits one layer further down and isn't needed here.
vi.mock("./enabled-capabilities.server.data-source.js", () => ({
    UaiEnabledCapabilitiesServerDataSource: class {
        getEnabledCapabilities = getEnabledCapabilities;
    },
}));

/**
 * Re-imports the repository module fresh so the module-scoped shared-request cache
 * (`sharedRequest`) can't leak between specs. `vi.resetModules()` would also clear the
 * cache for `@umbraco-cms/backoffice`'s own dependency graph, which re-runs its
 * `customElements.define()` side effects and throws "already been used"; a unique query
 * string instead busts the cache for only this one module.
 */
let importCount = 0;
async function importRepository() {
    const { UaiEnabledCapabilitiesRepository } = await import(
        /* @vite-ignore */ `./enabled-capabilities.repository.js?t=${importCount++}`
    );
    return UaiEnabledCapabilitiesRepository;
}

const TEST_HOST_TAG = "uai-enabled-capabilities-test-host";
if (!customElements.get(TEST_HOST_TAG)) {
    customElements.define(TEST_HOST_TAG, class extends UmbControllerHostElementMixin(HTMLElement) {});
}

function createHost() {
    const wrapper = document.createElement(TEST_HOST_TAG);
    document.body.appendChild(wrapper);
    return wrapper;
}

describe("Feature: enabled-capabilities caching", () => {
    beforeEach(() => {
        getEnabledCapabilities.mockReset();
    });

    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: two concurrent callers", () => {
        let callCount: number;

        beforeEach(async () => {
            getEnabledCapabilities.mockResolvedValue({ data: ["Chat"] });
            const Repository = await importRepository();
            const host = createHost();
            const repoA = new Repository(host);
            const repoB = new Repository(host);

            await Promise.all([repoA.getEnabledCapabilities(), repoB.getEnabledCapabilities()]);
            callCount = getEnabledCapabilities.mock.calls.length;
        });

        it("shares a single fetch across both callers", () => {
            expect(callCount).toBe(1);
        });
    });

    describe("Scenario: a call after a failed fetch", () => {
        let callCount: number;

        beforeEach(async () => {
            getEnabledCapabilities.mockResolvedValueOnce({ error: new Error("boom") });
            getEnabledCapabilities.mockResolvedValueOnce({ data: ["Chat"] });
            const Repository = await importRepository();
            const repo = new Repository(createHost());

            await repo.getEnabledCapabilities();
            await repo.getEnabledCapabilities();
            callCount = getEnabledCapabilities.mock.calls.length;
        });

        it("fetches again", () => {
            expect(callCount).toBe(2);
        });
    });

    describe("Scenario: a call after a successful fetch", () => {
        let callCount: number;

        beforeEach(async () => {
            getEnabledCapabilities.mockResolvedValue({ data: ["Chat"] });
            const Repository = await importRepository();
            const repo = new Repository(createHost());

            await repo.getEnabledCapabilities();
            await repo.getEnabledCapabilities();
            callCount = getEnabledCapabilities.mock.calls.length;
        });

        it("does not fetch a second time", () => {
            expect(callCount).toBe(1);
        });
    });
});
