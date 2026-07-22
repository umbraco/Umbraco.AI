import { describe, it, expect } from "vitest";
import { groupConversations } from "./grouping.js";
import type { ConversationResponseModel } from "./types.js";

// Fixed reference time: 2026-07-21T12:00:00 local (only used to derive relative timestamps).
const NOW = new Date(2026, 6, 21, 12, 0, 0).getTime();

function conv(overrides: Partial<ConversationResponseModel>): ConversationResponseModel {
    return {
        id: overrides.id ?? "c",
        projectId: overrides.projectId ?? null,
        title: overrides.title ?? "Untitled",
        agentIdOrAlias: null,
        profileId: null,
        isPinned: overrides.isPinned ?? false,
        isArchived: overrides.isArchived ?? false,
        dateCreated: overrides.dateCreated ?? new Date(NOW).toISOString(),
        dateModified: overrides.dateModified ?? new Date(NOW).toISOString(),
        lastMessageAt: overrides.lastMessageAt ?? null,
    };
}

/** A local ISO-ish timestamp `daysAgo` days before NOW at 09:00. */
function daysAgo(days: number): string {
    const d = new Date(NOW);
    d.setDate(d.getDate() - days);
    d.setHours(9, 0, 0, 0);
    return d.toISOString();
}

describe("groupConversations", () => {
    it("is empty when there are no conversations or projects", () => {
        const model = groupConversations([], new Map());
        expect(model.isEmpty).toBe(true);
        expect(model.pinned).toEqual([]);
        expect(model.projects).toEqual([]);
        expect(model.recent).toEqual([]);
    });

    it("floats pinned conversations into their own region regardless of project or date", () => {
        const model = groupConversations(
            [
                conv({ id: "old-pinned", projectId: "A", isPinned: true, lastMessageAt: daysAgo(30) }),
                conv({ id: "today", lastMessageAt: daysAgo(0) }),
            ],
            new Map([["A", "Alpha"]]),
        );
        expect(model.pinned.map((c) => c.id)).toEqual(["old-pinned"]);
        // The pinned conversation is not also listed under its project node.
        expect(model.projects[0].conversations).toEqual([]);
    });

    it("groups unpinned conversations by project, ordering projects by recency", () => {
        const model = groupConversations(
            [
                conv({ id: "a1", projectId: "A", lastMessageAt: daysAgo(5) }),
                conv({ id: "b1", projectId: "B", lastMessageAt: daysAgo(1) }),
                conv({ id: "a2", projectId: "A", lastMessageAt: daysAgo(2) }),
            ],
            new Map([
                ["A", "Alpha"],
                ["B", "Beta"],
            ]),
        );
        // Project B (most recent activity 1d ago) sorts above A (2d ago).
        expect(model.projects.map((p) => p.projectId)).toEqual(["B", "A"]);
        expect(model.projects[0].name).toBe("Beta");
        // Within A, the more recent conversation comes first.
        const projectA = model.projects.find((p) => p.projectId === "A")!;
        expect(projectA.conversations.map((c) => c.id)).toEqual(["a2", "a1"]);
    });

    it("puts project-less conversations in a flat recent list, most-recent-first", () => {
        const model = groupConversations(
            [
                conv({ id: "old", lastMessageAt: daysAgo(90) }),
                conv({ id: "today", lastMessageAt: daysAgo(0) }),
                conv({ id: "week", lastMessageAt: daysAgo(4) }),
            ],
            new Map(),
        );
        expect(model.projects).toEqual([]);
        expect(model.recent.map((c) => c.id)).toEqual(["today", "week", "old"]);
    });

    it("emits a node for a project with no conversations (sorted after active ones)", () => {
        const model = groupConversations(
            [conv({ id: "a1", projectId: "A", lastMessageAt: daysAgo(1) })],
            new Map([
                ["A", "Alpha"],
                ["Empty", "Empty Project"],
            ]),
        );
        expect(model.projects.map((p) => p.projectId)).toEqual(["A", "Empty"]);
        const empty = model.projects.find((p) => p.projectId === "Empty")!;
        expect(empty.name).toBe("Empty Project");
        expect(empty.conversations).toEqual([]);
    });

    it("drops empty projects when includeEmptyProjects is false (search mode)", () => {
        const model = groupConversations(
            [conv({ id: "a1", projectId: "A", lastMessageAt: daysAgo(1) })],
            new Map([
                ["A", "Alpha"],
                ["Empty", "Empty Project"],
            ]),
            { includeEmptyProjects: false },
        );
        // Only the project with a matching conversation survives.
        expect(model.projects.map((p) => p.projectId)).toEqual(["A"]);
    });

    it("folds conversations with an unknown project id into recent", () => {
        const model = groupConversations(
            [conv({ id: "x", projectId: "missing", lastMessageAt: daysAgo(0) })],
            new Map(),
        );
        expect(model.projects).toEqual([]);
        expect(model.recent.map((c) => c.id)).toEqual(["x"]);
    });
});
