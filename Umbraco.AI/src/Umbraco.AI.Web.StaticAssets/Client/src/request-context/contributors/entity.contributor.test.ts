import { describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "../../entity-adapter/entity-adapter.context-token.js";
import { UaiRequestContext } from "../extension-type.js";
import UaiEntityRequestContextContributor from "./entity.contributor.js";

/**
 * Regression coverage for umbraco/Umbraco.AI#353: this contributor used to construct its own
 * UaiEntityAdapterContext instead of consuming the one the hosting surface (Copilot) provides, so
 * it never heard about the user's actual selection in the context selector and silently
 * contributed whatever that separate, unrelated instance auto-selected instead.
 */
describe("UaiEntityRequestContextContributor", () => {
    /**
     * A real DOM-backed controller host, so `provideContext`/`getContext` genuinely wire up rather
     * than being mocked away. `hostConnected()` must be called explicitly -- a real `UmbLitElement`
     * wires this to its own `connectedCallback()`, but a bare `UmbElementControllerHost` has no
     * such lifecycle of its own, and controllers never activate (their context-request listeners
     * never attach) without it.
     */
    function createHost(): UmbControllerHost {
        const element = document.createElement("div");
        document.body.appendChild(element);
        const host = new UmbElementControllerHost(element);
        host.hostConnected();
        return host;
    }

    /**
     * Provides a fake entity adapter context on `host`, the same shape `UaiCopilotContext` provides
     * in the real app. Needs `getHostElement()` because `UmbContextConsumerController.setInstance`
     * calls it on the resolved instance to track scope -- the real `UaiEntityAdapterContext` has one
     * for free by extending `UmbControllerBase`.
     */
    function provideEntityAdapterContext(host: UmbControllerHost, serializeSelectedEntity: () => Promise<unknown>) {
        class FakeEntityAdapterContextProvider extends UmbControllerBase {
            constructor(providerHost: UmbControllerHost) {
                super(providerHost);
                this.provideContext(UAI_ENTITY_ADAPTER_CONTEXT, {
                    serializeSelectedEntity,
                    getHostElement: () => providerHost.getHostElement(),
                } as never);
            }
        }
        new FakeEntityAdapterContextProvider(host);
    }

    it("contributes the entity from the PROVIDED context, not one it constructs itself", async () => {
        const host = createHost();
        const serializeSelectedEntity = vi.fn().mockResolvedValue({
            entityType: "document",
            unique: "doc-1",
            name: "The document the user actually selected",
            data: {},
        });
        provideEntityAdapterContext(host, serializeSelectedEntity);

        const contributor = new UaiEntityRequestContextContributor(host);
        const requestContext = new UaiRequestContext();
        await contributor.contribute(requestContext);

        expect(serializeSelectedEntity).toHaveBeenCalledOnce();
        const items = requestContext.getItems();
        expect(items).toHaveLength(1);
        expect(items[0].description).toContain("The document the user actually selected");
    });

    it("contributes nothing when the provided context has no selection", async () => {
        const host = createHost();
        provideEntityAdapterContext(host, vi.fn().mockResolvedValue(undefined));

        const contributor = new UaiEntityRequestContextContributor(host);
        const requestContext = new UaiRequestContext();
        await contributor.contribute(requestContext);

        expect(requestContext.getItems()).toHaveLength(0);
    });
});
