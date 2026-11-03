import { css, customElement, html, query, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiCopilotWorkspaceChatContext } from "../../chat/copilot-workspace-chat.context.js";
import { UAI_CONVERSATION_WORKSPACE_CONTEXT } from "./conversation-workspace.context.js";

/**
 * Center region of the conversation workspace. Hosts a {@link UaiCopilotWorkspaceChatContext} (which
 * provides `UAI_CHAT_CONTEXT` + `UAI_HITL_CONTEXT` and re-keys itself off the workspace store) and renders
 * the shared `<uai-chat>` against it. Read-only/ready come from the store — the chat runtime no longer
 * owns them — so an archived or shared (read-only) conversation locks and there's no composer flash while a
 * conversation resolves.
 */
@customElement("uai-copilot-workspace-conversation-chat-view")
export class UaiCopilotWorkspaceConversationChatViewElement extends UmbLitElement {
    #context = new UaiCopilotWorkspaceChatContext(this);

    @state() private _readonly = false;
    /** Whether the open conversation is archived — picks the archived vs. shared read-only notice text. */
    @state() private _isArchived = false;
    /** Whether the open conversation is a recipient's shared (read-only) view, and by whom. */
    @state() private _ownerDisplayName?: string;
    /** True once the store knows the target's mode (loaded, or a draft) — gates the composer flash. */
    @state() private _resolved = false;
    /** True once the chat context's history load has resolved for the *currently targeted* conversation —
     *  false again the moment the target changes. Guards against sending before `#persisted` is correct
     *  (see `UaiServerPersistedConversationStrategy`). */
    @state() private _historyLoaded = false;

    /** The shared chat element; used to focus its composer and re-arm auto-scroll when the conversation changes. */
    @query("uai-chat")
    private _chat?: HTMLElement & { focusComposer?: () => void; resetScrollFollow?: () => void };

    constructor() {
        super();
        void this.#context.loadAgents();
        this.observe(this.#context.historyLoaded$, (value) => (this._historyLoaded = value ?? false));
        this.consumeContext(UAI_CONVERSATION_WORKSPACE_CONTEXT, (store) => {
            this.observe(store?.isReadonly$, (value) => (this._readonly = value ?? false));
            this.observe(store?.isResolved$, (value) => (this._resolved = value ?? false));
            // The collapsed `readonly` boolean (observed above) doesn't say *why* it's read-only, but the
            // notice text does (archived vs. shared-by-<owner>), so read `isArchived`/`isShared` directly.
            this.observe(store?.conversation$, (c) => {
                this._isArchived = c?.isArchived ?? false;
                this._ownerDisplayName = c?.isShared ? c.ownerDisplayName : undefined;
            });
            // Re-key auto-scroll and composer focus on every target the store is pointed at, not just
            // this view's first mount — the store re-targets within a mount (a draft promoted to its real
            // conversation, or the user picking a different saved conversation), and `<uai-chat>` is never
            // remounted across those switches. Without resetScrollFollow(), a scroll-up left over from the
            // previous conversation would silently suppress the new one's initial scroll-to-bottom.
            this.observe(store?.target$, () => {
                this._chat?.resetScrollFollow?.();
                this.updateComplete.then(() => this._chat?.focusComposer?.());
            });
        });
    }

    override render() {
        // Property bindings (not `?attr`): `ready` defaults true on the element, and a boolean-attribute
        // binding of false only removes the attribute (a no-op when never set), leaving the property true
        // and flashing the composer. Setting the property is unambiguous. Both the store's resolution and
        // the chat context's history load must be true — either alone lets the user send before the
        // server-persisted strategy's boundary is correct, re-sending already-stored messages as new.
        return html`<uai-chat
            .ready=${this._resolved && this._historyLoaded}
            .readonly=${this._readonly}
            readonly-notice=${this.#readonlyNotice()}
        ></uai-chat>`;
    }

    /** Picks the read-only notice by *why* the conversation is read-only — archived takes precedence, since
     *  the two are mutually exclusive in practice (a share is never also archived for its recipient). */
    #readonlyNotice(): string {
        return this._isArchived || !this._ownerDisplayName
            ? this.localize.term("uaiCopilotWorkspace_readOnlyNotice")
            : this.localize.term("uaiCopilotWorkspace_sharedReadOnlyNotice", this._ownerDisplayName);
    }

    static override styles = [
        css`
            :host {
                display: block;
                height: 100%;
                min-height: 0;
            }
            /* Let the chat fill the full width/height so its scroll bar sits at the far edge, and
               constrain only the inner message list + composer to a comfortable reading width. */
            uai-chat {
                display: block;
                height: 100%;
                width: 100%;
                --uai-chat-content-max-width: 860px;
                /* Tool chips, agent status, response bubbles, and approval cards default to a grey
                   "alternate surface" tuned for the contextual copilot sidebar's light background.
                   Copilot Workspace's own canvas is grey, so that default disappears into it -- override
                   to a proper elevated surface so these elements read as cards against the canvas. */
                --uai-chat-surface-alt: var(--uui-color-surface);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationChatViewElement;
