// GP-9 (AC2), GP-5: the Add-ons heading shows only to users holding a right registered in the group
import { afterEach, describe, expect, it } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_CURRENT_USER_CONTEXT } from "@umbraco-cms/backoffice/current-user";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UmbArrayState } from "@umbraco-cms/backoffice/observable-api";
import { createRegisteredGroupPermissionCondition } from "./area-conditions.js";
import { UAI_PERMISSION_ENTITY_TYPE, UAI_PERMISSION_GROUP_ADD_ONS } from "./constants.js";
import { UaiRegisteredGroupPermissionCondition } from "./registered-group-permission.condition.js";

const ADD_ON_VERB = "Uai.Widget.Manage";
const registered: string[] = [];

function registerRight(alias: string, group: string, verb: string, entityType = UAI_PERMISSION_ENTITY_TYPE) {
    umbExtensionsRegistry.register({
        type: "entityUserPermission",
        alias,
        name: alias,
        forEntityTypes: [entityType],
        meta: { verbs: [verb], label: alias, description: alias, group },
    } as UmbExtensionManifest);
    registered.push(alias);
}

function createCondition(userPermissions: string[] | undefined) {
    const element = document.createElement("div");
    document.body.appendChild(element);
    const host = new UmbElementControllerHost(element);
    host.hostConnected();
    if (userPermissions) {
        const permissions = userPermissions;
        class FakeCurrentUserContext extends UmbControllerBase {
            fallbackPermissions = new UmbArrayState<string>(permissions, (x) => x).asObservable();
            constructor(providerHost: UmbControllerHost) {
                super(providerHost);
                this.provideContext(UMB_CURRENT_USER_CONTEXT, this as never);
            }
        }
        new FakeCurrentUserContext(host);
    }
    return new UaiRegisteredGroupPermissionCondition(host, {
        host,
        config: createRegisteredGroupPermissionCondition(UAI_PERMISSION_GROUP_ADD_ONS),
        onChange: () => {},
    } as never);
}

// The registry and the user context both deliver through microtasks, so flushing a few of them
// is enough for the condition to have seen both.
const settle = async () => {
    for (let i = 0; i < 10; i++) await Promise.resolve();
};

afterEach(() => {
    registered.splice(0).forEach((alias) => umbExtensionsRegistry.unregister(alias));
    document.body.innerHTML = "";
});

describe("Feature: the Add-ons heading follows the rights add-ons register", () => {
    describe("Scenario: an add-on right is registered in the group", () => {
        it("is permitted for a user who holds that right", async () => {
            registerRight("Test.Right.Widget", UAI_PERMISSION_GROUP_ADD_ONS, ADD_ON_VERB);
            const condition = createCondition([ADD_ON_VERB]);
            await settle();
            expect(condition.permitted).toBe(true);
        });

        it("is not permitted for a user who holds only other rights", async () => {
            registerRight("Test.Right.Widget", UAI_PERMISSION_GROUP_ADD_ONS, ADD_ON_VERB);
            const condition = createCondition(["Uai.Connection.Manage"]);
            await settle();
            expect(condition.permitted).toBe(false);
        });

        it("becomes permitted when the right registers after the condition started", async () => {
            const condition = createCondition([ADD_ON_VERB]);
            await settle();
            registerRight("Test.Right.Widget", UAI_PERMISSION_GROUP_ADD_ONS, ADD_ON_VERB);
            await settle();
            expect(condition.permitted).toBe(true);
        });
    });

    describe("Scenario: no right is registered in the group", () => {
        it("is not permitted, even for a user holding the verb", async () => {
            const condition = createCondition([ADD_ON_VERB]);
            await settle();
            expect(condition.permitted).toBe(false);
        });

        it("ignores a right registered in another group", async () => {
            registerRight("Test.Right.Other", "uaiConfiguration", ADD_ON_VERB);
            const condition = createCondition([ADD_ON_VERB]);
            await settle();
            expect(condition.permitted).toBe(false);
        });

        it("ignores a right registered for another entity type", async () => {
            registerRight("Test.Right.Doc", UAI_PERMISSION_GROUP_ADD_ONS, ADD_ON_VERB, "document");
            const condition = createCondition([ADD_ON_VERB]);
            await settle();
            expect(condition.permitted).toBe(false);
        });
    });

    describe("Scenario: there is no current user yet", () => {
        it("is not permitted", async () => {
            registerRight("Test.Right.Widget", UAI_PERMISSION_GROUP_ADD_ONS, ADD_ON_VERB);
            const condition = createCondition(undefined);
            await settle();
            expect(condition.permitted).toBe(false);
        });
    });
});
