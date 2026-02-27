import type { UmbExtensionManifestKind } from '@umbraco-cms/backoffice/extension-registry';

/**
 * Entity container menu item kind.
 *
 * This kind provides a menu item that stays highlighted when navigating to any of its child entity types.
 * Perfect for flat collections where you have a root entity type (collection view) and detail entity types (edit views).
 *
 * Example usage:
 * ```typescript
 * {
 *   type: 'menuItem',
 *   kind: 'entityContainer',
 *   alias: 'UmbracoAI.MenuItem.Connections',
 *   meta: {
 *     label: 'Connections',
 *     entityType: 'uai:connection-root',
 *     childEntityTypes: ['uai:connection'],
 *     menus: ['UmbracoAI.Menu'],
 *   }
 * }
 * ```
 */
export const menuItemKinds: Array<UmbExtensionManifestKind> = [
	{
		type: 'kind',
		alias: 'UmbracoAI.Kind.MenuItem.EntityContainer',
		matchKind: 'entityContainer',
		matchType: 'menuItem',
		manifest: {
			type: 'menuItem',
			element: () => import('./entity-container-menu-item.element.js'),
		},
	},
];
