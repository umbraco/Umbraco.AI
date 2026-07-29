import { describe, expect, it } from "vitest";
import {
    createConversationDraft,
    toConversationDetailModel,
    toCreateConversationRequestModel,
    toUpdateConversationRequestModel,
} from "./types.js";
import type { ContextResourceModel, ConversationResponseModel } from "./types.js";

const PROJECT_ID = "33333333-3333-3333-3333-333333333333";
const CONTEXT_ID = "44444444-4444-4444-4444-444444444444";

function resource(overrides: Partial<ContextResourceModel> = {}): ContextResourceModel {
    return {
        id: "55555555-5555-5555-5555-555555555555",
        resourceTypeId: "text",
        name: "Notes",
        sortOrder: 0,
        injectionMode: "Always",
        ...overrides,
    };
}

function response(overrides: Partial<ConversationResponseModel> = {}): ConversationResponseModel {
    return {
        id: "66666666-6666-6666-6666-666666666666",
        projectId: null,
        title: null,
        agentIdOrAlias: null,
        profileId: null,
        contextIds: [],
        resources: [],
        isPinned: false,
        isArchived: false,
        dateCreated: "2026-07-29T12:00:00Z",
        dateModified: "2026-07-29T12:00:00Z",
        lastMessageAt: null,
        ...overrides,
    } as ConversationResponseModel;
}

describe("createConversationDraft", () => {
    it("has no id, so consumers can tell it apart from a saved conversation", () => {
        expect(createConversationDraft().id).toBeUndefined();
    });

    it("starts empty and unattached", () => {
        const draft = createConversationDraft();

        expect(draft.projectId).toBeNull();
        expect(draft.title).toBeNull();
        expect(draft.agentIdOrAlias).toBeNull();
        expect(draft.contextIds).toEqual([]);
        expect(draft.resources).toEqual([]);
        expect(draft.isPinned).toBe(false);
        expect(draft.isArchived).toBe(false);
    });

    it("pre-attaches a project when one is given", () => {
        expect(createConversationDraft(PROJECT_ID).projectId).toBe(PROJECT_ID);
    });
});

describe("toConversationDetailModel", () => {
    it("copies the arrays so editing the model cannot mutate the loaded response", () => {
        const loaded = response({ contextIds: [CONTEXT_ID], resources: [resource()] });

        const model = toConversationDetailModel(loaded);
        model.contextIds.push("77777777-7777-7777-7777-777777777777");
        model.resources.pop();

        expect(loaded.contextIds).toEqual([CONTEXT_ID]);
        expect(loaded.resources).toHaveLength(1);
    });

    it("normalises absent optionals to null", () => {
        const model = toConversationDetailModel(response({ title: undefined, projectId: undefined }));

        expect(model.title).toBeNull();
        expect(model.projectId).toBeNull();
    });
});

describe("toCreateConversationRequestModel", () => {
    it("carries the draft's own contexts and resources, so one request persists everything", () => {
        const draft = {
            ...createConversationDraft(PROJECT_ID),
            agentIdOrAlias: "content-assistant",
            contextIds: [CONTEXT_ID],
            resources: [resource()],
        };

        const request = toCreateConversationRequestModel(draft);

        expect(request.projectId).toBe(PROJECT_ID);
        expect(request.agentIdOrAlias).toBe("content-assistant");
        expect(request.contextIds).toEqual([CONTEXT_ID]);
        expect(request.resources).toHaveLength(1);
    });

    it("trims the title and sends null when it is only whitespace", () => {
        expect(toCreateConversationRequestModel({ ...createConversationDraft(), title: "  Hi  " }).title).toBe("Hi");
        expect(toCreateConversationRequestModel({ ...createConversationDraft(), title: "   " }).title).toBeNull();
    });
});

describe("toUpdateConversationRequestModel", () => {
    it("projects the full mutable surface, so a PUT cannot clobber unrelated fields", () => {
        const model = toConversationDetailModel(
            response({
                projectId: PROJECT_ID,
                title: "Kept",
                agentIdOrAlias: "auto",
                contextIds: [CONTEXT_ID],
                resources: [resource()],
                isPinned: true,
                isArchived: true,
            }),
        );

        expect(toUpdateConversationRequestModel(model)).toEqual({
            title: "Kept",
            projectId: PROJECT_ID,
            agentIdOrAlias: "auto",
            profileId: null,
            contextIds: [CONTEXT_ID],
            resources: [resource()],
            isPinned: true,
            isArchived: true,
        });
    });
});
