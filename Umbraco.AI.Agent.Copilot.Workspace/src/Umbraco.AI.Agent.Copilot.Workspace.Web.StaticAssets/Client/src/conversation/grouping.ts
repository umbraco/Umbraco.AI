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
 * group per project — including projects with no conversations, so a newly-created (empty) project
 * shows as a folder in the tree — then the remaining project-less conversations bucketed by recency.
 *
 * `now` is injected so the date bucketing is deterministic and unit-testable. `projectNames` is the
 * full set of projects (id → display name); a project group is emitted for every entry. Conversations
 * whose project is missing from the map (e.g. a deleted project) fall under a generic "unknown" group.
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

    // Bucket unpinned conversations: by known project, orphaned (project not in the map), or loose.
    const byProject = new Map<string, ConversationResponseModel[]>();
    const orphans: ConversationResponseModel[] = [];
    const projectless: ConversationResponseModel[] = [];
    for (const c of unpinned) {
        if (!c.projectId) {
            projectless.push(c);
        } else if (projectNames.has(c.projectId)) {
            const list = byProject.get(c.projectId) ?? [];
            list.push(c);
            byProject.set(c.projectId, list);
        } else {
            orphans.push(c);
        }
    }

    // One group per project (empty included), most-recently-active first, empty ones last (by name).
    const projectGroups: UaiConversationGroup[] = [...projectNames.entries()].map(([projectId, name]) => ({
        key: `project:${projectId}`,
        kind: "project" as const,
        label: name,
        projectId,
        conversations: (byProject.get(projectId) ?? []).sort(byActivityDesc),
    }));
    projectGroups.sort((a, b) => {
        const ta = a.conversations.length ? activityTime(a.conversations[0]) : Number.NEGATIVE_INFINITY;
        const tb = b.conversations.length ? activityTime(b.conversations[0]) : Number.NEGATIVE_INFINITY;
        return tb !== ta ? tb - ta : a.label.localeCompare(b.label);
    });
    groups.push(...projectGroups);

    if (orphans.length) {
        groups.push({
            key: "project:unknown",
            kind: "project",
            label: "#uaiCopilotWorkspace_groupUnknownProject",
            conversations: orphans.sort(byActivityDesc),
        });
    }

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
