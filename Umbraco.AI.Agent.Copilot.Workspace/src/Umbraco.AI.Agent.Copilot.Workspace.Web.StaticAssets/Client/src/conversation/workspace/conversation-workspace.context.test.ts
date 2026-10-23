// SC-18 — Wire: recipient reads a shared conversation end-to-end.
// Real entry point exercised: UaiConversationWorkspaceContext#reload()'s fallback to
// setSharedConversationId() when the owner lookup 404s — the only path `setConversationId()` (the
// one method the `conversation/:id` route calls) has to resolve either an owned or a shared
// conversation id.
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerHostElementMixin } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { firstValueFrom } from "@umbraco-cms/backoffice/external/rxjs";

// Stub at the real boundary: the repository the store delegates to.
const requestById = vi.fn();
const requestSharedView = vi.fn();
vi.mock("../repository/conversation.repository.js", () => ({
    UaiConversationRepository: class {
        requestById = requestById;
        requestSharedView = requestSharedView;
    },
}));

const requestProjectById = vi.fn();
vi.mock("../../project/repository/project.repository.js", () => ({
    UaiProjectRepository: class {
        requestById = requestProjectById;
    },
}));

import { UaiConversationWorkspaceContext } from "./conversation-workspace.context.js";
import type { ConversationResponseModel, SharedConversationResponseModel } from "../types.js";

const OWNED_ID = "11111111-1111-1111-1111-111111111111";
const SHARED_ID = "22222222-2222-2222-2222-222222222222";
const MISSING_ID = "33333333-3333-3333-3333-333333333333";

function ownedResponse(overrides: Partial<ConversationResponseModel> = {}): ConversationResponseModel {
    return {
        id: OWNED_ID,
        projectId: null,
        title: "Owned",
        agentIdOrAlias: null,
        profileId: null,
        contextIds: [],
        resources: [],
        isPinned: false,
        isArchived: false,
        hasActiveShares: false,
        dateCreated: "2026-07-29T12:00:00Z",
        dateModified: "2026-07-29T12:00:00Z",
        lastMessageAt: null,
        ...overrides,
    } as ConversationResponseModel;
}

function sharedResponse(overrides: Partial<SharedConversationResponseModel> = {}): SharedConversationResponseModel {
    return {
        id: SHARED_ID,
        title: "Shared",
        agentIdOrAlias: null,
        profileId: null,
        ownerDisplayName: "Jane Owner",
        contextIds: [],
        resources: [],
        messages: [],
        dateCreated: "2026-07-29T12:00:00Z",
        dateModified: "2026-07-29T12:00:00Z",
        lastMessageAt: null,
        ...overrides,
    };
}

const TEST_HOST_TAG = "uai-conversation-workspace-context-test-host";
if (!customElements.get(TEST_HOST_TAG)) {
    customElements.define(TEST_HOST_TAG, class extends UmbControllerHostElementMixin(HTMLElement) {});
}

function createHost(): UmbControllerHost {
    const element = document.createElement(TEST_HOST_TAG);
    document.body.appendChild(element);
    return element as unknown as UmbControllerHost;
}

describe("Feature: UaiConversationWorkspaceContext#setConversationId (owned-or-shared resolution)", () => {
    beforeEach(() => {
        requestById.mockReset();
        requestSharedView.mockReset();
        requestProjectById.mockReset();
    });

    describe("Scenario: the id belongs to a conversation the acting user owns", () => {
        let context: UaiConversationWorkspaceContext;

        beforeEach(async () => {
            requestById.mockResolvedValue({ data: ownedResponse() });
            context = new UaiConversationWorkspaceContext(createHost());
            await context.setConversationId(OWNED_ID);
        });

        it("loads the owner's view and never falls back to the shared view", () => {
            expect(requestSharedView).not.toHaveBeenCalled();
        });

        it("resolves the model as not shared / editable", async () => {
            const conversation = await firstValueFrom(context.conversation$);
            expect(conversation).toMatchObject({ id: OWNED_ID, isShared: false, readonly: false });
        });
    });

    describe("Scenario: the id belongs to a conversation shared with the acting user, not owned by them", () => {
        let context: UaiConversationWorkspaceContext;

        beforeEach(async () => {
            requestById.mockResolvedValue({ data: undefined });
            requestSharedView.mockResolvedValue({ data: sharedResponse() });
            context = new UaiConversationWorkspaceContext(createHost());
            await context.setConversationId(SHARED_ID);
        });

        it("tries the owner lookup first", () => {
            expect(requestById).toHaveBeenCalledWith(SHARED_ID);
        });

        it("falls back to the shared view once the owner lookup 404s", () => {
            expect(requestSharedView).toHaveBeenCalledWith(SHARED_ID);
        });

        it("resolves the model as shared and read-only, naming the owner", async () => {
            const conversation = await firstValueFrom(context.conversation$);
            expect(conversation).toMatchObject({
                id: SHARED_ID,
                isShared: true,
                readonly: true,
                ownerDisplayName: "Jane Owner",
            });
        });

        it("marks the target resolved", async () => {
            expect(await firstValueFrom(context.isResolved$)).toBe(true);
        });

        it("never fetches an owning project — a share never carries one", () => {
            expect(requestProjectById).not.toHaveBeenCalled();
        });
    });

    describe("Scenario: the id belongs to neither — truly not found", () => {
        let context: UaiConversationWorkspaceContext;

        beforeEach(async () => {
            requestById.mockResolvedValue({ data: undefined });
            requestSharedView.mockResolvedValue({ data: undefined });
            context = new UaiConversationWorkspaceContext(createHost());
            await context.setConversationId(MISSING_ID);
        });

        it("tries both the owner and shared lookups", () => {
            expect(requestById).toHaveBeenCalledWith(MISSING_ID);
            expect(requestSharedView).toHaveBeenCalledWith(MISSING_ID);
        });

        it("leaves the model undefined, same as today's not-found handling", async () => {
            expect(await firstValueFrom(context.conversation$)).toBeUndefined();
        });

        it("still marks the target resolved, so the view can show its own not-found state", async () => {
            expect(await firstValueFrom(context.isResolved$)).toBe(true);
        });
    });
});
