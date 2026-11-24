// GP-5 — Hide the dashboard's "Create a Connection" button from users without the connection right
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState } from "@umbraco-cms/backoffice/observable-api";
import { UMB_CURRENT_USER_CONTEXT } from "@umbraco-cms/backoffice/current-user";

import {
    UAI_PERMISSION_VERB_MANAGE_CONNECTIONS,
    UAI_PERMISSION_VERB_MANAGE_PROFILES,
} from "../../permissions/constants.js";
import "./ai-dashboard.element.js";
import type { UaiWelcomeDashboardElement } from "./ai-dashboard.element.js";

// happy-dom doesn't implement ElementInternals, which <uui-button> (a UUI form control) needs.
beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function () {
            return { setValidity: vi.fn(), form: null } as unknown as ElementInternals;
        };
    }
});

async function renderDashboard(fallbackPermissions: string[]) {
    const wrapper = document.createElement("div");
    document.body.appendChild(wrapper);
    const host = new UmbElementControllerHost(wrapper);
    host.hostConnected();
    class FakeCurrentUserContext extends UmbControllerBase {
        fallbackPermissions = new UmbArrayState<string>(fallbackPermissions, (x) => x).asObservable();
        constructor(providerHost: UmbControllerHost) {
            super(providerHost);
            this.provideContext(UMB_CURRENT_USER_CONTEXT, this as never);
        }
    }
    new FakeCurrentUserContext(host);
    const el = document.createElement("uai-welcome-dashboard") as UaiWelcomeDashboardElement;
    wrapper.appendChild(el);
    await el.updateComplete;
    await Promise.resolve();
    await el.updateComplete;
    return el;
}

const button = (el: HTMLElement) => el.shadowRoot!.querySelector('uui-button[label="Create a Connection"]');

describe("Feature: welcome dashboard connection call to action", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    it("shows Create a Connection to a user with the connection right", async () => {
        const el = await renderDashboard([UAI_PERMISSION_VERB_MANAGE_CONNECTIONS]);
        expect(button(el)).not.toBeNull();
    });

    it("hides Create a Connection from a user without the connection right", async () => {
        const el = await renderDashboard([UAI_PERMISSION_VERB_MANAGE_PROFILES]);
        expect(button(el)).toBeNull();
    });

    it("keeps the welcome text for a user without the connection right", async () => {
        const el = await renderDashboard([]);
        expect(el.shadowRoot!.querySelector("h1")?.textContent).toContain("Welcome to Umbraco AI");
    });
});
