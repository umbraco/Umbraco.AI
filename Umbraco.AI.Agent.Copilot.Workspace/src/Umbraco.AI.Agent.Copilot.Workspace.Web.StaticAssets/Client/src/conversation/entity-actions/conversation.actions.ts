import { UmbEntityActionBase, type UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbConfirmModal, umbOpenModal, UMB_ITEM_PICKER_MODAL } from "@umbraco-cms/backoffice/modal";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UaiConversationRepository } from "../repository/conversation.repository.js";
import { UaiProjectRepository } from "../../project/repository/project.repository.js";
import {
    UAI_CONVERSATION_ENTITY_CONTEXT,
    type UaiConversationEntityContext,
} from "../entity/conversation-entity.context.js";
import { UAI_RENAME_CONVERSATION_MODAL } from "../modal/rename-conversation-modal.token.js";
import type { ConversationResponseModel } from "../types.js";

/**
 * Base for conversation entity actions (shown in each sidebar tree item's ⋯ menu). Resolves the
 * conversation from the per-node {@link UaiConversationEntityContext} (falling back to a fetch) and
 * runs the mutation through the repository, which dispatches a `UaiEntityActionEvent` on the shared
 * action-event bus so the sidebar refreshes. Toggle pairs (Pin/Unpin, Archive/Unarchive) are gated by
 * the state conditions, not by execute().
 */
abstract class UaiConversationActionBase extends UmbEntityActionBase<never> {
    protected repository = new UaiConversationRepository(this);
    #context?: UaiConversationEntityContext;

    constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
        super(host, args);
        this.consumeContext(UAI_CONVERSATION_ENTITY_CONTEXT, (context) => {
            this.#context = context;
        });
    }

    /** Prefer the live model from the node context; fall back to a fetch by unique. */
    protected async resolveConversation(): Promise<ConversationResponseModel | undefined> {
        const fromContext = this.#context?.getModel();
        if (fromContext) return fromContext;
        if (!this.args.unique) return undefined;
        const { data } = await this.repository.requestById(this.args.unique);
        return data ?? undefined;
    }
}

export class UaiConversationPinAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const conversation = await this.resolveConversation();
        if (!conversation) return;
        await this.repository.setPinned(conversation, true);
    }
}

export class UaiConversationUnpinAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const conversation = await this.resolveConversation();
        if (!conversation) return;
        await this.repository.setPinned(conversation, false);
    }
}

export class UaiConversationArchiveAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const conversation = await this.resolveConversation();
        if (!conversation) return;
        await this.repository.setArchived(conversation, true);
    }
}

export class UaiConversationUnarchiveAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const conversation = await this.resolveConversation();
        if (!conversation) return;
        await this.repository.setArchived(conversation, false);
    }
}

export class UaiConversationRenameAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const conversation = await this.resolveConversation();
        if (!conversation) return;
        const result = await umbOpenModal(this, UAI_RENAME_CONVERSATION_MODAL, {
            data: { value: conversation.title ?? "" },
        }).catch(() => undefined);
        const title = result?.name?.trim();
        if (!title || title === (conversation.title ?? "")) return;
        await this.repository.rename(conversation, title);
    }
}

export class UaiConversationMoveAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const conversation = await this.resolveConversation();
        if (!conversation) return;

        const localize = new UmbLocalizationController(this);
        const { data } = await new UaiProjectRepository(this).requestCollection();
        const noProjectValue = "";
        const items = [
            { label: localize.term("uaiCopilotWorkspace_moveNoProject"), value: noProjectValue, icon: "icon-delete" },
            ...(data?.items ?? []).map((p) => ({ label: p.name, value: p.id, icon: "icon-folder" })),
        ];

        const chosen = await umbOpenModal(this, UMB_ITEM_PICKER_MODAL, {
            data: { headline: localize.term("uaiCopilotWorkspace_moveHeadline"), items },
        }).catch(() => undefined);
        if (!chosen) return;

        const projectId = chosen.value === noProjectValue ? null : chosen.value;
        if ((conversation.projectId ?? null) === projectId) return;
        await this.repository.moveToProject(conversation, projectId);
    }
}

export class UaiConversationDeleteAction extends UaiConversationActionBase {
    override async execute(): Promise<void> {
        const unique = this.args.unique;
        if (!unique) return;
        await umbConfirmModal(this, {
            headline: "#uaiCopilotWorkspace_deleteConfirmTitle",
            content: "#uaiCopilotWorkspace_deleteConfirmMessage",
            color: "danger",
            confirmLabel: "#uaiCopilotWorkspace_actionDelete",
        });
        const { error } = await this.repository.delete(unique);
        if (error) return;
    }
}
