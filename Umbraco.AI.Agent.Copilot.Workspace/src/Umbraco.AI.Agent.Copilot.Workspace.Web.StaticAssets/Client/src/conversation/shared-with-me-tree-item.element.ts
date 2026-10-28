import { css, customElement, html, nothing, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { SharedConversationSummaryResponseModel } from "./types.js";
import { UAI_CONVERSATION_ENTITY_TYPE, UAI_CONVERSATION_SHARED_WITH_ME_FLAG } from "../constants.js";
import { copilotWorkspaceConversationPath } from "../paths.js";
import { UaiSharedConversationRowContext } from "./shared-with-me-row.context.js";

/**
 * A single "shared with me" entry in the sidebar (STORIES.md US3). Visually mirrors
 * `uai-copilot-workspace-conversation-tree-item` (same plain-chat-icon + corner badge row shape, per
 * `SPEC.md`'s "identical row shape" requirement) but is otherwise a standalone element, not a subclass:
 * the data shape (`SharedConversationSummaryResponseModel`) has no `hasActiveShares`/`isPinned` fields —
 * every row here is definitionally shared-with-me — and renders no entity-actions dropdown at all (AC4).
 *
 * The badge renders through the real `entitySign`/`umb-entity-sign-bundle` extension system, same as the
 * owner-side "Is Shared" badge. The per-row "Shared by <owner>" tooltip (AC2) comes from
 * {@link UaiSharedConversationRowContext}, a per-row context this element provides — the sign api
 * (`UaiSharedWithMeSignApi`) consumes it and exposes a reactive `label` observable, which
 * `<umb-entity-sign-bundle>` subscribes to directly instead of a fixed `meta.label` string.
 */
@customElement("uai-copilot-workspace-shared-with-me-tree-item")
export class UaiCopilotWorkspaceSharedWithMeTreeItemElement extends UmbLitElement {
    #rowContext = new UaiSharedConversationRowContext(this);

    @property({ attribute: false })
    item?: SharedConversationSummaryResponseModel;

    /** Current router path, for active highlighting. */
    @property({ type: String })
    activePath?: string;

    override willUpdate(changed: Map<PropertyKey, unknown>) {
        super.willUpdate(changed);
        if (changed.has("item")) {
            this.#rowContext.setOwnerDisplayName(this.item?.ownerDisplayName);
        }
    }

    override render() {
        const item = this.item;
        if (!item) return nothing;
        const href = copilotWorkspaceConversationPath(item.conversationId);
        const active = this.activePath?.includes(href) ?? false;
        const title = item.title?.trim() || this.localize.term("uaiCopilotWorkspace_untitledConversation");

        return html`
            <uui-menu-item label=${title} href=${href} ?active=${active}>
                <div id="icon-container" slot="icon">
                    <umb-entity-sign-bundle
                        .entityType=${UAI_CONVERSATION_ENTITY_TYPE}
                        .entityFlags=${[{ alias: UAI_CONVERSATION_SHARED_WITH_ME_FLAG }]}
                    >
                        <uui-icon name="icon-chat"></uui-icon>
                    </umb-entity-sign-bundle>
                </div>
            </uui-menu-item>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }

            /*
             * Matches core's own tree-item-base + media-tree-item (dist-cms/.../tree-item-element-base.js,
             * dist-cms/.../media-tree-item.element.js), and the owner-side conversation tree item above:
             * the icon-container wrapper gets the positioning context and icon sizing the entity-sign
             * badge expects, and --umb-sign-bundle-bg is set on uui-menu-item per visual state so
             * <umb-entity-sign-bundle>'s cut-out ring matches the row's actual background.
             */
            #icon-container {
                position: relative;
                font-size: 15px;
            }

            uui-menu-item {
                --umb-sign-bundle-bg: var(--uui-color-surface);
            }

            uui-menu-item:hover {
                --umb-sign-bundle-bg: var(--uui-color-surface-emphasis);
            }

            uui-menu-item[active] {
                --umb-sign-bundle-bg: var(--uui-color-current);
            }

            uui-menu-item[active]:hover {
                --umb-sign-bundle-bg: var(--uui-color-current-emphasis);
            }

            uui-menu-item[selected] {
                --umb-sign-bundle-bg: var(--uui-color-selected);
            }

            uui-menu-item[selected]:hover {
                --umb-sign-bundle-bg: var(--uui-color-selected-emphasis);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceSharedWithMeTreeItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-shared-with-me-tree-item": UaiCopilotWorkspaceSharedWithMeTreeItemElement;
    }
}
