import {
	css,
	customElement,
	html,
	nothing,
	property,
	query,
	queryAll,
	repeat,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import type { UUIInputElement, UUIInputEvent, UUITagElement } from '@umbraco-cms/backoffice/external/uui';
import { UUIFormControlMixin } from '@umbraco-cms/backoffice/external/uui';

/**
 * Response model for tag lookup results.
 * @public
 */
export interface UaiTagItem {
	/** Unique identifier for the tag */
	id: string;
	/** Display text of the tag */
	text: string;
	/** Optional group the tag belongs to */
	group?: string;
}

/**
 * Callback type for looking up existing tags.
 * @param query - The search query string
 * @returns Promise resolving to an array of matching tag items
 * @public
 */
export type UaiTagLookupCallback = (query: string) => Promise<UaiTagItem[]>;

/**
 * A configurable tags input component that allows creating and selecting tags.
 * The tag lookup mechanism is configurable via a callback function.
 *
 * @fires change - Fires when tags are added or removed
 *
 * @example
 * ```html
 * <uai-tags-input
 *   .items=${['tag1', 'tag2']}
 *   .lookup=${async (query) => {
 *     const response = await fetch(`/api/tags?q=${query}`);
 *     return response.json();
 *   }}
 *   @change=${(e) => console.log(e.target.items)}
 * ></uai-tags-input>
 * ```
 *
 * @example Strict mode - only allow values from suggestions
 * ```html
 * <uai-tags-input
 *   strict
 *   .lookup=${async (query) => fetchAllowedTags(query)}
 *   @change=${(e) => console.log(e.target.items)}
 * ></uai-tags-input>
 * ```
 * @public
 */
@customElement('uai-tags-input')
export class UaiTagsInputElement extends UUIFormControlMixin(UmbLitElement, '') {
	/**
	 * Callback function for looking up existing tags.
	 * When provided, enables autocomplete suggestions as the user types.
	 */
	@property({ attribute: false })
	lookup?: UaiTagLookupCallback;

	@property({ type: Boolean })
	override required = false;

	@property({ type: String })
	override requiredMessage = 'This field is required';

	@property({ type: Array })
	public set items(newTags: string[]) {
		this.#items = newTags?.filter((x) => x !== '') || [];
		super.value = this.#items.join(',');
	}
	public get items(): string[] {
		return this.#items;
	}
	#items: string[] = [];

	/**
	 * Sets the input to readonly mode, meaning value cannot be changed but still able to read and select its content.
	 * @type {boolean}
	 * @attr
	 * @default false
	 */
	@property({ type: Boolean, reflect: true })
	readonly = false;

	/**
	 * Placeholder text shown in the input field.
	 * @type {string}
	 * @attr
	 * @default 'Enter tag'
	 */
	@property({ type: String })
	placeholder = 'Enter tag';

	/**
	 * When enabled, only allows values that come from the lookup suggestions.
	 * Users cannot create arbitrary tags - they must select from the dropdown.
	 * @type {boolean}
	 * @attr
	 * @default false
	 */
	@property({ type: Boolean, reflect: true })
	strict = false;

	@state()
	private _matches: Array<UaiTagItem> = [];

	@state()
	private _currentInput = '';

	@query('#main-tag')
	private _mainTag!: UUITagElement;

	@query('#tag-input')
	private _tagInput!: UUIInputElement;

	@query('#input-width-tracker')
	private _widthTracker!: HTMLElement;

	@queryAll('.options')
	private _optionCollection?: HTMLCollectionOf<HTMLInputElement>;

	@queryAll('.tag')
	private _tagEls?: NodeListOf<HTMLElement>;

	override connectedCallback() {
		super.connectedCallback();
		document.addEventListener('click', this.#onDocumentClick);
	}

	override disconnectedCallback() {
		super.disconnectedCallback();
		document.removeEventListener('click', this.#onDocumentClick);
	}

	#onDocumentClick = (e: MouseEvent) => {
		if (!this._matches.length) return;
		const path = e.composedPath();
		if (!path.includes(this)) {
			this._matches = [];
		}
	};

	public override focus() {
		this._tagInput.focus();
	}

	protected override getFormElement() {
		return undefined;
	}

	async #getExistingTags(query: string) {
		if (!this.lookup || !query) {
			this._matches = [];
			return;
		}
		try {
			this._matches = await this.lookup(query);
		} catch {
			this._matches = [];
		}
	}

	#onInputKeydown(e: KeyboardEvent) {
		const inputLength = (this._tagInput.value as string).trim().length;

		// Prevent tab away if there is text in the input
		// In strict mode, only create tag if there are matches
		if (e.key === 'Tab' && inputLength && !this._matches.length) {
			if (this.strict) {
				// In strict mode, allow tab away without creating tag
				return;
			}
			e.preventDefault();
			this.#createTag();
			return;
		}

		// If the input is empty we can navigate out of it using tab
		if (e.key === 'Tab' && !inputLength) {
			return;
		}

		// Create a new tag when enter is pressed
		// In strict mode, this will only succeed if value is in matches
		if (e.key === 'Enter') {
			this.#createTag();
			return;
		}

		// This one to show option collection if there is any
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			this._currentInput = this._optionCollection?.item(0)?.value ?? this._currentInput;
			this._optionCollection?.item(0)?.focus();
			return;
		}
		this.#inputError(false);
	}

	#focusTag(index: number) {
		const tag = this._tagEls?.[index];
		if (!tag) return;

		// Find the current element with the class .tab and tabindex=0 (will be the previous tag)
		const active = this.renderRoot.querySelector<HTMLElement>('.tag[tabindex="0"]');

		// Return it is tabindex to -1
		active?.setAttribute('tabindex', '-1');

		// Set the tabindex to 0 in the current target
		tag.setAttribute('tabindex', '0');

		tag.focus();
	}

	#onTagsWrapperKeydown(e: KeyboardEvent) {
		if ((e.key === 'Enter' || e.key === 'ArrowDown') && this.items.length) {
			e.preventDefault();
			this.#focusTag(0);
		}
	}

	#onTagKeydown(e: KeyboardEvent, idx: number) {
		if (e.key === 'ArrowRight') {
			e.preventDefault();
			if (idx < this.items.length - 1) {
				this.#focusTag(idx + 1);
			}
		}

		if (e.key === 'ArrowLeft') {
			e.preventDefault();
			if (idx > 0) {
				this.#focusTag(idx - 1);
			}
		}

		if (e.key === 'Backspace' || e.key === 'Delete') {
			e.preventDefault();
			if (this.#items.length - 1 === idx) {
				this.#focusTag(idx - 1);
			}
			this.#delete(this.#items[idx]);
			this.#focusTag(idx + 1);
		}
	}

	#onInput(e: UUIInputEvent) {
		this._currentInput = e.target.value as string;
		if (!this._currentInput || !this._currentInput.length) {
			this._matches = [];
		} else {
			this.#getExistingTags(this._currentInput);
		}
	}

	protected override updated(): void {
		if (this._currentInput) {
			this._mainTag.style.width = `${this._widthTracker.offsetWidth - 4}px`;
		} else {
			this._mainTag.style.width = '';
		}
	}

	#onBlur() {
		if (this._matches.length) return;
		// In strict mode, don't auto-create tag on blur - user must select from dropdown
		if (this.strict) {
			this._tagInput.value = '';
			this._currentInput = '';
			this.#inputError(false);
			return;
		}
		this.#createTag();
	}

	#createTag() {
		this.#inputError(false);
		const newTag = (this._tagInput.value as string).trim();
		if (!newTag) return;

		// In strict mode, only allow values from suggestions
		if (this.strict) {
			const matchedTag = this._matches.find((match) => match.text === newTag);
			if (!matchedTag) {
				this.#inputError(true);
				return;
			}
		}

		const tagExists = this.items.find((tag) => tag === newTag);
		if (tagExists) return this.#inputError(true);

		this.#inputError(false);
		this.items = [...this.items, newTag];
		this._tagInput.value = '';
		this._currentInput = '';
		this._matches = [];
		this.dispatchEvent(new UmbChangeEvent());
	}

	#inputError(error: boolean) {
		if (error) {
			this._mainTag.style.border = '1px solid var(--uui-color-danger)';
			this._tagInput.style.color = 'var(--uui-color-danger)';
			return;
		}
		this._mainTag.style.border = '';
		this._tagInput.style.color = '';
	}

	#delete(tag: string) {
		const currentItems = [...this.items];
		const index = currentItems.findIndex((x) => x === tag);
		currentItems.splice(index, 1);
		if (currentItems.length) {
			this.items = currentItems;
		} else {
			this.items = [];
		}
		// Remove focus from the tags container to clear focus ring
		(document.activeElement as HTMLElement)?.blur();
		this.dispatchEvent(new UmbChangeEvent());
	}

	/** Dropdown */

	#optionClick(index: number) {
		this._tagInput.value = this._optionCollection?.item(index)?.value ?? '';
		this.#createTag();
		this._matches = [];
		this.focus();
		return;
	}

	#optionKeydown(e: KeyboardEvent, index: number) {
		if (e.key === 'Enter' || e.key === 'Tab') {
			e.preventDefault();
			this._currentInput = this._optionCollection?.item(index)?.value ?? '';
			this.#createTag();
			this._matches = [];
			this.focus();
			return;
		}

		if (e.key === 'ArrowDown') {
			e.preventDefault();
			if (!this._optionCollection?.item(index + 1)) return;
			this._optionCollection?.item(index + 1)?.focus();
			this._currentInput = this._optionCollection?.item(index + 1)?.value ?? '';
			return;
		}

		if (e.key === 'ArrowUp') {
			e.preventDefault();
			if (!this._optionCollection?.item(index - 1)) return;
			this._optionCollection?.item(index - 1)?.focus();
			this._currentInput = this._optionCollection?.item(index - 1)?.value ?? '';
		}

		if (e.key === 'Backspace') {
			this.focus();
		}
	}

	/** Render */

	override render() {
		return html`
			<div id="wrapper">
				${this.#renderTags()}
				<span id="main-tag-wrapper">
					<uui-tag id="input-width-tracker" aria-hidden="true" style="visibility:hidden;opacity:0;position:absolute;">
						${this._currentInput}
					</uui-tag>
					${this.#renderAddButton()}
				</span>
			</div>
		`;
	}

	#renderTags() {
		return html`
			<div id="tags" tabindex="0" @keydown=${this.#onTagsWrapperKeydown}>
				${repeat(
					this.items,
					(tag) => tag,
					(tag, index) => html`
						<uui-tag class="tag" @keydown=${(e: KeyboardEvent) => this.#onTagKeydown(e, index)}>
							<span>${tag}</span>
							${this.#renderRemoveButton(tag)}
						</uui-tag>
					`,
				)}
			</div>
		`;
	}

	#renderTagOptions() {
		if (!this._matches.length) return nothing;
		const matchfilter = this._matches.filter((tag) => tag.text !== this.#items.find((x) => x === tag.text));
		if (!matchfilter.length) return;
		return html`
			<div id="matchlist">
				${repeat(
					matchfilter.slice(0, 5),
					(tag: UaiTagItem) => tag.id,
					(tag: UaiTagItem, index: number) => {
						return html`<input
								class="options"
								id="tag-${tag.id}"
								type="radio"
								name="${tag.group ?? ''}"
								@click="${() => this.#optionClick(index)}"
								@keydown="${(e: KeyboardEvent) => this.#optionKeydown(e, index)}"
								value="${tag.text ?? ''}"
								?readonly=${this.readonly} />
							<label for="tag-${tag.id}"> ${tag.text} </label>`;
					},
				)}
			</div>
		`;
	}

	#renderAddButton() {
		if (this.readonly) return nothing;
		return html`
			<uui-tag look="outline" id="main-tag" @click="${this.focus}" slot="trigger">
				<input
					id="tag-input"
					aria-label="tag input"
					autocomplete="off"
					placeholder="${this.placeholder}"
					.value="${this._currentInput}"
					@keydown="${this.#onInputKeydown}"
					@input="${this.#onInput}"
					@blur="${this.#onBlur}" />
				<uui-icon id="icon-add" name="icon-add"></uui-icon>
				${this.#renderTagOptions()}
			</uui-tag>
		`;
	}

	#renderRemoveButton(tag: string) {
		if (this.readonly) return nothing;
		return html`<uui-icon name="icon-wrong" @click="${() => this.#delete(tag)}"></uui-icon>`;
	}

	static override styles = [
		css`
			#wrapper {
				box-sizing: border-box;
				display: flex;
				gap: var(--uui-size-space-2);
				flex-wrap: wrap;
				align-items: center;
				padding: var(--uui-size-space-2);
				border: 1px solid var(--uui-color-border);
				border-radius: var(--uui-border-radius);
				background-color: var(--uui-input-background-color, var(--uui-color-surface));
				flex: 1;
				min-height: 40px;
			}

			#main-tag-wrapper {
				position: relative;
			}

			/** Tags */
			#tags {
				display: flex;
				gap: var(--uui-size-space-2);
				flex-wrap: wrap;
				border-radius: var(--uui-size-1);

				&:focus {
					outline: var(--uui-size-1) solid var(--uui-color-focus);
					outline-offset: var(--uui-size-1);
				}
			}

			uui-tag {
				position: relative;
				max-width: 200px;
			}

			uui-tag uui-icon {
				cursor: pointer;
				min-width: 12.8px !important;
			}

			uui-tag span {
				overflow: hidden;
				text-overflow: ellipsis;
				white-space: nowrap;
			}

			/** Existing tags */
			.tag {
				&:focus {
					outline: var(--uui-size-1) solid var(--uui-color-focus);
				}

				uui-icon {
					margin-left: var(--uui-size-space-2);

					&:hover,
					&:active {
						color: var(--uui-color-selected-contrast);
					}
				}
			}

			/** Main tag */

			#main-tag {
				padding: 3px;
				background-color: var(--uui-color-selected-contrast);
				min-width: 20px;
				position: relative;
				border-radius: var(--uui-size-5, 12px);
			}

			#main-tag uui-icon {
				position: absolute;
				top: 50%;
				left: 50%;
				transform: translate(-50%, -50%);
			}

			#main-tag:hover uui-icon,
			#main-tag:active uui-icon {
				color: var(--uui-color-selected);
			}

			#main-tag #tag-input:focus ~ uui-icon,
			#main-tag #tag-input:not(:placeholder-shown) ~ uui-icon {
				display: none;
			}

			#main-tag:has(*:hover),
			#main-tag:has(*:active),
			#main-tag:has(*:focus) {
				border: 1px solid var(--uui-color-selected-emphasis);
			}

			#main-tag:has(#tag-input:not(:focus)):hover {
				cursor: pointer;
				border: 1px solid var(--uui-color-selected-emphasis);
			}

			#main-tag:not(:focus-within) #tag-input:placeholder-shown {
				opacity: 0;
				width: 0;
				padding: 0;
			}

			#main-tag:has(#tag-input:focus),
			#main-tag:has(#tag-input:not(:placeholder-shown)) {
				min-width: 65px;
			}

			#main-tag #tag-input {
				box-sizing: border-box;
				max-height: 25.8px;
				background: none;
				font: inherit;
				color: var(--uui-color-selected);
				line-height: reset;
				padding: 0 var(--uui-size-space-2);
				margin: 0.5px 0 -0.5px;
				border: none;
				outline: none;
				width: 100%;
			}

			/** Dropdown matchlist */

			#matchlist input[type='radio'] {
				-webkit-appearance: none;
				appearance: none;
				/* For iOS < 15 to remove gradient background */
				background-color: transparent;
				/* Not removed via appearance */
				margin: 0;
			}

			uui-tag:focus-within #matchlist {
				display: flex;
			}

			#matchlist {
				display: flex;
				flex-direction: column;
				background-color: var(--uui-color-surface);
				position: absolute;
				width: 150px;
				left: 0;
				top: var(--uui-size-space-6);
				border-radius: var(--uui-border-radius);
				border: 1px solid var(--uui-color-border);
				z-index: 10;
			}

			#matchlist label {
				display: none;
				cursor: pointer;
				box-sizing: border-box;
				display: block;
				width: 100%;
				background: none;
				border: none;
				text-align: left;
				padding: 10px 12px;

				/** Overflow */
				overflow: hidden;
				text-overflow: ellipsis;
				white-space: nowrap;
			}

			#matchlist label:hover,
			#matchlist label:focus,
			#matchlist label:focus-within,
			#matchlist input[type='radio']:focus + label {
				display: block;
				background-color: var(--uui-color-focus);
				color: var(--uui-color-selected-contrast);
			}
		`,
	];
}

export default UaiTagsInputElement;

declare global {
	interface HTMLElementTagNameMap {
		'uai-tags-input': UaiTagsInputElement;
	}
}
