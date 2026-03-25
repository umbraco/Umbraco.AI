import { customElement, property, state, css, html, ref, createRef, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTemporaryFileConfigRepository } from "@umbraco-cms/backoffice/temporary-file";
import { UMB_NOTIFICATION_CONTEXT, type UmbNotificationContext } from "@umbraco-cms/backoffice/notification";
import { UAI_CHAT_CONTEXT, type UaiChatContextApi } from "../context.js";
import type { UaiAgentItem } from "../types/index.js";
import type { UaiInputContent } from "../types/index.js";
import "./voice-button.element.js";

/** Maximum file size in bytes (default 10MB) */
const MAX_FILE_SIZE = 10 * 1024 * 1024;

interface AttachmentInfo {
    file: File;
    previewUrl?: string;
}

/**
 * Chat input component.
 * Provides a text input with send button, agent selector, file picker, and keyboard support.
 * Consumes UAI_CHAT_CONTEXT for agent data.
 *
 * @fires send - Dispatched when user sends a message, detail: { text: string, contentParts?: UaiInputContent[] }
 */
@customElement("uai-chat-input")
export class UaiChatInputElement extends UmbLitElement {
    @property({ type: Boolean })
    disabled = false;

    @property({ type: String })
    placeholder = "Type a message...";

    @state()
    private _value = "";

    @state()
    private _agents: UaiAgentItem[] = [];

    @state()
    private _selectedAgentId = "";

    @state()
    private _attachments: AttachmentInfo[] = [];

    @state()
    private _isDragging = false;

    @state()
    private _allowedExtensions: Array<string> = [];

    @state()
    private _disallowedExtensions: Array<string> = [];

    #chatContext?: UaiChatContextApi;
    #notificationContext?: UmbNotificationContext;
    #temporaryFileConfigRepository = new UmbTemporaryFileConfigRepository(this);
    #textareaRef = createRef<HTMLElement>();
    #fileInputRef = createRef<HTMLInputElement>();

