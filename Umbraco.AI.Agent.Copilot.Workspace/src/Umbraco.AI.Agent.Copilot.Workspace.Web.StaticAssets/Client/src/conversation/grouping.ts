import type { ConversationResponseModel } from "./types.js";

/** A project node in the sidebar tree, with its (unpinned) conversations. */
export interface UaiSidebarProject {
    projectId: string;
    name: string;
    conversations: ConversationResponseModel[];
}

/**
 * The sidebar's three regions:
 *  - `pinned` — pinned conversations (from any project), floated to the top;
 *  - `projects` — one collapsible node per project (empty projects included), most-recently-active
 *    first, empty ones last (by name);
 *  - `recent` — the flat, most-recent-first list of conversations that belong to no project (or whose
 *    project is unknown, e.g. still loading or deleted).
 */
export interface UaiSidebarModel {
    pinned: ConversationResponseModel[];
    projects: UaiSidebarProject[];
    recent: ConversationResponseModel[];
    /** True when there is nothing at all to show. */
    isEmpty: boolean;
}

/** Effective activity timestamp for ordering (newest signal wins). */
function activityTime(c: ConversationResponseModel): number {
    const stamp = c.lastMessageAt ?? c.dateModified ?? c.dateCreated;
    const t = stamp ? Date.parse(stamp) : NaN;
    return Number.isFinite(t) ? t : 0;
}

function byActivityDesc(a: ConversationResponseModel, b: ConversationResponseModel): number {
    return activityTime(b) - activityTime(a);
}

/**
 * Shapes conversations for the tree sidebar. `projectNames` is the full set of projects
 * (id → display name); a project node is emitted for every entry — including empty ones, so a
 * newly-created project shows immediately. Conversations whose project id is not in the map (loose
 * or orphaned) fall into `recent`.
 */
export function groupConversations(
    conversations: readonly ConversationResponseModel[],
    projectNames: ReadonlyMap<string, string>,
): UaiSidebarModel {
    const pinned = conversations.filter((c) => c.isPinned).sort(byActivityDesc);
    const unpinned = conversations.filter((c) => !c.isPinned);

    // Bucket unpinned conversations by known project; everything else is "recent".
    const byProject = new Map<string, ConversationResponseModel[]>();
    const recent: ConversationResponseModel[] = [];
    for (const c of unpinned) {
        if (c.projectId && projectNames.has(c.projectId)) {
            const list = byProject.get(c.projectId) ?? [];
            list.push(c);
            byProject.set(c.projectId, list);
        } else {
            recent.push(c);
        }
    }

    // One node per project (empty included), most-recently-active first, empty ones last (by name).
    const projects: UaiSidebarProject[] = [...projectNames.entries()].map(([projectId, name]) => ({
        projectId,
        name,
        conversations: (byProject.get(projectId) ?? []).sort(byActivityDesc),
    }));
    projects.sort((a, b) => {
        const ta = a.conversations.length ? activityTime(a.conversations[0]) : Number.NEGATIVE_INFINITY;
        const tb = b.conversations.length ? activityTime(b.conversations[0]) : Number.NEGATIVE_INFINITY;
        return tb !== ta ? tb - ta : a.name.localeCompare(b.name);
    });

    recent.sort(byActivityDesc);

    return {
        pinned,
        projects,
        recent,
        isEmpty: pinned.length === 0 && projects.length === 0 && recent.length === 0,
    };
}
