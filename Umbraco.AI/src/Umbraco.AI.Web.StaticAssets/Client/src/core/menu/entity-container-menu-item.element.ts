import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { customElement, html, property, state } from '@umbraco-cms/backoffice/external/lit';
import { UMB_SECTION_CONTEXT } from '@umbraco-cms/backoffice/section';
import { debounce } from '@umbraco-cms/backoffice/utils';
import type { UaiEntityContainerMenuItemManifest } from './types.js';

/**
 * Entity container menu item element.
 *
 * This menu item type maintains its highlighted state when navigating to any of its child entity types.
 * It's designed for flat collections where a root entity type (e.g., "connection-root") contains
 * child entities (e.g., "connection") and you want the menu to stay highlighted when editing children.
 *
 * @element uai-entity-container-menu-item
 */
@customElement('uai-entity-container-menu-item')
export class UaiEntityContainerMenuItemElement extends UmbLitElement {
	@property({ type: Object, attribute: false })
	public manifest!: UaiEntityContainerMenuItemManifest;

	@state()
	private _href?: string;

	@state()
	private _isActive = false;

	#pathname?: string;
	#childEntityTypes: string[] = [];

	constructor() {
		super();

		this.consumeContext(UMB_SECTION_CONTEXT, (sectionContext) => {
			this.observe(
				sectionContext?.pathname,
				(pathname) => {
					this.#pathname = pathname;
					this.#constructHref();
				},
				'observePathname',
			);
		});
	}

	override connectedCallback() {
		super.connectedCallback();
		window.addEventListener('navigationend', this.#debouncedCheckIsActive);
	}

	override disconnectedCallback() {
		super.disconnectedCallback();
		window.removeEventListener('navigationend', this.#debouncedCheckIsActive);
	}

	#debouncedCheckIsActive = debounce(() => this.#checkIsActive(), 100);

	#constructHref() {
		if (!this.#pathname || !this.manifest?.meta?.entityType) return;

		// Store child entity types from manifest
		this.#childEntityTypes = this.manifest.meta.childEntityTypes || [];

		// Construct href to root entity workspace
		this._href = `section/${this.#pathname}/workspace/${this.manifest.meta.entityType}`;
		this.#checkIsActive();
	}

	#checkIsActive() {
		if (!this._href) {
			this._isActive = false;
			return;
		}

		// Normalize paths with leading/trailing slashes to avoid false matches
		// e.g., prevent "/section/ai/workspace/uai:connection" from matching "/section/ai/workspace/uai:connection-root"
		const ensureSlash = (path: string): string => {
			if (!path.startsWith('/')) path = '/' + path;
			if (!path.endsWith('/')) path = path + '/';
			return path;
		};

		const location = ensureSlash(window.location.pathname);

		// Check if current location matches root entity type
		const rootPath = ensureSlash(this._href);
		if (location.includes(rootPath)) {
			this._isActive = true;
			return;
		}

		// Check if current location matches any child entity type
		if (this.#childEntityTypes.length > 0 && this.#pathname) {
			this._isActive = this.#childEntityTypes.some((childType) => {
				const childPath = ensureSlash(`section/${this.#pathname}/workspace/${childType}`);
				return location.includes(childPath);
			});
			return;
		}

		this._isActive = false;
	}

	override render() {
		return html`
			<uui-menu-item
				.href=${this._href}
				?active=${this._isActive}
				label=${this.localize.string(this.manifest.meta.label ?? this.manifest.name)}>
				<uui-icon slot="icon" name=${this.manifest.meta.icon ?? ''}></uui-icon>
			</uui-menu-item>
		`;
	}
}

export { UaiEntityContainerMenuItemElement as element };

declare global {
	interface HTMLElementTagNameMap {
		'uai-entity-container-menu-item': UaiEntityContainerMenuItemElement;
	}
}
