import { beforeEach, describe, expect, it } from "vitest";
import { UmbControllerHostElementMixin } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextProvider } from "@umbraco-cms/backoffice/context-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { BehaviorSubject, Subject, firstValueFrom } from "@umbraco-cms/backoffice/external/rxjs";
import type { Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UaiEntityAdapterContext } from "./entity-adapter.context.js";
import { UAI_ENTITY_ADAPTER_EXTENSION_TYPE } from "./extension-type.js";
import { UAI_WORKSPACE_REGISTRY_CONTEXT } from "../workspace-registry/index.js";
import type { UaiEntityAdapterApi, UaiEntityContext } from "./types.js";

/**
 * A workspace registers with the registry *before* it has loaded its entity, so the first read of
 * `getUnique()` is undefined and the entity is detected as new. Nothing used to re-run detection
 * when the real id arrived — name and icon had live subscriptions, the unique did not — so the
 * entity key stayed `document:new` for the rest of the session. Anything keyed on that identity was
 * then wrong: per-node copilot chat history never persisted (it refuses to save under a `:new` key),
 * and two documents open in split view collided on one key.
 */

const HOST_TAG = "uai-entity-adapter-context-test-host";

class TestHostElement extends UmbControllerHostElementMixin(HTMLElement) {}
if (!customElements.get(HOST_TAG)) customElements.define(HOST_TAG, TestHostElement);

/**
 * Minimal stand-in for a document workspace context.
 *
 * Deliberately reproduces the real lag: the id is published on the `unique` observable first, and the
 * synchronous `getUnique()` only catches up later. Detection therefore cannot rely on re-reading the
 * snapshot when it's told the id exists — it has to use the value it was handed.
 */
class FakeWorkspaceContext {
    readonly unique: BehaviorSubject<string | undefined>;
    #snapshot?: string;

    constructor(initialUnique?: string) {
        this.unique = new BehaviorSubject<string | undefined>(initialUnique);
        this.#snapshot = initialUnique;
    }

    getUnique(): string | undefined {
        return this.#snapshot;
    }

    /** Simulates the workspace publishing its id, before the snapshot read reflects it. */
    publishUnique(unique: string | undefined): void {
        this.unique.next(unique);
    }

    /** Simulates the snapshot read finally catching up. */
    settleSnapshot(unique: string | undefined): void {
        this.#snapshot = unique;
    }
}

class FakeAdapter implements UaiEntityAdapterApi {
    readonly entityType = "document";

    canHandle(ctx: unknown): boolean {
        return ctx instanceof FakeWorkspaceContext;
    }

    extractEntityContext(ctx: unknown): UaiEntityContext {
        return { entityType: "document", unique: (ctx as FakeWorkspaceContext).getUnique() ?? null };
    }

    getUniqueObservable(ctx: unknown): Observable<string | undefined> | undefined {
        return (ctx as FakeWorkspaceContext).unique;
    }

    getName(): string {
        return "Home";
    }

    async serializeForLlm() {
        return { entityType: "document", unique: "new", name: "Home", data: {} } as never;
    }

    destroy(): void {}
}

const ADAPTER_ALIAS = "Uai.Test.EntityAdapter.Document";

function createHarness(workspace: FakeWorkspaceContext) {
    const host = new TestHostElement();
    document.body.appendChild(host);

    // Extends UmbControllerBase because the context API expects the provided instance to be a
    // controller (it reads getHostElement off it when wiring the consumer up).
    class FakeWorkspaceRegistry extends UmbControllerBase {
        readonly IS_WORKSPACE_REGISTRY_CONTEXT = true;
        readonly #changes$ = new Subject<unknown>();
        get changes$() {
            return this.#changes$.asObservable();
        }
        getAll() {
            return [{ context: workspace }];
        }
    }

    const registry = new FakeWorkspaceRegistry(host);
    new UmbContextProvider(host, UAI_WORKSPACE_REGISTRY_CONTEXT, registry as never).hostConnected();

    return { host, context: new UaiEntityAdapterContext(host) };
}

/** Lets pending microtasks (async adapter resolution, refresh passes) settle. */
const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

describe("UaiEntityAdapterContext entity keys", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
        if (!umbExtensionsRegistry.isRegistered(ADAPTER_ALIAS)) {
            umbExtensionsRegistry.register({
                type: UAI_ENTITY_ADAPTER_EXTENSION_TYPE,
                alias: ADAPTER_ALIAS,
                name: "Test Document Adapter",
                api: FakeAdapter,
            } as never);
        }
    });

    it("keys an entity by its unique once loaded", async () => {
        const workspace = new FakeWorkspaceContext("dcf18a51-6919-4cf8-89d1-36b94ce4d963");
        const { context } = createHarness(workspace);
        await settle();

        const entities = await firstValueFrom(context.detectedEntities$);
        expect(entities.map((e) => e.key)).toEqual(["document:dcf18a51-6919-4cf8-89d1-36b94ce4d963"]);
    });

    it("re-keys when the unique arrives after the workspace registered", async () => {
        // Registered while still loading: no id yet, so it is detected as new.
        const workspace = new FakeWorkspaceContext(undefined);
        const { context } = createHarness(workspace);
        await settle();

        expect((await firstValueFrom(context.detectedEntities$)).map((e) => e.key)).toEqual(["document:new"]);

        // The document publishes its id. Note the snapshot read is deliberately NOT settled here:
        // this is the regression, where the key stayed "document:new" for the rest of the session.
        workspace.publishUnique("dcf18a51-6919-4cf8-89d1-36b94ce4d963");
        await settle();

        const entities = await firstValueFrom(context.detectedEntities$);
        expect(entities.map((e) => e.key)).toEqual(["document:dcf18a51-6919-4cf8-89d1-36b94ce4d963"]);
        expect(entities[0].entityContext.unique).toBe("dcf18a51-6919-4cf8-89d1-36b94ce4d963");
    });

    it("keeps a genuinely new entity keyed as new", async () => {
        const workspace = new FakeWorkspaceContext(undefined);
        const { context } = createHarness(workspace);
        await settle();

        // A document being created stays unsaved; re-publishing "no id" must not churn the key.
        workspace.publishUnique(undefined);
        await settle();

        expect((await firstValueFrom(context.detectedEntities$)).map((e) => e.key)).toEqual(["document:new"]);
    });
});
