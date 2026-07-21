import { describe, it, expect } from "vitest";
import { groupConversations } from "./grouping.js";
import type { ConversationResponseModel } from "./types.js";

// Fixed "now": 2026-07-21T12:00:00 local.
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
    it("returns no groups when there are no conversations", () => {
        expect(groupConversations([], new Map(), NOW)).toEqual([]);
    });

    it("puts pinned conversations first regardless of project or date", () => {
        const groups = groupConversations(
            [
                conv({ id: "old-pinned", isPinned: true, lastMessageAt: daysAgo(30) }),
                conv({ id: "today", lastMessageAt: daysAgo(0) }),
            ],
            new Map(),
            NOW,
        );
        expect(groups[0].kind).toBe("pinned");
        expect(groups[0].conversations.map((c) => c.id)).toEqual(["old-pinned"]);
    });

    it("groups unpinned conversations by project, ordering projects by recency", () => {
        const groups = groupConversations(
            [
                conv({ id: "a1", projectId: "A", lastMessageAt: daysAgo(5) }),
                conv({ id: "b1", projectId: "B", lastMessageAt: daysAgo(1) }),
                conv({ id: "a2", projectId: "A", lastMessageAt: daysAgo(2) }),
            ],
            new Map([
                ["A", "Alpha"],
                ["B", "Beta"],
            ]),
            NOW,
        );
        const projectGroups = groups.filter((g) => g.kind === "project");
        // Project B (most recent activity 1d ago) sorts above A (2d ago).
        expect(projectGroups.map((g) => g.projectId)).toEqual(["B", "A"]);
        expect(projectGroups[0].label).toBe("Beta");
        // Within A, the more recent conversation comes first.
        const groupA = projectGroups.find((g) => g.projectId === "A")!;
        expect(groupA.conversations.map((c) => c.id)).toEqual(["a2", "a1"]);
    });

    it("buckets project-less conversations by recency", () => {
        const groups = groupConversations(
            [
                conv({ id: "today", lastMessageAt: daysAgo(0) }),
                conv({ id: "yesterday", lastMessageAt: daysAgo(1) }),
                conv({ id: "week", lastMessageAt: daysAgo(4) }),
                conv({ id: "old", lastMessageAt: daysAgo(90) }),
            ],
            new Map(),
            NOW,
        );
        const dateGroups = groups.filter((g) => g.kind === "date");
        expect(dateGroups.map((g) => g.key)).toEqual([
            "date:today",
            "date:yesterday",
            "date:previous7",
            "date:older",
        ]);
    });

    it("falls back to unknown-project label when the id is not in the name map", () => {
        const groups = groupConversations(
            [conv({ id: "x", projectId: "missing", lastMessageAt: daysAgo(0) })],
            new Map(),
            NOW,
        );
        const projectGroup = groups.find((g) => g.kind === "project")!;
        expect(projectGroup.label).toBe("#uaiCopilotWorkspace_groupUnknownProject");
    });
});
