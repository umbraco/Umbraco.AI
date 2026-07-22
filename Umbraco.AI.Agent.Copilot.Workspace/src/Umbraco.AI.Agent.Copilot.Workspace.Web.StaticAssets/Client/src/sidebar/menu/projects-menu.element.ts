import { customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT } from "../sidebar.context.js";
import type { UaiSidebarProject } from "../../conversation/grouping.js";
import "../../section/sidebar/project-tree-item.element.js";

const STORAGE_EXPANDED = "uai-cw-expanded-projects";

/**
 * Menu element for the Projects sidebar group. Renders one collapsible project node per project from
 * the shared sidebar context, owning the (persisted) expansion state. Hidden when there are no
 * projects (group-not-empty condition on its sidebar app). Its sidebar app is a `menuWithEntityActions`
 * whose header hosts the + New project action.
 */
@customElement("uai-copilot-workspace-projects-menu")
export class UaiCopilotWorkspaceProjectsMenuElement extends UmbLitElement {
    @state() private _projects: UaiSidebarProject[] = [];
    @state() private _activePath?: string;
    @state() private _searching = false;

    #expanded = readExpanded();

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_WORKSPACE_SIDEBAR_CONTEXT, (context) => {
            this.observe(context?.projects, (projects) => (this._projects = projects ?? []));
            this.observe(context?.activePath, (path) => (this._activePath = path));
            this.observe(context?.search, (search) => (this._searching = (search ?? "").trim().length > 0));
        });
    }

    #activeConversationId(): string | undefined {
        return this._activePath?.match(/\/conversation\/([^/]+)/)?.[1];
    }

    #isOpen(projectId: string, hasActiveChild: boolean): boolean {
        return this.#expanded.has(projectId) || hasActiveChild;
    }

    #toggle(projectId: string) {
        if (this.#expanded.has(projectId)) this.#expanded.delete(projectId);
        else this.#expanded.add(projectId);
        writeExpanded(this.#expanded);
        this.requestUpdate();
    }

    override render() {
        if (this._projects.length === 0) return nothing;
        const activeId = this.#activeConversationId();
        return repeat(
            this._projects,
            (p) => p.projectId,
            (p) => {
                const hasActiveChild = !!activeId && p.conversations.some((c) => c.id === activeId);
                // While searching, every shown project has a match — expand them all.
                const open = this._searching || this.#isOpen(p.projectId, hasActiveChild);
                return html`
                    <uai-copilot-workspace-project-tree-item
                        .project=${p}
                        .activePath=${this._activePath}
                        ?open=${open}
                        @toggle=${() => this.#toggle(p.projectId)}
                    ></uai-copilot-workspace-project-tree-item>
                `;
            },
        );
    }
}

function readExpanded(): Set<string> {
    try {
        const raw = localStorage.getItem(STORAGE_EXPANDED);
        return new Set(raw ? (JSON.parse(raw) as string[]) : []);
    } catch {
        return new Set();
    }
}

function writeExpanded(set: Set<string>): void {
    try {
        localStorage.setItem(STORAGE_EXPANDED, JSON.stringify([...set]));
    } catch {
        /* storage unavailable — in-session only */
    }
}

export default UaiCopilotWorkspaceProjectsMenuElement;
