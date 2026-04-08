import { css, customElement, html, property, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_NOTIFICATION_CONTEXT, type UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import type { Editor } from '@umbraco-cms/backoffice/tiptap';
import type { UmbTiptapToolbarElementApi } from '@umbraco-cms/backoffice/tiptap';
import { UaiAudioRecorder } from '../utils/audio-recorder.js';
import { UaiSpeechToTextController } from '../../speech-to-text/controllers/speech-to-text.controller.js';

type DictationState = "idle" | "recording" | "transcribing";

/**
 * Tiptap toolbar button for speech-to-text dictation.
 *
 * When clicked, records the current cursor position, starts recording audio,
 * and on stop transcribes the audio and inserts the text at the saved position.
 *
 * Receives `api`, `editor`, and `manifest` properties from the Tiptap toolbar.
 */
@customElement('uai-dictate-tiptap-toolbar')
export class UaiDictateTiptapToolbarElement extends UmbLitElement {
    public api?: UmbTiptapToolbarElementApi;

    @property({ attribute: false })
    public editor?: Editor;

    @property({ attribute: false })
    public manifest?: any;

    @state()
    private _state: DictationState = 'idle';

    #recorder = new UaiAudioRecorder(this);
    #sttController = new UaiSpeechToTextController(this);
    #savedCursorPos?: number;
    #notificationContext?: UmbNotificationContext;

    constructor() {
        super();
        this.observe(this.#recorder.state$, (s) => { this._state = s; });
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });
    }

    async #handleClick() {
        if (this._state === 'idle') {
            await this.#startRecording();
        } else if (this._state === 'recording') {
            await this.#stopAndInsert();
        }
    }

    async #startRecording() {
        if (!this.editor) return;

        // Capture cursor position before recording starts
        this.#savedCursorPos = this.editor.state.selection.from;

        try {
            await this.#recorder.start();
        } catch {
            this.#notificationContext?.peek('danger', {
                data: {
                    headline: 'Microphone access denied',
                    message: 'Please allow microphone access to use dictation.',
                },
            });
        }
    }

    async #stopAndInsert() {
        if (!this.editor) return;

        try {
            const audioBlob = await this.#recorder.stop();

            this._state = 'transcribing';
            const { data, error } = await this.#sttController.transcribe(audioBlob);

            if (error) {
                throw new Error(
                    (error as { detail?: string })?.detail ?? 'Transcription failed.',
                );
            }

            const text = data?.text;
            if (text) {
                const insertPos = this.#savedCursorPos ?? this.editor.state.doc.content.size;
                this.editor.chain().focus().insertContentAt(insertPos, text).run();
            }
        } catch (err) {
            this.#notificationContext?.peek('danger', {
                data: {
                    headline: 'Transcription failed',
                    message: err instanceof Error ? err.message : 'An error occurred during transcription.',
                },
            });
        } finally {
            this._state = 'idle';
            this.#savedCursorPos = undefined;
        }
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#recorder.cancel();
    }

    override render() {
        const isRecording = this._state === 'recording';
        const isTranscribing = this._state === 'transcribing';

        const title = isRecording
            ? 'Stop recording'
            : isTranscribing
              ? 'Transcribing...'
              : 'Dictate';

        return html`
            <uui-button
                compact
                look="default"
                label="Dictate"
                title=${title}
                ?disabled=${isTranscribing}
                class=${isRecording ? 'recording' : ''}
                @click=${this.#handleClick}>
                ${isTranscribing
                    ? html`<uui-loader-circle></uui-loader-circle>`
                    : html`<umb-icon name="icon-mic"></umb-icon>`}
            </uui-button>
        `;
    }

    static override styles = css`
        :host {
            margin-left: var(--uui-size-space-1);
            margin-bottom: var(--uui-size-space-1);
        }

        uui-button.recording {
            --uui-button-background-color: var(--uui-color-danger);
            --uui-button-background-color-hover: var(--uui-color-danger-emphasis);
            --uui-button-contrast: var(--uui-color-danger-contrast);
            animation: pulse 1.5s ease-in-out infinite;
        }

        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.65; }
        }
    `;
}

export { UaiDictateTiptapToolbarElement as element };

declare global {
    interface HTMLElementTagNameMap {
        'uai-dictate-tiptap-toolbar': UaiDictateTiptapToolbarElement;
    }
}
