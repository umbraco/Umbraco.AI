import { css, customElement, html, query, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiCopilotWorkspaceChatContext } from "../../chat/copilot-workspace-chat.context.js";
import { UAI_CONVERSATION_WORKSPACE_CONTEXT } from "./conversation-workspace.context.js";

/**
 * Center region of the conversation workspace. Hosts a {@link UaiCopilotWorkspaceChatContext} (which
 * provides `UAI_CHAT_CONTEXT` + `UAI_HITL_CONTEXT` and re-keys itself off the workspace store) and renders
 * the shared `<uai-chat>` against it. Read-only/ready come from the store — the chat runtime no longer
 * owns them — so an archived conversation locks and there's no composer flash while a conversation resolves.
 */
@customElement("uai-copilot-workspace-conversation-chat-view")
export class UaiCopilotWorkspaceConversationChatViewElement extends UmbLitElement {
    #context = new UaiCopilotWorkspaceChatContext(this);

    @state() private _readonly = false;
    @state() private _ready = false;

    /** The shared chat element; used to focus its composer when the conversation changes. */
    @query("uai-chat")
    private _chat?: HTMLElement & { focusComposer?: () => void };

    constructor() {
        super();
        void this.#context.loadAgents();
        this.consumeContext(UAI_CONVERSATION_WORKSPACE_CONTEXT, (store) => {
            this.observe(store?.isReadonly$, (value) => (this._readonly = value ?? false));
            this.observe(store?.isResolved$, (value) => (this._ready = value ?? false));
            // Focus the composer on every target the store is pointed at, not just this view's first
            // mount — the store re-targets within a mount (a draft promoted to its real conversation),
            // where the composer's own mount-time focus wouldn't fire.
            this.observe(store?.target$, () => {
                this.updateComplete.then(() => this._chat?.focusComposer?.());
            });
        });
    }

    override render() {
        // Property bindings (not `?attr`): `ready` defaults true on the element, and a boolean-attribute
        // binding of false only removes the attribute (a no-op when never set), leaving the property true
        // and flashing the composer. Setting the property is unambiguous.
        return html`<uai-chat
            .ready=${this._ready}
            .readonly=${this._readonly}
            readonly-notice=${this.localize.term("uaiCopilotWorkspace_readOnlyNotice")}
        ></uai-chat>`;
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
            }
        `,
    ];
}

export default UaiCopilotWorkspaceConversationChatViewElement;
