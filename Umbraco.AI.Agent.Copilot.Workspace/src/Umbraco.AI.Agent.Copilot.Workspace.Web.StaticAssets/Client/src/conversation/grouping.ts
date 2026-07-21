import type { ConversationResponseModel } from "./types.js";

export type UaiConversationGroupKind = "pinned" | "project" | "date";

export interface UaiConversationGroup {
    /** Stable key for list rendering. */
    key: string;
    kind: UaiConversationGroupKind;
    /** Display label (localization key for pinned/date; raw name for projects). */
    label: string;
    /** Present for `project` groups so the header can deep-link to the project. */
    projectId?: string;
    conversations: ConversationResponseModel[];
}

/** Effective activity timestamp for ordering/bucketing (newest signal wins). */
function activityTime(c: ConversationResponseModel): number {
    const stamp = c.lastMessageAt ?? c.dateModified ?? c.dateCreated;
    const t = stamp ? Date.parse(stamp) : NaN;
    return Number.isFinite(t) ? t : 0;
}

function byActivityDesc(a: ConversationResponseModel, b: ConversationResponseModel): number {
    return activityTime(b) - activityTime(a);
}

/** Start-of-day (local) for `now`, minus `daysAgo` days. */
function startOfDay(now: number, daysAgo: number): number {
    const d = new Date(now);
    d.setHours(0, 0, 0, 0);
    d.setDate(d.getDate() - daysAgo);
    return d.getTime();
}

interface DateBucketDef {
    key: string;
    label: string;
    /** Inclusive lower bound (ms); items with activity >= min fall in the first matching bucket. */
    min: number;
}

/**
 * Groups conversations for the sidebar: pinned first (any project), then one
 * group per project (most-recently-active project first), then the remaining
 * project-less conversations bucketed by recency. Empty groups are omitted.
 *
 * `now` is injected so the date bucketing is deterministic and unit-testable.
 * `projectNames` resolves a projectId to its display name; unknown ids fall
 * back to a generic label.
 */
export function groupConversations(
    conversations: readonly ConversationResponseModel[],
    projectNames: ReadonlyMap<string, string>,
    now: number,
): UaiConversationGroup[] {
    const groups: UaiConversationGroup[] = [];

    const pinned = conversations.filter((c) => c.isPinned).sort(byActivityDesc);
    if (pinned.length) {
        groups.push({ key: "pinned", kind: "pinned", label: "#uaiCopilotWorkspace_groupPinned", conversations: pinned });
    }

    const unpinned = conversations.filter((c) => !c.isPinned);

    // One group per project, ordered by the project's most recent activity.
    const byProject = new Map<string, ConversationResponseModel[]>();
    const projectless: ConversationResponseModel[] = [];
    for (const c of unpinned) {
        if (c.projectId) {
            const list = byProject.get(c.projectId) ?? [];
            list.push(c);
            byProject.set(c.projectId, list);
        } else {
            projectless.push(c);
        }
    }

    const projectGroups: UaiConversationGroup[] = [...byProject.entries()].map(([projectId, list]) => ({
        key: `project:${projectId}`,
        kind: "project" as const,
        label: projectNames.get(projectId) ?? "#uaiCopilotWorkspace_groupUnknownProject",
        projectId,
        conversations: list.sort(byActivityDesc),
    }));
    projectGroups.sort((a, b) => byActivityDesc(a.conversations[0], b.conversations[0]));
    groups.push(...projectGroups);

    // Project-less conversations bucketed by recency.
    const buckets: DateBucketDef[] = [
        { key: "today", label: "#uaiCopilotWorkspace_groupToday", min: startOfDay(now, 0) },
        { key: "yesterday", label: "#uaiCopilotWorkspace_groupYesterday", min: startOfDay(now, 1) },
        { key: "previous7", label: "#uaiCopilotWorkspace_groupPrevious7Days", min: startOfDay(now, 7) },
        { key: "older", label: "#uaiCopilotWorkspace_groupOlder", min: Number.NEGATIVE_INFINITY },
    ];
    const bucketed = new Map<string, ConversationResponseModel[]>();
    for (const c of projectless.sort(byActivityDesc)) {
        const t = activityTime(c);
        const bucket = buckets.find((b) => t >= b.min) ?? buckets[buckets.length - 1];
        const list = bucketed.get(bucket.key) ?? [];
        list.push(c);
        bucketed.set(bucket.key, list);
    }
    for (const b of buckets) {
        const list = bucketed.get(b.key);
        if (list?.length) {
            groups.push({ key: `date:${b.key}`, kind: "date", label: b.label, conversations: list });
        }
    }

    return groups;
}
