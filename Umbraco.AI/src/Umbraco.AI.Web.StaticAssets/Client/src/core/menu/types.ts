import type { ManifestMenuItem } from '@umbraco-cms/backoffice/menu';

/**
 * Extended menu item manifest that supports child entity types for entity container menu items.
 */
export interface UaiEntityContainerMenuItemManifest extends ManifestMenuItem {
	kind: 'entityContainer';
	meta: ManifestMenuItem['meta'] & {
		/**
		 * Child entity types that should keep this menu item highlighted when active.
		 * Used by the entityContainer menu item kind to maintain highlighting when navigating to child entities.
		 */
		childEntityTypes?: string[];
	};
}