    get #isDisabled(): boolean {
        return this.disabled || this._agents.length === 0;
    }

    constructor() {
        super();
        this.consumeContext(UAI_CHAT_CONTEXT, (context) => {
            if (context) {
                this.#chatContext = context;
                this.observe(context.agents, (agents) => (this._agents = agents));
                this.observe(context.selectedAgent, (agent) => (this._selectedAgentId = agent?.id ?? ""));
            }
        });
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });

        this.#temporaryFileConfigRepository.initialized.then(() => {
            this.observe(
                this.#temporaryFileConfigRepository.part("allowedUploadedFileExtensions"),
                (extensions) => (this._allowedExtensions = extensions ?? []),
            );
            this.observe(
                this.#temporaryFileConfigRepository.part("disallowedUploadedFilesExtensions"),
                (extensions) => (this._disallowedExtensions = extensions ?? []),
            );
        });
    }

    override updated(changedProperties: PropertyValues) {
        super.updated(changedProperties);

        if (changedProperties.has("disabled") && !this.disabled) {
            requestAnimationFrame(() => {
                this.#textareaRef.value?.focus();
            });
        }
    }

    #handleAgentChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this.#chatContext?.selectAgent(select.value);
    }

    #getAgentOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        return this._agents.map((agent) => ({
            name: agent.name,
            value: agent.id,
            selected: agent.id === this._selectedAgentId,
        }));
    }

    #handleKeydown(e: KeyboardEvent) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            this.#send();
        }
    }

    #handleInput(e: Event) {
        this._value = (e.target as HTMLTextAreaElement).value;
    }

    #handleTranscription(e: CustomEvent<{ text: string }>) {
        const transcribed = e.detail.text;
        if (transcribed) {
            this._value = this._value ? `${this._value} ${transcribed}` : transcribed;
        }
    }

    #handleAttachClick() {
        this.#fileInputRef.value?.click();
    }

    #handleFileSelect(e: Event) {
        const input = e.target as HTMLInputElement;
        if (!input.files) return;

        for (const file of Array.from(input.files)) {
            this.#addAttachment(file);
        }

        // Reset input so the same file can be selected again
        input.value = "";
    }

    #handleDragOver(e: DragEvent) {
        e.preventDefault();
        if (e.dataTransfer?.types.includes("Files")) {
            this._isDragging = true;
        }
    }

    #handleDragLeave(e: DragEvent) {
        // Only clear when the drag leaves the input-box entirely (not moving between children)
        const box = (e.currentTarget as HTMLElement);
        const related = e.relatedTarget as Node | null;
        if (!related || !box.contains(related)) {
            this._isDragging = false;
        }
    }

    #handleDrop(e: DragEvent) {
        e.preventDefault();
        this._isDragging = false;

        if (!e.dataTransfer?.files) return;
        for (const file of Array.from(e.dataTransfer.files)) {
            this.#addAttachment(file);
        }
    }

    #isFileAllowed(file: File): boolean {
        const ext = file.name.includes(".") ? file.name.split(".").pop()!.toLowerCase() : "";
        if (!ext) return true; // No extension — allow (backend will validate)

        if (this._allowedExtensions.length > 0) {
            return this._allowedExtensions.some((e) => e.toLowerCase() === ext);
        }

        if (this._disallowedExtensions.length > 0) {
            return !this._disallowedExtensions.some((e) => e.toLowerCase() === ext);
        }

        return true;
    }

    #addAttachment(file: File) {
        // Validate size
        if (file.size > MAX_FILE_SIZE) {
            this.#notificationContext?.peek("warning", {
                data: { headline: "File too large", message: `"${file.name}" exceeds maximum size of ${MAX_FILE_SIZE / 1024 / 1024}MB.` },
            });
            return;
        }

        // Validate file extension against CMS content settings
        if (!this.#isFileAllowed(file)) {
            const ext = file.name.includes(".") ? `.${file.name.split(".").pop()}` : "";
            this.#notificationContext?.peek("warning", {
                data: { headline: "File not allowed", message: `File type ${ext} is not permitted for upload.` },
            });
            return;
        }

        const attachment: AttachmentInfo = { file };

        // Create preview URL for images
        if (file.type.startsWith("image/")) {
            attachment.previewUrl = URL.createObjectURL(file);
        }

        this._attachments = [...this._attachments, attachment];
    }

    #removeAttachment(index: number) {
        const attachment = this._attachments[index];
        if (attachment.previewUrl) {
            URL.revokeObjectURL(attachment.previewUrl);
        }
        this._attachments = this._attachments.filter((_, i) => i !== index);
    }

    async #send() {
        if ((!this._value.trim() && this._attachments.length === 0) || this.disabled) return;

        let contentParts: UaiInputContent[] | undefined;

        if (this._attachments.length > 0) {
            contentParts = [];

            // Add text part if there's text
            if (this._value.trim()) {
                contentParts.push({ type: "text", text: this._value });
            }

            // Read files as base64 in parallel and add binary parts
            const binaryParts = await Promise.all(
                this._attachments.map(async (attachment) => {
                    const base64 = await this.#readFileAsBase64(attachment.file);
                    return {
                        type: "binary" as const,
                        mimeType: attachment.file.type,
                        data: base64,
                        filename: attachment.file.name,
                    };
                }),
            );
            contentParts.push(...binaryParts);
        }

        this.dispatchEvent(
            new CustomEvent("send", {
                detail: {
                    text: this._value,
                    contentParts,
                },
                bubbles: true,
                composed: true,
            }),
        );

        // Clean up preview URLs
        for (const attachment of this._attachments) {
            if (attachment.previewUrl) {
                URL.revokeObjectURL(attachment.previewUrl);
            }
        }

        this._value = "";
        this._attachments = [];
    }

    #readFileAsBase64(file: File): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                const result = reader.result as string;
                // Remove data URL prefix (e.g., "data:image/png;base64,")
                const base64 = result.split(",")[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }

    #formatFileSize(bytes: number): string {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }

    #renderAttachmentPreview() {
        if (this._attachments.length === 0) return nothing;

        return html`
            <div class="attachments-strip">
                ${repeat(
                    this._attachments,
                    (_, i) => i,
                    (attachment, index) => {
                        if (attachment.previewUrl) {
                            // Image thumbnail
                            return html`
                                <div class="attachment-item image-attachment">
                                    <img src=${attachment.previewUrl} alt=${attachment.file.name} />
                                    <button class="remove-btn" @click=${() => this.#removeAttachment(index)} title="Remove">
                                        <uui-icon name="icon-wrong"></uui-icon>
                                    </button>
                                </div>
                            `;
                        }
                        // Document chip
                        return html`
                            <div class="attachment-item doc-attachment">
                                <uui-icon name="icon-document"></uui-icon>
                                <span class="filename">${attachment.file.name}</span>
                                <span class="filesize">${this.#formatFileSize(attachment.file.size)}</span>
                                <button class="remove-btn" @click=${() => this.#removeAttachment(index)} title="Remove">
                                    <uui-icon name="icon-wrong"></uui-icon>
                                </button>
                            </div>
                        `;
                    },
                )}
            </div>
        `;
    }

    override render() {
        const hasNoAgents = this._agents.length === 0;
        const acceptTypes = this._allowedExtensions.length > 0 ? this._allowedExtensions.map((e) => `.${e}`).join(",") : "";

        return html`
            <div class="input-wrapper">
                <div
                    class="input-box ${this._isDragging ? "drag-over" : ""}"
                    @dragover=${this.#handleDragOver}
                    @dragleave=${this.#handleDragLeave}
                    @drop=${this.#handleDrop}
                >
                    <div class="drop-overlay ${this._isDragging ? "visible" : ""}">
                        <uui-icon name="icon-download-alt"></uui-icon>
                        <span>Drop files here</span>
                    </div>
                    <uui-textarea
                        ${ref(this.#textareaRef)}
                        .value=${this._value}
                        placeholder=${hasNoAgents ? "No agents available" : this.placeholder}
                        ?disabled=${this.#isDisabled}
                        auto-height
                        @input=${this.#handleInput}
                        @keydown=${this.#handleKeydown}
                    ></uui-textarea>
                    ${this.#renderAttachmentPreview()}
                    <hr class="divider" />
                    <div class="actions-row">
                        <div class="left-actions">
                            ${hasNoAgents
                                ? nothing
                                : html`
                                      <uui-select
                                          class="agent-select"
                                          .value=${this._selectedAgentId}
                                          .options=${this.#getAgentOptions()}
                                          @change=${this.#handleAgentChange}
                                      ></uui-select>
                                  `}
                            <uui-button
                                compact
                                look="secondary"
                                ?disabled=${this.#isDisabled}
                                @click=${this.#handleAttachClick}
                                title="Attach file"
                            >
                                <uui-icon name="icon-attachment"></uui-icon>
                            </uui-button>
                            <input
                                ${ref(this.#fileInputRef)}
                                type="file"
                                multiple
                                accept=${acceptTypes}
                                @change=${this.#handleFileSelect}
                                hidden
                            />
                            <uai-voice-button
                                ?disabled=${this.#isDisabled}
                                @transcription=${this.#handleTranscription}
                            ></uai-voice-button>
                        </div>
                        <uui-button
                            look="primary"
                            compact
                            ?disabled=${this.#isDisabled || (!this._value.trim() && this._attachments.length === 0)}
                            @click=${this.#send}
                        >
                            <uui-icon name="icon-navigation-right"></uui-icon>
                        </uui-button>
                    </div>
                </div>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }

        .input-wrapper {
            padding: var(--uui-size-space-4);
        }

        .input-box {
            position: relative;
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-3);
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            transition: border-color 0.15s ease;
        }

        .input-box.drag-over {
            border-color: var(--uui-color-focus);
        }

        .drop-overlay {
            position: absolute;
            inset: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: var(--uui-size-space-2);
            background: var(--uui-color-surface);
            color: var(--uui-color-focus);
            font-size: var(--uui-type-small-size);
            border-radius: var(--uui-border-radius);
            z-index: 1;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.15s ease;
        }

        .drop-overlay.visible {
            opacity: 1;
        }

        uui-textarea {
            --uui-textarea-min-height: 24px;
            --uui-textarea-max-height: 200px;
            --uui-textarea-background-color: transparent;
            --uui-textarea-border-color: transparent;
        }

        uui-textarea:focus-within {
            --uui-textarea-border-color: transparent;
        }

        .attachments-strip {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-1) 0;
        }

        .attachment-item {
            position: relative;
            border-radius: var(--uui-border-radius);
            border: 1px solid var(--uui-color-border);
            overflow: hidden;
        }

        .image-attachment {
            width: 64px;
            height: 64px;
        }

        .image-attachment img {
            width: 100%;
            height: 100%;
            object-fit: cover;
            display: block;
        }

        .doc-attachment {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-1);
            padding: var(--uui-size-space-1) var(--uui-size-space-2);
            background: var(--uui-color-surface-alt);
            font-size: 0.75rem;
        }

        .filename {
            max-width: 100px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .filesize {
            color: var(--uui-color-text-alt);
            white-space: nowrap;
        }

        .remove-btn {
            position: absolute;
            top: 2px;
            right: 2px;
            background: var(--uui-color-surface);
            border: none;
            border-radius: 50%;
            width: 18px;
            height: 18px;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 10px;
            opacity: 0;
            transition: opacity 0.15s ease;
            padding: 0;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
        }

        .attachment-item:hover .remove-btn {
            opacity: 1;
        }

        .doc-attachment .remove-btn {
            position: static;
            opacity: 1;
            box-shadow: none;
            background: transparent;
        }

        .divider {
            border: none;
            border-top: 1px solid var(--uui-color-border);
            margin: 0;
        }

        .actions-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .left-actions {
            display: flex;
            gap: var(--uui-size-space-2);
        }

        .agent-select {
            min-width: 120px;
            max-width: 180px;
        }
    `;
}

export default UaiChatInputElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-input": UaiChatInputElement;
    }
}
